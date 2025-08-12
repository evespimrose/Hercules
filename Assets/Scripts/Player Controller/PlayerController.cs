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

    float lastDownTapTime = -999f;  // S ������ ����
    bool movementLocked => (dash != null && dash.IsDashing) || (attack != null && attack.IsBusy);

    void Awake()
    {
        motor = GetComponent<CharacterMotor2D>();
        input = GetComponent<IInputSource>(); // UnityInputSource �Բ� ���̱�

        move = new MoveAbility(motor, moveCfg);
        jump = new JumpAbility(motor, jumpCfg);
        dash = new DashAbility(this, motor, dashCfg);
        attack = new AttackAbility(this, motor, atkCfg, transform);
    }

    void Update()
    {
        // ���� (Space)
        if (input.JumpDown) jump.OnJumpPressed();
        if (input.JumpUp) jump.OnJumpReleased();

        // ���/����
        if (input.DashDown) dash.TryStart(input.MoveX);
        if (input.AttackDown) attack.TryStart();

        // S ������ �� ��Ӵٿ� (������ ���� ��)
        if (input.DownDown)
        {
            if (Time.time - lastDownTapTime <= moveCfg.dropDoubleTapWindow)
                motor.TryDropThrough(); // ���ο��� IsGrounded, ���� üũ
            lastDownTapTime = Time.time;
        }

        // ���� Ÿ�̸� ����
        jump.UpdateTimers(Time.deltaTime, motor.IsGrounded);
    }

    void FixedUpdate()
    {
        bool crouching = input.DownHeld; // S ���� �� ��ũ����

        // �̵�/����
        move.Tick(input.MoveX, movementLocked, crouching);
        jump.TryConsume();
        jump.Tick();
    }
}
