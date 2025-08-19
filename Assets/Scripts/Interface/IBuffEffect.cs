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

// ---- 플레이어 전용 Buff ----
// 버프명 : 불굴
// 버프효과 : HP가 1미만으로 내려갈 시 5초간 무적버프
public class IndomitableEffect : IBuffEffect
{
    public void Apply(Unit target, float time, Vector2? dir, float magnitude)
    {
        if (target is Player p)
        {
            p.TriggerIndomitable(time);
            return;
        }

        // 그 외 유닛: "무적 + time 후 강제 사망"의 일반형
        target.ApplyInvincible(time);
        target.StartCoroutine(KillAfter(target, time));
    }

    private System.Collections.IEnumerator KillAfter(Unit t, float time)
    {
        yield return new WaitForSeconds(time);
        t.Die();
    }
}