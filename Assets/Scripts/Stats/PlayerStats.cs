using UnityEngine;

namespace Hercules.StatsSystem
{
    /// <summary>�÷��̾� ���� ����/�Ļ� ���.</summary>
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
            // �÷��̾� ���� �Ļ� ����� �ʿ��ϸ� ����
            // ex) ���/���� �������� IFrameDuration = Base * IFrameMultiplier ��
        }
    }
}
