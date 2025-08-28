using UnityEngine;
using System.Collections;
using Hercules.StatsSystem;

[RequireComponent(typeof(CharacterMotor2D))]
public class AttackAbility : MonoBehaviour
{
    public enum AttackAimMode { Facing, Mouse }   // 전방/마우스 방향 스위치
    public enum RangeScaleMode { Multiply, Additive, Level } // 미세 조절 모드

    [Header("Config (ScriptableObject)")]
    public AttackConfig cfg;

    [Header("Hitbox (persistent child)")]
    public Hitbox hitbox; // 자식 히트박스(Trigger Collider)

    [Header("Aim")]
    [Tooltip("Facing: WASD 기반 전방 / Mouse: 마우스 방향")]
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

    [Header("Range Control")]
    [Tooltip("사거리/판정 크기를 코드에서 간단히 조절")]
    public bool overrideRange = false;

    [Tooltip("미세 조절 방식: 배수 / 가산 / 레벨 스텝")]
    public RangeScaleMode rangeMode = RangeScaleMode.Multiply;

    // Multiply 모드용
    [Tooltip("오프셋(전진 거리) 배수")]
    public float rangeOffsetMul = 1f;
    [Tooltip("히트박스 가로/세로 크기 배수")]
    public float rangeSizeMulX = 1f;
    public float rangeSizeMulY = 1f;

    // Additive 모드용
    [Tooltip("오프셋(전진 거리) 가산값(+m)")]
    public float rangeOffsetAdd = 0f;
    [Tooltip("히트박스 가로/세로 가산값(+m)")]
    public float rangeSizeAddX = 0f;
    public float rangeSizeAddY = 0f;

    // Level 모드용
    [Tooltip("레벨 단계(정수). 레벨당 step만큼 증가")]
    public int rangeLevel = 0;
    [Tooltip("레벨 1단계당 오프셋 증가량(+m)")]
    public float levelOffsetStep = 0.15f;
    [Tooltip("레벨 1단계당 가로/세로 크기 증가량(+m)")]
    public float levelSizeStepX = 0.15f;
    public float levelSizeStepY = 0.00f;

    [Tooltip("크기 확장 시 뒤쪽(플레이어 쪽) 가장자리를 고정하고 앞쪽으로만 늘림")]
    public bool forwardOnlyRangeScale = true;

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

            // ===== 기본값 =====
            Vector2 baseSize = cfg ? cfg.hitboxSize : new Vector2(1f, 1f);
            Vector2 baseOffset = cfg ? cfg.hitboxOffset : new Vector2(0.8f, 0f);

            // ===== 크기/오프셋 계산(미세 조절) =====
            Vector2 finalSize;
            float finalOffsetX;
            ComputeRange(baseSize, baseOffset, out finalSize, out finalOffsetX);

            if (box)
            {
                box.size = finalSize;
                box.offset = new Vector2(finalOffsetX, baseOffset.y);
            }

            // ── 조준 방향
            Vector2 dir;
            if (aimMode == AttackAimMode.Mouse && (attackCamera != null || Camera.main != null))
            {
                dir = GetMouseAimDir(attackCamera ? attackCamera : Camera.main, transform);
            }
            else
            {
                dir = (lastFacingDir.sqrMagnitude > 0.0001f) ? lastFacingDir : Vector2.right;
            }

            // 각도 계산
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // 상위 체인 X 미러 보정(좌우 반전된 부모 스케일 대응)
            if (IsMirroredX(hitbox.transform))
                ang = 180f - ang;

            // 회전 적용(중심 이동 없음, 오프셋은 로컬 +X 기준)
            hitbox.transform.localRotation = Quaternion.Euler(0f, 0f, ang);

            // ===== Arm → 활성 구간 =====
            float baseDamage = (cfg ? cfg.damage : 10f);
            Vector2 kb = new Vector2(cfg ? cfg.knockback : 6f, 0f);

            // on-hit 옵션 덮어쓰기
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
    // Range 계산 (공격 전용)
    // ─────────────────────────────────────────────────────────────
    void ComputeRange(Vector2 baseSize, Vector2 baseOffset, out Vector2 outSize, out float outOffsetX)
    {
        Vector2 size = baseSize;
        float offsetX = Mathf.Abs(baseOffset.x); // 앞(로컬 +X)만 사용

        if (overrideRange)
        {
            switch (rangeMode)
            {
                case RangeScaleMode.Multiply:
                    size = new Vector2(
                        baseSize.x * Mathf.Max(0.01f, rangeSizeMulX),
                        baseSize.y * Mathf.Max(0.01f, rangeSizeMulY)
                    );
                    offsetX *= Mathf.Max(0f, rangeOffsetMul);
                    break;

                case RangeScaleMode.Additive:
                    size = new Vector2(
                        Mathf.Max(0.01f, baseSize.x + rangeSizeAddX),
                        Mathf.Max(0.01f, baseSize.y + rangeSizeAddY)
                    );
                    offsetX = Mathf.Max(0f, Mathf.Abs(baseOffset.x) + rangeOffsetAdd);
                    break;

                case RangeScaleMode.Level:
                    size = new Vector2(
                        Mathf.Max(0.01f, baseSize.x + rangeLevel * levelSizeStepX),
                        Mathf.Max(0.01f, baseSize.y + rangeLevel * levelSizeStepY)
                    );
                    offsetX = Mathf.Max(0f, Mathf.Abs(baseOffset.x) + rangeLevel * levelOffsetStep);
                    break;
            }

            if (forwardOnlyRangeScale)
            {
                // 크기 증가분의 절반만큼 앞쪽으로 더 밀어, 뒤쪽 경계 고정
                float deltaX = size.x - baseSize.x; // >0: 커짐
                if (deltaX > 0f) offsetX += 0.5f * deltaX;
            }
        }

        outSize = size;
        outOffsetX = offsetX;
    }

    // ─────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────
    static Vector2 GetMouseAimDir(Camera cam, Transform player)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return Vector2.right;

        Vector3 mouseScreen = Input.mousePosition;
        Vector3 mouseWorld;

        if (cam.orthographic)
        {
            mouseWorld = cam.ScreenToWorldPoint(mouseScreen);
            mouseWorld.z = player.position.z;
        }
        else
        {
            float zDist = Mathf.Abs(cam.transform.position.z - player.position.z);
            mouseScreen.z = zDist;
            mouseWorld = cam.ScreenToWorldPoint(mouseScreen);
        }

        Vector2 dir = (Vector2)(mouseWorld - player.position);
        if (dir.sqrMagnitude < 1e-6f) return Vector2.right;
        return dir.normalized;
    }

    static bool IsMirroredX(Transform t)
    {
        return t != null && t.lossyScale.x < 0f;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!cfg) return;

        Vector2 baseSize = cfg.hitboxSize;
        Vector2 baseOffset = cfg.hitboxOffset;

        // 계산 재사용
        Vector2 size;
        float offsetX;
        ComputeRange(baseSize, baseOffset, out size, out offsetX);

        // 방향
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

        // 로컬 +X 기준 회전된 오프셋
        Vector2 rotated = Quaternion.Euler(0, 0, ang) * new Vector2(offsetX, baseOffset.y);
        Vector3 center = transform.TransformPoint(rotated);
        Vector3 gsize = new Vector3(size.x, size.y, 0.01f);

        Color fill = new Color(1f, 0.5f, 0f, 0.08f);
        Color line = new Color(1f, 0.5f, 0f, 0.9f);
        Gizmos.color = fill; Gizmos.DrawCube(center, gsize);
        Gizmos.color = line; Gizmos.DrawWireCube(center, gsize);
    }
#endif
}
