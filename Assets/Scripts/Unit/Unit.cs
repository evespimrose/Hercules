//using System.Collections;
//using System.Collections.Generic;
//using System.Diagnostics;
//using UnityEngine;

//public abstract class Unit : MonoBehaviour, IDamageable, IHealable
//{
//    [Header("Unit Stats")]
//    public float maxHealth = 100f;
//    public float currentHealth;

//    protected virtual void Awake()
//    {
//        currentHealth = maxHealth;
//    }

//    public virtual void Damage(float amount, Unit source)
//    {
//        UnityEngine.Debug.Log($"{name}이(가) {source.name}로부터 {amount} 피해를 받음.");
//    }

//    public virtual void Heal(float amount, Unit source)
//    {
//        UnityEngine.Debug.Log($"{name}이(가) {source.name}로부터 {amount}만큼 회복됨.");
//    }
//}

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using UnityEngine;

public abstract class Unit : MonoBehaviour, IDamageable, IHealable
{
    [Header("Unit Stats")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Death")]
    public bool destroyOnDeath = false;
    public float deathDestroyDelay = 0.5f;

    public bool IsDead { get; private set; } = false;
    public event Action<Unit> OnDied;

    public bool IsResurrect { get; private set; } = false;

    protected virtual void Awake()
    {
        // currentHealth 초기화(0이거나 미설정이면 max로)
        currentHealth = (currentHealth <= 0f) ? maxHealth
                                              : Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    public virtual void Damage(float amount, Unit source)
    {
        if (IsDead) return;

        amount = Mathf.Max(0f, amount);
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        UnityEngine.Debug.Log($"{name}이(가) {(source ? source.name : "알 수 없음")} 로부터 {amount} 피해를 받음. HP={currentHealth}/{maxHealth}");

        if (currentHealth <= 0f) Die();
    }

    public virtual void Heal(float amount, Unit source)
    {
        if (IsDead) return;

        amount = Mathf.Max(0f, amount);
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UnityEngine.Debug.Log($"{name}이(가) {(source ? source.name : "알 수 없음")} 로부터 {amount} 회복. HP={currentHealth}/{maxHealth}");
    }

    public virtual void Die()
    {
        if (IsDead) return;

        if (IsResurrect)
        {
            Resurrect();
            return;
        }

        IsDead = true;
        UnityEngine.Debug.Log($"[Unit] {name} Die()");

        var rb = GetComponent<Rigidbody2D>();
        if (rb) rb.simulated = false;           //죽은 뒤 쓰러지는 연출을 원하면 rb.simulated=false 대신 rb.velocity = Vector2.zero; rb.constraints = Freeze...; 식으로 부분 제어.

                var cols = GetComponentsInChildren<Collider2D>(true);
        foreach (var col in cols) if (col) col.enabled = false;

        OnDied?.Invoke(this);

        if (destroyOnDeath) Destroy(gameObject, deathDestroyDelay);
        else gameObject.SetActive(false);
    }

    public virtual void Resurrect()
    {

    }
}
