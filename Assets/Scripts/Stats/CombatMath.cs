using System;
using UnityEngine;

namespace Hercules.StatsSystem
{
    /// <summary>
    /// 현재 공식(한 줄 등가식):
    /// Final = max(0, (baseDamage * atkMul * critFactor * incomingMul) - defense)
    ///   - atkMul = Attacker.DamageMultiplier
    ///   - critFactor = (rand01() < CritChance ? CritDamageMultiplier : 1)
    ///   - incomingMul = Defender.IncomingDamageMultiplier
    ///   - defense = Defender.Defense
    ///
    /// 유지보수 포인트:
    ///  1) 방어 단계 교체: CombatMath.DefenseStage 를 바꾸면 전역 반영됨.
    ///  2) 전체 한 줄 공식 교체: EvalOneLine(...)만 바꾸면 됨(외부 시그니처 불변).
    ///
    /// 주의:
    ///  - AttackAbility 등에서 baseDamage에 배수를 미리 곱해 넘기면 여기서 2중 적용됩니다.
    ///    반드시 "베이스 값"만 넘기세요.
    /// </summary>
    public static class CombatMath
    {
        // ─────────────────────────────────────────────────────────────
        // ① 방어 단계 교체 가능한 델리게이트 (기본: 합연산 감산 후 하한 0)
        //    필요하면 비율감쇠/하이브리드 등으로 갈아끼우세요.
        public static Func<float, float, float> DefenseStage = (damageAfterMultipliers, defense) =>
        {
            float def = Mathf.Max(0f, defense);
            return Mathf.Max(0f, damageAfterMultipliers - def);
        };

        // ─────────────────────────────────────────────────────────────
        // ② 한 줄 등가식(현재 동작) ? 이 함수 하나만 바꾸면 전체 수식이 교체됩니다.
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
            // 안전 가드(현재 동작과 동일한 경계)
            float bd = Mathf.Max(0f, baseDamage);
            float atk = Mathf.Max(0f, atkMul);
            float inc = Mathf.Max(0f, incomingMul);
            float cMul = Mathf.Max(1f, critMul);
            float cCh = Mathf.Clamp01(critChance);

            // 크리티컬 판정
            bool crit = false;
            float critFactor = 1f;
            if (cCh > 0f)
            {
                float r = rand01 != null ? rand01() : UnityEngine.Random.value;
                if (r < cCh) { crit = true; critFactor = cMul; }
            }
            isCrit = crit;

            // 한 줄 공식
            float afterMul = bd * atk * critFactor * inc;
            float final = DefenseStage(afterMul, defense);

            if (!float.IsFinite(final)) return 0f;
            return Mathf.Max(0f, final);
        }

        // ─────────────────────────────────────────────────────────────
        // 외부 공개 API (시그니처 유지)

        /// <summary>난수 기반(비결정적) 계산. (현재 동작과 동일)</summary>
        public static float ComputeDamage(StatsBase attacker, StatsBase defender, float baseDamage)
        {
            if (baseDamage <= 0f) return 0f;

            // 스탯 안전 추출
            float atkMul = 1f;
            float critCh = 0f;
            float critMul = 1.5f; // 기본 가정(코드와 동일)
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

        /// <summary>결정적(시드 기반) 계산. 크리 판정 고정 가능(현재 동작과 동일)</summary>
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

        /// <summary>난수 기반 + 크리 여부 out (현재 동작과 동일)</summary>
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

        // ─────────────────────────────────────────────────────────────
        // 레거시 호환: ApplyDefenseReduction(동작 동일, 내부적으로 DefenseStage 사용)
        public static float ApplyDefenseReduction(float damageAfterMultipliers, StatsBase defender)
        {
            float def = 0f;
            if (defender != null)
                def = SafeStat(defender?.Defense?.Value, 0f, 0f);
            return DefenseStage(damageAfterMultipliers, def);
        }

        // 공통 가드
        private static float SafeStat(float? v, float fallback, float min)
        {
            if (!v.HasValue) return fallback;
            if (float.IsNaN(v.Value) || float.IsInfinity(v.Value)) return fallback;
            return Mathf.Max(min, v.Value);
        }
    }
}
