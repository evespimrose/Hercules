using UnityEngine;

[DisallowMultipleComponent]
public class DeathZone : MonoBehaviour
{
    public bool onlyPlayer = true;
    public string playerTag = "Player";
    public bool debugLog = true;

    void Reset()
    {
        var col = GetComponent<Collider2D>() ?? gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        gameObject.name = "DeathZone";
    }

    void OnTriggerEnter2D(Collider2D other) => TryKill(other);
    void OnTriggerStay2D(Collider2D other) => TryKill(other);

    void TryKill(Collider2D other)
    {
        var root = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
        if (onlyPlayer && !root.CompareTag(playerTag)) return;

        var player = root.GetComponentInParent<Player>();
        if (player != null)
        {
            if (player.IsIndomitableActive) { if (debugLog) Debug.Log("[DeathZone] ºÒ±¼ ÁøÇà Áß ¡æ ³«»ç º¸·ù"); return; }
            if (player.TryTriggerIndomitableOnFatal()) { if (debugLog) Debug.Log("[DeathZone] ºÒ±¼ ¹ßµ¿ ¡æ ³«»ç º¸·ù"); return; }
        }

        var unit = root.GetComponentInParent<Unit>();
        if (unit != null) { if (debugLog) Debug.Log($"[DeathZone] {root.name} -> Unit.Die()"); unit.Die(); }
    }

}
