using System.Collections.Generic;
using UnityEngine;

namespace Hercules.StatsSystem
{
    /// <summary>
    ///Player/Monster�� �����ϴ� ���� ��� ����������
    /// �Ļ� Ŭ����(PlayerStats/MonsterStats)�� ���� ����/�Ļ� ����� �߰��� �� ����
    /// </summary>
    public class StatsBase : MonoBehaviour
    {
        // ===== ���ҽ�(���簪�� ���� �ڿ���) =====
        [Header("Resources")]
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private float currentStamina = 100f;

        // ===== �⺻ ���� =====
        [Header("Primary")]
        public StatValue MaxHealth = new StatValue { Base = 100f };
        public StatValue MoveSpeed = new StatValue { Base = 1f }; // ���/������
        public StatValue AttackSpeed = new StatValue { Base = 1f }; // ���/������
        public StatValue MaxJumpHeight = new StatValue { Base = 1f };

        [Header("Stamina")]
        public StatValue StaminaMax = new StatValue { Base = 100f };
        public StatValue StaminaRegenPerSec = new StatValue { Base = 0f };

        [Header("Combat (used by future CombatMath)")]
        public StatValue DamageMultiplier = new StatValue { Base = 1f }; // ���ϴ� ���� ���� �¼�
        public StatValue IncomingDamageMultiplier = new StatValue { Base = 1f }; // �޴� ���� ���� �¼�(>1=�� ����, <1=����)
        public StatValue CritChance = new StatValue { Base = 0f }; // 0~1
        public StatValue CritDamageMultiplier = new StatValue { Base = 1.5f }; // 1.5 = 150%

        // ===== ���(����) =====
        protected readonly List<IStatsModule> modules = new List<IStatsModule>();

        // ===== ���ҽ� ������Ƽ =====
        public float CurrentHealth
        {
            get => currentHealth;
            set => currentHealth = Mathf.Clamp(value, 0f, MaxHealth.Value);
        }

        public float CurrentStamina
        {
            get => currentStamina;
            set => currentStamina = Mathf.Clamp(value, 0f, StaminaMax.Value);
        }

        protected virtual void Awake()
        {
            // �ʿ�� OnChanged ���� �� �Ļ� ���� ��
            HookOnChanged(MoveSpeed, AttackSpeed, MaxJumpHeight,
                          MaxHealth, StaminaMax, StaminaRegenPerSec,
                          DamageMultiplier, IncomingDamageMultiplier,
                          CritChance, CritDamageMultiplier);
        }

        protected void HookOnChanged(params StatValue[] values)
        {
            foreach (var v in values)
                v.OnChanged += (_, __) => OnStatChanged();
        }

        protected virtual void OnEnable()
        {
            foreach (var m in modules) m.OnAttached(this);
        }

        protected virtual void OnDisable()
        {
            foreach (var m in modules) m.OnDetached(this);
        }

        protected virtual void OnDestroy()
        {
            foreach (var m in modules) m.OnDetached(this);
        }

        // ���� �Ļ� ���(�ʿ� �� override)
        public virtual void RecomputeDerived()
        {
            // �⺻ ������ ����. �Ļ����� �߰� ����.
        }

        protected virtual void OnStatChanged()
        {
            foreach (var m in modules) m.OnStatChanged(this);
            RecomputeDerived();
        }

        public void AddModule(IStatsModule module)
        {
            if (module == null || modules.Contains(module)) return;
            modules.Add(module);
            module.OnAttached(this);
            module.Recompute(this);
        }

        public void RemoveModule(IStatsModule module)
        {
            if (module == null) return;
            if (modules.Remove(module))
                module.OnDetached(this);
        }

        // ===== Drop Rate =====
        public virtual float GetDropRate()
        {
            // TODO: ����� ���� Ȯ�� �� ����
            return 0f;
        }
    }
}
