using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monster : Unit, IHitReceiver
{
    private MonsterController monsterController;

    protected override Dictionary<Buff, IBuffEffect> BuffEffects { get; } =
        new Dictionary<Buff, IBuffEffect>()
        {
            { Buff.Knockback,  new KnockbackEffect()  },
            { Buff.Stun,       new StunEffect()       },
            { Buff.Invincible, new InvincibleEffect() },
            { Buff.Bleeding,   new BleedingEffect()   },
            { Buff.BleedingStack, new BleedingStackEffect() },
        };

    protected override void Awake()
    {
        base.Awake();
        monsterController = GetComponent<MonsterController>();
    }

    public void ReceiveHit(float dmg, Vector2 knockback, Vector2 hitPoint)
    {
        Damage(dmg, null);

        // 넉백(넘겨받은 벡터를 방향/세기로 분해)
        if (knockback.sqrMagnitude > 0f)
        {
            var dir = knockback.normalized;
            var force = knockback.magnitude;
            ApplyKnockback(dir, force);
        }

        Debug.Log($"[Monster] {name} ReceiveHit dmg={dmg}, HP={currentHealth}/{maxHealth}");
    }

    // 몬스터가 다른 유닛에 데미지를 주는 메서드
    public void DealDamage(Unit target, float amount)
    {
        if (target is IDamageable damageable)
        {
            damageable.Damage(amount, this);
            Debug.Log($"{name}이(가) {target.name}에게 {amount} 피해를 줌.");
        }
        else
        {
            Debug.Log($"{target.name}은(는) 데미지를 받을 수 없음.");
        }
    }

    public override void Damage(float amount, Unit source)
    {
        base.Damage(amount, source);
    }

    public override void Heal(float amount, Unit source)
    {
        base.Heal(amount, source);
    }
}
