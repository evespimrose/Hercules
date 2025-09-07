using System;

public interface IContainer
{
    int Capacity { get; }
    event Action<IContainer> OnChanged;

    Item GetItem(int index);
    bool SetItem(int index, Item item);
    bool CanAccept(Item item);

    // 통합 이동/스왑 메서드
    bool TryMoveOrSwap(int fromIndex, IContainer target, int toIndex);

    // 안전하게 다른 컨테이너의 변경 알림을 요청할 수 있는 메서드
    void NotifyChanged();

    // 원자 연산(트랜잭션) API: 호출자는 성공 시 반드시 EndAtomicOperation 호출해야 한다
    bool BeginAtomicOperation();
    void EndAtomicOperation();
}
