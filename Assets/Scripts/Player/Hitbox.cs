using UnityEngine;

public interface IHitReceiver
{
    void ReceiveHit(float damage, Vector2 knockback, Vector2 hitPoint);
}

public class Hitbox : MonoBehaviour
{
    public float damage = 10f;
    public Vector2 knockback = new Vector2(6f, 2f);

    void OnTriggerEnter2D(Collider2D other)
    {
        var recv = other.GetComponent<IHitReceiver>();
        if (recv != null)
        {
            recv.ReceiveHit(damage, knockback, transform.position);
        }
    }
}

/*
 * 예시 적 컴포넌트:
 * public class DummyEnemy : MonoBehaviour, IHitReceiver
 * {
 *     public void ReceiveHit(float dmg, Vector2 kb, Vector2 p)
 *     {
 *         Debug.Log($"Hit {name} dmg={dmg}");
 *         var rb = GetComponent<Rigidbody2D>();
 *         if (rb) rb.AddForce(kb, ForceMode2D.Impulse);
 *     }
 * }
 */
