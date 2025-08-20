using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterMotor2D))]
public class AttackAbilityMB : MonoBehaviour
{
    public AttackConfig cfg;

    CharacterMotor2D motor;
    bool cooling, busy;
    public bool IsBusy => busy;

    void Awake()
    {
        motor = GetComponent<CharacterMotor2D>();
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

        // 공격 시작 로그
        Debug.Log("attack");

        // StartUp
        yield return new WaitForSeconds(cfg.startUp);

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
        hb.damage = cfg.damage;
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
        yield return new WaitForSeconds(cfg.active);
        Destroy(go);

        // Recovery
        yield return new WaitForSeconds(cfg.recovery);

        busy = false;

        // 공격 종료 로그(히트 합계)
        Debug.Log($"attack end (hits={hitCount})");

        // Cooldown
        yield return new WaitForSeconds(cfg.cooldown);
        cooling = false;
    }
}
