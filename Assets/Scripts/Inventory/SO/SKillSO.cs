using UnityEngine;

[CreateAssetMenu(menuName = "Items/Skill")]
public class Skill : Item
{
    public float cooldown;
    public virtual void Execute(GameObject user)
    {
        // concrete skill logic or triggers events/ability system.
        Debug.Log($"Skill {itemName} executed by {user.name}");
    }

    public override void Use(GameObject user) => Execute(user);
}