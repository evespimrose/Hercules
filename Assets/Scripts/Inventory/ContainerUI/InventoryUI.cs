using UnityEngine;

public class InventoryUI : ContainerUI
{
    private InventoryContainer inventory;

    protected override void Start()
    {
        base.Start();
        inventory = linkedContainer as InventoryContainer;
    }

    // 인벤토리는 단순히 아이템 저장/이동만 담당
    // 추가적인 로직이 필요하다면 여기에 작성 가능
}
