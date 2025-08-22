using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterMotor2D))]
public class AttackAbility : MonoBehaviour
{
    public AttackConfig cfg;

    CharacterMotor2D motor;
    bool cooling, busy;
    public bool IsBusy => busy;

    // 공격 배수 적용을 위해 Unit 참조
    Unit ownerUnit;

    void Awake()
    {
        motor = GetComponent<CharacterMotor2D>();
        ownerUnit = GetComponent<Unit>(); // 없으면 null 허용
    }

    public void TryStart()
    {
        if (busy || cooling) return;
        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        busy = true; cooling = true;
        int hitCount = 0;

        // 배수 캐싱 (없으면 1)
        float timeScale = (ownerUnit != null) ? ownerUnit.AttackTimeScale : 1f;          // >1이면 느려짐
        float dmgMul = (ownerUnit != null) ? ownerUnit.AttackDamageMultiplier : 1f;   // <1이면 약해짐

        // 공격 시작 로그
        Debug.Log($"attack (timeScale={timeScale:F2}, dmgMul={dmgMul:F2})");

        // StartUp
        // yield return new WaitForSeconds(cfg.startUp);
        float startUp = (cfg != null ? cfg.startUp : 0f) * timeScale;   // 시간 배수 적용
        if (startUp > 0f) yield return new WaitForSeconds(startUp);

        // Active: 히트박스 생성
        Vector2 off = cfg.hitboxOffset;
        if (!motor.FacingRight) off.x = -off.x;

        var go = new GameObject("Hitbox");
        int layer = LayerMask.NameToLayer("PlayerAttack");
        if (layer >= 0) go.layer = layer;

        go.transform.SetParent(transform);
        go.transform.position = (Vector2)transform.position + off;

        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = cfg.hitboxSize;

        var hb = go.AddComponent<Hitbox>();
        hb.attackerName = name;
        hb.debugLog = true;

        // hb.damage = cfg.damage;
        hb.damage = (cfg != null ? cfg.damage : 0f) * dmgMul;           // 데미지 배수 적용

        hb.knockback = new Vector2(motor.FacingRight ? cfg.knockback : -cfg.knockback, 2f);

        hb.OnHit += (col, dmg) =>
        {
            hitCount++;

            // 출혈 부여 시도: 피격자에서 Unit 찾기
            var victim =
                   col.GetComponentInParent<Unit>()
                ?? col.GetComponent<Unit>()
                ?? col.GetComponentInChildren<Unit>();

            if (victim != null)
            {
                //victim.Mesmerize(5f, Unit.Buff.Bleeding);  // 5초 출혈(0.5초마다 7)
                victim.Mesmerize(5f, Unit.Buff.BleedingStack);  // 5초 출혈(0.5초마다 7) + (스택당)1
                Debug.Log($"[Bleeding] applied to {victim.name}");
            }
            else
            {
                // 디버그 : 어떤 오브젝트에서 실패했는지, 가지고 있는 컴포넌트 목록
                var comps = col.GetComponents<Component>();
                string compNames = string.Join(", ", System.Array.ConvertAll(comps, c => c ? c.GetType().Name : "null"));
                string layerName = LayerMask.LayerToName(col.gameObject.layer);
                Debug.LogWarning($"[Bleeding] NO Unit on hit target. collider={col.name}, layer={layerName}, comps=[{compNames}]");
            }
        };

        // Active 유지
        // yield return new WaitForSeconds(cfg.active);
        float active = (cfg != null ? cfg.active : 0f) * timeScale;     // 시간 배수 적용
        if (active > 0f) yield return new WaitForSeconds(active);
        Destroy(go);

        // Recovery
        // yield return new WaitForSeconds(cfg.recovery);
        float recovery = (cfg != null ? cfg.recovery : 0f) * timeScale; // 시간 배수 적용
        if (recovery > 0f) yield return new WaitForSeconds(recovery);

        busy = false;

        // 공격 종료 로그(히트 합계)
        Debug.Log($"attack end (hits={hitCount})");

        // Cooldown
        // yield return new WaitForSeconds(cfg.cooldown);
        float cooldown = (cfg != null ? cfg.cooldown : 0f) * timeScale; // 시간 배수 적용
        if (cooldown > 0f) yield return new WaitForSeconds(cooldown);
        cooling = false;
    }
}
