using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hercules.StatsSystem;

/// <summary>
/// 유닛 공통 베이스: 체력/피격/사망/상태/버프 라우팅.
/// Player/Monster가 상속하며, 체력/스탯은 StatsBase를 통해 접근.
/// </summary>
public abstract class Unit : MonoBehaviour, IDamageable, IHealable
{
    [Header("Legacy (Migration Only)")]
    public float maxHealth = 100f;     // 과도기: 초기값 세팅에만 사용
    public float currentHealth = 100f; // 과도기: 초기값 세팅에만 사용

    [Header("Death")]
    public float deathDestroyDelay = 0.5f;

    [Header("Physics")]
    public Rigidbody2D body;

    [Header("Hit/State")]
    public float defaultKnockbackForce = 8f;
    public bool zeroHorizontalDuringStun = true;

    // 런타임 상태
    public bool IsDead { get; protected set; } = false;
    protected bool isStunned;
    protected bool isInvincible;
    protected Coroutine invincibleRoutine;

    protected Vector2 lastHitDirection;
    protected Unit lastAttacker;

    public event Action<Unit> OnDied;

    // 스탯
    protected StatsBase stats;

    // ===== Buff 종류 =====
    public enum Buff
    {
        Knockback,
        Stun,
        Invincible,
        Bleeding,
        BleedingStack,
        Exhaustion,
        SlowMove,
        SlowAttack,
        WeakAttack,
        LowJump,
    }

    // 출혈 관리
    private bool isBleeding;
    private Coroutine bleedingRoutine;
    private int bleedStacks;
    private float bleedRemain;

    [Header("Bleeding Defaults")]
    public float bleedBase = 2f;
    public float bleedPerStack = 1f;
    public float bleedTick = 1f; // 1초
    public float bleedDuration = 5f;

    protected virtual void Awake()
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        stats = GetComponent<StatsBase>();
        if (stats == null) stats = gameObject.AddComponent<StatsBase>(); // 과도기 안전장치

        // 과도기: 인스펙터 값 → Stats로 복사
        stats.MaxHealth.Base = Mathf.Max(1f, maxHealth);
        stats.CurrentHealth = Mathf.Clamp(currentHealth, 0f, stats.MaxHealth.Value);
    }

    // ===== Damage / Heal =====
    public virtual void Damage(float amount, Unit source)
    {
        if (IsDead) return;
        if (isInvincible) { Debug.Log($"{name} 무적: 피해 무시"); return; }

        amount = Mathf.Max(0f, amount);
        stats.CurrentHealth = Mathf.Max(0f, stats.CurrentHealth - amount);

        if (source)
        {
            lastAttacker = source;
            Vector2 dir = (transform.position - source.transform.position);
            lastHitDirection = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.zero;
        }

        Debug.Log($"{name}이(가) {(source ? source.name : "알 수 없음")}에게 {amount} 피해. HP={stats.CurrentHealth}/{stats.MaxHealth.Value}");

        if (stats.CurrentHealth <= 0f)
            Die();
    }

    public virtual void Heal(float amount, Unit source)
    {
        if (IsDead) return;

        amount = Mathf.Max(0f, amount);
        stats.CurrentHealth = Mathf.Min(stats.MaxHealth.Value, stats.CurrentHealth + amount);

        Debug.Log($"{name}이(가) {(source ? source.name : "알 수 없음")}에게 {amount} 회복. HP={stats.CurrentHealth}/{stats.MaxHealth.Value}");
    }

    // ===== Death =====
    public virtual void Die()
    {
        if (IsDead) return;
        IsDead = true;
        Debug.Log($"[Unit] {name} Die()");
        OnDied?.Invoke(this);
        StartCoroutine(DestroyAfter(deathDestroyDelay));
    }

    IEnumerator DestroyAfter(float t)
    {
        if (t > 0f) yield return new WaitForSeconds(t);
        Destroy(gameObject);
    }

    // ===== Knockback / Stun / Invincible =====
    public virtual void ApplyKnockback(Vector2 direction, float force)
    {
        if (body == null) return;
        var dir = direction.sqrMagnitude < 0.0001f ? Vector2.zero : direction.normalized;
        body.AddForce(dir * force, ForceMode2D.Impulse);
    }

    public virtual void ApplyStun(float time)
    {
        if (time <= 0f) return;
        if (isStunned) return;

        StartCoroutine(StunRoutine(time));
    }

    IEnumerator StunRoutine(float t)
    {
        isStunned = true;
        float timer = t;

        while (timer > 0f)
        {
            if (zeroHorizontalDuringStun && body != null)
                body.velocity = new Vector2(0f, body.velocity.y);

            timer -= Time.deltaTime;
            yield return null;
        }

        isStunned = false;
    }

    public virtual void ApplyInvincible(float time)
    {
        if (time <= 0f) return;
        if (isInvincible) return;

        isInvincible = true;
        if (invincibleRoutine != null) StopCoroutine(invincibleRoutine);
        invincibleRoutine = StartCoroutine(InvincibleRoutine(time));
    }

    IEnumerator InvincibleRoutine(float t)
    {
        float timer = t;
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            yield return null;
        }
        isInvincible = false;
        invincibleRoutine = null;
    }

    // ===== Buff Router =====
    public virtual void Mesmerize(float time, Buff type, Vector2? dir = null, float magnitude = 0f)
    {
        IBuffEffect effect = null;

        switch (type)
        {
            case Buff.Knockback: effect = new KnockbackEffect(); break;
            case Buff.Stun: effect = new StunEffect(); break;
            case Buff.Invincible: effect = new InvincibleEffect(); break;
            case Buff.Bleeding: effect = new BleedingEffect(); break;
            case Buff.BleedingStack: effect = new BleedingStackEffect(); break;
            case Buff.Exhaustion: effect = new ExhaustionEffect(); break;
            case Buff.SlowMove: effect = new SlowMoveEffect(); break;
            case Buff.SlowAttack: effect = new SlowAttackEffect(); break;
            case Buff.WeakAttack: effect = new WeakAttackEffect(); break;
            case Buff.LowJump: effect = new LowJumpEffect(); break;
            default:
                Debug.LogWarning($"[Unit] Unknown Buff {type}");
                return;
        }

        effect.Apply(this, time, dir, magnitude);
    }

    // ===== Bleeding =====
    public void StartBleeding(float duration)
    {
        if (isBleeding && bleedingRoutine != null) StopCoroutine(bleedingRoutine);
        isBleeding = true;
        bleedingRoutine = StartCoroutine(BleedingRoutine(duration));
    }

    public void StartBleedingStack(float duration, int addStacks = 1)
    {
        bleedStacks = Mathf.Max(1, bleedStacks + Mathf.Max(1, addStacks));
        bleedRemain = Mathf.Max(bleedRemain, duration);
        if (!isBleeding || bleedingRoutine == null)
        {
            isBleeding = true;
            bleedingRoutine = StartCoroutine(BleedingStackRoutine());
        }
    }

    IEnumerator BleedingRoutine(float duration)
    {
        var wait = new WaitForSeconds(bleedTick);
        float remain = Mathf.Max(duration, bleedDuration);
        while (remain > 0f && !IsDead)
        {
            Damage(Mathf.Max(0f, bleedBase), lastAttacker);
            remain -= bleedTick;
            yield return wait;
        }
        isBleeding = false;
        bleedingRoutine = null;
        Debug.Log("Out of bleeding");
    }

    IEnumerator BleedingStackRoutine()
    {
        var wait = new WaitForSeconds(bleedTick);
        while (bleedStacks > 0 && bleedRemain > 0f && !IsDead)
        {
            float dmg = Mathf.Max(0f, bleedBase + bleedPerStack * (bleedStacks - 1));
            Damage(dmg, lastAttacker);
            bleedRemain -= bleedTick;
            yield return wait;
        }
        isBleeding = false;
        bleedStacks = 0;
        bleedingRoutine = null;
        Debug.Log("Out of bleeding (stack)");
    }

    // ===== 호환성 어댑터 =====
    #region Backward-Compatibility Shims
    /// <summary>공격속도 ↑ → 시간 ↓. 예전 코드에서 쓰던 AttackTimeScale</summary>
    public float AttackTimeScale => 1f / Mathf.Max(0.01f, (stats != null ? stats.AttackSpeed.Value : 1f));

    /// <summary>가하는 최종 피해 승수. 예전 AttackDamageMultiplier 대체</summary>
    public float AttackDamageMultiplier => (stats != null ? stats.DamageMultiplier.Value : 1f);

    /// <summary>점프 높이 배수. 예전 JumpHeightMultiplier 대체</summary>
    public float JumpHeightMultiplier => (stats != null ? stats.MaxJumpHeight.Value : 1f);

    /// <summary>이동 속도 배수. 예전 MoveSpeedMultiplier 대체</summary>
    public float MoveSpeedMultiplier => (stats != null ? stats.MoveSpeed.Value : 1f);

    /// <summary>예전 MonsterController에서 참조하던 Rigidbody 프로퍼티 대응</summary>
    public Rigidbody2D Rigidbody => body;

    /// <summary>예전 MonsterController의 DealDamage(...) 래퍼 대응</summary>
    public void DealDamage(Unit target, float amount)
    {
        if (target == null) return;
        target.Damage(amount, this);
    }
    #endregion
}
