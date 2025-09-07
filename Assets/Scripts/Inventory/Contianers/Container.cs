using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Container<T> : MonoBehaviour, IContainer where T : Item
{
    [SerializeField] protected int capacity = 20;
    protected List<T> items;

    // 원자 연산 플래그 (reentrancy / concurrent operation 방지)
    protected bool inAtomicOperation = false;

    public int Capacity => capacity;
    public event Action<IContainer> OnChanged;

    protected virtual void Awake()
    {
        items = new List<T>(capacity);
        for (int i = 0; i < capacity; i++) items.Add(null);
    }

    // ---------- 기본 접근자 / 변경자 ----------
    public virtual Item GetItem(int index)
    {
        if (index < 0 || index >= capacity) return null;
        return items[index];
    }

    // SetItem은 inAtomicOperation 플래그가 켜져 있으면 OnChanged 호출을 유예한다
    public virtual bool SetItem(int index, Item item)
    {
        if (index < 0 || index >= capacity) return false;

        if (item == null)
        {
            items[index] = null;
            if (!inAtomicOperation) OnChanged?.Invoke(this);
            return true;
        }

        if (!(item is T typed)) return false;
        items[index] = typed;
        if (!inAtomicOperation) OnChanged?.Invoke(this);
        return true;
    }

    public virtual bool CanAccept(Item item)
    {
        return item == null || item is T;
    }

    // 안전하게 이벤트를 발행하도록 외부에서 호출할 수 있는 메서드
    public virtual void NotifyChanged()
    {
        OnChanged?.Invoke(this);
    }

    // ---------- 원자 연산 API ----------
    // 간단한 busy 체크 (Unity는 대부분 메인스레드라 lock 대신 bool 플래그로 충분)
    public virtual bool BeginAtomicOperation()
    {
        if (inAtomicOperation) return false;
        inAtomicOperation = true;
        return true;
    }

    public virtual void EndAtomicOperation()
    {
        inAtomicOperation = false;
    }

    // ---------- 핵심: TryMoveOrSwap ----------
    public virtual bool TryMoveOrSwap(int fromIndex, IContainer target, int toIndex)
    {
        // 기본 검증
        if (target == null) return false;
        if (fromIndex < 0 || fromIndex >= Capacity) return false;
        if (toIndex < 0 || toIndex >= target.Capacity) return false;

        // same-slot early-out
        if (ReferenceEquals(this, target) && fromIndex == toIndex) return false;

        bool sameContainer = ReferenceEquals(this, target);

        // Acquire atomic locks: 우리(this) 먼저, 다른 컨테이너는 그 다음
        if (!BeginAtomicOperation()) return false;
        bool targetLocked = false;
        if (!sameContainer)
        {
            if (!target.BeginAtomicOperation())
            {
                // 실패 시 해제하고 리턴
                EndAtomicOperation();
                return false;
            }
            targetLocked = true;
        }

        try
        {
            // 현재 상태 캡처
            Item sourceItem = GetItem(fromIndex);
            Item destItem = target.GetItem(toIndex);

            // 수용 가능성 검사 (타입/비즈니스 룰)
            if (!target.CanAccept(sourceItem) || !this.CanAccept(destItem))
            {
                return false;
            }

            // 수행: SetItem은 inAtomicOperation 동안 OnChanged를 발생시키지 않음
            bool okA = target.SetItem(toIndex, sourceItem);   // target <- source
            bool okB = this.SetItem(fromIndex, destItem);    // this <- dest

            if (!okA || !okB)
            {
                // 롤백 시도: 가능한 범위 내에서 원상 복구
                // (설계상 okA/okB 실패은 드물지만 안전하게 복구)
                target.SetItem(toIndex, destItem);
                this.SetItem(fromIndex, sourceItem);
                return false;
            }

            // 성공: 알림 (한 번씩)
            if (sameContainer)
            {
                // 같은 컨테이너이면 한 번만 알림
                NotifyChanged();
            }
            else
            {
                // 서로 다른 컨테이너: 각자 소유자가 알림을 발행하도록 요청
                NotifyChanged();
                target.NotifyChanged();
            }

            return true;
        }
        finally
        {
            // 반드시 락 해제 (target 먼저 해제하지 않아도 됨)
            if (targetLocked) target.EndAtomicOperation();
            EndAtomicOperation();
        }
    }

    // ---------- 유틸리티(예) ----------
    public int FindFirstEmpty()
    {
        for (int i = 0; i < items.Count; i++) if (items[i] == null) return i;
        return -1;
    }

    public bool AddItem(Item item)
    {
        int idx = FindFirstEmpty();
        if (idx < 0) return false;
        return SetItem(idx, item);
    }
}
