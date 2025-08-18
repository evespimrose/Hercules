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
    public enum Buff
    {
        None = 0,
        Knockback,
        Stun,
        Invincible,
        // 확장 여지: Slow, Root, Poison, Burn, Shield, Regeneration 등
        Hitstop // 기본 유닛은 무시, 플레이어에서만 처리
    }

    [Header("Unit Stats")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Death")]
    public bool destroyOnDeath = false;
    public float deathDestroyDelay = 0.5f;

    public bool IsDead { get; private set; } = false;
    public event Action<Unit> OnDied;

    public bool IsResurrect { get; private set; } = false;

    [Header("Hit Reaction / Status")]
    [Tooltip("기본 넉백 세기(가해자→피격자 방향으로 Impulse)")]
    public float defaultKnockbackForce = 8f;
    [Tooltip("스턴 중 수평 속도 0으로 고정할지 여부")]
    public bool zeroHorizontalDuringStun = true;

    protected bool isStunned;
    protected bool isInvincible;
    protected Vector2 lastHitDirection; // (attacker -> this)

    protected Coroutine stunRoutine;
    protected Coroutine invincibleRoutine;

    protected virtual void Awake()
    {
        // currentHealth 초기화(0이거나 미설정이면 max로)
        currentHealth = (currentHealth <= 0f) ? maxHealth
                                              : Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    public virtual void Damage(float amount, Unit source)
    {
        if (IsDead) return;
        if (isInvincible)
        {
            UnityEngine.Debug.Log($"{name}은(는) 무적 상태로 피해 무시");
            return;
        }

        amount = Mathf.Max(0f, amount);
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        UnityEngine.Debug.Log($"{name}이(가) {(source ? source.name : "알 수 없음")} 로부터 {amount} 피해를 받음. HP={currentHealth}/{maxHealth}");

        if (source)
        {
            Vector2 dir = (transform.position - source.transform.position);
            lastHitDirection = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.zero;
        }

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
        UnityEngine.Debug.Log($"[Unit] {name} Resurrect()");
    }

    // ============ Buff 시스템 ============
    protected virtual Dictionary<Buff, IBuffEffect> BuffEffects { get; } =
        new Dictionary<Buff, IBuffEffect>()
        {
            { Buff.Knockback, new KnockbackEffect() },
            { Buff.Stun, new StunEffect() },
            { Buff.Invincible, new InvincibleEffect() }
        };

    public virtual void Mesmerize(float time, Buff buff, Vector2? dir = null, float magnitude = 0f)
    {
        if (BuffEffects.TryGetValue(buff, out var effect))
            effect.Apply(this, time, dir, magnitude);
    }

    // ===== 개별 효과 구현 =====
    public virtual void ApplyKnockback(Vector2 dir, float force)
    {
        var rb = GetComponent<Rigidbody2D>();
        if (!rb) return;
        rb.AddForce(dir.normalized * force, ForceMode2D.Impulse);
    }

    public virtual void ApplyStun(float time)
    {
        if (time <= 0f) return;
        if (stunRoutine != null) StopCoroutine(stunRoutine);
        stunRoutine = StartCoroutine(StunCoroutine(time));
    }

    protected virtual IEnumerator StunCoroutine(float time)
    {
        var rb = GetComponent<Rigidbody2D>();
        if (zeroHorizontalDuringStun && rb) rb.velocity = new Vector2(0f, rb.velocity.y);
        yield return new WaitForSeconds(time);
        stunRoutine = null;
    }

    public virtual void ApplyInvincible(float time)
    {
        if (time <= 0f) return;
        if (invincibleRoutine != null) StopCoroutine(invincibleRoutine);
        invincibleRoutine = StartCoroutine(InvincibleCoroutine(time));
    }

    protected virtual IEnumerator InvincibleCoroutine(float time)
    {
        yield return new WaitForSeconds(time);
        invincibleRoutine = null;
    }

    // Hitstop은 기본적으로 의미 없음
    public virtual void ApplyHitstop(float time) { }
}
