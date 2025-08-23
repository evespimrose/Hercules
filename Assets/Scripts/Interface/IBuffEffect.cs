using System.Collections;
using UnityEngine;
using Hercules.StatsSystem;

public interface IBuffEffect
{
    void Apply(Unit target, float time, Vector2? direction = null, float magnitude = 0f);
}

// ─── 공용: 넉백/스턴/무적/출혈(기존 로직 유지) ───

public class KnockbackEffect : IBuffEffect
{
    public void Apply(Unit target, float time, Vector2? dir, float magnitude)
    {
        var d = dir ?? Vector2.zero;
        float f = magnitude > 0f ? magnitude : target.defaultKnockbackForce;
        target.ApplyKnockback(d, f);
    }
}

public class StunEffect : IBuffEffect
{
    public void Apply(Unit target, float time, Vector2? dir, float magnitude)
    {
        if (time <= 0f) return;
        target.ApplyStun(time);
    }
}

public class InvincibleEffect : IBuffEffect
{
    public void Apply(Unit target, float time, Vector2? dir, float magnitude)
    {
        if (time <= 0f) return;
        target.ApplyInvincible(time);
    }
}

public class BleedingEffect : IBuffEffect
{
    public void Apply(Unit target, float time, Vector2? dir, float magnitude)
    {
        if (time <= 0f) return;
        target.StartBleeding(time);
    }
}

public class BleedingStackEffect : IBuffEffect
{
    public void Apply(Unit target, float time, Vector2? dir, float magnitude)
    {
        int stacks = Mathf.Max(1, Mathf.RoundToInt(magnitude));
        target.StartBleedingStack(time, stacks);
    }
}

// ─── 플레이어/유닛 공통 스탯에 직접 Modifier로 반영되는 디버프들 ───

public class ExhaustionEffect : IBuffEffect
{
    public void Apply(Unit target, float time, Vector2? dir, float magnitude)
    {
        var stats = target.GetComponent<StatsBase>();
        if (stats == null) return;

        var src = target;
        stats.MoveSpeed.AddModifier(new StatModifier { op = StatOp.Mult, value = 0.8f, source = src });
        stats.AttackSpeed.AddModifier(new StatModifier { op = StatOp.Mult, value = 0.9f, source = src });
        stats.MaxJumpHeight.AddModifier(new StatModifier { op = StatOp.Mult, value = 0.9f, source = src });

        if (time > 0f)
            target.StartCoroutine(RemoveAfter(time, stats, src,
                removeMove: true, removeAtk: true, removeJump: true));
    }

    private IEnumerator RemoveAfter(float t, StatsBase s, Object src,
        bool removeMove, bool removeAtk, bool removeJump)
    {
        yield return new WaitForSeconds(t);
        if (removeMove) s.MoveSpeed.RemoveModifiersBySource(src);
        if (removeAtk) s.AttackSpeed.RemoveModifiersBySource(src);
        if (removeJump) s.MaxJumpHeight.RemoveModifiersBySource(src);
    }
}

public class SlowMoveEffect : IBuffEffect
{
    public void Apply(Unit target, float time, Vector2? dir, float magnitude)
    {
        var stats = target.GetComponent<StatsBase>();
        if (stats == null) return;

        float mul = (magnitude > 0f) ? Mathf.Clamp(magnitude, 0.01f, 1f) : 0.7f;
        var src = target;
        stats.MoveSpeed.AddModifier(new StatModifier { op = StatOp.Mult, value = mul, source = src });

        if (time > 0f) target.StartCoroutine(RemoveAfter(time, stats, src));
    }

    private IEnumerator RemoveAfter(float t, StatsBase s, Object src)
    {
        yield return new WaitForSeconds(t);
        s.MoveSpeed.RemoveModifiersBySource(src);
    }
}

public class SlowAttackEffect : IBuffEffect
{
    public void Apply(Unit target, float time, Vector2? dir, float magnitude)
    {
        var stats = target.GetComponent<StatsBase>();
        if (stats == null) return;

        float mul = (magnitude > 0f) ? Mathf.Clamp(magnitude, 0.01f, 1f) : 0.8f;
        var src = target;
        stats.AttackSpeed.AddModifier(new StatModifier { op = StatOp.Mult, value = mul, source = src });

        if (time > 0f) target.StartCoroutine(RemoveAfter(time, stats, src));
    }

    private IEnumerator RemoveAfter(float t, StatsBase s, Object src)
    {
        yield return new WaitForSeconds(t);
        s.AttackSpeed.RemoveModifiersBySource(src);
    }
}

public class WeakAttackEffect : IBuffEffect // Week → Weak
{
    public void Apply(Unit target, float time, Vector2? dir, float magnitude)
    {
        var stats = target.GetComponent<StatsBase>();
        if (stats == null) return;

        // 실제 공식 확정 시 CombatMath에서 DamageMultiplier를 사용하도록 설계.
        float mul = (magnitude > 0f) ? Mathf.Clamp(magnitude, 0.01f, 1f) : 0.8f;
        var src = target;
        stats.DamageMultiplier.AddModifier(new StatModifier { op = StatOp.Mult, value = mul, source = src });

        if (time > 0f) target.StartCoroutine(RemoveAfter(time, stats, src));
    }

    private IEnumerator RemoveAfter(float t, StatsBase s, Object src)
    {
        yield return new WaitForSeconds(t);
        s.DamageMultiplier.RemoveModifiersBySource(src);
    }
}

public class LowJumpEffect : IBuffEffect
{
    public void Apply(Unit target, float time, Vector2? dir, float magnitude)
    {
        var stats = target.GetComponent<StatsBase>();
        if (stats == null) return;

        float mul = (magnitude > 0f) ? Mathf.Clamp(magnitude, 0.01f, 1f) : 0.8f;
        var src = target;
        stats.MaxJumpHeight.AddModifier(new StatModifier { op = StatOp.Mult, value = mul, source = src });

        if (time > 0f) target.StartCoroutine(RemoveAfter(time, stats, src));
    }

    private IEnumerator RemoveAfter(float t, StatsBase s, Object src)
    {
        yield return new WaitForSeconds(t);
        s.MaxJumpHeight.RemoveModifiersBySource(src);
    }
}
