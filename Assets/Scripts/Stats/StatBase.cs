using System.Collections.Generic;
using UnityEngine;

namespace Hercules.StatsSystem
{
    /// <summary>
    ///Player/Monster가 공유하는 값과 계산 파이프라인
    /// 파생 클래스(PlayerStats/MonsterStats)는 전용 스탯/파생 계산을 추가할 수 있음
    /// </summary>
    public class StatsBase : MonoBehaviour
    {
        // ===== 리소스(현재값을 갖는 자원형) =====
        [Header("Resources")]
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private float currentStamina = 100f;

        // ===== 기본 스탯 =====
        [Header("Primary")]
        public StatValue MaxHealth = new StatValue { Base = 100f };
        public StatValue MoveSpeed = new StatValue { Base = 1f }; // 배수/스케일
        public StatValue AttackSpeed = new StatValue { Base = 1f }; // 배수/스케일
        public StatValue MaxJumpHeight = new StatValue { Base = 1f };

        [Header("Stamina")]
        public StatValue StaminaMax = new StatValue { Base = 100f };
        public StatValue StaminaRegenPerSec = new StatValue { Base = 0f };

        [Header("Combat (used by future CombatMath)")]
        public StatValue DamageMultiplier = new StatValue { Base = 1f }; // 가하는 최종 피해 승수
        public StatValue IncomingDamageMultiplier = new StatValue { Base = 1f }; // 받는 최종 피해 승수(>1=더 아픔, <1=저항)
        public StatValue CritChance = new StatValue { Base = 0f }; // 0~1
        public StatValue CritDamageMultiplier = new StatValue { Base = 1.5f }; // 1.5 = 150%

        // ===== 모듈(선택) =====
        protected readonly List<IStatsModule> modules = new List<IStatsModule>();

        // ===== 리소스 프로퍼티 =====
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
            // 필요시 OnChanged 구독 → 파생 재계산 훅
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

        // 공통 파생 계산(필요 시 override)
        public virtual void RecomputeDerived()
        {
            // 기본 구현은 없음. 파생에서 추가 가능.
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
            // TODO: 드랍률 공식 확정 시 구현
            return 0f;
        }
    }
}
