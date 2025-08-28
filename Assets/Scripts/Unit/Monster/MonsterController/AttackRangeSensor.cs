using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))] // �� ���� RB2D ����
public class AttackRangeSensor : MonoBehaviour
{
    [Header("Filter")]
    public LayerMask validLayers;          // Player ���̾ ����
    public string targetTag = "Player";    // �±� �˻�
    public bool ignoreTriggerColliders = true;

    [Header("Sync")]
    public bool syncRadiusWithController = true; // MonsterController.attackRange ����ȭ
    public bool compensateScale = true;          // �θ� ������ ����

    [Header("Timing")]
    [Tooltip("OnTriggerStay�� �� �ð� ���� ���ŵǸ� InRange ����")]
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

        // ���� RB2D�� Kinematic + NeverSleep���� ����
        rb2d = GetComponent<Rigidbody2D>();
        rb2d.bodyType = RigidbodyType2D.Kinematic;
        rb2d.simulated = true;
        rb2d.gravityScale = 0f;
        rb2d.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        rb2d.sleepMode = RigidbodySleepMode2D.NeverSleep; // �� �ٽ�
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
        currentTarget = root; // �ĺ� ���
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!PassFilter(other)) return;
        var root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
        if (root == currentTarget) lastStayTime = Time.time; // �ӹ��� ���� ����
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!currentTarget) return;
        var root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
        if (root == currentTarget) currentTarget = null;
    }

    bool PassFilter(Collider2D other)
    {
        // ���̾�
        if (((1 << other.gameObject.layer) & validLayers) == 0) return false;
        // Ʈ����(�÷��̾� ��Ʈ�ڽ�) ����
        if (ignoreTriggerColliders && other.isTrigger) return false;
        // �±�
        if (!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag)) return false;
        return true;
    }
}
