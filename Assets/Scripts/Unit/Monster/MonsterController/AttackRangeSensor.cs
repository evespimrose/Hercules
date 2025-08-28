using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))] // ← 전용 RB2D 강제
public class AttackRangeSensor : MonoBehaviour
{
    [Header("Filter")]
    public LayerMask validLayers;          // Player 레이어만 권장
    public string targetTag = "Player";    // 태그 검사
    public bool ignoreTriggerColliders = true;

    [Header("Sync")]
    public bool syncRadiusWithController = true; // MonsterController.attackRange 동기화
    public bool compensateScale = true;          // 부모 스케일 보정

    [Header("Timing")]
    [Tooltip("OnTriggerStay가 이 시간 내에 갱신되면 InRange 유지")]
    public float stayTimeout = 0.25f;

    private CircleCollider2D circle;
    private Rigidbody2D rb2d;

    private Transform currentTarget;
    private float lastStayTime;

    public Transform CurrentTarget => currentTarget;
    public bool InRange => currentTarget != null && (Time.time - lastStayTime) < stayTimeout;

    void Awake()
    {
        circle = GetComponent<CircleCollider2D>();
        circle.isTrigger = true;

        // 전용 RB2D를 Kinematic + NeverSleep으로 구성
        rb2d = GetComponent<Rigidbody2D>();
        rb2d.bodyType = RigidbodyType2D.Kinematic;
        rb2d.simulated = true;
        rb2d.gravityScale = 0f;
        rb2d.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        rb2d.sleepMode = RigidbodySleepMode2D.NeverSleep; // ← 핵심
        rb2d.interpolation = RigidbodyInterpolation2D.None;
    }

    void Update()
    {
        if (syncRadiusWithController)
        {
            var ctrl = GetComponentInParent<MonsterController>();
            if (ctrl) SetRadius(ctrl.attackRange);
        }
    }

    public void SetRadius(float r)
    {
        if (!circle) return;
        if (compensateScale)
        {
            float sx = Mathf.Abs(transform.lossyScale.x);
            float sy = Mathf.Abs(transform.lossyScale.y);
            float s = Mathf.Max(0.0001f, Mathf.Sqrt(sx * sy));
            circle.radius = r / s;
        }
        else
        {
            circle.radius = r;
        }
    }

    public bool InRangeOf(Transform t)
    {
        return InRange && currentTarget == t;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!PassFilter(other)) return;
        var root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
        currentTarget = root; // 후보 등록
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!PassFilter(other)) return;
        var root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
        if (root == currentTarget) lastStayTime = Time.time; // 머무는 동안 갱신
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!currentTarget) return;
        var root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
        if (root == currentTarget) currentTarget = null;
    }

    bool PassFilter(Collider2D other)
    {
        // 레이어
        if (((1 << other.gameObject.layer) & validLayers) == 0) return false;
        // 트리거(플레이어 히트박스) 무시
        if (ignoreTriggerColliders && other.isTrigger) return false;
        // 태그
        if (!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag)) return false;
        return true;
    }
}
