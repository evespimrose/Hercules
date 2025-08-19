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

    protected override Dictionary<Buff, IBuffEffect> BuffEffects { get; } =
        new Dictionary<Buff, IBuffEffect>()
        {
            { Buff.Knockback,  new KnockbackEffect()  },
            { Buff.Stun,       new StunEffect()       },
            { Buff.Invincible, new InvincibleEffect() },
            { Buff.Hitstop,    new HitstopEffect()    },
            { Buff.Indomitable,new IndomitableEffect() } // 불굴
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

    public void ResetIndomitable()
    {
        if (indomitableRoutine != null) { StopCoroutine(indomitableRoutine); indomitableRoutine = null; }
        indomitableActive = false;
        indomitableConsumed = false;
    }

    public override void Die()
    {
        if (IsDead) return;
        foreach (var c in EnumerateControlBehaviours())
            if (c) c.enabled = false;
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
        var mv = GetComponent<MoveAbilityMB>(); if (mv) yield return mv;
        var jp = GetComponent<JumpAbilityMB>(); if (jp) yield return jp;
        var ds = GetComponent<DashAbilityMB>(); if (ds) yield return ds;
        var atk = GetComponent<AttackAbilityMB>(); if (atk) yield return atk;
    }
}
