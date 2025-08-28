using System.Collections;
using UnityEngine;
using Hercules.StatsSystem;

[DisallowMultipleComponent]
public class MonsterHitboxAttackController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("빨간 원(AttackRange) 오브젝트의 Hitbox를 할당하세요")]
    public Hitbox hitbox;                 // ← AttackRange에 붙은 Hitbox

    private Unit self;
    private StatsBase stats;

    [Header("Attack Numbers")]
    public float baseDamage = 10f;
    public float knockback = 6f;
    public Hitbox.HitMode mode = Hitbox.HitMode.Single;

    [Header("Timings (seconds)")]
    public float startup = 0.08f;
    public float active = 0.12f;
    public float recovery = 0.20f;
    public float cooldown = 0.35f;

    [Header("Options")]
    public bool logDebug = false;

    bool _busy, _cooling;

    void Awake()
    {
        if (!hitbox)
        {
            // 자식에서 자동 탐색
            hitbox = GetComponentInChildren<Hitbox>(includeInactive: true);
        }
        self = GetComponent<Unit>();
        stats = GetComponent<StatsBase>() ?? gameObject.AddComponent<StatsBase>();

        if (hitbox) hitbox.Disarm();
    }

    /// <summary>애니/BT에서 공격 활성 프레임 시작</summary>
    public void Attack_Activate()
    {
        if (!hitbox || !self || !stats) return;
        hitbox.Arm(self, stats, baseDamage, new Vector2(knockback, 0f), mode);
        if (logDebug) Debug.Log($"[{name}] Attack_Activate (Arm)");
    }

    /// <summary>애니/BT에서 공격 활성 프레임 종료</summary>
    public void Attack_Deactivate()
    {
        if (!hitbox) return;
        hitbox.Disarm();
        if (logDebug) Debug.Log($"[{name}] Attack_Deactivate (Disarm)");
    }

    /// <summary>스타트업~액티브~리커버리~쿨다운</summary>
    public void TryAttackOnce()
    {
        if (!_busy && !_cooling) StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        _busy = true;

        if (startup > 0f) yield return new WaitForSeconds(startup);

        if (hitbox && self && stats)
        {
            hitbox.Arm(self, stats, baseDamage, new Vector2(knockback, 0f), mode);
            if (logDebug) Debug.Log($"[{name}] Arm -> active {active:0.###}s");
        }

        if (active > 0f) yield return new WaitForSeconds(active);

        if (hitbox)
        {
            hitbox.Disarm();
            if (logDebug) Debug.Log($"[{name}] Disarm()");
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
