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

////    // �� �����/�ݹ��
////    public string attackerName = "Player";
////    public bool debugLog = false;
////    public Action<Collider2D, float> OnHit;  // (���� �ݶ��̴�, ������)

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

//    // �����/�ݹ�
//    public string attackerName = "Player";
//    public bool debugLog = false;
//    public Action<Collider2D, float> OnHit;  // (���� �ݶ��̴�, ������)

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
/// ��� �����ϴ� ���� ���� ������Ʈ.
/// - ��ҿ� Disarm ����(armed=false, Collider.enabled=false)
/// - ����/��ų�� ��Ƽ�� �������� Arm(...)���� ���� & On/Off
/// - Mode.Single : OnTriggerEnter�� 1ȸ Ÿ��
/// - Mode.Continuous : OnTriggerStay�� tickInterval���� �ֱ� Ÿ��
/// - ���� �������� CombatMath���� ���(����� baseDamage ���)
/// + Gizmo: ��Ʈ ���� �ð�ȭ ����
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Hitbox : MonoBehaviour
{
    public enum HitMode { Single, Continuous }

    [Header("Debug")]
    public string attackerName = "Attacker";
    public bool debugLog = false;

    [Header("Gizmo")]
    public bool drawGizmoAlways = true;            // ������ �׻� ǥ��
    public bool drawGizmoWhenSelected = true;      // ���� �� ǥ��
    public Color gizmoFill = new Color(1f, 0f, 0f, 0.12f);
    public Color gizmoLine = new Color(1f, 0f, 0f, 0.9f);
    public Color gizmoArmedLine = new Color(0.25f, 1f, 0.25f, 1f);

    [Header("Runtime (read-only)")]
    public bool armed;
    public HitMode mode = HitMode.Single;
    public float tickInterval = 0.2f; // Continuous�� ���� ���

    // ������/�⺻ �Ķ����(Arm���� ����)
    public Unit ownerUnit;
    public StatsBase ownerStats;
    public float damage = 10f;          // base damage (�¼��� CombatMath�� ���� ����)
    public Vector2 knockback = new Vector2(6f, 2f);

    // �ܺ� ��(����Ʈ/���� ��)
    public event Action<Collider2D, float> OnHit;

    // ���� ����
    Collider2D col;
    // Continuous ���: ���ֺ� ������ Ÿ�� �ð� �� tickInterval���� 1ȸ
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
        Disarm(); // ������ ����
    }

    void OnEnable() { _lastHitAt.Clear(); }
    void OnDisable() { _lastHitAt.Clear(); }

    /// <summary>��Ƽ�� ���� ���� �� ȣ��.</summary>
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

    /// <summary>��Ƽ�� ���� ���� �� ȣ��.</summary>
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
            TryApplyHit(other, /*respectTick*/ true); // ���� �� 1ȸ(����)
    }

    void OnTriggerStay2D(Collider2D other)              // Ȯ�强 �����ؼ� �̸� �߰��ص�(���� Ȥ�� ���ǿ�)
    {
        if (!armed || mode != HitMode.Continuous) return;
        TryApplyHit(other, /*respectTick*/ true); // ƽ ���� ����
    }

    bool TryApplyHit(Collider2D other, bool respectTick)
    {
        // ��� Unit/Stats ã��
        var targetUnit =
               other.GetComponentInParent<Unit>()
            ?? other.GetComponent<Unit>()
            ?? other.GetComponentInChildren<Unit>();
        if (targetUnit == null) return false;
        if (ownerUnit != null && targetUnit == ownerUnit) return false; // �ڱ� �ڽ� ����

        if (respectTick)
        {
            if (_lastHitAt.TryGetValue(targetUnit, out float last))
                if (Time.time - last < tickInterval) return false;
        }

        var targetStats =
               other.GetComponentInParent<StatsBase>()
            ?? other.GetComponent<StatsBase>()
            ?? other.GetComponentInChildren<StatsBase>();

        // ���� ������ ���
        float finalDamage = (ownerStats != null && targetStats != null)
            ? CombatMath.ComputeDamage(ownerStats, targetStats, damage)
            : damage;

        // Ÿ�� ����
        targetUnit.Damage(finalDamage, ownerUnit);

        // �˹�: �����ڡ��ǰ��� ����, force = knockback.x
        Vector2 dir = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;
        float force = knockback.x;
        if (force > 0f) targetUnit.ApplyKnockback(dir, force);

        if (debugLog) Debug.Log($"[HIT] {attackerName} -> {other.name} dmg={finalDamage}");

        _lastHitAt[targetUnit] = Time.time;
        OnHit?.Invoke(other, finalDamage);
        return true;
    }

#if UNITY_EDITOR
    // ���� Gizmo: ��Ʈ ���� �ð�ȭ ��������������������������������������������������������������������������������������������
    void DrawBoxGizmo(BoxCollider2D box, Color line, Color fill)
    {
        if (!box) return;
        var prev = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        Vector3 center = (Vector3)box.offset;
        Vector3 size = (Vector3)box.size;
        size.z = 0.01f;

        // ä��
        Gizmos.color = fill;
        Gizmos.DrawCube(center, size);
        // �ܰ���
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
        Gizmos.DrawSphere(center, 0.001f); // ���� ä�� ���
        // ������ ���� ��
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
            // ĸ���� �뷫���� �ڽ��� ǥ��
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


