using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterMotor2D : MonoBehaviour
{
    [Header("Ground Check")]
    public LayerMask groundMask;
    public Transform groundProbe;    // �߹� üũ ����Ʈ
    public float groundProbeRadius = 0.1f;

    [Header("One-Way Platform")]
    public LayerMask oneWayMask;     // ������ ���� ���̾�

    public bool IsGrounded { get; private set; }
    public bool FacingRight { get; private set; } = true;

    public Vector2 Velocity => _rb.velocity;
    public float Gravity => Physics2D.gravity.y * _rb.gravityScale;

    Rigidbody2D _rb;
    Collider2D[] _myCols;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _myCols = GetComponents<Collider2D>();
    }

    void FixedUpdate()
    {
        if (groundProbe != null)
            IsGrounded = Physics2D.OverlapCircle(groundProbe.position, groundProbeRadius, groundMask);
    }

    public void SetHorizontalVelocity(float vx)
    {
        _rb.velocity = new Vector2(vx, _rb.velocity.y);
        if (Mathf.Abs(vx) > 0.001f) Face(vx > 0f);
    }

    public void AddVerticalVelocity(float vy)
    {
        _rb.velocity = new Vector2(_rb.velocity.x, vy);
    }

    public void SetVelocity(Vector2 v) => _rb.velocity = v;
    public void AddForce(Vector2 f, ForceMode2D mode = ForceMode2D.Impulse) => _rb.AddForce(f, mode);

    public void Face(bool right)
    {
        if (FacingRight == right) return;
        FacingRight = right;
        var s = transform.localScale; s.x *= -1f; transform.localScale = s;
    }

    // ������ ���� ��Ӵٿ�
    public void TryDropThrough(float duration = 0.25f)
    {
        if (!IsGrounded || groundProbe == null) return;

        var plats = Physics2D.OverlapCircleAll(groundProbe.position, groundProbeRadius, oneWayMask);
        if (plats == null || plats.Length == 0) return;

        StartCoroutine(DropThroughRoutine(plats, duration));
    }

    IEnumerator DropThroughRoutine(Collider2D[] platforms, float duration)
    {
        foreach (var p in platforms)
            foreach (var me in _myCols)
                if (me && p) Physics2D.IgnoreCollision(me, p, true);

        // ��� �ϰ� ����
        AddVerticalVelocity(Mathf.Min(-0.1f, _rb.velocity.y));

        yield return new WaitForSeconds(duration);

        foreach (var p in platforms)
            foreach (var me in _myCols)
                if (me && p) Physics2D.IgnoreCollision(me, p, false);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (groundProbe == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundProbe.position, groundProbeRadius);
    }
#endif
}
