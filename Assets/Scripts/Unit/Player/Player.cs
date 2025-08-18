using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Unit
{
    private PlayerController playerController;
    Coroutine hitstopRoutine;

    protected override void Awake()
    {
        base.Awake();
        playerController = GetComponent<PlayerController>();
    }

    public override void Die()
    {
        if (IsDead) return;

        // 플레이어 전용: 입력/능력 컴포넌트 비활성화
        foreach (var c in EnumerateControlBehaviours())
            if (c) c.enabled = false;

        base.Die();
    }

    // 플레이어 전용 히트스톱
    protected override void ApplyHitstop(float time)
    {
        time = Mathf.Max(0f, time);
        if (time <= 0f) return;
        if (hitstopRoutine != null) StopCoroutine(hitstopRoutine);
        hitstopRoutine = StartCoroutine(HitstopCoroutine(time));
    }

    IEnumerator HitstopCoroutine(float time)
    {
        float originalScale = Time.timeScale;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(time);
        Time.timeScale = originalScale;
        hitstopRoutine = null;
    }

    // === Player 전용 Stun 처리 (컨트롤 차단 + 복원) ===
    protected override IEnumerator StunCoroutine(float time)
    {
        isStunned = true;
        var rb = GetComponent<Rigidbody2D>();

        // 제어 관련 컴포넌트 수집 및 현재 상태 저장
        var controls = new List<Behaviour>(EnumerateControlBehaviours());
        var prev = new Dictionary<Behaviour, bool>(controls.Count);
        foreach (var c in controls) if (c) prev[c] = c.enabled;

        // 비활성화
        foreach (var c in controls) if (c) c.enabled = false;

        if (zeroHorizontalDuringStun && rb) rb.velocity = new Vector2(0f, rb.velocity.y);

        yield return new WaitForSeconds(time);

        if (!IsDead)
        {
            foreach (var kv in prev) if (kv.Key) kv.Key.enabled = kv.Value;
        }

        isStunned = false;
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
