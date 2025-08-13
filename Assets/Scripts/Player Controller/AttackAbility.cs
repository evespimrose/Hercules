//using UnityEngine;
//using System.Collections;
//using System.Diagnostics;

//public class AttackAbility : MonoBehaviour
//{
//    readonly MonoBehaviour runner;
//    readonly CharacterMotor2D motor;
//    readonly AttackConfig cfg;
//    readonly Transform root;

//    bool _cooling, _busy;
//    public bool IsBusy => _busy;

//    public AttackAbility(MonoBehaviour runner, CharacterMotor2D motor, AttackConfig cfg, Transform root)
//    { this.runner = runner; this.motor = motor; this.cfg = cfg; this.root = root; }

//    public void TryStart()
//    {
//        if (_busy || _cooling) return;
//        runner.StartCoroutine(AttackRoutine());
//    }

//    IEnumerator AttackRoutine()
//    {
//        _busy = true;
//        _cooling = true;

//        int hitCount = 0;

//        Debug.Log("attack");

//        // StartUp
//        yield return new WaitForSeconds(cfg.startUp);

//        // Active: ��Ʈ�ڽ� ����
//        Vector2 off = cfg.hitboxOffset;
//        if (!motor.FacingRight) off.x = -off.x;

//        var go = new GameObject("Hitbox");
//        int layer = LayerMask.NameToLayer("PlayerAttack");
//        if (layer >= 0) go.layer = layer;

//        go.transform.SetParent(root);
//        go.transform.position = (Vector2)root.position + off;

//        var box = go.AddComponent<BoxCollider2D>();
//        box.isTrigger = true;
//        box.size = cfg.hitboxSize;

//        var hb = go.AddComponent<Hitbox>();
//        hb.damage = cfg.damage;
//        hb.knockback = new Vector2(motor.FacingRight ? cfg.knockback : -cfg.knockback, 2f);

//        // �� ����� ����/�ݹ�
//        hb.attackerName = root.name;
//        hb.debugLog = true;
//        hb.OnHit += (col, dmg) =>
//        {
//            hitCount++;
//        };

//        // Active ����
//        yield return new WaitForSeconds(cfg.active);
//        Destroy(go);

//        // Recovery
//        yield return new WaitForSeconds(cfg.recovery);

//        _busy = false;

//        // ���� ���� + �̹� ���ݿ��� �� �� �� �¾Ҵ���
//        Debug.Log($"attack end (hits={hitCount})");

//        // Cooldown
//        yield return new WaitForSeconds(cfg.cooldown);
//        _cooling = false;
//    }
//}

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

        hb.OnHit += (col, dmg) => { hitCount++; };

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
