using UnityEngine;

namespace Hercules.StatsSystem
{
    /// <summary>몬스터 전용 스탯/파생 계산.</summary>
    public class MonsterStats : StatsBase
    {
        [Header("Monster Only")]
        public StatValue AggroRange = new StatValue { Base = 6f };
        public StatValue EnrageMultiplier = new StatValue { Base = 1f };

        protected override void Awake()
        {
            base.Awake();
            HookOnChanged(AggroRange, EnrageMultiplier);

            CritChance.Base = 1f;  // 25%
        }

        public override void RecomputeDerived()
        {
            // 몬스터 전용 파생 계산이 필요하면 여기
        }
    }
}
