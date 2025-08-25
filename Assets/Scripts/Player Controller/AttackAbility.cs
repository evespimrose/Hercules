using UnityEngine;
using System.Collections;
using Hercules.StatsSystem;

[RequireComponent(typeof(CharacterMotor2D))]
public class AttackAbility : MonoBehaviour
{
    public enum AttackAimMode { Facing, Mouse }   // 전방/마우스 방향 스위치

    [Header("Config (ScriptableObject)")]
    public AttackConfig cfg;

    [Header("Hitbox (persistent child)")]
    public Hitbox hitbox; // 자식 히트박스(Trigger Collider)

    [Header("Aim")]
    [Tooltip("Facing: 캐릭터 전방 기준 / Mouse: 마우스 방향 기준(사거리는 고정)")]
    public AttackAimMode aimMode = AttackAimMode.Facing;

    [Tooltip("명시적으로 사용할 카메라(비어있으면 Camera.main)")]
    public Camera attackCamera;

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

        if (!attackCamera) attackCamera = Camera.main;

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

        float timeScale = (ownerUnit != null) ? ownerUnit.AttackTimeScale : 1f;

        // 1) StartUp
        float startUp = (cfg ? cfg.startUp : 0f) * timeScale;
        if (startUp > 0f) yield return new WaitForSeconds(startUp);

        // 2) Active — 이 구간에서만 히트박스 On
        float active = (cfg ? cfg.active : 0.1f) * timeScale;

        if (hitbox)
        {
            var box = hitbox.GetComponent<BoxCollider2D>();
            if (box && cfg) box.size = cfg.hitboxSize;

            Vector2 off = cfg ? cfg.hitboxOffset : Vector2.right; // 기본 오프셋(+X 방향)

            if (aimMode == AttackAimMode.Mouse && (attackCamera != null || Camera.main != null))
            {
                // ★ 마우스 방향 계산(플레이어 기준): 회전만 적용, 중심 이동 없음
                Vector2 aimDir = GetMouseAimDir(attackCamera ? attackCamera : Camera.main, transform);

                float ang = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
                hitbox.transform.localRotation = Quaternion.Euler(0f, 0f, ang);

                // 사거리 고정: 로컬 +X로 오프셋.x(양수)만큼, Y는 설정 그대로
                if (box) box.offset = new Vector2(Mathf.Abs(off.x), off.y);
            }
            else
            {
                // ★ 캐릭터 전방 기준: 좌/우만 뒤집기(회전 없음)
                hitbox.transform.localRotation = Quaternion.identity;

                float facing = GetFacingSign(); // flipX 또는 localScale.x
                if (box) box.offset = new Vector2(off.x * facing, off.y);
            }

            // 최종 데미지는 CombatMath에서만 계산(배수 선적용 금지)
            float baseDamage = (cfg ? cfg.damage : 10f);
            Vector2 kb = new Vector2(cfg ? cfg.knockback : 6f, 0f);

            hitbox.Arm(ownerUnit, ownerStats, baseDamage, kb, Hitbox.HitMode.Single);

            if (active > 0f) yield return new WaitForSeconds(active);

            hitbox.Disarm();

            // 다음 공격에 영향 없도록(모드 전환 대비) 마우스 모드면 회전 복원
            if (aimMode == AttackAimMode.Mouse)
                hitbox.transform.localRotation = Quaternion.identity;
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

    // 마우스 → 월드 → 플레이어 기준 방향(정규화) 반환
    static Vector2 GetMouseAimDir(Camera cam, Transform player)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return Vector2.right;

        Vector3 mouseScreen = Input.mousePosition;
        Vector3 mouseWorld;

        if (cam.orthographic)
        {
            // 직교 카메라: XY 그대로, z만 플레이어 z에 맞춤
            mouseWorld = cam.ScreenToWorldPoint(mouseScreen);
            mouseWorld.z = player.position.z;
        }
        else
        {
            // 원근 카메라: 카메라→플레이어 평면까지의 거리로 z 지정
            float zDist = Mathf.Abs(cam.transform.position.z - player.position.z);
            mouseScreen.z = zDist;
            mouseWorld = cam.ScreenToWorldPoint(mouseScreen);
        }

        Vector2 dir = (Vector2)(mouseWorld - player.position);
        if (dir.sqrMagnitude < 1e-6f) return Vector2.right;
        return dir.normalized;
    }

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

        if (aimMode == AttackAimMode.Mouse && (attackCamera != null || Camera.main != null))
        {
            var cam = attackCamera ? attackCamera : Camera.main;
            Vector2 aimDir = GetMouseAimDir(cam, transform);
            float ang = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;

            // 로컬 +X 기준 오프셋.x(양수)만큼 회전하여 월드 위치 미리보기(중심 이동은 안 함)
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
