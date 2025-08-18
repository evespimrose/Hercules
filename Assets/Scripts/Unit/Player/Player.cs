using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Unit
{
    private PlayerController playerController;
    Coroutine hitstopRoutine;

    protected override Dictionary<Buff, IBuffEffect> BuffEffects { get; } =
        new Dictionary<Buff, IBuffEffect>()
        {
            { Buff.Knockback, new KnockbackEffect() },
            { Buff.Stun, new StunEffect() },
            { Buff.Invincible, new InvincibleEffect() },
            { Buff.Hitstop, new HitstopEffect() }
        };

    protected override void Awake()
    {
        base.Awake();
        playerController = GetComponent<PlayerController>();
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
        // 조작 차단
        var controls = new List<Behaviour>(EnumerateControlBehaviours());
        var prev = new Dictionary<Behaviour, bool>(controls.Count);
        foreach (var c in controls) if (c) prev[c] = c.enabled;
        foreach (var c in controls) if (c) c.enabled = false;

        yield return new WaitForSeconds(time);

        // 원래 상태 복구
        foreach (var kv in prev) if (kv.Key) kv.Key.enabled = kv.Value;
        stunRoutine = null;
    }

    // 이동/공격 제어를 담당하는 컴포넌트 나열 (Die, Stun에서 모두 사용)
    private IEnumerable<Behaviour> EnumerateControlBehaviours()
    {
        var pc = GetComponent<PlayerController>(); if (pc) yield return pc;
        var move = GetComponent<MoveAbilityMB>(); if (move) yield return move;
        var jump = GetComponent<JumpAbilityMB>(); if (jump) yield return jump;
        var dash = GetComponent<DashAbilityMB>(); if (dash) yield return dash;
        var atk = GetComponent<AttackAbilityMB>(); if (atk) yield return atk;
    }
}
