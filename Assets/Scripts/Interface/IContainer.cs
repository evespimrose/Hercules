using System;

public interface IContainer
{
    int Capacity { get; }
    event Action<IContainer> OnChanged; // UI listens to update

    Item GetItem(int index);
    bool SetItem(int index, Item item); // set only if item type accepted (or null to clear)
    bool CanAccept(Item item); // quick check

    // 이동/스왑 시 통일된 메서드 (fromIndex in this, toIndex in target)
    bool TryMoveOrSwap(int fromIndex, IContainer target, int toIndex);
}
