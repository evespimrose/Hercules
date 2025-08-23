//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class Monster : Unit, IHitReceiver
//{
//    private MonsterController monsterController;
//    private Rigidbody2D rb;

//    [Header("Collision Settings")]
//    public float collisionRadius = 0.5f;
//    public LayerMask hitDetectionLayers = -1; // 모든 레이어와 충돌

//    private CircleCollider2D circleCollider;
//    private List<Collider2D> overlappingColliders = new List<Collider2D>();

//    protected override Dictionary<Buff, IBuffEffect> BuffEffects { get; } =
//        new Dictionary<Buff, IBuffEffect>()
//        {
//            { Buff.Knockback,  new KnockbackEffect()  },
//            { Buff.Stun,       new StunEffect()       },
//            { Buff.Invincible, new InvincibleEffect() },
//            { Buff.Bleeding,   new BleedingEffect()   },
//            { Buff.BleedingStack, new BleedingStackEffect() },
//        };

//    protected override void Awake()
//    {
//        base.Awake();
//        monsterController = GetComponent<MonsterController>();

//        // Rigidbody2D 컴포넌트 가져오기 또는 생성
//        rb = GetComponent<Rigidbody2D>();
//        if (rb == null)
//        {
//            rb = gameObject.AddComponent<Rigidbody2D>();
//            rb.gravityScale = 0f; // 2D 플랫포머용 중력 비활성화
//            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // 회전 고정
//            rb.drag = 0.5f; // 약간의 저항 추가
//        }

//        // CircleCollider2D 컴포넌트 가져오기 또는 생성
//        circleCollider = GetComponent<CircleCollider2D>();
//        if (circleCollider == null)
//        {
//            circleCollider = gameObject.AddComponent<CircleCollider2D>();
//            circleCollider.radius = collisionRadius;
//            circleCollider.isTrigger = false; // 물리 충돌 활성화
//        }

//        // 충돌 이벤트 등록
//        SetupCollisionDetection();
//    }

//    void SetupCollisionDetection()
//    {
//        // OnTriggerEnter2D 대신 OnCollisionEnter2D 사용 (물리 기반)
//        // CircleCollider2D가 isTrigger = false이므로 물리 충돌 발생
//    }

//    void OnCollisionEnter2D(Collision2D collision)
//    {
//        HandleCollision(collision.gameObject, collision.contacts[0].point);
//    }

//    void OnCollisionStay2D(Collision2D collision)
//    {
//        // 지속적인 충돌 처리 (예: 지속 데미지, 밀어내기 등)
//        HandleOngoingCollision(collision.gameObject);
//    }

//    void HandleCollision(GameObject other, Vector2 contactPoint)
//    {
//        // IHitReceiver를 구현한 오브젝트와의 충돌 처리
//        var hitReceiver = other.GetComponent<IHitReceiver>();
//        if (hitReceiver != null)
//        {
//            // 몬스터가 다른 IHitReceiver를 공격할 때
//            ProcessAttackCollision(hitReceiver, contactPoint);
//        }

//        // HitBox와의 충돌 처리
//        var hitbox = other.GetComponent<Hitbox>();
//        if (hitbox != null)
//        {
//            // 몬스터가 HitBox에 맞았을 때
//            ProcessHitboxCollision(hitbox, contactPoint);
//        }

//        // 다른 몬스터나 플레이어와의 충돌 처리
//        var otherUnit = other.GetComponent<Unit>();
//        if (otherUnit != null && otherUnit != this)
//        {
//            ProcessUnitCollision(otherUnit, contactPoint);
//        }
//    }

//    void HandleOngoingCollision(GameObject other)
//    {
//        // 지속적인 충돌에 대한 처리 (예: 밀어내기, 지속 데미지 등)
//        var otherUnit = other.GetComponent<Unit>();
//        if (otherUnit != null && otherUnit != this)
//        {
//            // 너무 가까이 있을 때 밀어내기
//            float distance = Vector2.Distance(transform.position, other.transform.position);
//            if (distance < collisionRadius * 0.8f)
//            {
//                Vector2 pushDirection = (transform.position - other.transform.position).normalized;
//                rb.AddForce(pushDirection * 2f, ForceMode2D.Impulse);
//            }
//        }
//    }

//    void ProcessAttackCollision(IHitReceiver hitReceiver, Vector2 contactPoint)
//    {
//        // 몬스터가 공격할 때의 처리
//        if (canAttack && hitReceiver != null)
//        {
//            // 공격 데미지 계산
//            float attackDamage = 10f; // 기본 공격력

//            // 넉백 방향 계산 (몬스터에서 타겟으로)
//            Vector2 knockbackDirection = (contactPoint - (Vector2)transform.position).normalized;
//            Vector2 knockbackForce = knockbackDirection * 5f; // 넉백 세기

//            // IHitReceiver의 ReceiveHit 호출
//            hitReceiver.ReceiveHit(attackDamage, knockbackForce, contactPoint);

//            Debug.Log($"{name}이(가) {hitReceiver.GetType().Name}을(를) 공격했습니다!");
//        }
//    }

//    void ProcessHitboxCollision(Hitbox hitbox, Vector2 contactPoint)
//    {
//        // HitBox에 맞았을 때의 처리
//        if (hitbox != null)
//        {
//            // HitBox의 정보를 가져와서 데미지 처리
//            float damage = hitbox.damage;
//            Vector2 knockback = hitbox.knockback;

//            // ReceiveHit 호출 (기존 로직 보존)
//            ReceiveHit(damage, knockback, contactPoint);

//            Debug.Log($"{name}이(가) HitBox에 맞았습니다! 데미지: {damage}");
//        }
//    }

//    void ProcessUnitCollision(Unit otherUnit, Vector2 contactPoint)
//    {
//        // 다른 유닛과의 충돌 처리
//        if (otherUnit != null)
//        {
//            // 충돌 시 약간의 밀어내기 효과
//            Vector2 pushDirection = (transform.position - otherUnit.transform.position).normalized;
//            rb.AddForce(pushDirection * 1f, ForceMode2D.Impulse);
//        }
//    }

//    public void ReceiveHit(float dmg, Vector2 knockback, Vector2 hitPoint)
//    {
//        Damage(dmg, null);

//        // 넉백(넘겨받은 벡터를 방향/세기로 분해)
//        if (knockback.sqrMagnitude > 0f)
//        {
//            var dir = knockback.normalized;
//            var force = knockback.magnitude;
//            ApplyKnockback(dir, force);
//        }

//        Debug.Log($"[Monster] {name} ReceiveHit dmg={dmg}, HP={currentHealth}/{maxHealth}");
//    }

//    // 몬스터가 다른 유닛에 데미지를 주는 메서드
//    public void DealDamage(Unit target, float amount)
//    {
//        if (target is IDamageable damageable)
//        {
//            Debug.Log($"[{name}] ===== 데미지 적용 시작 =====");
//            Debug.Log($"[{name}] 공격자: {name} (HP: {currentHealth}/{maxHealth})");
//            Debug.Log($"[{name}] 피격자: {target.name}");
//            Debug.Log($"[{name}] 적용 데미지: {amount}");

//            // 데미지 적용
//            damageable.Damage(amount, this);

//            Debug.Log($"[{name}] 데미지 적용 완료: {target.name}에게 {amount} 피해");
//            Debug.Log($"[{name}] ===== 데미지 적용 완료 =====");
//        }
//        else
//        {
//            Debug.LogWarning($"[{name}] 데미지 적용 실패: {target.name}은(는) IDamageable을 구현하지 않음");
//        }
//    }

//    public override void Damage(float amount, Unit source)
//    {
//        base.Damage(amount, source);
//    }

//    public override void Heal(float amount, Unit source)
//    {
//        base.Heal(amount, source);
//    }

//    // Rigidbody2D 접근자
//    public Rigidbody2D Rigidbody => rb;

//    // 공격 가능 여부 (충돌 처리에서 사용)
//    private bool canAttack => true; // 임시, 나중에 공격 쿨다운 등으로 개선

//    // 충돌 반지름 접근자
//    public float CollisionRadius => collisionRadius;
//}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hercules.StatsSystem;

/// <summary>
/// 몬스터: 기존 충돌/피격/IHitReceiver 로직을 유지하면서
/// 새 Stats/CombatMath 파이프라인을 사용합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Monster : Unit, IHitReceiver
{
    private MonsterController monsterController;

    [Header("Collision Settings")]
    [Tooltip("물리 접촉 반경(원형 콜라이더 기본값)")]
    public float collisionRadius = 0.5f;

    [Tooltip("히트 감지에 사용할 레이어(필요시)")]
    public LayerMask hitDetectionLayers = ~0; // 기본: 모든 레이어

    private Rigidbody2D rb;
    private CircleCollider2D circleCollider;

    // ─────────────────────────────────────────────────────────────
    // Unity
    // ─────────────────────────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake();

        monsterController = GetComponent<MonsterController>();

        // Rigidbody2D (Unit.body와 동일 인스턴스 보장)
        rb = body != null ? body : GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // 2D 탑다운/접촉용. 사이드뷰면 프로젝트 규칙에 맞게 조정
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 원형 콜라이더(물리 접촉용)
        circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider == null)
            circleCollider = gameObject.AddComponent<CircleCollider2D>();
        circleCollider.radius = collisionRadius;
        circleCollider.isTrigger = false; // 물리 충돌

        // 히트박스(공격 판정)는 Trigger Collider입니다.
        // 몬스터는 비-Trigger 콜라이더로 Trigger와 접촉 시 OnTriggerEnter2D가 호출됩니다.
    }

    // ─────────────────────────────────────────────────────────────
    // Physics Events
    // ─────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        var hitbox = other.GetComponent<Hitbox>();
        if (hitbox != null)
        {
            ProcessHitboxTrigger(hitbox, other.ClosestPoint(transform.position));
            return;
        }
    }

    // 물리 접촉 충돌: 밀어내기/접촉 공격 등
    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.gameObject,
            collision.contacts.Length > 0 ? collision.contacts[0].point : (Vector2)transform.position);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        HandleOngoingCollision(collision.gameObject);
    }

    // ─────────────────────────────────────────────────────────────
    // Collision Handlers
    // ─────────────────────────────────────────────────────────────

    // (Trigger) 플레이어 Hitbox에 맞았을 때
    private void ProcessHitboxTrigger(Hitbox hitbox, Vector2 contactPoint)
    {
        // Hitbox가 이미 CombatMath를 거쳐 Damage를 넣을 수도 있지만
        // 방어적으로 한 번 더 처리 라인을 둡니다(중복 방지는 Hitbox 설계에 따름).
        float damage = hitbox.damage;
        Vector2 knockback = hitbox.knockback;

        // 직접 피격 처리
        ReceiveHit(damage, knockback, contactPoint);

        if (hitbox.debugLog)
            Debug.Log($"{name} (Monster) was hit by Hitbox: dmg={damage}");
    }

    // (Collision) 다른 오브젝트와의 물리 충돌 처리
    private void HandleCollision(GameObject other, Vector2 contactPoint)
    {
        // IHitReceiver 대상에게 공격 시나리오 (근접 접촉형 몬스터의 바디 어택 등)
        var hitReceiver = other.GetComponent<IHitReceiver>();
        if (hitReceiver != null && hitReceiver != (IHitReceiver)this)
        {
            ProcessAttackCollision(hitReceiver, contactPoint);
        }

        // 유닛 간 밀어내기
        var otherUnit = other.GetComponent<Unit>();
        if (otherUnit != null && otherUnit != this)
        {
            ProcessUnitCollision(otherUnit, contactPoint);
        }
    }

    private void HandleOngoingCollision(GameObject other)
    {
        // 지속 밀어내기(겹침 완화)
        var otherUnit = other.GetComponent<Unit>();
        if (otherUnit != null && otherUnit != this)
        {
            float distance = Vector2.Distance(transform.position, other.transform.position);
            if (distance < collisionRadius * 0.8f)
            {
                Vector2 pushDirection = (transform.position - other.transform.position).normalized;
                rb.AddForce(pushDirection * 2f, ForceMode2D.Impulse);
            }
        }
    }

    // 몬스터의 접촉 공격 처리(예시)
    private void ProcessAttackCollision(IHitReceiver hitReceiver, Vector2 contactPoint)
    {
        // 공격 가능 조건(쿨/상태)은 프로젝트 룰에 맞게 교체
        bool canAttack = true;
        if (!canAttack || hitReceiver == null) return;

        // 기본 피해(예시): 10
        float baseDamage = 10f;

        // CombatMath 경유 (상대가 Unit/Stats를 갖고 있으면 스탯 반영)
        float finalDamage = baseDamage;
        var attackerStats = GetComponent<StatsBase>();

        Unit targetUnit = (hitReceiver as Component)?.GetComponent<Unit>();
        var defenderStats = targetUnit ? targetUnit.GetComponent<StatsBase>() : null;

        if (attackerStats && defenderStats)
            finalDamage = CombatMath.ComputeDamage(attackerStats, defenderStats, baseDamage);

        // 넉백 계산(몬스터→타겟 방향)
        Vector2 knockbackDirection = (contactPoint - (Vector2)transform.position).normalized;
        Vector2 knockbackForce = knockbackDirection * 5f;

        hitReceiver.ReceiveHit(finalDamage, knockbackForce, contactPoint);
        Debug.Log($"{name} attacked {hitReceiver.GetType().Name} for {finalDamage} dmg");
    }

    // 유닛 간 충돌 시 약간 밀어내기
    private void ProcessUnitCollision(Unit otherUnit, Vector2 contactPoint)
    {
        Vector2 pushDirection = (transform.position - otherUnit.transform.position).normalized;
        rb.AddForce(pushDirection * 1f, ForceMode2D.Impulse);
    }

    // ─────────────────────────────────────────────────────────────
    // IHitReceiver
    // ─────────────────────────────────────────────────────────────
    public void ReceiveHit(float dmg, Vector2 knockback, Vector2 hitPoint)
    {
        Damage(dmg, null); // 가해자 모르면 null

        if (knockback.sqrMagnitude > 0f)
        {
            var dir = knockback.normalized;
            var force = knockback.magnitude;
            ApplyKnockback(dir, force);
        }

        // 로그는 Stats 기준으로
        var s = GetComponent<StatsBase>();
        float hp = s ? s.CurrentHealth : 0f;
        float max = s ? s.MaxHealth.Value : 0f;
        Debug.Log($"[Monster] {name} ReceiveHit dmg={dmg}, HP={hp}/{max}");
    }

    // ─────────────────────────────────────────────────────────────
    // Backward-compat note
    // ─────────────────────────────────────────────────────────────
    // 주의: Unit에 이미 DealDamage(Unit,float)와 Rigidbody 프로퍼티(Shim)가 있으므로
    // 여기서 다시 정의하지 않습니다. (중복 정의 시 CS0108 경고 발생)
    // 필요하면 Unit의 public 멤버를 그대로 사용하세요.
}

