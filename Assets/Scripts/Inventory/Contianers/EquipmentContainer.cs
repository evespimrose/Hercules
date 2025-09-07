using UnityEngine;
public class EquipmentContainer : Container<Equipment>
{
    [SerializeField] int initialCapacity = 6;
    protected override void Awake()
    {
        capacity = initialCapacity;
        base.Awake();
    }
}