using UnityEngine;

public class EquipmentContainerUI : ContainerUI
{
    private EquipmentContainer equipment;

    protected override void Start()
    {
        base.Start();
        equipment = linkedContainer as EquipmentContainer;
    }

    // 장비 특화 로직 (예: 슬롯 별 아이템 제약)
    // 예시: 무기/방어구 슬롯 구분 표시
    protected override void RefreshAll()
    {
        base.RefreshAll();
        for (int i = 0; i < equipment.Capacity; i++)
        {
            var item = equipment.GetItem(i) as Equipment;
            if (item != null)
            {
                // 슬롯 UI에 장비 타입(무기, 방어구 등) 텍스트나 아이콘 표시 가능
                // slotPool[i].SetSlotType(item.equipmentSlot);
            }
            else
            {
                // 빈 슬롯이면 기본 슬롯 타입 안내 유지
                // slotPool[i].SetSlotType("Empty");
            }
        }
    }
}
