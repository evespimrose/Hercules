using System;
using System.Collections.Generic;
using UnityEngine;
using Hercules.StatsSystem;

/// <summary>
/// 히트박스(Trigger).
/// - BT/액션/애니에서 Arm/Disarm으로 켜고 끔
/// - Arm 직후 1회 판정(hitOnceOnArm) 선택 가능
/// - CombatMath(크리/증뎀/감뎀), on-hit 상태이상(출혈/스턴/히트스톱) 옵션 제공
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class Hitbox : MonoBehaviour
{
    public enum HitMode { Single, Continuous }

    // ─────────────────────────────────────────────────────────────
    [Header("Damage")]
    [Tooltip("기본 피해(CombatMath의 baseDamage로 들어감)")]
    public float damage = 10f;

    [Tooltip("넉백(우리는 X성분을 force로 사용)")]
    public Vector2 knockback = new Vector2(6f, 0f);

    // ─────────────────────────────────────────────────────────────
    [Header("Runtime (read-only)")]
    [SerializeField] private bool armed = false;

    [Tooltip("Single: 1스윙 1히트 / Continuous: tick 간격 반복 히트")]
    public HitMode mode = HitMode.Single;

    [Tooltip("Continuous 모드에서 동일 대상 최소 타격 간격(초)")]
    public float tickInterval = 0.2f;

    // ─────────────────────────────────────────────────────────────
    [Header("Owner (runtime set)")]
    public string attackerName = "Attacker";
    public bool debugLog = false;
    public Unit ownerUnit;
    public StatsBase ownerStats; // Arm 시 ownerUnit에서 찾아도 OK

    // ─────────────────────────────────────────────────────────────
    [Header("On-Hit Status (optional)")]
    public bool applyBleeding = false;
    [Tooltip("일반 출혈(고정틱)의 지속시간")]
    public float bleedingDuration = 2f;

    public bool applyBleedingStack = false;
    [Tooltip("스택형 출혈의 추가 스택 수")]
    public int bleedingStacks = 1;
    [Tooltip("스택형 출혈의 지속시간")]
    public float bleedingStackDuration = 3f;

    public bool applyStun = false;
    public float stunTime = 0.4f;

    public bool applyHitstop = false;
    public float hitstopTime = 0.05f;
    [Tooltip("히트스톱 타임스케일(0.01~1). 0 이하면 0.05 사용")]
    public float hitstopScale = 0.05f;

    // ─────────────────────────────────────────────────────────────
    [Header("Activation")]
    [Tooltip("Arm() 하자마자 즉시 1회 판정 실행")]
    public bool hitOnceOnArm = true;

    // ─────────────────────────────────────────────────────────────
    // Internals
    private readonly HashSet<Unit> _hitThisSwing = new HashSet<Unit>();
    private readonly Dictionary<Unit, float> _lastHitAt = new Dictionary<Unit, float>();

    private Collider2D _col;
    private static readonly List<Collider2D> _overlaps = new List<Collider2D>(16);
    private ContactFilter2D _noFilter;

    void Awake()
    {
        _col = GetComponent<Collider2D>();
        if (_col == null) _col = gameObject.AddComponent<BoxCollider2D>();
        _col.isTrigger = true;

        _noFilter = new ContactFilter2D
        {
            useTriggers = true,
            useLayerMask = false
        };

        Disarm(); // 시작은 항상 Off
    }

    // ====== Public API ========================================================

    /// <summary>BT/액션/애니에서 공격 시작(판정 On)</summary>
    public void Arm(Unit owner, StatsBase stats, float baseDamage, Vector2 kb, HitMode m)
    {
        ownerUnit = owner;
        ownerStats = stats;
        damage = baseDamage;
        knockback = kb;
        mode = m;

        _hitThisSwing.Clear();
        armed = true;

        // Arm 직후 1회 즉시 판정 (선택)
        if (hitOnceOnArm) ImmediateHitScan();
    }

    /// <summary>공격 종료(판정 Off)</summary>
    public void Disarm()
    {
        armed = false;
        _hitThisSwing.Clear();
    }

    // ====== Physics ===========================================================

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!armed) return;
        TryApplyHit(other, respectTick: mode == HitMode.Continuous);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!armed) return;
        if (mode == HitMode.Continuous)
            TryApplyHit(other, respectTick: true);
    }

    // ====== Immediate Scan (Arm 즉시 1회) =====================================

    private void ImmediateHitScan()
    {
        if (_col == null) return;

        _overlaps.Clear();
        int count = _col.OverlapCollider(_noFilter, _overlaps);
        for (int i = 0; i < count; i++)
        {
            var other = _overlaps[i];
            // Single 모드에서는 _hitThisSwing으로 중복 방지
            TryApplyHit(other, respectTick: false);
        }
    }

    // ====== Core =============================================================

    private bool TryApplyHit(Collider2D other, bool respectTick)
    {
        // 1) 대상 Unit 찾기
        Unit targetUnit =
               other.GetComponentInParent<Unit>()
            ?? other.GetComponent<Unit>()
            ?? other.GetComponentInChildren<Unit>();

        if (targetUnit == null) return false;
        if (ownerUnit != null && targetUnit == ownerUnit) return false; // 자기 자신 무시

        // Single: 한 스윙 내 중복 방지
        if (mode == HitMode.Single && _hitThisSwing.Contains(targetUnit))
            return false;

        // Continuous: 틱 간격
        if (respectTick && _lastHitAt.TryGetValue(targetUnit, out float last))
            if (Time.time - last < tickInterval) return false;

        // 2) 공격/방어 Stats
        StatsBase atkStats =
              ownerStats
           ?? ownerUnit?.GetComponentInParent<StatsBase>()
           ?? ownerUnit?.GetComponentInChildren<StatsBase>();

        StatsBase defStats =
              targetUnit.GetComponentInParent<StatsBase>()
           ?? targetUnit.GetComponentInChildren<StatsBase>();

        // 3) 최종 데미지 + 크리 여부
        bool isCrit = false;
        float finalDamage =
            (atkStats != null && defStats != null)
                ? CombatMath.ComputeDamage(atkStats, defStats, damage, out isCrit)
                : damage;

        if (isCrit)
            Debug.Log("Critical Hits !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");

        // 4) 데미지/넉백
        targetUnit.Damage(finalDamage, ownerUnit);

        Vector2 dir = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;
        float force = knockback.x;
        if (force > 0f) targetUnit.ApplyKnockback(dir, force);

        // 5) on-hit 상태이상(옵션)
        if (applyBleeding && bleedingDuration > 0f)
            targetUnit.Mesmerize(bleedingDuration, Unit.Buff.Bleeding);

        if (applyBleedingStack && bleedingStackDuration > 0f && bleedingStacks > 0)
            targetUnit.Mesmerize(bleedingStackDuration, Unit.Buff.BleedingStack, magnitude: bleedingStacks);

        if (applyStun && stunTime > 0f)
            targetUnit.Mesmerize(stunTime, Unit.Buff.Stun);

        if (applyHitstop && hitstopTime > 0f)
            targetUnit.Mesmerize(hitstopTime, Unit.Buff.Hitstop, magnitude: Mathf.Max(0.01f, hitstopScale));

        if (debugLog)
            Debug.Log($"[HIT] {attackerName} -> {other.name} dmg={finalDamage}");

        // 6) 북키핑
        _hitThisSwing.Add(targetUnit);
        _lastHitAt[targetUnit] = Time.time;

        return true;
    }
}
