using UnityEngine;

namespace Hercules.StatsSystem
{
    /// <summary>
    /// ������ ���� ���� ������. (����� �÷��̽�Ȧ��)
    /// Crit(Ȯ��/���), ��/���� �¼� ���� ����ϴ� "����"�� ǥ���� ��.
    /// </summary>
    public static class CombatMath
    {
        public static float ComputeDamage(StatsBase attacker, StatsBase defender, float baseDamage)
        {
            // TODO: ���� ���� Ȯ�� �� ��ü
            // ����(��):
            // 1) ġ��Ÿ
            // bool crit = Random.value < Mathf.Clamp01(attacker.CritChance.Value);
            // float critMul = crit ? Mathf.Max(1f, attacker.CritDamageMultiplier.Value) : 1f;
            //
            // 2) ���� �¼�
            // float outMul = Mathf.Max(0f, attacker.DamageMultiplier.Value);
            // float incMul = Mathf.Max(0f, defender.IncomingDamageMultiplier.Value);
            //
            // 3) �Ӽ�/���/����/Ŭ���� �� �߰�
            //
            // return Mathf.Max(0f, baseDamage * critMul * outMul * incMul);

            return baseDamage; // ������ �׳� ���
        }
    }
}
