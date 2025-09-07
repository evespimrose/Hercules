using UnityEngine;
public class InventoryContainer : Container<Item>
{
    [SerializeField] int initialCapacity = 30;
    protected override void Awake()
    {
        capacity = initialCapacity;
        base.Awake();
    }
}