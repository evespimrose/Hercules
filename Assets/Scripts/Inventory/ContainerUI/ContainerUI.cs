using UnityEngine;
using System.Collections.Generic;

public abstract class ContainerUI : MonoBehaviour
{
    public IContainer linkedContainer;
    public GameObject slotPrefab;
    public Transform slotParent; // content with LayoutGroup
    protected List<SlotUI> slotPool = new List<SlotUI>();

    protected virtual void Start()
    {
        if (linkedContainer != null) Bind(linkedContainer);
    }

    public virtual void Bind(IContainer container)
    {
        if (linkedContainer != null) linkedContainer.OnChanged -= OnContainerChanged;
        linkedContainer = container;
        linkedContainer.OnChanged += OnContainerChanged;
        SetupSlots();
        RefreshAll();
    }

    protected virtual void SetupSlots()
    {
        // pool slots up to capacity
        int cap = linkedContainer.Capacity;
        while (slotPool.Count < cap)
        {
            var go = Instantiate(slotPrefab, slotParent);
            var slotUi = go.GetComponent<SlotUI>();
            slotPool.Add(slotUi);
        }
        // if pool bigger than capacity, disable extras
        for (int i = 0; i < slotPool.Count; i++)
        {
            slotPool[i].gameObject.SetActive(i < cap);
            if (i < cap) slotPool[i].Initialize(linkedContainer, i);
        }
    }

    protected virtual void OnContainerChanged(IContainer c)
    {
        RefreshAll();
    }

    protected virtual void RefreshAll()
    {
        int cap = linkedContainer.Capacity;
        for (int i = 0; i < cap; i++)
            slotPool[i].Refresh();
    }

    // Handle slot click: default implementation (click-to-pick then click-to-place swap)
    // You can centralize single selected slot state in a UIManager for multi-panel flow.
    protected IContainer pendingContainer;
    protected int pendingIndex = -1;

    public virtual void OnSlotClicked(IContainer container, int index, SlotUI slotUI)
    {
        if (pendingIndex == -1)
        {
            // pick up
            pendingContainer = container;
            pendingIndex = index;
            // visually mark selection in slotUI
        }
        else
        {
            // attempt move/swap
            if (pendingContainer.TryMoveOrSwap(pendingIndex, container, index))
            {
                // success
            }
            // clear pending
            pendingContainer = null;
            pendingIndex = -1;
        }
    }
}