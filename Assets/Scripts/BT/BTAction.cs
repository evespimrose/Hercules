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

[System.Serializable]
public class WanderAction : BTNode
{
    private Transform self;
    private AIBlackboard bb;
    private MonsterController monsterController;

    private float minStep = 0.05f;
    private bool wasWandering = false;
    private enum Phase { Outbound, Return }
    private Phase phase = Phase.Outbound;

    public WanderAction(Transform self, AIBlackboard bb)
    {
        this.self = self;
        this.bb = bb;
        if (self != null) monsterController = self.GetComponent<MonsterController>();
    }

    private void EnsureAnchor(bool firstEnter)
    {
        // 다른 상태에서 wander로 전환 시 현재 위치를 앵커로
        if (firstEnter)
            bb.wanderAnchor = self.position;
        // MaxRange가 0이면 기본값
        if (bb.wanderMaxRange <= 0f)
            bb.wanderMaxRange = 1.5f;
    }

    private Vector2 PickNextDestination()
    {
        float radius = Random.Range(minStep, bb.wanderMaxRange);
        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        return bb.wanderAnchor + offset;
    }

    public override State Tick()
    {
        if (self == null || bb == null) return State.Failure;

        bool firstEnter = !wasWandering;
        EnsureAnchor(firstEnter);

        // Wander 활성화 표시
        bb.moveWander = true;
        wasWandering = true;

        // 중력 on
        if (monsterController != null)
            monsterController.SetGravityEnabled(true);

        // 목적지 도달 로직과 왕복
        float sqrDistToDest = (bb.wanderDestination - (Vector2)self.position).sqrMagnitude;
        bool noDest = bb.wanderDestination == default(Vector2);
        bool arrived = sqrDistToDest < 0.01f;
        if (noDest || arrived)
        {
            if (phase == Phase.Outbound)
            {
                if (noDest || firstEnter)
                {
                    bb.wanderDestination = PickNextDestination();
                }
                else
                {
                    // 목적지에 도달했으면 앵커로 복귀
                    bb.wanderDestination = bb.wanderAnchor;
                    phase = Phase.Return;
                }
            }
            else // Return -> 다음 Outbound 목적지 생성
            {
                bb.wanderDestination = PickNextDestination();
                phase = Phase.Outbound;
            }
        }

        // 목적지로 이동(속도는 moveSpeed 사용)
        if (monsterController != null)
        {
            monsterController.MoveTowardsPoint(bb.wanderDestination, monsterController.moveSpeed);
        }

        return State.Running;
    }
}

