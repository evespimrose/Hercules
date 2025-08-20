//using UnityEngine; 
//[RequireComponent(typeof(CharacterMotor2D))]
//public class PlayerController : MonoBehaviour
//{
//    [Header("Configs")]
//    public MovementConfig moveCfg;
//    public JumpConfig jumpCfg;
//    public DashConfig dashCfg;
//    public AttackConfig atkCfg;
//
//    CharacterMotor2D motor;
//    IInputSource input; 
//
//    MoveAbility move;
//    JumpAbility jump;
//    DashAbility dash;
//    AttackAbility attack;
//
//    float lastDownTapTime = -999f;  // S ������ ����
//    bool movementLocked => (dash != null && dash.IsDashing) || (attack != null && attack.IsBusy);
//
//    void Awake()
//    {
//        motor = GetComponent<CharacterMotor2D>();
//        input = GetComponent<IInputSource>(); // UnityInputSource �Բ� ���̱� 
//        move = new MoveAbility(motor, moveCfg);
//        jump = new JumpAbility(motor, jumpCfg);
//        dash = new DashAbility(this, motor, dashCfg);
//        attack = new AttackAbility(this, motor, atkCfg, transform);
//    }
//
//    void Update()
//    {
//        // ���� (Space)
//        if (input.JumpDown) jump.OnJumpPressed();
//        if (input.JumpUp) jump.OnJumpReleased();
//
//        // ���/����
//        if (input.DashDown) dash.TryStart(input.MoveX);
//        if (input.AttackDown) attack.TryStart();
//
//        // S ������ �� ��Ӵٿ� (������ ���� ��)
//        if (input.DownDown)
//        {
//            if (Time.time - lastDownTapTime <= moveCfg.dropDoubleTapWindow)
//                motor.TryDropThrough(); // ���ο��� IsGrounded, ���� üũ
//            lastDownTapTime = Time.time;
//        }
//
//        // ���� Ÿ�̸� ����
//        jump.UpdateTimers(Time.deltaTime, motor.IsGrounded);
//    }
//
//    void FixedUpdate()
//    {
//        bool crouching = input.DownHeld; // S ���� �� ��ũ���� 
//        // �̵�/����
//        move.Tick(input.MoveX, movementLocked, crouching);
//        jump.TryConsume();
//        jump.Tick();
//    }
//}

using System.Diagnostics;
using UnityEngine;

[RequireComponent(typeof(CharacterMotor2D))]
[RequireComponent(typeof(MoveAbilityMB))]
[RequireComponent(typeof(JumpAbilityMB))]
[RequireComponent(typeof(DashAbilityMB))]
[RequireComponent(typeof(AttackAbilityMB))]
public class PlayerController : MonoBehaviour
{
    [Header("Configs (ScriptableObject)")]
    public MovementConfig moveCfg;
    public JumpConfig jumpCfg;
    public DashConfig dashCfg;
    public AttackConfig atkCfg;
    private Player player;

    CharacterMotor2D motor;
    MoveAbilityMB move;
    JumpAbilityMB jump;
    DashAbilityMB dash;
    AttackAbilityMB attack;

    // 입력 캐시
    float moveX;
    bool jumpDown, jumpUp;
    bool dashDown, attackDown;
    bool downHeld, downDown;

    float lastDownTapTime = -999f;  // S 더블탭 감지
    bool movementLocked => (dash && dash.IsDashing) || (attack && attack.IsBusy);

    void Awake()
    {
        motor = GetComponent<CharacterMotor2D>();
        move = GetComponent<MoveAbilityMB>();
        jump = GetComponent<JumpAbilityMB>();
        dash = GetComponent<DashAbilityMB>();
        attack = GetComponent<AttackAbilityMB>();
        player = GetComponent<Player>();

        // SO 연결
        if (move) move.cfg = moveCfg;
        if (jump) jump.cfg = jumpCfg;
        if (dash) dash.cfg = dashCfg;
        if (attack) attack.cfg = atkCfg;

        // 빠른 진단
        if (!moveCfg) UnityEngine.Debug.LogError("MovementConfig 미지정");
        if (!jumpCfg) UnityEngine.Debug.LogError("JumpConfig 미지정");
        if (!dashCfg) UnityEngine.Debug.LogError("DashConfig 미지정");
        if (!atkCfg) UnityEngine.Debug.LogError("AttackConfig 미지정");
    }

    void Update()
    {
        // ===== 입력 폴링 ===== 

        // 수평 이동
        // moveX = Input.GetAxisRaw("Horizontal"); // A/D, ←/→
        float x = 0f;
        if (Input.GetKey(KeyCode.A)) x -= 1f;
        if (Input.GetKey(KeyCode.D)) x += 1f;
        moveX = Mathf.Clamp(x, -1f, 1f);

        // 점프
        jumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space);
        jumpUp = Input.GetButtonUp("Jump") || Input.GetKeyUp(KeyCode.Space);

        // 대시/공격
        dashDown = Input.GetKeyDown(KeyCode.LeftShift);
        attackDown = Input.GetMouseButtonDown(0);

        // 웅크리기/드롭: S
        downHeld = Input.GetKey(KeyCode.S);
        downDown = Input.GetKeyDown(KeyCode.S);

        // ===== 상태 전이/즉시 처리 =====
        if (jumpDown) jump?.OnJumpPressed();
        if (jumpUp) jump?.OnJumpReleased();

        if (dashDown) dash?.TryStart(moveX);
        if (attackDown) attack?.TryStart();

        // S 더블탭 → 원웨이 드롭
        if (downDown)
        {
            if (Time.time - lastDownTapTime <= moveCfg.dropDoubleTapWindow)
                motor.TryDropThrough(); // 내부에서 발판/지상 체크
            lastDownTapTime = Time.time;
        }

        // 점프 타이머 갱신
        jump?.UpdateTimers(Time.deltaTime);

        // ===== Exhaustion 트리거 =====
        if (Input.GetKeyDown(KeyCode.T) && player != null)
            player.ApplyExhaustion(); 
        if (Input.GetKeyDown(KeyCode.Y) && player != null)
            player.ClearExhaustion();


        if (Input.GetKeyDown(KeyCode.R) && player != null)
        {
            bool wasUsable = player.CanUseIndomitable;              // 리셋 전 사용 가능 여부
            player.ResetIndomitable(clearInvincibility: true);      // 완전 초기화(무적도 해제)
            bool nowUsable = player.CanUseIndomitable;              // 리셋 후 사용 가능 여부

            if (!wasUsable && nowUsable)
                UnityEngine.Debug.Log("불굴 사용 불가 -> 사용가능");
            else if (wasUsable && nowUsable)
                UnityEngine.Debug.Log("불굴 사용 가능 -> 사용가능");
            else
                UnityEngine.Debug.Log("불굴 사용 불가 -> 사용불가 (enableIndomitable=false 등)");
        }
    }

    void FixedUpdate()
    {
        // 이동/점프 처리(물리 틱) — Exhaustion의 이동속도 배수 반영
        float moveMul = (player != null) ? player.MoveSpeedMultiplier : 1f;
        move?.Tick(moveX * moveMul, movementLocked, downHeld);
        jump?.TryConsume();
        jump?.Tick();
    }
}
