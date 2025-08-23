using UnityEngine;

namespace Hercules.StatsSystem
{
    /// <summary>플레이어 전용 스탯/파생 계산.</summary>
    public class PlayerStats : StatsBase
    {
        [Header("Player Only")]
        public StatValue DashStaminaCost = new StatValue { Base = 25f };
        public StatValue IFrameMultiplier = new StatValue { Base = 1f };

        protected override void Awake()
        {
            base.Awake();
            HookOnChanged(DashStaminaCost, IFrameMultiplier);
        }

        public override void RecomputeDerived()
        {
            // 플레이어 전용 파생 계산이 필요하면 여기
            // ex) 장비/버프 조합으로 IFrameDuration = Base * IFrameMultiplier 등
        }
    }
}
