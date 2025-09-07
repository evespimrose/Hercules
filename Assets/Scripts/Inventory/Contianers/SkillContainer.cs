using UnityEngine;
public class SkillContainer : Container<Skill>
{
    [SerializeField] int initialCapacity = 4;
    protected override void Awake()
    {
        capacity = initialCapacity;
        base.Awake();
    }

    // skill usage API
    public bool Use(int slotIndex, GameObject user)
    {
        var item = GetItem(slotIndex) as Skill;
        if (item == null) return false;
        item.Execute(user);
        return true;
    }
}
