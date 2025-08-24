////using UnityEngine;
////using System;

////public interface IHitReceiver
////{
////    void ReceiveHit(float damage, Vector2 knockback, Vector2 hitPoint);
////}

////public class Hitbox : MonoBehaviour
////{
////    public float damage = 10f;
////    public Vector2 knockback = new Vector2(6f, 2f);

////    // ↓ 디버그/콜백용
////    public string attackerName = "Player";
////    public bool debugLog = false;
////    public Action<Collider2D, float> OnHit;  // (맞은 콜라이더, 데미지)

////    void OnTriggerEnter2D(Collider2D other)
////    {
////        var recv = other.GetComponent<IHitReceiver>()
////            ?? other.GetComponentInParent<IHitReceiver>()
////            ?? other.GetComponentInChildren<IHitReceiver>();

////        if (recv != null)
////        {
////            recv.ReceiveHit(damage, knockback, transform.position);

////            if (debugLog)
////                Debug.Log($"[HIT] {attackerName} -> {other.name} dmg={damage} @ {transform.position}");

////            OnHit?.Invoke(other, damage);
////        }
////        else
////        {
////            if (debugLog)
////            {
////                string layer = LayerMask.LayerToName(other.gameObject.layer);
////                Debug.LogWarning($"[HIT but no IHitReceiver] {attackerName} -> {other.name} (layer={layer})");
////            }
////        }
////    }

////}


//using UnityEngine;
//using System;
//using System.Diagnostics;

//public interface IHitReceiver
//{
//    void ReceiveHit(float damage, Vector2 knockback, Vector2 hitPoint);
//}

//public class Hitbox : MonoBehaviour
//{
//    public float damage = 10f;
//    public Vector2 knockback = new Vector2(6f, 2f);

//    // 디버그/콜백
//    public string attackerName = "Player";
//    public bool debugLog = false;
//    public Action<Collider2D, float> OnHit;  // (맞은 콜라이더, 데미지)

//    void OnTriggerEnter2D(Collider2D other)
//    {
//        var recv = other.GetComponent<IHitReceiver>()
//               ?? other.GetComponentInParent<IHitReceiver>()
//               ?? other.GetComponentInChildren<IHitReceiver>();

//        if (recv != null)
//        {
//            recv.ReceiveHit(damage, knockback, transform.position);

//            if (debugLog)
//                UnityEngine.Debug.Log($"[HIT] {attackerName} -> {other.name} dmg={damage} @ {transform.position}");

//            OnHit?.Invoke(other, damage);
//        }
//        else
//        {
//            if (debugLog)
//            {
//                string layer = LayerMask.LayerToName(other.gameObject.layer);
//                UnityEngine.Debug.LogWarning($"[HIT but no IHitReceiver] {attackerName} -> {other.name} (layer={layer})");
//            }
//        }
//    }
//}


using System;
using System.Collections.Generic;
using UnityEngine;
using Hercules.StatsSystem;

/// <summary>
/// 상시 보유하는 공격 판정 컴포넌트.
/// - 평소엔 Disarm 상태(armed=false, Collider.enabled=false)
/// - 공격/스킬의 액티브 구간에만 Arm(...)으로 무장 & On/Off
/// - Mode.Single : OnTriggerEnter로 1회 타격
/// - Mode.Continuous : OnTriggerStay로 tickInterval마다 주기 타격
/// - 최종 데미지는 CombatMath에서 계산(현재는 baseDamage 통과)
/// + Gizmo: 히트 범위 시각화 지원
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Hitbox : MonoBehaviour
{
    public enum HitMode { Single, Continuous }

    [Header("Debug")]
    public string attackerName = "Attacker";
    public bool debugLog = false;

    [Header("Gizmo")]
    public bool drawGizmoAlways = true;            // 씬에서 항상 표시
    public bool drawGizmoWhenSelected = true;      // 선택 시 표시
    public Color gizmoFill = new Color(1f, 0f, 0f, 0.12f);
    public Color gizmoLine = new Color(1f, 0f, 0f, 0.9f);
    public Color gizmoArmedLine = new Color(0.25f, 1f, 0.25f, 1f);

    [Header("Runtime (read-only)")]
    public bool armed;
    public HitMode mode = HitMode.Single;
    public float tickInterval = 0.2f; // Continuous일 때만 사용

    // 공격자/기본 파라미터(Arm으로 주입)
    public Unit ownerUnit;
    public StatsBase ownerStats;
    public float damage = 10f;          // base damage (승수는 CombatMath에 위임 권장)
    public Vector2 knockback = new Vector2(6f, 2f);

    // 외부 훅(이펙트/사운드 등)
    public event Action<Collider2D, float> OnHit;

    // 내부 상태
    Collider2D col;
    // Continuous 모드: 유닛별 마지막 타격 시간 → tickInterval마다 1회
    readonly Dictionary<Unit, float> _lastHitAt = new Dictionary<Unit, float>();

    void Reset()
    {
        col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    void Awake()
    {
        col = GetComponent<Collider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        Disarm(); // 시작은 비무장
    }

    void OnEnable() { _lastHitAt.Clear(); }
    void OnDisable() { _lastHitAt.Clear(); }

    /// <summary>액티브 구간 시작 시 호출.</summary>
    public void Arm(Unit owner, StatsBase stats, float baseDamage, Vector2 kb, HitMode m, float tick = 0.2f)
    {
        ownerUnit = owner;
        ownerStats = stats;
        damage = baseDamage;
        knockback = kb;
        mode = m;
        tickInterval = Mathf.Max(0.01f, tick);

        armed = true;
        _lastHitAt.Clear();

        if (col) col.enabled = true;
    }

    /// <summary>액티브 구간 종료 시 호출.</summary>
    public void Disarm()
    {
        armed = false;
        _lastHitAt.Clear();
        if (col) col.enabled = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!armed) return;

        if (mode == HitMode.Single)
            TryApplyHit(other, /*respectTick*/ false);
        else // Continuous
            TryApplyHit(other, /*respectTick*/ true); // 진입 시 1회(선택)
    }

    void OnTriggerStay2D(Collider2D other)              // 확장성 생각해서 미리 추가해둠(지딜 혹은 장판용)
    {
        if (!armed || mode != HitMode.Continuous) return;
        TryApplyHit(other, /*respectTick*/ true); // 틱 간격 적용
    }

    bool TryApplyHit(Collider2D other, bool respectTick)
    {
        // 대상 Unit/Stats 찾기
        var targetUnit =
               other.GetComponentInParent<Unit>()
            ?? other.GetComponent<Unit>()
            ?? other.GetComponentInChildren<Unit>();
        if (targetUnit == null) return false;
        if (ownerUnit != null && targetUnit == ownerUnit) return false; // 자기 자신 무시

        if (respectTick)
        {
            if (_lastHitAt.TryGetValue(targetUnit, out float last))
                if (Time.time - last < tickInterval) return false;
        }

        var targetStats =
               other.GetComponentInParent<StatsBase>()
            ?? other.GetComponent<StatsBase>()
            ?? other.GetComponentInChildren<StatsBase>();

        // 최종 데미지 계산
        float finalDamage = (ownerStats != null && targetStats != null)
            ? CombatMath.ComputeDamage(ownerStats, targetStats, damage)
            : damage;

        // 타격 적용
        targetUnit.Damage(finalDamage, ownerUnit);

        // 넉백: 공격자→피격자 방향, force = knockback.x
        Vector2 dir = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;
        float force = knockback.x;
        if (force > 0f) targetUnit.ApplyKnockback(dir, force);

        if (debugLog) Debug.Log($"[HIT] {attackerName} -> {other.name} dmg={finalDamage}");

        _lastHitAt[targetUnit] = Time.time;
        OnHit?.Invoke(other, finalDamage);
        return true;
    }

#if UNITY_EDITOR
    // ── Gizmo: 히트 범위 시각화 ──────────────────────────────────────────────
    void DrawBoxGizmo(BoxCollider2D box, Color line, Color fill)
    {
        if (!box) return;
        var prev = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        Vector3 center = (Vector3)box.offset;
        Vector3 size = (Vector3)box.size;
        size.z = 0.01f;

        // 채움
        Gizmos.color = fill;
        Gizmos.DrawCube(center, size);
        // 외곽선
        Gizmos.color = line;
        Gizmos.DrawWireCube(center, size);

        Gizmos.matrix = prev;
    }

    void DrawCircleGizmo(CircleCollider2D c, Color line, Color fill)
    {
        if (!c) return;
        var prev = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Vector3 center = (Vector3)c.offset;
        float r = c.radius;

        Gizmos.color = fill;
        Gizmos.DrawSphere(center, 0.001f); // 얇은 채움 대용
        // 에디터 전용 원
        UnityEditor.Handles.color = line;
        UnityEditor.Handles.DrawWireDisc(center, Vector3.forward, r);

        Gizmos.matrix = prev;
    }

    void OnDrawGizmos()
    {
        if (!drawGizmoAlways) return;
        var line = armed ? gizmoArmedLine : gizmoLine;

        if (TryGetComponent(out BoxCollider2D box)) { DrawBoxGizmo(box, line, gizmoFill); return; }
        if (TryGetComponent(out CircleCollider2D cir)) { DrawCircleGizmo(cir, line, gizmoFill); return; }
        if (TryGetComponent(out CapsuleCollider2D cap))
        {
            // 캡슐은 대략적인 박스로 표시
            var prev = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Vector3 center = (Vector3)cap.offset;
            Vector3 size = new Vector3(cap.size.x, cap.size.y, 0.01f);
            Gizmos.color = gizmoFill; Gizmos.DrawCube(center, size);
            Gizmos.color = line; Gizmos.DrawWireCube(center, size);
            Gizmos.matrix = prev;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmoWhenSelected || drawGizmoAlways) return;
        OnDrawGizmos();
    }
#endif
}


