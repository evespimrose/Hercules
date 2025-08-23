////using UnityEngine;
////using System;

////public interface IHitReceiver
////{
////    void ReceiveHit(float damage, Vector2 knockback, Vector2 hitPoint);
////}

////public class Hitbox : MonoBehaviour
////{
////    public float damage = 10f;
////    public Vector2 knockback = new Vector2(6f, 2f);

////    // �� �����/�ݹ��
////    public string attackerName = "Player";
////    public bool debugLog = false;
////    public Action<Collider2D, float> OnHit;  // (���� �ݶ��̴�, ������)

////    void OnTriggerEnter2D(Collider2D other)
////    {
////        var recv = other.GetComponent<IHitReceiver>()
////            ?? other.GetComponentInParent<IHitReceiver>()
////            ?? other.GetComponentInChildren<IHitReceiver>();

////        if (recv != null)
////        {
////            recv.ReceiveHit(damage, knockback, transform.position);

////            if (debugLog)
////                Debug.Log($"[HIT] {attackerName} -> {other.name} dmg={damage} @ {transform.position}");

////            OnHit?.Invoke(other, damage);
////        }
////        else
////        {
////            if (debugLog)
////            {
////                string layer = LayerMask.LayerToName(other.gameObject.layer);
////                Debug.LogWarning($"[HIT but no IHitReceiver] {attackerName} -> {other.name} (layer={layer})");
////            }
////        }
////    }

////}


//using UnityEngine;
//using System;
//using System.Diagnostics;

//public interface IHitReceiver
//{
//    void ReceiveHit(float damage, Vector2 knockback, Vector2 hitPoint);
//}

//public class Hitbox : MonoBehaviour
//{
//    public float damage = 10f;
//    public Vector2 knockback = new Vector2(6f, 2f);

//    // �����/�ݹ�
//    public string attackerName = "Player";
//    public bool debugLog = false;
//    public Action<Collider2D, float> OnHit;  // (���� �ݶ��̴�, ������)

//    void OnTriggerEnter2D(Collider2D other)
//    {
//        var recv = other.GetComponent<IHitReceiver>()
//               ?? other.GetComponentInParent<IHitReceiver>()
//               ?? other.GetComponentInChildren<IHitReceiver>();

//        if (recv != null)
//        {
//            recv.ReceiveHit(damage, knockback, transform.position);

//            if (debugLog)
//                UnityEngine.Debug.Log($"[HIT] {attackerName} -> {other.name} dmg={damage} @ {transform.position}");

//            OnHit?.Invoke(other, damage);
//        }
//        else
//        {
//            if (debugLog)
//            {
//                string layer = LayerMask.LayerToName(other.gameObject.layer);
//                UnityEngine.Debug.LogWarning($"[HIT but no IHitReceiver] {attackerName} -> {other.name} (layer={layer})");
//            }
//        }
//    }
//}


using System;
using UnityEngine;
using Hercules.StatsSystem;

/// <summary>
/// ���� ����. �浹 �� Unit/Stats�� ã�� CombatMath�� ���� �������� ���ϰ�, ��󿡰� Damage�� ����.
/// </summary>
public class Hitbox : MonoBehaviour
{
    public float damage = 10f;
    public Vector2 knockback = new Vector2(6f, 2f);

    public string attackerName = "Attacker";
    public bool debugLog = false;

    public Unit ownerUnit;
    public StatsBase ownerStats;

    public event Action<Collider2D, float> OnHit;

    void Awake()
    {
        var col = gameObject.GetComponent<Collider2D>();
        if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var targetUnit =
               other.GetComponentInParent<Unit>()
            ?? other.GetComponent<Unit>()
            ?? other.GetComponentInChildren<Unit>();

        var targetStats =
               other.GetComponentInParent<StatsBase>()
            ?? other.GetComponent<StatsBase>()
            ?? other.GetComponentInChildren<StatsBase>();

        float finalDamage = (ownerStats != null && targetStats != null)
            ? CombatMath.ComputeDamage(ownerStats, targetStats, damage)
            : damage;

        if (targetUnit != null)
        {
            targetUnit.Damage(finalDamage, ownerUnit);

            // �˹�
            Vector2 dir = (other.transform.position - transform.position).normalized;
            targetUnit.ApplyKnockback(dir, knockback.x);

            if (debugLog)
                Debug.Log($"[HIT] {attackerName} -> {other.name} dmg={finalDamage}");

            OnHit?.Invoke(other, finalDamage);
        }
        else if (debugLog)
        {
            Debug.LogWarning($"[HIT no Unit] {attackerName} -> {other.name}");
        }
    }
}
