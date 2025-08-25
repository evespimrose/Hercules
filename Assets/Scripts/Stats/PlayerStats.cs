using UnityEngine;

namespace Hercules.StatsSystem
{
    /// <summary>�÷��̾� ���� ����/�Ļ� ���.</summary>
    public class PlayerStats : StatsBase
    {
        [Header("Player Only")]
        public StatValue IFrameMultiplier = new StatValue { Base = 1f };

        protected override void Awake()
        {
            base.Awake();
            HookOnChanged(IFrameMultiplier);

            // �÷��̾ �⺻ ũ�� 25%
            CritChance.Base = 0.25f;  // 25%
        }

        public override void RecomputeDerived()
        {
            // �÷��̾� ���� �Ļ� ����� �ʿ��ϸ� ����
            // ex) ���/���� �������� IFrameDuration = Base * IFrameMultiplier ��
        }
    }
}
