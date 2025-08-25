using System.Collections.Generic;
using UnityEngine;

public class MonsterBT : MonoBehaviour
{
    public Transform target;          // 추적/공격 대상
    public Transform selfTransform;   // 몬스터 자신의 Transform
    public AIBlackboard bb;           // AI 상태 공유 블랙보드
    
    [Header("BT Settings")]
    [SerializeField] private MonsterController monsterController;  // MonsterController 참조
    
    private BTNode root;

    void Start()
    {
        // selfTransform이 설정되지 않았다면 자동으로 설정
        if (selfTransform == null)
            selfTransform = transform;
            
        // MonsterController 참조 가져오기
        if (monsterController == null)
            monsterController = GetComponent<MonsterController>();
            
        // 블랙보드 초기화 - 일반 클래스 인스턴스 생성
        if (bb == null)
        {
            bb = new AIBlackboard();
        }

        // Debug.Log($"[{name}] MonsterBT 초기화 완료 - 블랙보드 생성됨");

        // 이동과 공격을 병렬로 실행하도록 구조 변경
        // 회피와 추적도 병렬로 실행하여 더 자연스러운 행동 구현
        var moveEvadeNode = new MoveEvadeAction(selfTransform, bb, 4f);
        var moveChaseNode = new MoveChaseAction(selfTransform, bb, 3f);
        // 공격 서브트리
        var attackNode = new AttackAction(selfTransform, bb, 2.2f, 1f);  // 1.5f에서 2.2f로 증가

        // 루트 트리: 이동(회피+추적)과 공격을 병렬로 실행
        //root = new Parallel(new List<BTNode> { moveEvadeNode, moveChaseNode, attackNode });
        root = new Parallel(new List<BTNode> { moveChaseNode, attackNode });


        // Debug.Log($"[{name}] BT 구조 생성 완료 - 회피/추적/공격 병렬 실행");
    }

    void Update()
    {
        if (selfTransform != null)
        {
            selfTransform.position = transform.position;
            selfTransform.rotation = transform.rotation;
        }
        
        if (root != null)
        {
            root.Tick();
        }
        
        // BT 파라미터 업데이트
        UpdateBTParameters();
        
        UpdateAIState();
    }
    
    // MonsterController의 변수들을 BT에 실시간으로 반영
    void UpdateBTParameters()
    {
        if (monsterController == null) return;
        
        // 현재 BT 파라미터들을 MonsterController에서 가져와서 로그로 표시
        float currentAttackRange = monsterController.attackRange;
        float currentSafeDistance = monsterController.safeDistance;
        float currentDetectionRange = monsterController.detectionRange;
        float currentAttackCooldown = monsterController.attackCooldown;
        
        // 파라미터 변경 시 로그 출력 (너무 자주 출력되지 않도록 제한)
        //if (Time.frameCount % 300 == 0) // 300프레임마다 한 번씩
        //{
        //    Debug.Log($"[{name}] BT 파라미터 업데이트 - 공격범위: {currentAttackRange:F2}, 안전거리: {currentSafeDistance:F2}, 감지범위: {currentDetectionRange:F2}, 쿨다운: {currentAttackCooldown:F2}");
        //}
    }
    
    void UpdateAIState()
    {
        if (bb == null || target == null) return;
        
        float distanceToTarget = Vector2.Distance(selfTransform.position, target.position);
        
        // MonsterController의 변수들을 실시간으로 참조하여 BT 판단에 반영
        float currentAttackRange = monsterController != null ? monsterController.attackRange : 2.2f;
        float currentSafeDistance = monsterController != null ? monsterController.safeDistance : 4f;
        float currentDetectionRange = monsterController != null ? monsterController.detectionRange : 8f;
        
        // 공격 가능 여부 (거리와 쿨다운 체크는 MonsterController에서 처리)
        // 공격 범위를 MonsterController에서 실시간으로 가져와서 사용
        bool previousAttackState = bb.attack;
        bb.attack = distanceToTarget <= currentAttackRange;
        
        // 공격 상태 변경 시 로그
        //if (previousAttackState != bb.attack)
        //{
        //    if (bb.attack)
        //        Debug.Log($"[{name}] BT 공격 판단: 공격 범위 내 진입 (거리: {distanceToTarget:F2}, 범위: {currentAttackRange:F2})");
        //    else
        //        Debug.Log($"[{name}] BT 공격 판단: 공격 범위 밖 (거리: {distanceToTarget:F2}, 범위: {currentAttackRange:F2})");
        //}
        
        // 이동 상태는 BT의 액션에서 결정되므로 그대로 유지
        // BT의 액션이 실행되면 해당 상태가 true로 설정됨
    }
}
