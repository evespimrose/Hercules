using UnityEngine;

public abstract class Item : ScriptableObject
{
    public string id;         // unique id
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;

    // Optional categorization property (not required for logic)
    public virtual string Category => GetType().Name;

    // Optional runtime action hook (override in Skill/Consumable if needed)
    public virtual void Use(GameObject user) { }
}