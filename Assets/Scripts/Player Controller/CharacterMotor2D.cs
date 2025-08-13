using UnityEngine;
using System.Collections;
using System.Diagnostics;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterMotor2D : MonoBehaviour
{
    [Header("Ground Check")]
    public LayerMask groundMask;
    public Transform groundProbe;    // 발밑 체크 포인트(플레이어 '자식'이어야 함)
    public float groundProbeRadius = 0.12f;

    [Header("One-Way Platform")]
    public LayerMask oneWayMask;     // 원웨이 발판 레이어

    public bool IsGrounded { get; private set; }
    public bool FacingRight { get; private set; } = true;

    public Vector2 Velocity => _rb.velocity;
    public float Gravity => Physics2D.gravity.y * _rb.gravityScale;

    Rigidbody2D _rb;
    Collider2D[] _myCols;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _myCols = GetComponentsInChildren<Collider2D>();
        if (groundProbe == null)
            UnityEngine.Debug.LogWarning("[CharacterMotor2D] GroundProbe가 비어 있습니다. (Player의 자식 Transform를 지정하세요)");
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

    // 원웨이 발판 드롭다운
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

        // 즉시 하강 시작
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
