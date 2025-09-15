using UnityEngine;

namespace Hercules.StatsSystem
{
    /// <summary>
    /// �ӽ� ������ ����:
    ///   final = baseDamage
    ///           * (������ DamageMultiplier)
    ///           * (ũ��Ƽ���̸� CritDamageMultiplier)
    ///           * (�ǰ��� IncomingDamageMultiplier)
    ///           �� (���� �տ���) damage - defense (���� 0)
    ///
    /// ����:
    ///  - AttackAbility �ʿ��� �̹� ���� ����� �̸� ���� Hitbox�� �ѱ�� �ִٸ�
    ///    ���⼭ �� �������� "2�� ����" �˴ϴ�. => AttackAbility�� ���̽��� �ѱ⼼��.
    /// </summary>
    public static class CombatMath
    {
        /// <summary>
        /// ���� ���(�������) �ӽ� ���.
        /// </summary>
        public static float ComputeDamage(StatsBase attacker, StatsBase defender, float baseDamage)
        {
            if (baseDamage <= 0f) return 0f;

            float dmg = Mathf.Max(0f, baseDamage);

            // ���� ������ ��� ������������������������������������������������������������������������������������������
            float atkMul = 1f;
            float critCh = 0f;   // 0~1
            float critMul = 1f;   // 1.0 �̻�

            if (attacker != null)
            {
                // StatsBase�� StatValue�� .Value �� �б�
                // (������ 1�� ó��)
                try { atkMul = Mathf.Max(0f, attacker.DamageMultiplier.Value); } catch { atkMul = 1f; }
                try { critCh = Mathf.Clamp01(attacker.CritChance.Value); } catch { critCh = 0f; }
                try { critMul = Mathf.Max(1f, attacker.CritDamageMultiplier.Value); } catch { critMul = 1.5f; }
            }

            dmg *= atkMul;

            // ũ��Ƽ��
            if (critCh > 0f && Random.value < critCh)
                dmg *= critMul;

            // ���� �ǰ��� ��� ����������������������������������������������������������������������������������������
            if (defender != null)
            {
                try
                {
                    float incomingMul = Mathf.Max(0f, defender.IncomingDamageMultiplier.Value);
                    dmg *= incomingMul;
                }
                catch { /* ���� */ }
            }

            // ����(�տ���) ����
            dmg = ApplyDefenseReduction(dmg, defender);

            if (!float.IsFinite(dmg)) return 0f;
            return Mathf.Max(0f, dmg);
        }

        /// <summary>
        /// ������(�׽�Ʈ��) ����. �ܺ� �õ�� ũ�� ���� ���� ����.
        /// </summary>
        public static float ComputeDamage(StatsBase attacker, StatsBase defender, float baseDamage, int rngSeed, out bool isCrit)
        {
            isCrit = false;
            if (baseDamage <= 0f) return 0f;

            System.Random rng = new System.Random(rngSeed);
            float dmg = Mathf.Max(0f, baseDamage);

            float atkMul = 1f;
            float critCh = 0f;
            float critMul = 1f;

            if (attacker != null)
            {
                atkMul = SafeStat(attacker?.DamageMultiplier?.Value, 1f, 0f);
                critCh = Mathf.Clamp01(SafeStat(attacker?.CritChance?.Value, 0f, 0f));
                critMul = SafeStat(attacker?.CritDamageMultiplier?.Value, 1.5f, 1f);
            }

            dmg *= atkMul;

            if (critCh > 0f && rng.NextDouble() < critCh)
            {
                isCrit = true;
                dmg *= critMul;
            }

            if (defender != null)
            {
                float incomingMul = SafeStat(defender?.IncomingDamageMultiplier?.Value, 1f, 0f);
                dmg *= incomingMul;
            }

            // ����(�տ���) ����
            dmg = ApplyDefenseReduction(dmg, defender);

            if (!float.IsFinite(dmg)) return 0f;
            return Mathf.Max(0f, dmg);
        }

        // ������ �� �б� ����
        private static float SafeStat(float? v, float fallback, float min)
        {
            if (!v.HasValue) return fallback;
            if (float.IsNaN(v.Value) || float.IsInfinity(v.Value)) return fallback;
            return Mathf.Max(min, v.Value);
        }

        /// <summary>
        /// ���� ���� �ܰ� (Ȯ�� ����Ʈ)
        /// - ���� ����: ���� �ܰ迡�� damage - defense (���� 0)
        /// - �ʳ� ����/����Ʈĸ/���� ���� �� �Լ��� �����Ͽ� ���� �ݿ�
        /// </summary>
        public static float ApplyDefenseReduction(float damageAfterMultipliers, StatsBase defender)
        {
            if (defender == null) return Mathf.Max(0f, damageAfterMultipliers);
            float defense = 0f;
            try { defense = defender.Defense.Value; } catch { defense = 0f; }
            float reduced = damageAfterMultipliers - defense;
            return Mathf.Max(0f, reduced);
        }

        public static float ComputeDamage(StatsBase attacker, StatsBase defender, float baseDamage, out bool isCrit)
        {
            isCrit = false;
            if (baseDamage <= 0f) return 0f;

            float dmg = Mathf.Max(0f, baseDamage);

            // ������ ���/ũ��
            float atkMul = 1f;
            float critCh = 0f;   // 0~1
            float critMul = 1f;   // >=1

            if (attacker != null)
            {
                atkMul = Mathf.Max(0f, attacker.DamageMultiplier.Value);
                critCh = Mathf.Clamp01(attacker.CritChance.Value);
                critMul = Mathf.Max(1f, attacker.CritDamageMultiplier.Value);
            }

            dmg *= atkMul;

            // ũ�� ����
            if (critCh > 0f && Random.value < critCh)
            {
                isCrit = true;
                dmg *= critMul;
            }

            // �ǰ��� ���
            if (defender != null)
            {
                float incomingMul = Mathf.Max(0f, defender.IncomingDamageMultiplier.Value);
                dmg *= incomingMul;
            }

            // ����(�տ���) ����
            dmg = ApplyDefenseReduction(dmg, defender);

            if (!float.IsFinite(dmg)) return 0f;
            return Mathf.Max(0f, dmg);
        }
    }
}
