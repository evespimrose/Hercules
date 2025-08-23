using UnityEngine;

namespace Hercules.StatsSystem
{
    /// <summary>
    /// 필요 시 StatsBase에 부착할 수 있는 모듈 확장 포인트
    /// </summary>
    public interface IStatsModule
    {
        void OnAttached(StatsBase stats);
        void OnDetached(StatsBase stats);
        void OnStatChanged(StatsBase stats);
        void Recompute(StatsBase stats);
    }
}
