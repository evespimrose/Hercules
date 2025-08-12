using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveChaseAction : BTNode
{
    private Transform self;
    private AIBlackboard bb;
    private float speed;

    public MoveChaseAction(Transform self, AIBlackboard bb, float speed)
    {
        this.self = self;
        this.bb = bb;
        this.speed = speed;
    }

    public override State Tick()
    {
        if (!bb.moveChase || bb.target == null) return State.Failure;
        Vector3 dir = (bb.target.position - self.position).normalized;
        self.position += dir * speed * Time.deltaTime;
        return State.Running;
    }
}

public class MoveEvadeAction : BTNode
{
    private Transform self;
    private AIBlackboard bb;
    private float speed;

    public MoveEvadeAction(Transform self, AIBlackboard bb, float speed)
    {
        this.self = self;
        this.bb = bb;
        this.speed = speed;
    }

    public override State Tick()
    {
        if (!bb.moveEvade || bb.target == null) return State.Failure;
        Vector3 dir = (self.position - bb.target.position).normalized;
        self.position += dir * speed * Time.deltaTime;
        return State.Running;
    }
}

public class AttackAction : BTNode
{
    private Transform self; 
    private AIBlackboard bb;
    private float range;
    private float cooldown;
    private float timer;

    // 생성자에 Transform 추가
    public AttackAction(Transform self, AIBlackboard bb, float range, float cooldown)
    {
        this.self = self;
        this.bb = bb;
        this.range = range;
        this.cooldown = cooldown;
        this.timer = 0f;
    }

    public override State Tick()
    {
        timer -= Time.deltaTime;
        if (!bb.attack || bb.target == null) return State.Failure;

        float dist = Vector3.Distance(bb.target.position, self.position);
        if (dist <= range && timer <= 0f)
        {
            Debug.Log("Monster attacks target!");
            timer = cooldown;
            return State.Success;
        }
        return State.Running;
    }
}
