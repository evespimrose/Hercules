using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Unit
{
    private PlayerController playerController;

    protected override void Awake()
    {
        base.Awake();
        playerController = GetComponent<PlayerController>();
    }

    public override void Damage(float amount, Unit source)
    {
        base.Damage(amount, source);
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Debug.Log($"{name} 사망!");
        }
    }

    public override void Heal(float amount, Unit source)
    {
        base.Heal(amount, source);
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }
}