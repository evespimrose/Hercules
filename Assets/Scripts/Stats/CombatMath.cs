using System;
using UnityEngine;

namespace Hercules.StatsSystem
{
    /// <summary>
    /// ���� ����(�� �� ���):
    /// Final = max(0, (baseDamage * atkMul * critFactor * incomingMul) - defense)
    ///   - atkMul = Attacker.DamageMultiplier
    ///   - critFactor = (rand01() < CritChance ? CritDamageMultiplier : 1)
    ///   - incomingMul = Defender.IncomingDamageMultiplier
    ///   - defense = Defender.Defense
    ///
    /// �������� ����Ʈ:
    ///  1) ��� �ܰ� ��ü: CombatMath.DefenseStage �� �ٲٸ� ���� �ݿ���.
    ///  2) ��ü �� �� ���� ��ü: EvalOneLine(...)�� �ٲٸ� ��(�ܺ� �ñ״�ó �Һ�).
    ///
    /// ����:
    ///  - AttackAbility ��� baseDamage�� ����� �̸� ���� �ѱ�� ���⼭ 2�� ����˴ϴ�.
    ///    �ݵ�� "���̽� ��"�� �ѱ⼼��.
    /// </summary>
    public static class CombatMath
    {
        // ��������������������������������������������������������������������������������������������������������������������������
        // �� ��� �ܰ� ��ü ������ ��������Ʈ (�⺻: �տ��� ���� �� ���� 0)
        //    �ʿ��ϸ� ��������/���̺긮�� ������ ���Ƴ��켼��.
        public static Func<float, float, float> DefenseStage = (damageAfterMultipliers, defense) =>
        {
            float def = Mathf.Max(0f, defense);
            return Mathf.Max(0f, damageAfterMultipliers - def);
        };

        // ��������������������������������������������������������������������������������������������������������������������������
        // �� �� �� ���(���� ����) ? �� �Լ� �ϳ��� �ٲٸ� ��ü ������ ��ü�˴ϴ�.
        private static float EvalOneLine(
            float baseDamage,
            float atkMul,
            float critChance,
            float critMul,
            float incomingMul,
            float defense,
            Func<float> rand01,
            out bool isCrit)
        {
            // ���� ����(���� ���۰� ������ ���)
            float bd = Mathf.Max(0f, baseDamage);
            float atk = Mathf.Max(0f, atkMul);
            float inc = Mathf.Max(0f, incomingMul);
            float cMul = Mathf.Max(1f, critMul);
            float cCh = Mathf.Clamp01(critChance);

            // ũ��Ƽ�� ����
            bool crit = false;
            float critFactor = 1f;
            if (cCh > 0f)
            {
                float r = rand01 != null ? rand01() : UnityEngine.Random.value;
                if (r < cCh) { crit = true; critFactor = cMul; }
            }
            isCrit = crit;

            // �� �� ����
            float afterMul = bd * atk * critFactor * inc;
            float final = DefenseStage(afterMul, defense);

            if (!float.IsFinite(final)) return 0f;
            return Mathf.Max(0f, final);
        }

        // ��������������������������������������������������������������������������������������������������������������������������
        // �ܺ� ���� API (�ñ״�ó ����)

        /// <summary>���� ���(�������) ���. (���� ���۰� ����)</summary>
        public static float ComputeDamage(StatsBase attacker, StatsBase defender, float baseDamage)
        {
            if (baseDamage <= 0f) return 0f;

            // ���� ���� ����
            float atkMul = 1f;
            float critCh = 0f;
            float critMul = 1.5f; // �⺻ ����(�ڵ�� ����)
            float incMul = 1f;
            float defense = 0f;

            if (attacker != null)
            {
                atkMul = SafeStat(attacker?.DamageMultiplier?.Value, 1f, 0f);
                critCh = Mathf.Clamp01(SafeStat(attacker?.CritChance?.Value, 0f, 0f));
                critMul = SafeStat(attacker?.CritDamageMultiplier?.Value, 1.5f, 1f);
            }
            if (defender != null)
            {
                incMul = SafeStat(defender?.IncomingDamageMultiplier?.Value, 1f, 0f);
                defense = SafeStat(defender?.Defense?.Value, 0f, 0f);
            }

            bool _;
            return EvalOneLine(baseDamage, atkMul, critCh, critMul, incMul, defense, () => UnityEngine.Random.value, out _);
        }

        /// <summary>������(�õ� ���) ���. ũ�� ���� ���� ����(���� ���۰� ����)</summary>
        public static float ComputeDamage(StatsBase attacker, StatsBase defender, float baseDamage, int rngSeed, out bool isCrit)
        {
            isCrit = false;
            if (baseDamage <= 0f) return 0f;

            System.Random rng = new System.Random(rngSeed);

            float atkMul = 1f;
            float critCh = 0f;
            float critMul = 1.5f;
            float incMul = 1f;
            float defense = 0f;

            if (attacker != null)
            {
                atkMul = SafeStat(attacker?.DamageMultiplier?.Value, 1f, 0f);
                critCh = Mathf.Clamp01(SafeStat(attacker?.CritChance?.Value, 0f, 0f));
                critMul = SafeStat(attacker?.CritDamageMultiplier?.Value, 1.5f, 1f);
            }
            if (defender != null)
            {
                incMul = SafeStat(defender?.IncomingDamageMultiplier?.Value, 1f, 0f);
                defense = SafeStat(defender?.Defense?.Value, 0f, 0f);
            }

            return EvalOneLine(baseDamage, atkMul, critCh, critMul, incMul, defense, () => (float)rng.NextDouble(), out isCrit);
        }

        /// <summary>���� ��� + ũ�� ���� out (���� ���۰� ����)</summary>
        public static float ComputeDamage(StatsBase attacker, StatsBase defender, float baseDamage, out bool isCrit)
        {
            isCrit = false;
            if (baseDamage <= 0f) return 0f;

            float atkMul = 1f;
            float critCh = 0f;
            float critMul = 1.5f;
            float incMul = 1f;
            float defense = 0f;

            if (attacker != null)
            {
                atkMul = Mathf.Max(0f, attacker.DamageMultiplier.Value);
                critCh = Mathf.Clamp01(attacker.CritChance.Value);
                critMul = Mathf.Max(1f, attacker.CritDamageMultiplier.Value);
            }
            if (defender != null)
            {
                incMul = Mathf.Max(0f, defender.IncomingDamageMultiplier.Value);
                defense = Mathf.Max(0f, defender.Defense.Value);
            }

            return EvalOneLine(baseDamage, atkMul, critCh, critMul, incMul, defense, () => UnityEngine.Random.value, out isCrit);
        }

        // ��������������������������������������������������������������������������������������������������������������������������
        // ���Ž� ȣȯ: ApplyDefenseReduction(���� ����, ���������� DefenseStage ���)
        public static float ApplyDefenseReduction(float damageAfterMultipliers, StatsBase defender)
        {
            float def = 0f;
            if (defender != null)
                def = SafeStat(defender?.Defense?.Value, 0f, 0f);
            return DefenseStage(damageAfterMultipliers, def);
        }

        // ���� ����
        private static float SafeStat(float? v, float fallback, float min)
        {
            if (!v.HasValue) return fallback;
            if (float.IsNaN(v.Value) || float.IsInfinity(v.Value)) return fallback;
            return Mathf.Max(min, v.Value);
        }
    }
}
