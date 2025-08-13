//using UnityEngine;
//using System;

//public interface IHitReceiver
//{
//    void ReceiveHit(float damage, Vector2 knockback, Vector2 hitPoint);
//}

//public class Hitbox : MonoBehaviour
//{
//    public float damage = 10f;
//    public Vector2 knockback = new Vector2(6f, 2f);

//    // ↓ 디버그/콜백용
//    public string attackerName = "Player";
//    public bool debugLog = false;
//    public Action<Collider2D, float> OnHit;  // (맞은 콜라이더, 데미지)

//    void OnTriggerEnter2D(Collider2D other)
//    {
//        var recv = other.GetComponent<IHitReceiver>()
//            ?? other.GetComponentInParent<IHitReceiver>()
//            ?? other.GetComponentInChildren<IHitReceiver>();

//        if (recv != null)
//        {
//            recv.ReceiveHit(damage, knockback, transform.position);

//            if (debugLog)
//                Debug.Log($"[HIT] {attackerName} -> {other.name} dmg={damage} @ {transform.position}");

//            OnHit?.Invoke(other, damage);
//        }
//        else
//        {
//            if (debugLog)
//            {
//                string layer = LayerMask.LayerToName(other.gameObject.layer);
//                Debug.LogWarning($"[HIT but no IHitReceiver] {attackerName} -> {other.name} (layer={layer})");
//            }
//        }
//    }

//}


using UnityEngine;
using System;
using System.Diagnostics;

public interface IHitReceiver
{
    void ReceiveHit(float damage, Vector2 knockback, Vector2 hitPoint);
}

public class Hitbox : MonoBehaviour
{
    public float damage = 10f;
    public Vector2 knockback = new Vector2(6f, 2f);

    // 디버그/콜백
    public string attackerName = "Player";
    public bool debugLog = false;
    public Action<Collider2D, float> OnHit;  // (맞은 콜라이더, 데미지)

    void OnTriggerEnter2D(Collider2D other)
    {
        var recv = other.GetComponent<IHitReceiver>()
               ?? other.GetComponentInParent<IHitReceiver>()
               ?? other.GetComponentInChildren<IHitReceiver>();

        if (recv != null)
        {
            recv.ReceiveHit(damage, knockback, transform.position);

            if (debugLog)
                UnityEngine.Debug.Log($"[HIT] {attackerName} -> {other.name} dmg={damage} @ {transform.position}");

            OnHit?.Invoke(other, damage);
        }
        else
        {
            if (debugLog)
            {
                string layer = LayerMask.LayerToName(other.gameObject.layer);
                UnityEngine.Debug.LogWarning($"[HIT but no IHitReceiver] {attackerName} -> {other.name} (layer={layer})");
            }
        }
    }
}
