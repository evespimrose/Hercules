using UnityEngine;

public class DummyEnemy : MonoBehaviour, IHitReceiver
{
    public float hp = 30f;
    Rigidbody2D rb;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    public void ReceiveHit(float dmg, Vector2 knockback, Vector2 hitPoint)
    {
        hp -= dmg;
        Debug.Log($"[Enemy] {name} hit! dmg={dmg}, hp={hp}");

        if (rb) rb.AddForce(knockback, ForceMode2D.Impulse);

        if (hp <= 0f)
        {
            Debug.Log($"[Enemy] {name} dead");
            Destroy(gameObject);
        }
    }
}
