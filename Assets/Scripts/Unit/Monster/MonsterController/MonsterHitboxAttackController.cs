using System.Collections;
using UnityEngine;
using Hercules.StatsSystem;

/// <summary>
/// BT/애니메이션에서 호출하기 위한 히트박스 브릿지.
/// - 기존 BT가 DealDamage를 직접 호출하던 부분을 Arm/Disarm 기반으로 치환
/// - 애니 이벤트/BT 타이밍에서 Attack_Activate()/Attack_Deactivate()만 호출해도 되고,
///   TryAttackOnce() 하나로 스타트업~액티브~리커버리~쿨다운까지 자동 실행도 가능.
/// </summary>
[DisallowMultipleComponent]
public class MonsterHitboxAttackController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("자식 오브젝트에 있는 Hitbox 컴포넌트를 할당 (BoxCollider2D isTrigger=true 필수)")]
    public Hitbox hitbox;

    private Unit self;
    private StatsBase stats;

    [Header("Attack Numbers")]
    [Tooltip("CombatMath에 전달되는 baseDamage")]
    public float baseDamage = 10f;
    [Tooltip("넉백 힘(히트박스가 X성분을 force로 사용)")]
    public float knockback = 6f;
    public Hitbox.HitMode mode = Hitbox.HitMode.Single;

    [Header("Timings (seconds)")]
    public float startup = 0.08f;    // 예열
    public float active = 0.12f;     // 판정 on 구간
    public float recovery = 0.20f;   // 후딜
    public float cooldown = 0.35f;   // 쿨다운 (TryAttackOnce 전용)

    [Header("Options")]
    public bool logDebug = false;

    private bool _busy;     // TryAttackOnce 중
    private bool _cooling;  // 쿨다운 중

    void Awake()
    {
        if (!hitbox)
        {
            // 자식에서 자동 탐색
            hitbox = GetComponentInChildren<Hitbox>(includeInactive: true);
            if (!hitbox) Debug.LogError($"{name}: Hitbox reference is missing.");
        }

        self = GetComponent<Unit>();
        stats = GetComponent<StatsBase>();
        if (!stats) stats = gameObject.AddComponent<StatsBase>(); // 과도기 안전장치

        if (hitbox) hitbox.Disarm();
    }

    // ─────────────────────────────────────────────────────────────
    // ① 애니/BT가 프레임 타이밍을 직접 제어하는 방식
    //    (애니 이벤트로 액티브 시작/종료 프레임에 연결)
    // ─────────────────────────────────────────────────────────────

    /// <summary>액티브 프레임 시작: 히트박스 On</summary>
    public void Attack_Activate()
    {
        if (!hitbox || !self || !stats) return;
        hitbox.Arm(self, stats, baseDamage, new Vector2(knockback, 0f), mode);
        if (logDebug) Debug.Log($"[{name}] Attack_Activate (Arm)");
    }

    /// <summary>액티브 프레임 종료: 히트박스 Off</summary>
    public void Attack_Deactivate()
    {
        if (!hitbox) return;
        hitbox.Disarm();
        if (logDebug) Debug.Log($"[{name}] Attack_Deactivate (Disarm)");
    }

    // ─────────────────────────────────────────────────────────────
    // ② 한 번 호출로 스타트업~액티브~리커버리~쿨다운 자동 실행
    //    (BT의 기존 DealDamage 호출 위치를 TryAttackOnce로 교체)
    // ─────────────────────────────────────────────────────────────

    public void TryAttackOnce()
    {
        if (!_busy && !_cooling)
            StartCoroutine(AttackRoutine());
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

    public bool IsBusyOrCooling => _busy || _cooling;
}
