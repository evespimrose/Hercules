using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Range(0.1f, 10f)] public float moveSpeed = 3f;
    [Range(0.1f, 10f)] public float chaseSpeed = 4f;
    [Range(0.1f, 10f)] public float evadeSpeed = 4f;

    [Header("Combat Settings")]
    [Range(0.1f, 20f)] public float attackRange = 2.2f;   // 빨간 원(센서)의 반경
    [Range(0.05f, 5f)] public float attackCooldown = 1f;
    [Range(0.1f, 20f)] public float stopChaseRange = 2.1f;

    [Header("AI Settings")]
    [Range(1f, 30f)] public float detectionRange = 8f;
    [Range(0.5f, 10f)] public float safeDistance = 4f;
    [Range(0.1f, 30f)] public float chaseStartRange = 3.0f;

    [Header("Gizmo Settings")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color chaseRangeColor = Color.blue;
    [SerializeField] private Color attackRangeColor = Color.red;
    [SerializeField] private Color stopChaseRangeColor = Color.cyan;
    [SerializeField] private Color evadeRangeColor = Color.yellow;
    [SerializeField] private Color detectionRangeColor = Color.green;

    // === 추가: 센서 참조 ===
    [Header("Sensor")]
    public AttackRangeSensor attackSensor;     // Monster/AttackRange 에 붙임

    private Monster monster;
    private MonsterBT monsterBT;
    private Rigidbody2D rb;
    private Transform target;
    private float lastAttackTime;

    private bool isChasing = false;
    private bool isEvading = false;
    private bool isWandering = false;
    private bool canAttack = false;

    void Start()
    {
        monster = GetComponent<Monster>();
        monsterBT = GetComponent<MonsterBT>();

        if (monster != null) rb = monster.Rigidbody;
        if (rb == null) { Debug.LogError($"{name}에 Rigidbody2D가 없습니다!"); return; }

        if (!attackSensor) attackSensor = GetComponentInChildren<AttackRangeSensor>(true);

        // 플레이어 찾기
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            target = player.transform;
            if (monsterBT != null)
            {
                monsterBT.target = target;
                if (monsterBT.bb != null) monsterBT.bb.target = target;
            }
        }

        lastAttackTime = -attackCooldown;
    }

    void Update()
    {
        if (target == null) return;

        // 센서가 있다면 반경 자동 동기화
        if (attackSensor && attackSensor.syncRadiusWithController)
            attackSensor.SetRadius(attackRange);

        ReadBTState();
        ExecuteActions();
    }

    // 외부(BTAction)에서 사용: 센서 기반 범위 체크
    public bool IsTargetInAttackRange(Transform t)
    {
        if (!t) return false;
        if (attackSensor) return attackSensor.InRangeOf(t);
        return Vector2.Distance(t.position, transform.position) <= attackRange;
    }

    void ReadBTState()
    {
        if (monsterBT == null || monsterBT.bb == null) return;
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
        if (isWandering != bb.moveWander)
        {
            // 다른 상태 -> Wander 전환 시 현재 위치를 앵커로 지정
            if (!isWandering && bb.moveWander)
            {
                bb.wanderAnchor = transform.position;
            }
            isWandering = bb.moveWander;
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
        if (isChasing) MoveTowardsTarget(chaseSpeed);
        else if (isEvading) MoveAwayFromTarget(evadeSpeed);
        else StopMovement();

        // 센서가 “범위 안”이라고 판단해야만 공격
        bool inRange = IsTargetInAttackRange(target);

        if (canAttack && inRange)
        {
            float elapsed = Time.time - lastAttackTime;
            if (elapsed >= attackCooldown) Attack();
        }
        else if (isEvading)
        {
            MoveAwayFromTarget(evadeSpeed);
        }
        else if (isWandering)
        {
            // WanderAction이 목적지 계산 및 호출, 여기서는 중력만 on 유지
            SetGravityEnabled(true);
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
        Vector2 dir = (target.position - transform.position).normalized;
        rb.velocity = dir * speed;
    }

    void MoveAwayFromTarget(float speed)
    {
        Vector2 dir = (transform.position - target.position).normalized;
        rb.velocity = dir * speed;
    }

    void StopMovement()
    {
        rb.velocity = Vector2.zero;
    }

    // Wander/물리 기반 이동을 위한 유틸
    public void SetGravityEnabled(bool enabled)
    {
        if (rb != null)
        {
            rb.gravityScale = enabled ? 1f : 0f;
        }
    }

    public void MoveTowardsPoint(Vector2 point, float speed)
    {
        Vector2 direction = ((Vector2)point - (Vector2)transform.position).normalized;
        rb.velocity = direction * speed;
    }
    
    void Attack()
    {
        if (target == null) return;

        lastAttackTime = Time.time;

        var targetUnit = target.GetComponent<Unit>();
        if (targetUnit != null)
        {
            var ctrl = GetComponent<MonsterHitboxAttackController>();
            if (ctrl && !ctrl.IsBusyOrCooling)
                ctrl.TryAttackOnce(); // 히트박스가 Arm/Disarm 됨
        }
    }


    // --- (기즈모) ---
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        Vector3 c = transform.position;

        Gizmos.color = detectionRangeColor; Gizmos.DrawWireSphere(c, detectionRange);
        Gizmos.color = attackRangeColor; Gizmos.DrawWireSphere(c, attackRange);
        Gizmos.color = stopChaseRangeColor; Gizmos.DrawWireSphere(c, stopChaseRange);
        Gizmos.color = evadeRangeColor; Gizmos.DrawWireSphere(c, safeDistance);
        Gizmos.color = chaseRangeColor; Gizmos.DrawWireSphere(c, chaseStartRange);
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        Vector3 c = transform.position;

        Gizmos.color = new Color(detectionRangeColor.r, detectionRangeColor.g, detectionRangeColor.b, 0.1f); Gizmos.DrawSphere(c, detectionRange);
        Gizmos.color = new Color(attackRangeColor.r, attackRangeColor.g, attackRangeColor.b, 0.1f); Gizmos.DrawSphere(c, attackRange);
        Gizmos.color = new Color(stopChaseRangeColor.r, stopChaseRangeColor.g, stopChaseRangeColor.b, 0.1f); Gizmos.DrawSphere(c, stopChaseRange);
        Gizmos.color = new Color(evadeRangeColor.r, evadeRangeColor.g, evadeRangeColor.b, 0.1f); Gizmos.DrawSphere(c, safeDistance);
        Gizmos.color = new Color(chaseRangeColor.r, chaseRangeColor.g, chaseRangeColor.b, 0.1f); Gizmos.DrawSphere(c, chaseStartRange);
    }
}
