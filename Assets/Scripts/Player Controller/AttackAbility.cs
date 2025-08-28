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
    [Tooltip("Facing: WASD 기반 전방 / Mouse: 마우스 방향 (사거리는 고정)")]
    public AttackAimMode aimMode = AttackAimMode.Facing;

    [Tooltip("명시적으로 사용할 카메라(비어있으면 Camera.main)")]
    public Camera attackCamera;

    [Header("On-Hit Overrides (optional)")]
    [Tooltip("체크 시, 이번 공격 동안 Hitbox의 on-hit 설정을 덮어씀")]
    public bool overrideOnHitOptions = true;
    public bool onHitBleeding = true;
    public float onHitBleedingDuration = 2f;
    public bool onHitBleedingStack = false;
    public int onHitBleedingStacks = 1;
    public float onHitBleedingStackDuration = 3f;
    public bool onHitStun = false;
    public float onHitStunTime = 0.4f;
    public bool onHitHitstop = false;
    public float onHitHitstopTime = 0.05f;
    public float onHitHitstopScale = 0.05f;

    CharacterMotor2D motor;
    Unit ownerUnit;
    StatsBase ownerStats;

    // ── Facing 모드용 상태
    int lastHorizSign = +1;            // 마지막 A/D
    int lastVertSign = +1;            // 마지막 W/S
    Vector2 lastFacingDir = Vector2.right; // 이번 프레임 공격에 사용할 바라보기

    bool cooling, busy;
    public bool IsBusy => busy;

    void Awake()
    {
        motor = GetComponent<CharacterMotor2D>();
        ownerUnit = GetComponent<Unit>();
        ownerStats = GetComponent<StatsBase>();

        if (!attackCamera) attackCamera = Camera.main;

        if (!hitbox) Debug.LogError($"{name}: AttackAbility.hitbox 가 비었습니다.");
        if (hitbox) hitbox.Disarm(); // 시작 시 꺼두기
    }

    void Update()
    {
        // ── 키다운(마지막 클릭) 추적
        if (Input.GetKeyDown(KeyCode.A)) lastHorizSign = -1;
        if (Input.GetKeyDown(KeyCode.D)) lastHorizSign = +1;
        if (Input.GetKeyDown(KeyCode.W)) lastVertSign = +1;
        if (Input.GetKeyDown(KeyCode.S)) lastVertSign = -1;

        // ── 현재 유지 입력 상태
        bool aHeld = Input.GetKey(KeyCode.A);
        bool dHeld = Input.GetKey(KeyCode.D);
        bool wHeld = Input.GetKey(KeyCode.W);
        bool sHeld = Input.GetKey(KeyCode.S);

        // 규칙:
        // 1) A/D가 눌려 있으면 수평만 사용(수직 무시)
        if (aHeld ^ dHeld)
        {
            lastFacingDir = new Vector2(dHeld ? +1f : -1f, 0f);
        }
        else if (!aHeld && !dHeld)
        {
            // 2) 수평이 중립일 때만 W/S 적용
            if (wHeld ^ sHeld)
            {
                lastFacingDir = new Vector2(0f, wHeld ? +1f : -1f);
            }
            else if (wHeld && sHeld)
            {
                // 동시 입력이면 마지막 수직 클릭 우선
                lastFacingDir = new Vector2(0f, lastVertSign >= 0 ? +1f : -1f);
            }
            else
            {
                // 3) 완전 중립이면 마지막 A/D 기준
                lastFacingDir = new Vector2(lastHorizSign >= 0 ? +1f : -1f, 0f);
            }
        }
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

            Vector2 off = cfg ? cfg.hitboxOffset : Vector2.right; // +X 기준 오프셋

            // ── 조준 방향
            Vector2 dir;
            if (aimMode == AttackAimMode.Mouse && (attackCamera != null || Camera.main != null))
            {
                dir = GetMouseAimDir(attackCamera ? attackCamera : Camera.main, transform);
            }
            else
            {
                // Facing: WASD 규칙으로 계산된 lastFacingDir 사용
                dir = (lastFacingDir.sqrMagnitude > 0.0001f) ? lastFacingDir : Vector2.right;
            }

            // 각도 계산
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // 상위 체인 X 미러 보정(좌우 반전된 부모 스케일 대응)
            if (IsMirroredX(hitbox.transform))
            {
                ang = 180f - ang;
            }

            // 회전만 적용(중심 이동 없음). 사거리 자체는 고정: offset.x의 절댓값을 사용
            hitbox.transform.localRotation = Quaternion.Euler(0f, 0f, ang);
            if (box)
                box.offset = new Vector2(Mathf.Abs(off.x), off.y);

            // ===== Arm → 활성 구간 =====
            float baseDamage = (cfg ? cfg.damage : 10f);
            Vector2 kb = new Vector2(cfg ? cfg.knockback : 6f, 0f);

            // ★ 이 한 줄로 “이번 공격”의 on-hit 출혈 옵션을 확실히 적용(선택)
            if (overrideOnHitOptions)
            {
                hitbox.applyBleeding = onHitBleeding;
                hitbox.bleedingDuration = onHitBleedingDuration;
                hitbox.applyBleedingStack = onHitBleedingStack;
                hitbox.bleedingStacks = onHitBleedingStacks;
                hitbox.bleedingStackDuration = onHitBleedingStackDuration;
                hitbox.applyStun = onHitStun;
                hitbox.stunTime = onHitStunTime;
                hitbox.applyHitstop = onHitHitstop;
                hitbox.hitstopTime = onHitHitstopTime;
                hitbox.hitstopScale = onHitHitstopScale;
            }

            hitbox.Arm(ownerUnit, ownerStats, baseDamage, kb, Hitbox.HitMode.Single);

            if (active > 0f) yield return new WaitForSeconds(active);

            hitbox.Disarm();

            // 다음 공격에 영향 없도록 회전 복원
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

    // 마우스 → 월드 → 플레이어 기준 방향(정규화)
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

    // 상위 체인 어딘가에 X 미러가 있으면 true
    static bool IsMirroredX(Transform t)
    {
        return t != null && t.lossyScale.x < 0f;
    }

#if UNITY_EDITOR
    // 예상 판정 프리뷰(선택 시) — 실제 로직과 동일한 보정 사용
    void OnDrawGizmosSelected()
    {
        if (!cfg) return;

        Vector2 off = cfg.hitboxOffset;
        Vector3 center;
        Vector3 size = new Vector3(cfg.hitboxSize.x, cfg.hitboxSize.y, 0.01f);

        Vector2 dir;
        if (aimMode == AttackAimMode.Mouse && (attackCamera != null || Camera.main != null))
        {
            var cam = attackCamera ? attackCamera : Camera.main;
            dir = GetMouseAimDir(cam, transform);
        }
        else
        {
            dir = (lastFacingDir.sqrMagnitude > 0.0001f) ? lastFacingDir : Vector2.right;
        }

        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (IsMirroredX(hitbox != null ? hitbox.transform : transform))
            ang = 180f - ang;

        Vector2 rotated = Quaternion.Euler(0, 0, ang) * new Vector2(Mathf.Abs(off.x), off.y);
        center = transform.TransformPoint(rotated);

        Color fill = new Color(1f, 0.5f, 0f, 0.08f);
        Color line = new Color(1f, 0.5f, 0f, 0.9f);
        Gizmos.color = fill; Gizmos.DrawCube(center, size);
        Gizmos.color = line; Gizmos.DrawWireCube(center, size);
    }
#endif
}
