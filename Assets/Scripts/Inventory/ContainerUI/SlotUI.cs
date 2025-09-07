using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SlotUI : MonoBehaviour, IPointerClickHandler
{
    public Image iconImage;
    public Button button; // optional
    private IContainer container;
    private int index;

    public void Initialize(IContainer container, int index)
    {
        this.container = container;
        this.index = index;
        Refresh();
    }

    public void Refresh()
    {
        Item item = container.GetItem(index);
        if (item != null)
        {
            iconImage.sprite = item.icon;
            iconImage.enabled = true;
        }
        else
        {
            iconImage.sprite = null;
            iconImage.enabled = false; // show empty
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 중앙 UI 매니저 호출
        //ContainerUI.OnSlotClicked(container, index, this); 
    }
}