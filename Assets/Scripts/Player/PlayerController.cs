using UnityEngine;

[RequireComponent(typeof(CharacterMotor2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Configs")]
    public MovementConfig moveCfg;
    public JumpConfig jumpCfg;
    public DashConfig dashCfg;
    public AttackConfig atkCfg;

    CharacterMotor2D motor;
    IInputSource input;

    MoveAbility move;
    JumpAbility jump;
    DashAbility dash;
    AttackAbility attack;

    float lastDownTapTime = -999f;  // S 더블탭 감지
    bool movementLocked => (dash != null && dash.IsDashing) || (attack != null && attack.IsBusy);

    void Awake()
    {
        motor = GetComponent<CharacterMotor2D>();
        input = GetComponent<IInputSource>(); // UnityInputSource 함께 붙이기

        move = new MoveAbility(motor, moveCfg);
        jump = new JumpAbility(motor, jumpCfg);
        dash = new DashAbility(this, motor, dashCfg);
        attack = new AttackAbility(this, motor, atkCfg, transform);
    }

    void Update()
    {
        // 점프 (Space)
        if (input.JumpDown) jump.OnJumpPressed();
        if (input.JumpUp) jump.OnJumpReleased();

        // 대시/공격
        if (input.DashDown) dash.TryStart(input.MoveX);
        if (input.AttackDown) attack.TryStart();

        // S 더블탭 → 드롭다운 (원웨이 발판 위)
        if (input.DownDown)
        {
            if (Time.time - lastDownTapTime <= moveCfg.dropDoubleTapWindow)
                motor.TryDropThrough(); // 내부에서 IsGrounded, 발판 체크
            lastDownTapTime = Time.time;
        }

        // 점프 타이머 갱신
        jump.UpdateTimers(Time.deltaTime, motor.IsGrounded);
    }

    void FixedUpdate()
    {
        bool crouching = input.DownHeld; // S 유지 → 웅크리기

        // 이동/점프
        move.Tick(input.MoveX, movementLocked, crouching);
        jump.TryConsume();
        jump.Tick();
    }
}
