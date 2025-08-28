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

        if (isChasing != bb.moveChase) isChasing = bb.moveChase;
        if (isEvading != bb.moveEvade) isEvading = bb.moveEvade;
        if (canAttack != bb.attack) canAttack = bb.attack;

        if (bb.target != target) target = bb.target;
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
