using UnityEngine;

namespace Hercules.StatsSystem
{
    /// <summary>���� ���� ����/�Ļ� ���.</summary>
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
            // ���� ���� �Ļ� ����� �ʿ��ϸ� ����
        }
    }
}
