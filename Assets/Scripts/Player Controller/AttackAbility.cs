using UnityEngine;
using System.Collections;
using Hercules.StatsSystem;

[RequireComponent(typeof(CharacterMotor2D))]
public class AttackAbility : MonoBehaviour
{
    public enum AttackAimMode { Facing, Mouse }   // 조준 방식 스위치

    [Header("Config (ScriptableObject)")]
    public AttackConfig cfg;

    [Header("Hitbox (persistent child)")]
    public Hitbox hitbox; // 자식 히트박스(Trigger Collider) 참조

    [Header("Aim")]
    [Tooltip("Facing: 캐릭터 전방 기준 / Mouse: 마우스 방향 기준")]
    public AttackAimMode aimMode = AttackAimMode.Facing; // 인스펙터에서 전환

    [Header("On-Hit Flags")]
    [Tooltip("공격 적중 시 출혈을 적용(조준 모드와 무관)")]
    public bool applyBleedingOnHit = true;
    [Tooltip("출혈 지속 시간(초)")]
    public float bleedingDuration = 2f;

    [Tooltip("공격 시작 시 자신(공격자)에게 불굴(Indomitable)을 부여")]
    public bool applyIndomitableOnAttack = false;
    [Tooltip("불굴 지속 시간(초). 무적 부여 후 이 시간이 지나면 강제 사망(기존 불굴 설계 유지)")]
    public float indomitableOnAttackDuration = 5f;

    CharacterMotor2D motor;
    Unit ownerUnit;
    StatsBase ownerStats;
    SpriteRenderer sr;   // flipX 사용하는 프로젝트 대비

    bool cooling, busy;
    public bool IsBusy => busy;

    void Awake()
    {
        motor = GetComponent<CharacterMotor2D>();
        ownerUnit = GetComponent<Unit>();
        ownerStats = GetComponent<StatsBase>();
        sr = GetComponentInChildren<SpriteRenderer>(); // 없을 수도 있음

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

        // 0) (옵션) 공격 시작 시 불굴 부여
        if (applyIndomitableOnAttack && ownerUnit is Player p)
        {
            // 중복 방지: 사용 가능할 때만
            if (p.CanUseIndomitable)
            {
                p.TriggerIndomitable(indomitableOnAttackDuration);
            }
        }

        // 1) StartUp
        float startUp = (cfg ? cfg.startUp : 0f) * timeScale;
        if (startUp > 0f) yield return new WaitForSeconds(startUp);

        // 2) Active — 이 구간에서만 히트박스 On
        float active = (cfg ? cfg.active : 0.1f) * timeScale;

        if (hitbox)
        {
            // ── 히트박스 크기/방향/오프셋 설정
            var box = hitbox.GetComponent<BoxCollider2D>();
            if (box)
            {
                if (cfg) box.size = cfg.hitboxSize;

                Vector2 off = cfg ? cfg.hitboxOffset : Vector2.right; // 기본 오프셋(+X 방향)

                if (aimMode == AttackAimMode.Mouse && Camera.main != null)
                {
                    // 마우스 방향 기준: 자식을 마우스 각도로 회전
                    Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    Vector2 aimDir = ((Vector2)mouseWorld - (Vector2)transform.position);
                    if (aimDir.sqrMagnitude < 0.0001f) aimDir = Vector2.right;

                    float ang = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
                    hitbox.transform.localRotation = Quaternion.Euler(0f, 0f, ang);

                    // 오프셋은 로컬 +X 기준으로 고정(양수 유지)
                    box.offset = new Vector2(Mathf.Abs(off.x), off.y);
                }
                else
                {
                    // 캐릭터 전방 기준: 좌/우만 뒤집기
                    hitbox.transform.localRotation = Quaternion.identity;
                    float facing = GetFacingSign(); // flipX 또는 localScale.x
                    box.offset = new Vector2(off.x * facing, off.y);
                }
            }

            // ── 출혈 플래그/지속시간: 조준 모드와 무관하게 적용
            hitbox.applyBleeding = applyBleedingOnHit;
            hitbox.bleedingDuration = bleedingDuration;

            // 최종 데미지는 CombatMath에서만 계산(배수 선적용 금지)
            float baseDamage = (cfg ? cfg.damage : 10f);
            Vector2 kb = new Vector2(cfg ? cfg.knockback : 6f, 0f);

            // 1스윙-1히트 보장: Single 권장 (Continuous는 Hitbox에서 tick 관리)
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

    // ─────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────
    float GetFacingSign()
    {
        // SpriteRenderer.flipX를 우선 사용 (flipX == true => 보통 '왼쪽을 봄')
        if (sr != null) return sr.flipX ? -1f : 1f;

        // 없으면 localScale.x 기준
        float sx = transform.localScale.x;
        if (Mathf.Approximately(sx, 0f)) return 1f;
        return Mathf.Sign(sx);
    }

#if UNITY_EDITOR
    // 예상 판정 프리뷰(선택 시)
    void OnDrawGizmosSelected()
    {
        if (!cfg) return;

        Vector2 off = cfg.hitboxOffset;
        Vector3 center;
        Vector3 size = new Vector3(cfg.hitboxSize.x, cfg.hitboxSize.y, 0.01f);

        if (aimMode == AttackAimMode.Mouse && Camera.main != null)
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 aimDir = ((Vector2)mouseWorld - (Vector2)transform.position).normalized;
            float ang = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;

            // 로컬 +X 기준 오프셋을 회전시킨 월드 위치
            Vector2 rotated = Quaternion.Euler(0, 0, ang) * new Vector2(Mathf.Abs(off.x), off.y);
            center = transform.TransformPoint(rotated);
        }
        else
        {
            float facing = GetFacingSign();
            center = transform.TransformPoint(new Vector3(off.x * facing, off.y, 0f));
        }

        Color fill = new Color(1f, 0.5f, 0f, 0.08f);
        Color line = new Color(1f, 0.5f, 0f, 0.9f);
        Gizmos.color = fill; Gizmos.DrawCube(center, size);
        Gizmos.color = line; Gizmos.DrawWireCube(center, size);
    }
#endif
}
