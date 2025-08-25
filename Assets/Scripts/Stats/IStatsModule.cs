using UnityEngine;

namespace Hercules.StatsSystem
{
    /// <summary>
    /// �ʿ� �� StatsBase�� ������ �� �ִ� ��� Ȯ�� ����Ʈ
    /// </summary>
    public interface IStatsModule
    {
        void OnAttached(StatsBase stats);
        void OnDetached(StatsBase stats);
        void OnStatChanged(StatsBase stats);
        void Recompute(StatsBase stats);
    }
}
