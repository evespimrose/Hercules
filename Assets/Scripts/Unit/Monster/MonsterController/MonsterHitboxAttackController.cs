using System.Collections;
using UnityEngine;
using Hercules.StatsSystem;

/// <summary>
/// BT/�ִϸ��̼ǿ��� ȣ���ϱ� ���� ��Ʈ�ڽ� �긴��.
/// - ���� BT�� DealDamage�� ���� ȣ���ϴ� �κ��� Arm/Disarm ������� ġȯ
/// - �ִ� �̺�Ʈ/BT Ÿ�ֿ̹��� Attack_Activate()/Attack_Deactivate()�� ȣ���ص� �ǰ�,
///   TryAttackOnce() �ϳ��� ��ŸƮ��~��Ƽ��~��Ŀ����~��ٿ���� �ڵ� ���൵ ����.
/// </summary>
[DisallowMultipleComponent]
public class MonsterHitboxAttackController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("�ڽ� ������Ʈ�� �ִ� Hitbox ������Ʈ�� �Ҵ� (BoxCollider2D isTrigger=true �ʼ�)")]
    public Hitbox hitbox;

    private Unit self;
    private StatsBase stats;

    [Header("Attack Numbers")]
    [Tooltip("CombatMath�� ���޵Ǵ� baseDamage")]
    public float baseDamage = 10f;
    [Tooltip("�˹� ��(��Ʈ�ڽ��� X������ force�� ���)")]
    public float knockback = 6f;
    public Hitbox.HitMode mode = Hitbox.HitMode.Single;

    [Header("Timings (seconds)")]
    public float startup = 0.08f;    // ����
    public float active = 0.12f;     // ���� on ����
    public float recovery = 0.20f;   // �ĵ�
    public float cooldown = 0.35f;   // ��ٿ� (TryAttackOnce ����)

    [Header("Options")]
    public bool logDebug = false;

    private bool _busy;     // TryAttackOnce ��
    private bool _cooling;  // ��ٿ� ��

    void Awake()
    {
        if (!hitbox)
        {
            // �ڽĿ��� �ڵ� Ž��
            hitbox = GetComponentInChildren<Hitbox>(includeInactive: true);
            if (!hitbox) Debug.LogError($"{name}: Hitbox reference is missing.");
        }

        self = GetComponent<Unit>();
        stats = GetComponent<StatsBase>();
        if (!stats) stats = gameObject.AddComponent<StatsBase>(); // ������ ������ġ

        if (hitbox) hitbox.Disarm();
    }

    // ��������������������������������������������������������������������������������������������������������������������������
    // �� �ִ�/BT�� ������ Ÿ�̹��� ���� �����ϴ� ���
    //    (�ִ� �̺�Ʈ�� ��Ƽ�� ����/���� �����ӿ� ����)
    // ��������������������������������������������������������������������������������������������������������������������������

    /// <summary>��Ƽ�� ������ ����: ��Ʈ�ڽ� On</summary>
    public void Attack_Activate()
    {
        if (!hitbox || !self || !stats) return;
        hitbox.Arm(self, stats, baseDamage, new Vector2(knockback, 0f), mode);
        if (logDebug) Debug.Log($"[{name}] Attack_Activate (Arm)");
    }

    /// <summary>��Ƽ�� ������ ����: ��Ʈ�ڽ� Off</summary>
    public void Attack_Deactivate()
    {
        if (!hitbox) return;
        hitbox.Disarm();
        if (logDebug) Debug.Log($"[{name}] Attack_Deactivate (Disarm)");
    }

    // ��������������������������������������������������������������������������������������������������������������������������
    // �� �� �� ȣ��� ��ŸƮ��~��Ƽ��~��Ŀ����~��ٿ� �ڵ� ����
    //    (BT�� ���� DealDamage ȣ�� ��ġ�� TryAttackOnce�� ��ü)
    // ��������������������������������������������������������������������������������������������������������������������������

    public void TryAttackOnce()
    {
        if (!_busy && !_cooling)
            StartCoroutine(AttackRoutine());
    }

    // attackRange와 타겟을 전달받아 실제 히트가 범위 내에서만 발생하도록 보장
    public void TryAttackOnce(float maxRange, Transform target)
    {
        if (!_busy && !_cooling)
            StartCoroutine(AttackRoutineWithRange(maxRange, target));
    }

    private IEnumerator AttackRoutine()
    {
        _busy = true;

        // Startup
        if (startup > 0f) yield return new WaitForSeconds(startup);

        // Active (On)
        if (hitbox && self && stats)
        {
            hitbox.Arm(self, stats, baseDamage, new Vector2(knockback, 0f), mode);
            if (logDebug) Debug.Log($"[{name}] Arm() -> active {active:0.###}s");
        }

        if (active > 0f) yield return new WaitForSeconds(active);

        // Active (Off)
        if (hitbox)
        {
            hitbox.Disarm();
            if (logDebug) Debug.Log($"[{name}] Disarm()");
        }

        // Recovery
        if (recovery > 0f) yield return new WaitForSeconds(recovery);

        _busy = false;

        // Cooldown
        if (cooldown > 0f)
        {
            _cooling = true;
            yield return new WaitForSeconds(cooldown);
            _cooling = false;
        }
    }

    private IEnumerator AttackRoutineWithRange(float maxRange, Transform target)
    {
        _busy = true;

        // Startup 전 빠른 거리 체크
        if (target == null || self == null)
        {
            _busy = false;
            yield break;
        }

        if (startup > 0f) yield return new WaitForSeconds(startup);

        // Active (On) 직전 거리 재확인 - 범위를 벗어나면 공격 생략
        if (target == null || self == null)
        {
            _busy = false;
            yield break;
        }

        float dist = Vector2.Distance(self.transform.position, target.position);
        if (dist <= maxRange)
        {
            if (hitbox && self && stats)
            {
                hitbox.Arm(self, stats, baseDamage, new Vector2(knockback, 0f), mode);
                if (logDebug) Debug.Log($"[{name}] Arm() -> active {active:0.###}s (range ok: {dist:0.##} <= {maxRange:0.##})");
            }

            if (active > 0f) yield return new WaitForSeconds(active);

            if (hitbox)
            {
                hitbox.Disarm();
                if (logDebug) Debug.Log($"[{name}] Disarm()");
            }
        }
        else if (logDebug)
        {
            Debug.Log($"[{name}] Attack skipped (out of range: {dist:0.##} > {maxRange:0.##})");
        }

        if (recovery > 0f) yield return new WaitForSeconds(recovery);

        _busy = false;

        if (cooldown > 0f)
        {
            _cooling = true;
            yield return new WaitForSeconds(cooldown);
            _cooling = false;
        }
    }

    public bool IsBusyOrCooling => _busy || _cooling;
}
