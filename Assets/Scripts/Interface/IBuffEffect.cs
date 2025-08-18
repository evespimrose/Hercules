using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBuffEffect
{
    void Apply(Unit target, float time, Vector2? direction = null, float magnitude = 0f);
}

public class KnockbackEffect : IBuffEffect
{
    public void Apply(Unit target, float time, Vector2? dir, float magnitude)
    {
        if (dir.HasValue)
            target.ApplyKnockback(dir.Value, magnitude > 0 ? magnitude : target.defaultKnockbackForce);
    }
}

public class StunEffect : IBuffEffect
{
    public void Apply(Unit target, float time, Vector2? dir, float magnitude)
    {
        target.ApplyStun(time);
    }
}

public class InvincibleEffect : IBuffEffect
{
    public void Apply(Unit target, float time, Vector2? dir, float magnitude)
    {
        target.ApplyInvincible(time);
    }
}

// ---- 플레이어 전용 Buff ----
public class HitstopEffect : IBuffEffect
{
    public void Apply(Unit target, float time, Vector2? dir, float magnitude)
    {
        if (target is Player player)
            player.ApplyHitstop(time);
    }
}