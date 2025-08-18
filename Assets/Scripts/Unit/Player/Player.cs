using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Unit
{
    private PlayerController playerController;

    protected override void Awake()
    {
        base.Awake();
        playerController = GetComponent<PlayerController>();
    }

    public override void Damage(float amount, Unit source)
    {
        base.Damage(amount, source);
    }

    public override void Heal(float amount, Unit source)
    {
        base.Heal(amount, source);
    }

    public override void Die()
    {
        if (IsDead) return;

        // 플레이어 전용: 입력/능력 컴포넌트 비활성화 (기본 Die 처리 전에)
        var pc = GetComponent<PlayerController>(); if (pc) pc.enabled = false;
        var move = GetComponent<MoveAbilityMB>(); if (move) move.enabled = false;
        var jump = GetComponent<JumpAbilityMB>(); if (jump) jump.enabled = false;
        var dash = GetComponent<DashAbilityMB>(); if (dash) dash.enabled = false;
        var atk = GetComponent<AttackAbilityMB>(); if (atk) atk.enabled = false;

        base.Die();
    }
}