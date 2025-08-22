using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monster : Unit, IHitReceiver
{
    private MonsterController monsterController;
    private Rigidbody2D rb;
    
    [Header("Collision Settings")]
    public float collisionRadius = 0.5f;
    public LayerMask hitDetectionLayers = -1; // 모든 레이어와 충돌
    
    private CircleCollider2D circleCollider;
    private List<Collider2D> overlappingColliders = new List<Collider2D>();

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
        
        // Rigidbody2D 컴포넌트 가져오기 또는 생성
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f; // 2D 플랫포머용 중력 비활성화
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // 회전 고정
            rb.drag = 0.5f; // 약간의 저항 추가
        }
        
        // CircleCollider2D 컴포넌트 가져오기 또는 생성
        circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider == null)
        {
            circleCollider = gameObject.AddComponent<CircleCollider2D>();
            circleCollider.radius = collisionRadius;
            circleCollider.isTrigger = false; // 물리 충돌 활성화
        }
        
        // 충돌 이벤트 등록
        SetupCollisionDetection();
    }
    
    void SetupCollisionDetection()
    {
        // OnTriggerEnter2D 대신 OnCollisionEnter2D 사용 (물리 기반)
        // CircleCollider2D가 isTrigger = false이므로 물리 충돌 발생
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.gameObject, collision.contacts[0].point);
    }
    
    void OnCollisionStay2D(Collision2D collision)
    {
        // 지속적인 충돌 처리 (예: 지속 데미지, 밀어내기 등)
        HandleOngoingCollision(collision.gameObject);
    }
    
    void HandleCollision(GameObject other, Vector2 contactPoint)
    {
        // IHitReceiver를 구현한 오브젝트와의 충돌 처리
        var hitReceiver = other.GetComponent<IHitReceiver>();
        if (hitReceiver != null)
        {
            // 몬스터가 다른 IHitReceiver를 공격할 때
            ProcessAttackCollision(hitReceiver, contactPoint);
        }
        
        // HitBox와의 충돌 처리
        var hitbox = other.GetComponent<Hitbox>();
        if (hitbox != null)
        {
            // 몬스터가 HitBox에 맞았을 때
            ProcessHitboxCollision(hitbox, contactPoint);
        }
        
        // 다른 몬스터나 플레이어와의 충돌 처리
        var otherUnit = other.GetComponent<Unit>();
        if (otherUnit != null && otherUnit != this)
        {
            ProcessUnitCollision(otherUnit, contactPoint);
        }
    }
    
    void HandleOngoingCollision(GameObject other)
    {
        // 지속적인 충돌에 대한 처리 (예: 밀어내기, 지속 데미지 등)
        var otherUnit = other.GetComponent<Unit>();
        if (otherUnit != null && otherUnit != this)
        {
            // 너무 가까이 있을 때 밀어내기
            float distance = Vector2.Distance(transform.position, other.transform.position);
            if (distance < collisionRadius * 0.8f)
            {
                Vector2 pushDirection = (transform.position - other.transform.position).normalized;
                rb.AddForce(pushDirection * 2f, ForceMode2D.Impulse);
            }
        }
    }
    
    void ProcessAttackCollision(IHitReceiver hitReceiver, Vector2 contactPoint)
    {
        // 몬스터가 공격할 때의 처리
        if (canAttack && hitReceiver != null)
        {
            // 공격 데미지 계산
            float attackDamage = 10f; // 기본 공격력
            
            // 넉백 방향 계산 (몬스터에서 타겟으로)
            Vector2 knockbackDirection = (contactPoint - (Vector2)transform.position).normalized;
            Vector2 knockbackForce = knockbackDirection * 5f; // 넉백 세기
            
            // IHitReceiver의 ReceiveHit 호출
            hitReceiver.ReceiveHit(attackDamage, knockbackForce, contactPoint);
            
            Debug.Log($"{name}이(가) {hitReceiver.GetType().Name}을(를) 공격했습니다!");
        }
    }
    
    void ProcessHitboxCollision(Hitbox hitbox, Vector2 contactPoint)
    {
        // HitBox에 맞았을 때의 처리
        if (hitbox != null)
        {
            // HitBox의 정보를 가져와서 데미지 처리
            float damage = hitbox.damage;
            Vector2 knockback = hitbox.knockback;
            
            // ReceiveHit 호출 (기존 로직 보존)
            ReceiveHit(damage, knockback, contactPoint);
            
            Debug.Log($"{name}이(가) HitBox에 맞았습니다! 데미지: {damage}");
        }
    }
    
    void ProcessUnitCollision(Unit otherUnit, Vector2 contactPoint)
    {
        // 다른 유닛과의 충돌 처리
        if (otherUnit != null)
        {
            // 충돌 시 약간의 밀어내기 효과
            Vector2 pushDirection = (transform.position - otherUnit.transform.position).normalized;
            rb.AddForce(pushDirection * 1f, ForceMode2D.Impulse);
        }
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
            Debug.Log($"[{name}] ===== 데미지 적용 시작 =====");
            Debug.Log($"[{name}] 공격자: {name} (HP: {currentHealth}/{maxHealth})");
            Debug.Log($"[{name}] 피격자: {target.name}");
            Debug.Log($"[{name}] 적용 데미지: {amount}");
            
            // 데미지 적용
            damageable.Damage(amount, this);
            
            Debug.Log($"[{name}] 데미지 적용 완료: {target.name}에게 {amount} 피해");
            Debug.Log($"[{name}] ===== 데미지 적용 완료 =====");
        }
        else
        {
            Debug.LogWarning($"[{name}] 데미지 적용 실패: {target.name}은(는) IDamageable을 구현하지 않음");
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
    
    // Rigidbody2D 접근자
    public Rigidbody2D Rigidbody => rb;
    
    // 공격 가능 여부 (충돌 처리에서 사용)
    private bool canAttack => true; // 임시, 나중에 공격 쿨다운 등으로 개선
    
    // 충돌 반지름 접근자
    public float CollisionRadius => collisionRadius;
}
