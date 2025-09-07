using UnityEngine;

[CreateAssetMenu(menuName = "Items/Equipment")]
public class Equipment : Item
{
    // equipment-specific fields (slot type, stat modifiers...)
    public string equipmentSlot; // e.g. "Weapon", "Armor"
}
