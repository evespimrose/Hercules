using UnityEngine;

[CreateAssetMenu(menuName = "Items/Consumable")]
public class Consumable : Item
{
    public int Amount;
    public override void Use(GameObject user)
    {
        // example : heal
        Heal(user);
    }

    public void Heal(GameObject user)
    {
        var unit = user.GetComponent<Unit>();
        if (unit != null) unit.Heal(Amount, null);
    }
}