using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MoveChaseAction : BTNode
{
    private Transform self;
    private AIBlackboard bb;
    private float speed;
    private MonsterController monsterController;  // MonsterController 참조

    public MoveChaseAction(Transform self, AIBlackboard bb, float speed)
    {
        this.self = self;
        this.bb = bb;
        this.speed = speed;

        if (self != null)
            this.monsterController = self.GetComponent<MonsterController>();
    }

    private (float chaseStartRange, float minChaseDistance, float stopChaseRange) GetCurrentParameters()
    {
        float currentChaseStartRange = monsterController != null ? monsterController.chaseStartRange : 3.0f;
        float minChaseDistance = 0.8f;
        float currentStopChaseRange = monsterController != null ? monsterController.stopChaseRange : 2.1f;
        return (currentChaseStartRange, minChaseDistance, currentStopChaseRange);
    }

    public override State Tick()
    {
        if (bb.target == null) return State.Failure;

        float distanceToTarget = Vector2.Distance(self.position, bb.target.position);
        var (currentChaseStartRange, minChaseDistance, currentStopChaseRange) = GetCurrentParameters();

        bool isWithinChaseWindow = distanceToTarget > currentStopChaseRange && distanceToTarget <= currentChaseStartRange;

        if (!bb.moveEvade && isWithinChaseWindow && distanceToTarget > minChaseDistance)
        {
            bb.moveChase = true;
            return State.Running;
        }
        else
        {
            bb.moveChase = false;
            return State.Success;
        }
    }
}

[System.Serializable]
public class MoveEvadeAction : BTNode
{
    private Transform self;
    private AIBlackboard bb;
    private float speed;
    private MonsterController monsterController;

    public MoveEvadeAction(Transform self, AIBlackboard bb, float speed)
    {
        this.self = self;
        this.bb = bb;
        this.speed = speed;

        if (self != null)
            this.monsterController = self.GetComponent<MonsterController>();
    }

    private float GetCurrentSafeDistance()
    {
        return monsterController != null ? monsterController.safeDistance : 4f;
    }

    public override State Tick()
    {
        if (bb.target == null) return State.Failure;

        float distanceToTarget = Vector2.Distance(self.position, bb.target.position);
        float currentSafeDistance = GetCurrentSafeDistance();

        if (distanceToTarget < currentSafeDistance) // 너무 가까우면 회피
        {
            bb.moveEvade = true;
            bb.moveChase = false;
            return State.Running;
        }
        else
        {
            bb.moveEvade = false;
            return State.Success;
        }
    }
}

[System.Serializable]
public class AttackAction : BTNode
{
    private Transform self;
    private AIBlackboard bb;
    private float range;
    private float cooldown;
    private float timer;

    private MonsterController monsterController;
    private AttackRangeSensor sensor;   // 센서 직접 참조(컨트롤러 메서드 의존 X)

    public AttackAction(Transform self, AIBlackboard bb, float range, float cooldown)
    {
        this.self = self;
        this.bb = bb;
        this.range = range;
        this.cooldown = cooldown;
        this.timer = 0f;

        if (self != null)
        {
            this.monsterController = self.GetComponent<MonsterController>();
            this.sensor = self.GetComponentInChildren<AttackRangeSensor>(true);
        }
    }

    public override State Tick()
    {
        timer -= Time.deltaTime;
        if (bb.target == null) return State.Failure;

        // 센서가 있으면 센서 우선, 없으면 거리 폴백
        bool inRange = (sensor != null)
            ? sensor.InRangeOf(bb.target)
            : Vector2.Distance(self.position, bb.target.position) <= range;

        if (inRange && timer <= 0f)
        {
            bb.attack = true; // 컨트롤러가 Attack() 실행
            float cd = (monsterController ? monsterController.attackCooldown : cooldown);
            timer = cd;
            return State.Success;
        }

        bb.attack = false;
        return State.Running;
    }
}
