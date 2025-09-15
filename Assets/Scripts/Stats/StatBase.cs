using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hercules.StatsSystem
{
    /// <summary>
    /// Player/Monster�� �����ϴ� ���� ��� ����������
    /// �Ļ� Ŭ����(PlayerStats/MonsterStats)�� ���� ����/�Ļ� ����� �߰��� �� ����
    /// </summary>
    public class StatsBase : MonoBehaviour
    {
        // ===== ���ҽ�(���簪�� ���� �ڿ���) =====
        [Header("Resources")]
        [SerializeField] private float currentHealth = 100f;                            // ���� ü��

        // ===== �⺻ ���� =====
        [Header("Primary")]
        public StatValue MaxHealth = new StatValue { Base = 100f };                     // �ִ� ü��
        public StatValue MoveSpeed = new StatValue { Base = 1f };                       // �̼�
        public StatValue AttackSpeed = new StatValue { Base = 1f };                     // ����
        public StatValue MaxJumpHeight = new StatValue { Base = 1f };                   // ��������

        [Header("Combat (used by future CombatMath)")]
        public StatValue DamageMultiplier = new StatValue { Base = 1f };                // ���ϴ� ���� ���� �¼�
        public StatValue IncomingDamageMultiplier = new StatValue { Base = 1f };        // �޴� ���� ���� �¼�(>1=�� ����, <1=����)
        public StatValue CritChance = new StatValue { Base = 0f };                      // ġ��Ÿ Ȯ�� // 0~1
        public StatValue CritDamageMultiplier = new StatValue { Base = 1.5f };          // ġ��Ÿ ������ // 1.5 = 150%

        [Header("Combat (Flat)")]
        public StatValue Defense = new StatValue { Base = 0f };                         // ���� // �տ��� ����(���� ���������� -Defense)

        // ===== ���(����) =====
        protected readonly List<IStatsModule> modules = new List<IStatsModule>();

        // ===== ���ҽ� ������Ƽ =====
        public event Action<float, float> OnHealthChanged;

        public float CurrentHealth
        {
            get => currentHealth;
            set
            {
                float old = currentHealth;
                currentHealth = Mathf.Clamp(value, 0f, MaxHealth.Value);
                if (!Mathf.Approximately(old, currentHealth))
                    OnHealthChanged?.Invoke(old, currentHealth);
            }
        }

        protected virtual void Awake()
        {
            // �ʿ�� OnChanged ���� �� �Ļ� ���� ��
            HookOnChanged(
                MoveSpeed, AttackSpeed, MaxJumpHeight,
                MaxHealth,
                DamageMultiplier, IncomingDamageMultiplier,
                CritChance, CritDamageMultiplier,
                Defense
            );
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
            // MaxHealth ���� �� ���� ü�� Ŭ����
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, MaxHealth.Value);
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
