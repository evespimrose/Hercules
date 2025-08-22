//using System.Collections; 
//using System.Collections.Generic; 
//using System.Diagnostics; 
//using UnityEngine; 

//public abstract class Unit : MonoBehaviour, IDamageable, IHealable 
//{ 
//    [Header("Unit Stats")] 
//    public float maxHealth = 100f; 
//    public float currentHealth; 
//
//    protected virtual void Awake() 
//    { 
//        currentHealth = maxHealth; 
//    } 
//
//    public virtual void Damage(float amount, Unit source) 
//    { 
//        UnityEngine.Debug.Log($"{name}이(가) {source.name}로부터 {amount} 피해를 받음."); 
//    } 
//
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
        Knockback,       //넉백 :
        Stun,            //기절 : 
        Invincible,
        // 확장 여지: Slow, Root, Poison, Burn, Shield, Regeneration 등
        Hitstop, // 기본 유닛은 무시, 플레이어에서만 처리
        Indomitable,     //불굴 : HP 1미만이면 5초 무적 후 사망
        Bleeding,        //출혈 : 출혈에 걸리면 HP가 5초동안 0.5초마다 7씩 닳음
        BleedingStack,   //스택형 출혈 : 적용 시 스택+1, 지속시간 리프레시, 틱 피해 증가
        Exhaustion,      //탈진 : 이동/공속/피해/점프 약화
        SlowMove,        //이속 감소
        SlowAttack,      //공속 감소
        WeekAttack,      //공격 데미지 감소
        LowJump,         //점프 높이 감소
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

    // ===== Bleeding 상태 관리 =====
    protected Unit lastAttacker;                  // DOT 귀속용
    protected Coroutine bleedingRoutine;          // 출혈 코루틴
    protected bool isBleeding;                    // 출혈 중 여부
    public bool IsBleeding => isBleeding;

    // 스택형 출혈 런타임 파라미터
    protected int bleedStacks;
    protected float bleedRemain;                   // 남은 지속시간(초)
    protected float bleedTick;                     // 틱 간격(초)
    protected float bleedBase;                     // 1스택 기본 데미지
    protected float bleedPerStack;                 // 스택당 추가 데미지
    protected int bleedMaxStacks;                  // 최대 스택

    // ==== 런타임 스탯 배수(디버프/버프가 조정) ====
    [Header("Stat Multipliers (runtime)")]
    [SerializeField] protected float _moveSpeedMultiplier = 1f;         // 이동속도 배수
    [SerializeField] protected float _attackTimeScale = 1f;             // 공격 타이밍 배수(>1 느려짐)
    [SerializeField] protected float _attackDamageMultiplier = 1f;      // 공격 피해 배수
    [SerializeField] protected float _jumpHeightMultiplier = 1f;        // 점프 높이 배수

    public float MoveSpeedMultiplier => _moveSpeedMultiplier;
    public float AttackTimeScale => _attackTimeScale;
    public float AttackDamageMultiplier => _attackDamageMultiplier;
    public float JumpHeightMultiplier => _jumpHeightMultiplier;

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
            lastAttacker = source; // DOT 가해자 기록
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

        // 진행 중 상태 코루틴 정리
        if (bleedingRoutine != null) { StopCoroutine(bleedingRoutine); bleedingRoutine = null; isBleeding = false; bleedStacks = 0; }
        if (invincibleRoutine != null) { StopCoroutine(invincibleRoutine); invincibleRoutine = null; isInvincible = false; }

        var rb = GetComponent<Rigidbody2D>();
        if (rb) rb.simulated = false;  //죽은 뒤 쓰러지는 연출을 원하면 rb.simulated=false 대신 rb.velocity = Vector2.zero; rb.constraints = Freeze...; 식으로 부분 제어.

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
            { Buff.Knockback,  new KnockbackEffect()  },
            { Buff.Stun,       new StunEffect()       },
            { Buff.Invincible, new InvincibleEffect() },                   // 불굴
            { Buff.Bleeding,   new BleedingEffect()   },                   // 단일형 출혈
            { Buff.BleedingStack, new BleedingStackEffect() },             // 스택형 출혈
            { Buff.Exhaustion,       new ExhaustionEffect()       },       // 탈진
            { Buff.SlowMove,       new SlowMoveEffect()       },           // 이속감소
            { Buff.SlowAttack,       new SlowAttackEffect()       },       // 공속감소
            { Buff.WeekAttack,       new WeekAttackEffect()       },       // 공격 데미지 감소
            { Buff.LowJump,       new LowJumpEffect()       },             // 점프 높이 감소
        };

    public virtual void Mesmerize(float time, Buff buff, Vector2? dir = null, float magnitude = 0f)
    {
        if (BuffEffects.TryGetValue(buff, out var effect))
            effect.Apply(this, time, dir ?? lastHitDirection, magnitude);
        else
            UnityEngine.Debug.LogWarning($"[Buff] Effect not mapped: {buff} on {name}");
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
        // 무적 on/off 토글
        isInvincible = true;
        yield return new WaitForSeconds(time);
        isInvincible = false;
        invincibleRoutine = null;
    }

    // Hitstop은 기본적으로 의미 없음
    public virtual void ApplyHitstop(float time) { }

    // ===== 단일형 출혈 =====
    public virtual void ApplyBleeding(float duration, float tickInterval = 0.5f, float damagePerTick = 7f)
    {
        // 단일형: 스택 1, perStackBonus 0, 최대 1스택 → 과거 동작 유지
        ApplyBleedingStacking(duration, tickInterval, baseDamage: damagePerTick, perStackBonus: 0f, addStacks: 1, maxStacks: 1);
    }

    // ===== 스택형 출혈 =====
    public virtual void ApplyBleedingStacking(
        float duration,
        float tickInterval = 0.5f,
        float baseDamage = 7f,
        float perStackBonus = 1f,
        int addStacks = 1,
        int maxStacks = 5)
    {
        if (duration <= 0f || tickInterval <= 0f || baseDamage <= 0f) return;

        // 파라미터 갱신
        bleedTick = tickInterval;
        bleedBase = baseDamage;
        bleedPerStack = Mathf.Max(0f, perStackBonus);
        bleedMaxStacks = Mathf.Max(1, maxStacks);

        // 스택 증가 + 지속시간 리프레시
        int prev = bleedStacks;
        bleedStacks = Mathf.Clamp(bleedStacks + Mathf.Max(1, addStacks), 1, bleedMaxStacks);
        bleedRemain = duration;

        if (!isBleeding || bleedingRoutine == null)
        {
            bleedingRoutine = StartCoroutine(BleedingStackCoroutine());
            isBleeding = true;
            UnityEngine.Debug.Log($"BleedingStack start (stacks={bleedStacks})");
        }
        else
        {
            if (bleedStacks != prev)
                UnityEngine.Debug.Log($"BleedingStack stack +{bleedStacks - prev} → {bleedStacks} (refresh {duration}s)");
            else
                UnityEngine.Debug.Log($"BleedingStack refresh {duration}s (stacks={bleedStacks})");
        }
    }

    protected virtual IEnumerator BleedingStackCoroutine()
    {
        var wait = new WaitForSeconds(bleedTick);

        while (bleedStacks > 0 && bleedRemain > 0f && !IsDead)
        {
            float dmg = Mathf.Max(0f, bleedBase + bleedPerStack * (bleedStacks - 1));
            Damage(dmg, lastAttacker);
            yield return wait;
            bleedRemain -= bleedTick;
        }

        isBleeding = false;
        bleedStacks = 0;
        bleedingRoutine = null;
        UnityEngine.Debug.Log("Out of bleeding (stack)");
    }
}
