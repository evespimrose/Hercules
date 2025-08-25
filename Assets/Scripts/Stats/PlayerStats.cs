using UnityEngine;

namespace Hercules.StatsSystem
{
    /// <summary>플레이어 전용 스탯/파생 계산.</summary>
    public class PlayerStats : StatsBase
    {
        [Header("Player Only")]
        public StatValue IFrameMultiplier = new StatValue { Base = 1f };

        protected override void Awake()
        {
            base.Awake();
            HookOnChanged(IFrameMultiplier);

            // 플레이어만 기본 크리 25%
            CritChance.Base = 0.25f;  // 25%
        }

        public override void RecomputeDerived()
        {
            // 플레이어 전용 파생 계산이 필요하면 여기
            // ex) 장비/버프 조합으로 IFrameDuration = Base * IFrameMultiplier 등
        }
    }
}
