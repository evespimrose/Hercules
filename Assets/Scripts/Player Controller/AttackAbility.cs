using UnityEngine;
using System.Collections;
using Hercules.StatsSystem;

[RequireComponent(typeof(CharacterMotor2D))]
public class AttackAbility : MonoBehaviour
{
    [Header("Config (ScriptableObject)")]
    public AttackConfig cfg;

    [Header("Hitbox (persistent child)")]
    public Hitbox hitbox; // 자식 히트박스(Trigger Collider) 참조

    CharacterMotor2D motor;
    Unit ownerUnit;
    StatsBase ownerStats;

    bool cooling, busy;
    public bool IsBusy => busy;

    void Awake()
    {
        motor = GetComponent<CharacterMotor2D>();
        ownerUnit = GetComponent<Unit>();
        ownerStats = GetComponent<StatsBase>();

        if (!hitbox) Debug.LogError($"{name}: AttackAbility.hitbox 가 비었습니다.");
        if (hitbox) hitbox.Disarm(); // 시작 시 꺼두기
    }

    public void TryStart()
    {
        if (cooling || busy) return;
        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        cooling = true;
        busy = true;

        float timeScale = ownerUnit != null ? ownerUnit.AttackTimeScale : 1f;

        // 1) StartUp
        float startUp = (cfg ? cfg.startUp : 0f) * timeScale;
        if (startUp > 0f) yield return new WaitForSeconds(startUp);

        // 2) Active — 이 구간에서만 히트박스 On
        float active = (cfg ? cfg.active : 0.1f) * timeScale;

        if (hitbox)
        {
            // 히트박스 크기/오프셋 세팅(자식 BoxCollider2D 기준)
            var box = hitbox.GetComponent<BoxCollider2D>();
            if (box)
            {
                if (cfg) box.size = cfg.hitboxSize;
                float facing = Mathf.Sign(transform.localScale.x == 0f ? 1f : transform.localScale.x);
                Vector2 off = cfg ? cfg.hitboxOffset : Vector2.right;
                box.offset = new Vector2(off.x * facing, off.y);
            }

            float baseDamage = (cfg ? cfg.damage : 10f);

            Vector2 kb = new Vector2(cfg ? cfg.knockback : 6f, 0f);
            // 배수는 CombatMath에서만 적용: 베이스만 넘김
            hitbox.Arm(ownerUnit, ownerStats, baseDamage, kb, Hitbox.HitMode.Single);


            if (active > 0f) yield return new WaitForSeconds(active);

            hitbox.Disarm();
        }
        else
        {
            if (active > 0f) yield return new WaitForSeconds(active);
        }

        // 3) Recovery
        float recovery = (cfg ? cfg.recovery : 0f) * timeScale;
        if (recovery > 0f) yield return new WaitForSeconds(recovery);

        busy = false;

        // 4) Cooldown
        float cooldown = (cfg ? cfg.cooldown : 0f) * timeScale;
        if (cooldown > 0f) yield return new WaitForSeconds(cooldown);
        cooling = false;
    }

#if UNITY_EDITOR
    // ── Gizmo: AttackConfig 기준 "예상 판정" 미리보기 ─────────────────────
    void OnDrawGizmosSelected()
    {
        if (!cfg) return;

        float facing = Mathf.Sign(transform.localScale.x == 0f ? 1f : transform.localScale.x);
        Vector2 off = cfg.hitboxOffset;
        Vector3 center = transform.TransformPoint(new Vector3(off.x * facing, off.y, 0f));
        Vector3 size = new Vector3(cfg.hitboxSize.x, cfg.hitboxSize.y, 0.01f);

        // 반투명 채움 + 외곽선
        var prev = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.identity;

        Color fill = new Color(1f, 0.5f, 0f, 0.08f);
        Color line = new Color(1f, 0.5f, 0f, 0.9f);

        Gizmos.color = fill; Gizmos.DrawCube(center, size);
        Gizmos.color = line; Gizmos.DrawWireCube(center, size);

        Gizmos.matrix = prev;
    }
#endif
}
