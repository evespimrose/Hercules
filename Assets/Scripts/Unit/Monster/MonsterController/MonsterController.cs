using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Range(0.1f, 10f)]
    public float moveSpeed = 3f;
    [Range(0.1f, 10f)]
    public float chaseSpeed = 4f;
    [Range(0.1f, 10f)]
    public float evadeSpeed = 4f;
    
    [Header("Combat Settings")]
    [Range(0.1f, 5f)]
    public float attackRange = 2.2f;  // 1.5f에서 2.2f로 증가
    [Range(0.1f, 5f)]
    public float attackCooldown = 1f;
    
    [Header("AI Settings")]
    [Range(1f, 20f)]
    public float detectionRange = 8f;
    [Range(0.5f, 10f)]
    public float safeDistance = 4f;
    
    [Header("Gizmo Settings")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color chaseRangeColor = Color.blue;
    [SerializeField] private Color attackRangeColor = Color.red;
    [SerializeField] private Color evadeRangeColor = Color.yellow;
    [SerializeField] private Color detectionRangeColor = Color.green;
    
    private Monster monster;
    private MonsterBT monsterBT;
    private Rigidbody2D rb;
    private Transform target;
    private float lastAttackTime;
    
    // AI 상태 (BT에서 받아올 상태들)
    private bool isChasing = false;
    private bool isEvading = false;
    private bool canAttack = false;
    
    void Start()
    {
        monster = GetComponent<Monster>();
        monsterBT = GetComponent<MonsterBT>();
        
        // Monster의 Rigidbody2D 사용
        if (monster != null)
        {
            rb = monster.Rigidbody;
        }
        
        if (rb == null)
        {
            Debug.LogError($"{name}에 Rigidbody2D가 없습니다!");
            return;
        }
        
        // MonsterBT가 없다면 경고
        if (monsterBT == null)
        {
            Debug.LogWarning($"{name}에 MonsterBT 컴포넌트가 없습니다!");
        }
        
        // 플레이어 찾기 (임시, 나중에 GameManager나 다른 방법으로 개선)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
            // MonsterBT의 타겟도 설정
            if (monsterBT != null)
            {
                monsterBT.target = target;
                monsterBT.bb.target = target;
            }
        }
        
        // 공격 쿨다운 초기화
        lastAttackTime = -attackCooldown; // 첫 공격을 즉시 할 수 있도록 설정
    }
    
    void Update()
    {
        if (target == null) return;
        
        // BT의 상태를 읽어와서 실제 행동으로 변환
        ReadBTState();
        ExecuteActions();
    }
    
    void ReadBTState()
    {
        if (monsterBT == null || monsterBT.bb == null) return;
        
        // BT의 블랙보드에서 상태 읽기
        var bb = monsterBT.bb;
        
        // 상태 변경 감지 및 로그 출력
        if (isChasing != bb.moveChase)
        {
            isChasing = bb.moveChase;
            //if (isChasing)
            //    Debug.Log($"[{name}] BT 상태 전환: 추적 모드 시작");
            //else
            //    Debug.Log($"[{name}] BT 상태 전환: 추적 모드 종료");
        }
        
        if (isEvading != bb.moveEvade)
        {
            isEvading = bb.moveEvade;
            //if (isEvading)
            //    Debug.Log($"[{name}] BT 상태 전환: 회피 모드 시작");
            //else
            //    Debug.Log($"[{name}] BT 상태 전환: 회피 모드 종료");
        }
        
        if (canAttack != bb.attack)
        {
            canAttack = bb.attack;
            //if (canAttack)
            //    Debug.Log($"[{name}] BT 상태 전환: 공격 가능 상태");
            //else
            //    Debug.Log($"[{name}] BT 상태 전환: 공격 불가 상태");
            }
        
        // 타겟이 변경되었는지 확인
        if (bb.target != target)
        {
            target = bb.target;
            //if (target != null)
            //    Debug.Log($"[{name}] BT 타겟 변경: {target.name}");
        }
    }
    
    void ExecuteActions()
    {
        // 이동 처리
        if (isChasing)
        {
            MoveTowardsTarget(chaseSpeed);
        }
        else if (isEvading)
        {
            MoveAwayFromTarget(evadeSpeed);
        }
        else
        {
            // 정지
            StopMovement();
        }
        
        // 공격 처리 - 디버그 로그 추가
        if (canAttack)
        {
            float timeSinceLastAttack = Time.time - lastAttackTime;
            if (timeSinceLastAttack >= attackCooldown)
            {
                //Debug.Log($"[{name}] 공격 조건 만족: canAttack={canAttack}, 쿨다운={timeSinceLastAttack:F2}/{attackCooldown}");
                Attack();
            }
            else
            {
                // 쿨다운 중일 때 로그 (너무 자주 출력되지 않도록)
                //if (Time.frameCount % 120 == 0) // 120프레임마다 한 번씩
                //{
                //    Debug.Log($"[{name}] 공격 대기 중: 쿨다운 {timeSinceLastAttack:F2}/{attackCooldown}");
                //}
            }
        }
        //else
        //{
        //    // 공격 불가 상태일 때 로그 (너무 자주 출력되지 않도록)
        //    if (Time.frameCount % 120 == 0) // 120프레임마다 한 번씩
        //    {
        //        Debug.Log($"[{name}] 공격 불가: canAttack={canAttack}");
        //    }
        //}
    }
    
    void MoveTowardsTarget(float speed)
    {
        Vector2 direction = (target.position - transform.position).normalized;
        rb.velocity = direction * speed;
        
        // 이동 로그 (너무 자주 출력되지 않도록 제한)
        //if (Time.frameCount % 60 == 0) // 60프레임마다 한 번씩
        //{
        //    Debug.Log($"[{name}] 실제 행동: 추적 이동 중 (속도: {speed}, 방향: {direction})");
        //}
    }
    
    void MoveAwayFromTarget(float speed)
    {
        Vector2 direction = (transform.position - target.position).normalized;
        rb.velocity = direction * speed;
        
        // 이동 로그 (너무 자주 출력되지 않도록 제한)
        //if (Time.frameCount % 60 == 0) // 60프레임마다 한 번씩
        //{
        //    Debug.Log($"[{name}] 실제 행동: 회피 이동 중 (속도: {speed}, 방향: {direction})");
        //}
    }
    
    void StopMovement()
    {
        //if (rb.velocity.sqrMagnitude > 0.01f) // 정지 상태가 아닐 때만 로그
        //{
        //    Debug.Log($"[{name}] 실제 행동: 이동 정지");
        //}
        rb.velocity = Vector2.zero;
    }
    
    void Attack()
    {
        if (target == null) return;
        
        // 공격 실행
        lastAttackTime = Time.time;
        
        //Debug.Log($"[{name}] ===== 공격 실행! =====");
        //Debug.Log($"[{name}] 공격자: {name} (HP: {monster?.currentHealth}/{monster?.maxHealth})");
        //Debug.Log($"[{name}] 공격 대상: {target.name}");
        //Debug.Log($"[{name}] 공격력: 10");
        //Debug.Log($"[{name}] 공격 위치: {transform.position}");
        //Debug.Log($"[{name}] 타겟 위치: {target.position}");
        //Debug.Log($"[{name}] 공격 거리: {Vector2.Distance(transform.position, target.position):F2}");
        
        // Monster의 DealDamage 메서드 호출
        if (monster != null)
        {
            Unit targetUnit = target.GetComponent<Unit>();
            if (targetUnit != null)
            {
                monster.DealDamage(targetUnit, 10f); // 기본 공격력 10
                //Debug.Log($"[{name}] 공격 결과: {target.name}에게 10 피해 적용 완료");
            }
            //else
            //{
            //    Debug.LogWarning($"[{name}] 공격 실패: {target.name}에 Unit 컴포넌트가 없음");
            //}
        }
        //else
        //{
        //    Debug.LogError($"[{name}] 공격 실패: Monster 컴포넌트가 없음");
        //}
        
        // Debug.Log($"[{name}] ===== 공격 완료 =====");
    }
    
    // 외부에서 호출할 수 있는 공개 메서드들
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (monsterBT != null && monsterBT.bb != null)
        {
            monsterBT.bb.target = newTarget;
        }
    }
    
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }
    
    // BT 상태를 외부에서 설정할 수 있는 메서드들
    public void SetBTState(bool chase, bool evade, bool attack)
    {
        if (monsterBT != null && monsterBT.bb != null)
        {
            monsterBT.bb.moveChase = chase;
            monsterBT.bb.moveEvade = evade;
            monsterBT.bb.attack = attack;
        }
    }
    
    public bool IsChasing => isChasing;
    public bool IsEvading => isEvading;
    public bool CanAttack => canAttack;
    public Transform CurrentTarget => target;
    
    // 기즈모 그리기
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Vector3 center = transform.position;
        
        // 추적 범위 (detectionRange)
        Gizmos.color = detectionRangeColor;
        Gizmos.DrawWireSphere(center, detectionRange);
        
        // 공격 범위
        Gizmos.color = attackRangeColor;
        Gizmos.DrawWireSphere(center, attackRange);
        
        // 회피 판단 범위 (safeDistance)
        Gizmos.color = evadeRangeColor;
        Gizmos.DrawWireSphere(center, safeDistance);
        
        // 추적 최소 거리 (0.8f)
        Gizmos.color = chaseRangeColor;
        Gizmos.DrawWireSphere(center, 0.8f);
        
        // 타겟이 있을 때 방향 표시
        if (target != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(center, target.position);
            
            // 현재 거리 표시
            float distance = Vector2.Distance(center, target.position);
            Vector3 labelPos = (center + target.position) * 0.5f;
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(labelPos, $"거리: {distance:F2}");
            #endif
        }
    }
    
    // 선택된 오브젝트일 때만 기즈모 표시 (Scene 뷰에서 더 잘 보임)
    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        
        Vector3 center = transform.position;
        
        // 선택된 오브젝트일 때는 반투명한 구체로 표시
        // 추적 범위
        Gizmos.color = new Color(detectionRangeColor.r, detectionRangeColor.g, detectionRangeColor.b, 0.1f);
        Gizmos.DrawSphere(center, detectionRange);
        
        // 공격 범위
        Gizmos.color = new Color(attackRangeColor.r, attackRangeColor.g, attackRangeColor.b, 0.1f);
        Gizmos.DrawSphere(center, attackRange);
        
        // 회피 판단 범위
        Gizmos.color = new Color(evadeRangeColor.r, evadeRangeColor.g, evadeRangeColor.b, 0.1f);
        Gizmos.DrawSphere(center, safeDistance);
        
        // 추적 최소 거리
        Gizmos.color = new Color(chaseRangeColor.r, chaseRangeColor.g, chaseRangeColor.b, 0.1f);
        Gizmos.DrawSphere(center, 0.8f);
    }
}
