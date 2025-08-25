using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MoveChaseAction : BTNode
{
    private Transform self;
    private AIBlackboard bb;
    private float speed;
    private MonsterController monsterController;  // MonsterController 참조 추가

    public MoveChaseAction(Transform self, AIBlackboard bb, float speed)
    {
        this.self = self;
        this.bb = bb;
        this.speed = speed;
        
        // MonsterController 참조 가져오기
        if (self != null)
        {
            this.monsterController = self.GetComponent<MonsterController>();
        }
    }
    
    // MonsterController의 현재 파라미터들을 가져오는 메소드
    private (float detectionRange, float minChaseDistance, float stopChaseRange) GetCurrentParameters()
    {
        float currentDetectionRange = monsterController != null ? monsterController.detectionRange : 8f;
        float minChaseDistance = 0.8f;  // 추적 최소 거리는 고정값
        float currentStopChaseRange = monsterController != null ? monsterController.stopChaseRange : 2.1f;
        return (currentDetectionRange, minChaseDistance, currentStopChaseRange);
    }

    public override State Tick()
    {
        if (bb.target == null) return State.Failure;
        
        // 추적 상태를 블랙보드에 설정 (실제 이동은 MonsterController가 처리)
        bool previousChase = bb.moveChase;
        bool previousEvade = bb.moveEvade;
        
        // 거리 체크로 추적 가능 여부 판단
        float distanceToTarget = Vector2.Distance(self.position, bb.target.position);
        
        // MonsterController의 변수들을 실시간으로 참조
        var (currentDetectionRange, minChaseDistance, currentStopChaseRange) = GetCurrentParameters();
        
        // [수정] 추적 중지 범위를 적용하여 비비는 현상 방지
        // 최소 거리(0.8f) 이상, 추적 중지 범위(stopChaseRange) 이하에서만 추적
        if (!bb.moveEvade && distanceToTarget > minChaseDistance && distanceToTarget < currentStopChaseRange)
        {
            bb.moveChase = true;
            
            // 상태 변경 시 로그
            if (!previousChase)
                Debug.Log($"[BT] MoveChaseAction: 추적 모드 활성화 (거리: {distanceToTarget:F2}, 범위: {minChaseDistance:F2}~{currentStopChaseRange:F2})");
            
            return State.Running;
        }
        else
        {
            // 추적 모드 비활성화
            if (previousChase)
            {
                if (bb.moveEvade)
                    Debug.Log($"[BT] MoveChaseAction: 회피 모드로 인한 추적 모드 비활성화");
                else if (distanceToTarget <= minChaseDistance)
                    Debug.Log($"[BT] MoveChaseAction: 타겟과 너무 가까움 - 추적 모드 비활성화 (거리: {distanceToTarget:F2})");
                else if (distanceToTarget >= currentStopChaseRange)
                    Debug.Log($"[BT] MoveChaseAction: 추적 중지 범위 도달 - 추적 모드 비활성화 (거리: {distanceToTarget:F2}, 중지범위: {currentStopChaseRange:F2})");
            }
            
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
    private MonsterController monsterController;  // MonsterController 참조 추가

    public MoveEvadeAction(Transform self, AIBlackboard bb, float speed)
    {
        this.self = self;
        this.bb = bb;
        this.speed = speed;
        
        // MonsterController 참조 가져오기
        if (self != null)
        {
            this.monsterController = self.GetComponent<MonsterController>();
        }
    }
    
    // MonsterController의 현재 파라미터들을 가져오는 메소드
    private float GetCurrentSafeDistance()
    {
        return monsterController != null ? monsterController.safeDistance : 4f;
    }

    public override State Tick()
    {
        if (bb.target == null) return State.Failure;
        
        // 회피 상태를 블랙보드에 설정 (실제 이동은 MonsterController가 처리)
        bool previousChase = bb.moveChase;
        bool previousEvade = bb.moveEvade;
        
        // 거리 체크로 회피 필요 여부 판단
        float distanceToTarget = Vector2.Distance(self.position, bb.target.position);
        
        // MonsterController의 변수들을 실시간으로 참조
        float currentSafeDistance = GetCurrentSafeDistance();
        
        if (distanceToTarget < currentSafeDistance) // 너무 가까이 있을 때만 회피
        {
            bb.moveEvade = true;
            bb.moveChase = false;
            
            // 상태 변경 시 로그
            //if (!previousEvade)
            //    Debug.Log($"[BT] MoveEvadeAction: 회피 모드 활성화 (거리: {distanceToTarget:F2}, 안전거리: {currentSafeDistance:F2})");
            //if (previousChase)
            //    Debug.Log($"[BT] MoveEvadeAction: 추적 모드에서 회피 모드로 전환");
            
            return State.Running;
        }
        else
        {
            // 안전 거리에 있으면 회피 모드 비활성화
            //if (previousEvade)
            //{
            //    Debug.Log($"[BT] MoveEvadeAction: 안전 거리 확보 - 회피 모드 비활성화 (거리: {distanceToTarget:F2}, 안전거리: {currentSafeDistance:F2})");
            //}
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
    private MonsterController monsterController;  // MonsterController 참조 추가

    // 생성자에 Transform 추가
    public AttackAction(Transform self, AIBlackboard bb, float range, float cooldown)
    {
        this.self = self;
        this.bb = bb;
        this.range = range;
        this.cooldown = cooldown;
        this.timer = 0f;
        
        // MonsterController 참조 가져오기
        if (self != null)
        {
            this.monsterController = self.GetComponent<MonsterController>();
        }
    }
    
    // MonsterController의 현재 파라미터들을 가져오는 메소드
    private (float attackRange, float attackCooldown) GetCurrentParameters()
    {
        float currentAttackRange = monsterController != null ? monsterController.attackRange : range;
        float currentCooldown = monsterController != null ? monsterController.attackCooldown : cooldown;
        return (currentAttackRange, currentCooldown);
    }

    public override State Tick()
    {
        timer -= Time.deltaTime;
        if (bb.target == null) return State.Failure;

        float dist = Vector3.Distance(bb.target.position, self.position);
        
        // MonsterController의 변수들을 실시간으로 참조
        var (currentAttackRange, currentCooldown) = GetCurrentParameters();
        
        // [수정] 공격 상태 제어를 더 명확하게 수정
        if (dist <= currentAttackRange && timer <= 0f)
        {
            // 공격 실행 조건 만족 시 공격 상태 활성화
            bool previousAttack = bb.attack;
            bb.attack = true;
            
            // 공격 실행 시 로그
            if (!previousAttack)
                // Debug.Log($"[BT] AttackAction: 공격 실행! (거리: {dist:F2}, 범위: {currentAttackRange:F2})");
            
            // Debug.Log("Monster attacks target!");
            timer = currentCooldown;
            return State.Success;
        }
        
        // [수정] 공격 범위 밖이거나 쿨다운 중일 때는 반드시 공격 상태 해제
        if (bb.attack)
        {
            if (dist > currentAttackRange)
            {
                Debug.Log($"[BT] AttackAction: 공격 범위 밖으로 이동 - 공격 상태 해제 (거리: {dist:F2}, 범위: {currentAttackRange:F2})");
            }
            else if (timer > 0f)
            {
                Debug.Log($"[BT] AttackAction: 쿨다운 중 - 공격 상태 해제 (남은 쿨다운: {timer:F2})");
            }
            bb.attack = false;
        }
        
        return State.Running;
    }
}

