using UnityEngine;

namespace Hercules.StatsSystem
{
    /// <summary>
    /// 임시 데미지 공식:
    ///   final = baseDamage
    ///           * (공격측 DamageMultiplier)
    ///           * (크리티컬이면 CritDamageMultiplier)
    ///           * (피격측 IncomingDamageMultiplier)
    ///           → (방어력 합연산) damage - defense (하한 0)
    ///
    /// 주의:
    ///  - AttackAbility 쪽에서 이미 공격 배수를 미리 곱해 Hitbox로 넘기고 있다면
    ///    여기서 또 곱해져서 "2중 적용" 됩니다. => AttackAbility는 베이스만 넘기세요.
    /// </summary>
    public static class CombatMath
    {
        /// <summary>
        /// 난수 기반(비결정적) 임시 계산.
        /// </summary>
        public static float ComputeDamage(StatsBase attacker, StatsBase defender, float baseDamage)
        {
            if (baseDamage <= 0f) return 0f;

            float dmg = Mathf.Max(0f, baseDamage);

            // ── 공격측 배수 ─────────────────────────────────────────────
            float atkMul = 1f;
            float critCh = 0f;   // 0~1
            float critMul = 1f;   // 1.0 이상

            if (attacker != null)
            {
                // StatsBase의 StatValue는 .Value 로 읽기
                // (없으면 1로 처리)
                try { atkMul = Mathf.Max(0f, attacker.DamageMultiplier.Value); } catch { atkMul = 1f; }
                try { critCh = Mathf.Clamp01(attacker.CritChance.Value); } catch { critCh = 0f; }
                try { critMul = Mathf.Max(1f, attacker.CritDamageMultiplier.Value); } catch { critMul = 1.5f; }
            }

            dmg *= atkMul;

            // 크리티컬
            if (critCh > 0f && Random.value < critCh)
                dmg *= critMul;

            // ── 피격측 배수 ────────────────────────────────────────────
            if (defender != null)
            {
                try
                {
                    float incomingMul = Mathf.Max(0f, defender.IncomingDamageMultiplier.Value);
                    dmg *= incomingMul;
                }
                catch { /* 무시 */ }
            }

            // 방어력(합연산) 적용
            dmg = ApplyDefenseReduction(dmg, defender);

            if (!float.IsFinite(dmg)) return 0f;
            return Mathf.Max(0f, dmg);
        }

        /// <summary>
        /// 결정적(테스트용) 버전. 외부 시드로 크리 판정 고정 가능.
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

            // 방어력(합연산) 적용
            dmg = ApplyDefenseReduction(dmg, defender);

            if (!float.IsFinite(dmg)) return 0f;
            return Mathf.Max(0f, dmg);
        }

        // 안전한 값 읽기 헬퍼
        private static float SafeStat(float? v, float fallback, float min)
        {
            if (!v.HasValue) return fallback;
            if (float.IsNaN(v.Value) || float.IsInfinity(v.Value)) return fallback;
            return Mathf.Max(min, v.Value);
        }

        /// <summary>
        /// 방어력 적용 단계 (확장 포인트)
        /// - 현재 공식: 최종 단계에서 damage - defense (하한 0)
        /// - 훗날 관통/소프트캡/비선형 등은 이 함수만 수정하여 전역 반영
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

            // 공격측 배수/크리
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

            // 크리 판정
            if (critCh > 0f && Random.value < critCh)
            {
                isCrit = true;
                dmg *= critMul;
            }

            // 피격측 배수
            if (defender != null)
            {
                float incomingMul = Mathf.Max(0f, defender.IncomingDamageMultiplier.Value);
                dmg *= incomingMul;
            }

            // 방어력(합연산) 적용
            dmg = ApplyDefenseReduction(dmg, defender);

            if (!float.IsFinite(dmg)) return 0f;
            return Mathf.Max(0f, dmg);
        }
    }
}
