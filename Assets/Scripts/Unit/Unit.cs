using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Unit : MonoBehaviour, IDamageable, IHealable
{
    [Header("Unit Stats")]
    public float maxHealth = 100f;
    public float currentHealth;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
    }

    public virtual void Damage(float amount, Unit source)
    {
        Debug.Log($"{name}이(가) {source.name}로부터 {amount} 피해를 받음.");
    }

    public virtual void Heal(float amount, Unit source)
    {
        Debug.Log($"{name}이(가) {source.name}로부터 {amount}만큼 회복됨.");
    }
}
