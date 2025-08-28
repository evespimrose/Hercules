using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class AttackRangeSensor : MonoBehaviour
{
    [Header("Filter")]
    public LayerMask validLayers;            // Player 레이어만 켜둘것
    public string targetTag = "Player";      // 태그 체크(없으면 무시됨)
    public bool ignoreTriggerColliders = true;

    [Header("Sync")]
    public bool syncRadiusWithController = true;  // MonsterController.attackRange와 동기화
    public bool compensateScale = true;           // 부모 스케일 보정

    private CircleCollider2D circle;
    private Transform currentTarget;              // 감지된 타겟(루트)
    private float lastStayTime;

    public Transform CurrentTarget => currentTarget;
    public bool InRange => currentTarget != null && (Time.time - lastStayTime) < 0.2f;

    void Awake()
    {
        circle = GetComponent<CircleCollider2D>();
        circle.isTrigger = true;

        // validLayers 비어 있으면 Player만 기본 세팅
        if (validLayers.value == 0)
        {
            int player = LayerMask.NameToLayer("Player");
            if (player >= 0) validLayers = 1 << player;
        }
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
            float s = Mathf.Max(0.0001f, Mathf.Sqrt(sx * sy)); // 대략적인 보정
            circle.radius = r / s;
        }
        else
        {
            circle.radius = r;
        }
    }

    public bool InRangeOf(Transform t)
    {
        return InRange && currentTarget == (t ? (t.GetComponent<Rigidbody2D>() ? t.GetComponent<Rigidbody2D>().transform : t) : null);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 실제 “인정”은 Stay에서만 함. Enter는 후보만 기록
        if (!PassFilter(other)) return;
        var root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
        currentTarget = root;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!PassFilter(other)) return;
        var root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
        if (root == currentTarget) lastStayTime = Time.time;   // Stay가 들어오는 동안만 InRange 유지
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!currentTarget) return;
        var root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
        if (root == currentTarget) { currentTarget = null; }
    }

    bool PassFilter(Collider2D other)
    {
        // 레이어 필터
        if (((1 << other.gameObject.layer) & validLayers) == 0) return false;

        // 트리거 히트박스(플레이어 공격 등) 무시
        if (ignoreTriggerColliders && other.isTrigger) return false;

        // 태그 필터(설정돼 있으면)
        if (!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag)) return false;

        return true;
    }
}
