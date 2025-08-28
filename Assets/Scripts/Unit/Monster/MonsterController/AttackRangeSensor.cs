using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CircleCollider2D))]
public class AttackRangeSensor : MonoBehaviour
{
    [Header("Filter")]
    public LayerMask validLayers;            // Player ���̾ �ѵѰ�
    public string targetTag = "Player";      // �±� üũ(������ ���õ�)
    public bool ignoreTriggerColliders = true;

    [Header("Sync")]
    public bool syncRadiusWithController = true;  // MonsterController.attackRange�� ����ȭ
    public bool compensateScale = true;           // �θ� ������ ����

    private CircleCollider2D circle;
    private Transform currentTarget;              // ������ Ÿ��(��Ʈ)
    private float lastStayTime;

    public Transform CurrentTarget => currentTarget;
    public bool InRange => currentTarget != null && (Time.time - lastStayTime) < 0.2f;

    void Awake()
    {
        circle = GetComponent<CircleCollider2D>();
        circle.isTrigger = true;

        // validLayers ��� ������ Player�� �⺻ ����
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
            float s = Mathf.Max(0.0001f, Mathf.Sqrt(sx * sy)); // �뷫���� ����
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
        // ���� ���������� Stay������ ��. Enter�� �ĺ��� ���
        if (!PassFilter(other)) return;
        var root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
        currentTarget = root;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!PassFilter(other)) return;
        var root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
        if (root == currentTarget) lastStayTime = Time.time;   // Stay�� ������ ���ȸ� InRange ����
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!currentTarget) return;
        var root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
        if (root == currentTarget) { currentTarget = null; }
    }

    bool PassFilter(Collider2D other)
    {
        // ���̾� ����
        if (((1 << other.gameObject.layer) & validLayers) == 0) return false;

        // Ʈ���� ��Ʈ�ڽ�(�÷��̾� ���� ��) ����
        if (ignoreTriggerColliders && other.isTrigger) return false;

        // �±� ����(������ ������)
        if (!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag)) return false;

        return true;
    }
}
