using UnityEngine;
using System.Collections;

public class AttackAbility
{
    readonly MonoBehaviour runner;
    readonly CharacterMotor2D motor;
    readonly AttackConfig cfg;
    readonly Transform root;

    bool _cooling, _busy;
    public bool IsBusy => _busy;

    public AttackAbility(MonoBehaviour runner, CharacterMotor2D motor, AttackConfig cfg, Transform root)
    { this.runner = runner; this.motor = motor; this.cfg = cfg; this.root = root; }

    public void TryStart()
    {
        if (_busy || _cooling) return;
        runner.StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        _busy = true;
        _cooling = true;

        int hitCount = 0;

        // 공격 시작
        Debug.Log("attack");

        // StartUp
        yield return new WaitForSeconds(cfg.startUp);

        // Active: 히트박스 생성
        Vector2 off = cfg.hitboxOffset;
        if (!motor.FacingRight) off.x = -off.x;

        var go = new GameObject("Hitbox");
        int layer = LayerMask.NameToLayer("PlayerAttack");
        if (layer >= 0) go.layer = layer;

        go.transform.SetParent(root);
        go.transform.position = (Vector2)root.position + off;

        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = cfg.hitboxSize;

        var hb = go.AddComponent<Hitbox>();
        hb.damage = cfg.damage;
        hb.knockback = new Vector2(motor.FacingRight ? cfg.knockback : -cfg.knockback, 2f);

        // ↓ 디버그 설정/콜백
        hb.attackerName = root.name;
        hb.debugLog = true;
        hb.OnHit += (col, dmg) =>
        {
            hitCount++;
            // 필요시 여기서 추가 로직 가능(히트스톱, 콤보 등)
        };

        // Active 유지
        yield return new WaitForSeconds(cfg.active);
        Object.Destroy(go);

        // Recovery
        yield return new WaitForSeconds(cfg.recovery);

        _busy = false;

        // 공격 종료 + 이번 공격에서 총 몇 번 맞았는지
        Debug.Log($"attack end (hits={hitCount})");

        // Cooldown
        yield return new WaitForSeconds(cfg.cooldown);
        _cooling = false;
    }
}
