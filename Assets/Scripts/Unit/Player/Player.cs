using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class Player : Unit
{
    private PlayerController playerController;
    Coroutine hitstopRoutine;

    // === Indomitable(불굴) ===
    [Header("Indomitable (Player-only)")]
    public bool enableIndomitable = true;
    public float indomitableDuration = 5f;
    bool indomitableActive;
    bool indomitableConsumed;
    Coroutine indomitableRoutine;

    // === Exhaustion(탈진) ===
    [Header("Exhaustion (Player-only)")]
    [Tooltip("Exhaustion 중 이동 속도 배수(0.2f = 80% 감소)")]
    public float ExhaustionMoveMul = 0.2f;
    [Tooltip("Exhaustion 중 공격 타이밍 배수(2f = 100% 느려짐)")]
    public float ExhaustionAttackTimeScale = 2f;
    [Tooltip("Exhaustion 중 공격 피해 배수(0.5f = 50% 감소)")]
    public float ExhaustionDamageMul = 0.5f;
    [Tooltip("Exhaustion 중 점프 높이 배수(0.5f = 50% 감소)")]
    public float ExhaustionJumpMul = 0.5f;

    bool ExhaustionActive;
    bool SlowMoveActive;
    bool SlowAttackActive;
    bool WeakAttackActive;
    bool LowJumpActive;
    Coroutine ExhaustionRoutine;

    protected override Dictionary<Buff, IBuffEffect> BuffEffects { get; } =
        new Dictionary<Buff, IBuffEffect>()
        {
            { Buff.Knockback,  new KnockbackEffect()  },
            { Buff.Stun,       new StunEffect()       },
            { Buff.Invincible, new InvincibleEffect() },
            { Buff.Hitstop,    new HitstopEffect()    },
            { Buff.Indomitable,new IndomitableEffect() },                   // 불굴
            { Buff.Bleeding,   new BleedingEffect()   },                    // 출혈
            { Buff.BleedingStack, new BleedingStackEffect() },              //출혈 - 스택형
            { Buff.Exhaustion,       new ExhaustionEffect()       },        // 탈진
            { Buff.SlowMove,       new SlowMoveEffect()       },            // 이속감소
            { Buff.SlowAttack,       new SlowAttackEffect()       },        // 공속감소
            { Buff.WeakAttack,       new WeakAttackEffect()       },        // 공격 데미지 감소
            { Buff.LowJump,       new LowJumpEffect()       },              // 점프 높이 감소
        };

    protected override void Awake()
    {
        base.Awake();
        playerController = GetComponent<PlayerController>();
    }

    // HP<1로 내려가면 Indomitable 트리거
    public override void Damage(float amount, Unit source)
    {
        if (IsDead) return;
        if (isInvincible) { UnityEngine.Debug.Log($"{name} 무적: 피해 무시"); return; }

        amount = Mathf.Max(0f, amount);
        float pendingHp = Mathf.Max(0f, currentHealth - amount);

        // 자동 발동
        if (enableIndomitable && !indomitableActive && !indomitableConsumed && pendingHp < 1f)
        {
            if (source)
            {
                var dir = (transform.position - source.transform.position);
                lastHitDirection = dir.sqrMagnitude > 1e-6f ? (Vector2)dir.normalized : Vector2.zero;
            }
            TriggerIndomitable(indomitableDuration);
            return; // 즉사 방지
        }

        base.Damage(amount, source);
    }

    // 외부에서도 버프로 호출 가능하게 공개 메서드 제공
    public void TriggerIndomitable(float duration)
    {
        if (!enableIndomitable || indomitableActive || indomitableConsumed) return;

        currentHealth = Mathf.Max(1f, currentHealth); // 즉시 사망 방지
        indomitableActive = true;
        indomitableConsumed = true;

        // 5초 무적
        Mesmerize(duration, Buff.Invincible);

        // 5초 뒤 강제 사망
        if (indomitableRoutine != null) StopCoroutine(indomitableRoutine);
        indomitableRoutine = StartCoroutine(IndomitableRoutine(duration));

        UnityEngine.Debug.Log($"[Player] Indomitable TRIGGERED: {duration}초 후 사망");
    }

    IEnumerator IndomitableRoutine(float time)
    {
        yield return new WaitForSeconds(time);
        indomitableActive = false;
        Die(); // 무적 종료와 무관하게 강제 사망
    }

    public bool CanUseIndomitable => enableIndomitable && !indomitableActive && !indomitableConsumed;

    public void ResetIndomitable(bool clearInvincibility = true)
    {
        //  불굴 사망 타이머 중단
        if (indomitableRoutine != null) { StopCoroutine(indomitableRoutine); indomitableRoutine = null; }

        //  상태 플래그 리셋 → 다시 발동 가능
        indomitableActive = false;
        indomitableConsumed = false;

        //  무적도 즉시 해제하여 완전 초기화
        if (clearInvincibility)
        {
            if (invincibleRoutine != null) { StopCoroutine(invincibleRoutine); invincibleRoutine = null; }
            isInvincible = false; // 코루틴을 끊고 false로 내려줌
        }

        UnityEngine.Debug.Log("[Player] Indomitable reset" + (clearInvincibility ? " (invincibility cleared)" : ""));
    }

    public override void Die()
    {
        if (IsDead) return;
        foreach (var c in EnumerateControlBehaviours())
            if (c) c.enabled = false;

        // Exhaustion 디버프도 정리
        ClearExhaustion();

        base.Die();
    }

    public override void ApplyHitstop(float time)
    {
        if (hitstopRoutine != null) StopCoroutine(hitstopRoutine);
        hitstopRoutine = StartCoroutine(HitstopCoroutine(time));
    }

    private IEnumerator HitstopCoroutine(float time)
    {
        float originalScale = Time.timeScale;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(time);
        Time.timeScale = originalScale;
        hitstopRoutine = null;
    }

    protected override IEnumerator StunCoroutine(float time)
    {
        var controls = new List<Behaviour>(EnumerateControlBehaviours());
        var prev = new Dictionary<Behaviour, bool>(controls.Count);
        foreach (var c in controls) if (c) prev[c] = c.enabled;
        foreach (var c in controls) if (c) c.enabled = false;

        yield return new WaitForSeconds(time);

        foreach (var kv in prev) if (kv.Key) kv.Key.enabled = kv.Value;
        stunRoutine = null;
    }

    private IEnumerable<Behaviour> EnumerateControlBehaviours()
    {
        var pc = GetComponent<PlayerController>(); if (pc) yield return pc;
        var mv = GetComponent<MoveAbility>(); if (mv) yield return mv;
        var jp = GetComponent<JumpAbility>(); if (jp) yield return jp;
        var ds = GetComponent<DashAbility>(); if (ds) yield return ds;
        var atk = GetComponent<AttackAbility>(); if (atk) yield return atk;
    }

    // ===== Exhaustion =====
    public void ApplyExhaustion()
    {
        // 이미 적용 중이면 다시 실행할 필요 없음
        if (SlowMoveActive && SlowAttackActive && WeakAttackActive && LowJumpActive) return;

        //SetExhaustionActive(true);
        SetSlowMoveActive(true);
        SetSlowAttackActive(true);
        SetWeakAttackActive(true);
        SetLowJumpActive(true);
        UnityEngine.Debug.Log("Exhaustion 시작");
    }

    // Exhaustion 해제
    public void ClearExhaustion()
    {
        SetSlowMoveActive(false);
        SetSlowAttackActive(false);
        SetWeakAttackActive(false);
        SetLowJumpActive(false);
        UnityEngine.Debug.Log("Exhaustion 해제");
    }

    // SlowMove
    public void ApplySlowMove()
    {
        // 이미 적용 중이면 다시 실행할 필요 없음
        if (SlowMoveActive) return;

        SetSlowMoveActive(true);
        UnityEngine.Debug.Log("SlowMove 시작");
    }

    // SlowAttack
    public void ApplySlowAttack()
    {
        // 이미 적용 중이면 다시 실행할 필요 없음
        if (SlowAttackActive) return;

        SetSlowAttackActive(true);
        UnityEngine.Debug.Log("SlowAttack 시작");
    }

    // WeakAttack
    public void ApplyWeakAttack()
    {
        // 이미 적용 중이면 다시 실행할 필요 없음
        if (WeakAttackActive) return;

        SetWeakAttackActive(true);
        UnityEngine.Debug.Log("WeakAttack 시작");
    }

    // LowJump
    public void ApplyLowJump()
    {
        // 이미 적용 중이면 다시 실행할 필요 없음
        if (LowJumpActive) return;

        SetLowJumpActive(true);
        UnityEngine.Debug.Log("LowJump 시작");
    }

    void SetSlowMoveActive(bool active)
    {
        SlowMoveActive = active;
        if (active)
        {
            _moveSpeedMultiplier = Mathf.Clamp(ExhaustionMoveMul, 0.01f, 1f);
        }
        else
        {
            _moveSpeedMultiplier = 1f;
        }
    }

    void SetSlowAttackActive(bool active)
    {
        SlowAttackActive = active;
        if (active)
        {
            _attackTimeScale = Mathf.Max(1f, ExhaustionAttackTimeScale);
        }
        else
        {
            _attackTimeScale = 1f;
        }
    }

    void SetWeakAttackActive(bool active)
    {
        WeakAttackActive = active;
        if (active)
        {
            _attackDamageMultiplier = Mathf.Clamp(ExhaustionDamageMul, 0.01f, 1f);
        }
        else
        {
            _attackDamageMultiplier = 1f;
        }
    }

    void SetLowJumpActive(bool active)
    {
        LowJumpActive = active;
        if (active)
        {
            _jumpHeightMultiplier = Mathf.Clamp(ExhaustionJumpMul, 0.01f, 1f);
        }
        else
        {
            _jumpHeightMultiplier = 1f;
        }
    }
}
