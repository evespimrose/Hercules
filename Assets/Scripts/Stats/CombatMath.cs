using UnityEngine;

namespace Hercules.StatsSystem
{
    /// <summary>
    /// 데미지 공식 단일 진입점. (현재는 플레이스홀더)
    /// Crit(확률/배수), 가/받피 승수 등을 사용하는 "가정"만 표기해 둠.
    /// </summary>
    public static class CombatMath
    {
        public static float ComputeDamage(StatsBase attacker, StatsBase defender, float baseDamage)
        {
            // TODO: 실제 공식 확정 시 교체
            // 가정(예):
            // 1) 치명타
            // bool crit = Random.value < Mathf.Clamp01(attacker.CritChance.Value);
            // float critMul = crit ? Mathf.Max(1f, attacker.CritDamageMultiplier.Value) : 1f;
            //
            // 2) 최종 승수
            // float outMul = Mathf.Max(0f, attacker.DamageMultiplier.Value);
            // float incMul = Mathf.Max(0f, defender.IncomingDamageMultiplier.Value);
            //
            // 3) 속성/방어/저항/클램프 등 추가
            //
            // return Mathf.Max(0f, baseDamage * critMul * outMul * incMul);

            return baseDamage; // 지금은 그냥 통과
        }
    }
}
