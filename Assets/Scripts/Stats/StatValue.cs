using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hercules.StatsSystem
{
    public enum StatOp { Flat, AddPct, Mult }

    [Serializable]
    public struct StatModifier
    {
        public StatOp op;               // Flat / AddPct / Mult
        public float value;             // +10, +0.2(=+20%), x1.1(=+10%) 등
        public UnityEngine.Object source; // 버프/장비/스킬 등 출처(해제할 때 식별용)
    }

    /// <summary>
    /// 하나의 스탯을 Base→Flat→AddPct(합산)→Mult(곱연속) 순으로 계산.
    /// 값 변경시 OnChanged(old,new) 이벤트를 발행합니다.
    /// </summary>
    [Serializable]
    public class StatValue
    {
        [SerializeField] private float @base;
        [SerializeField] private List<StatModifier> mods = new List<StatModifier>();

        public float Base
        {
            get => @base;
            set
            {
                if (Mathf.Approximately(@base, value)) return;
                var old = Value;
                @base = value;
                OnChanged?.Invoke(old, Value);
            }
        }

        public event Action<float, float> OnChanged;

        public float Value
        {
            get
            {
                float v = @base;
                float flat = 0f, addPct = 0f, mult = 1f;

                for (int i = 0; i < mods.Count; i++)
                {
                    var m = mods[i];
                    switch (m.op)
                    {
                        case StatOp.Flat: flat += m.value; break;
                        case StatOp.AddPct: addPct += m.value; break; // 0.2 => +20%
                        case StatOp.Mult: mult *= m.value; break; // 1.1 => ×1.1
                    }
                }

                v += flat;
                v *= (1f + addPct);
                v *= mult;
                return v;
            }
        }

        public void AddModifier(StatModifier mod)
        {
            var old = Value;
            mods.Add(mod);
            OnChanged?.Invoke(old, Value);
        }

        public void RemoveModifiersBySource(UnityEngine.Object source)
        {
            if (source == null) return;
            var old = Value;
            mods.RemoveAll(m => m.source == source);
            OnChanged?.Invoke(old, Value);
        }

        public void ClearModifiers()
        {
            var old = Value;
            mods.Clear();
            OnChanged?.Invoke(old, Value);
        }
    }
}
