using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monster : Unit
{
    private MonsterController monsterController;

    protected override void Awake()
    {
        base.Awake();
        monsterController = GetComponent<MonsterController>();
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