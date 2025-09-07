using System;
using System.Collections.Generic;
using UnityEngine;

public class Container<T> : MonoBehaviour, IContainer where T : Item
{
    [SerializeField] protected int capacity = 20;
    public int Capacity => capacity;

    // internal storage (allow null)
    protected List<T> items;

    public event Action<IContainer> OnChanged;

    protected virtual void Awake()
    {
        items = new List<T>(capacity);
        for (int i = 0; i < capacity; i++) items.Add(null);
    }

    // IContainer impl
    public virtual Item GetItem(int index) => items[index];
    public virtual bool SetItem(int index, Item item)
    {
        if (item == null)
        {
            items[index] = null;
            OnChanged?.Invoke(this);
            return true;
        }

        if (!(item is T typed)) return false; // type-safety: reject
        items[index] = typed;
        OnChanged?.Invoke(this);
        return true;
    }

    public virtual bool CanAccept(Item item) => item == null || item is T;

    // TryMoveOrSwap: move from this[fromIndex] to target[toIndex]. If occupied -> swap.
    public virtual bool TryMoveOrSwap(int fromIndex, IContainer target, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= Capacity) return false;
        if (toIndex < 0 || toIndex >= target.Capacity) return false;

        Item sourceItem = GetItem(fromIndex);
        Item targetItem = target.GetItem(toIndex);

        // check acceptance
        if (!target.CanAccept(sourceItem)) return false;
        if (!CanAccept(targetItem)) return false;

        // perform swap
        bool setA = target.SetItem(toIndex, sourceItem);
        bool setB = SetItem(fromIndex, targetItem);

        if (setA && setB)
        {
            OnChanged?.Invoke(this);
            //target.OnChanged?.Invoke(target);
            return true;
        }

        // rollback if necessary (rare): try restore previous state
        target.SetItem(toIndex, targetItem);
        SetItem(fromIndex, sourceItem);
        return false;
    }

    // helper to find first empty slot
    public int FindFirstEmpty()
    {
        for (int i = 0; i < items.Count; i++) if (items[i] == null) return i;
        return -1;
    }

    // add item to first empty slot
    public bool AddItem(Item item)
    {
        int idx = FindFirstEmpty();
        if (idx < 0) return false;
        return SetItem(idx, item);
    }
}