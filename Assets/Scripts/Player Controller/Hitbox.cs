using System;
using System.Collections.Generic;
using UnityEngine;
using Hercules.StatsSystem;

/// <summary>
/// ������ Ʈ���� ��Ʈ�ڽ�.
/// - BT/�ִ� �����ӿ��� Arm/Disarm���� ���� Ȱ�� ������ �Ѱ� ��
/// - Arm ������ �̹� ���� �־ 1ȸ Ÿ�� ����(hitOnceOnArm)
/// - CombatMath ����(ũ��/���/���� �ݿ�), on-hit ����(����/����/��Ʈ��ž) �ɼ� ����
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class Hitbox : MonoBehaviour
{
    public enum HitMode { Single, Continuous }

    // ��������������������������������������������������������������������������������������������������������������������������
    [Header("Damage")]
    [Tooltip("�⺻ ����(CombatMath�� baseDamage�� ����)")]
    public float damage = 10f;

    [Tooltip("�˹�(force�� X�� ���). ������ (�ǰ���-��Ʈ�ڽ�)")]
    public Vector2 knockback = new Vector2(6f, 0f);

    // ��������������������������������������������������������������������������������������������������������������������������
    [Header("Runtime (read-only)")]
    [SerializeField] private bool armed = false;

    [Tooltip("Single: �� ���� 1��Ʈ / Continuous: tick �������� �ݺ� ��Ʈ")]
    public HitMode mode = HitMode.Single;

    [Tooltip("Continuous ��忡�� ���� ��� ��Ÿ�� ����(��)")]
    public float tickInterval = 0.2f;

    // ��������������������������������������������������������������������������������������������������������������������������
    [Header("Owner (runtime set)")]
    public string attackerName = "Attacker";
    public bool debugLog = false;
    public Unit ownerUnit;
    public StatsBase ownerStats;  // ������ Arm �� ownerUnit���� ã��

    // ��������������������������������������������������������������������������������������������������������������������������
    [Header("On-Hit Status (optional)")]
    public bool applyBleeding = false;
    [Tooltip("�Ϲ� ����(��-����)�� ���ӽð�")]
    public float bleedingDuration = 2f;

    public bool applyBleedingStack = false;
    [Tooltip("������ ������ ���� ��")]
    public int bleedingStacks = 1;
    [Tooltip("������ ������ ���ӽð�")]
    public float bleedingStackDuration = 3f;

    public bool applyStun = false;
    public float stunTime = 0.4f;

    public bool applyHitstop = false;
    public float hitstopTime = 0.05f;
    [Tooltip("��Ʈ��ž Ÿ�ӽ�����(0.01~1). 0 ���ϸ� 0.05 ���")]
    public float hitstopScale = 0.05f;

    // ��������������������������������������������������������������������������������������������������������������������������
    [Header("Activation")]
    [Tooltip("Arm() �ϴ� �����ӿ� �̹� �����־ 1ȸ Ÿ�� ����")]
    public bool hitOnceOnArm = true;

    // ��������������������������������������������������������������������������������������������������������������������������
    // ���� ����
    private readonly HashSet<Unit> _hitThisSwing = new HashSet<Unit>();
    private readonly Dictionary<Unit, float> _lastHitAt = new Dictionary<Unit, float>();

    private Collider2D _col;
    private static readonly List<Collider2D> _overlaps = new List<Collider2D>(16);
    private ContactFilter2D _noFilter;

    // ��������������������������������������������������������������������������������������������������������������������������
    void Awake()
    {
        _col = GetComponent<Collider2D>();
        if (_col == null) _col = gameObject.AddComponent<BoxCollider2D>();
        _col.isTrigger = true;

        _noFilter = new ContactFilter2D
        {
            useTriggers = true,   // Ʈ���ŵ� ��ĵ
            useLayerMask = false  // ���̾� ��Ʈ������ ���������� ó��
        };

        Disarm(); // ���� �� ����
    }

    // ====== Public API ========================================================

    /// <summary>BT/�ִ� �����ӿ��� ���� Ȱ�� ���� �� ȣ��</summary>
    public void Arm(Unit owner, StatsBase stats, float baseDamage, Vector2 kb, HitMode m)
    {
        ownerUnit = owner;
        ownerStats = stats;
        damage = baseDamage;
        knockback = kb;
        mode = m;

        _hitThisSwing.Clear();
        armed = true;

        // �Ѵ� �����ӿ� �̹� ��ģ ��� 1ȸ Ÿ��
        if (hitOnceOnArm) ImmediateHitScan();
    }

    /// <summary>BT/�ִ� �����ӿ��� ���� Ȱ�� ���� �� ȣ��</summary>
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

    // ====== Immediate Scan (Arm�� 1ȸ) =======================================

    private void ImmediateHitScan()
    {
        if (_col == null) return;

        _overlaps.Clear();
        int count = _col.OverlapCollider(_noFilter, _overlaps);
        for (int i = 0; i < count; i++)
        {
            var other = _overlaps[i];
            // Single ����� _hitThisSwing���� �ߺ� ����
            TryApplyHit(other, respectTick: false);
        }
    }

    // ====== Core =============================================================

    private bool TryApplyHit(Collider2D other, bool respectTick)
    {
        // 1) ��� Unit ã��(�а�)
        Unit targetUnit =
               other.GetComponentInParent<Unit>()
            ?? other.GetComponent<Unit>()
            ?? other.GetComponentInChildren<Unit>();

        if (targetUnit == null) return false;
        if (ownerUnit != null && targetUnit == ownerUnit) return false; // �ڱ� �ڽ� ����

        // Single: ���� ���� �� �ߺ� ����
        if (mode == HitMode.Single && _hitThisSwing.Contains(targetUnit))
            return false;

        // Continuous: ƽ ����
        if (respectTick && _lastHitAt.TryGetValue(targetUnit, out float last))
            if (Time.time - last < tickInterval) return false;

        // 2) ����/��� ����
        StatsBase atkStats =
              ownerStats
           ?? ownerUnit?.GetComponentInParent<StatsBase>()
           ?? ownerUnit?.GetComponentInChildren<StatsBase>();

        StatsBase defStats =
              targetUnit.GetComponentInParent<StatsBase>()
           ?? targetUnit.GetComponentInChildren<StatsBase>();

        // 3) ���� ������ + ũ�� ����
        bool isCrit = false;
        float finalDamage =
            (atkStats != null && defStats != null)
                ? CombatMath.ComputeDamage(atkStats, defStats, damage, out isCrit)
                : damage;

        if (isCrit)
            Debug.Log("ġ�ڸ��Ÿ !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");

        // 4) ����/�˹� ����
        targetUnit.Damage(finalDamage, ownerUnit);

        Vector2 dir = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;
        float force = knockback.x;
        if (force > 0f) targetUnit.ApplyKnockback(dir, force);

        // 5) on-hit ���� (�ɼ�)
        if (applyBleeding && bleedingDuration > 0f)
            targetUnit.Mesmerize(bleedingDuration, Unit.Buff.Bleeding);

        if (applyBleedingStack && bleedingStackDuration > 0f && bleedingStacks > 0)
            targetUnit.Mesmerize(bleedingStackDuration, Unit.Buff.BleedingStack, magnitude: bleedingStacks);

        if (applyStun && stunTime > 0f)
            targetUnit.Mesmerize(stunTime, Unit.Buff.Stun);

        if (applyHitstop && hitstopTime > 0f)
            targetUnit.Mesmerize(hitstopTime, Unit.Buff.Hitstop, magnitude: hitstopScale);

        if (debugLog)
            Debug.Log($"[HIT] {attackerName} -> {other.name} dmg={finalDamage}");

        // 6) å����
        _hitThisSwing.Add(targetUnit);
        _lastHitAt[targetUnit] = Time.time;

        return true;
    }
}
