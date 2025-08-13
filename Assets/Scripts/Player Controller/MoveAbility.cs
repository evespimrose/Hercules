//using UnityEngine;

//public class MoveAbility
//{
//    readonly CharacterMotor2D motor;
//    readonly MovementConfig cfg;

//    public MoveAbility(CharacterMotor2D motor, MovementConfig cfg)
//    { this.motor = motor; this.cfg = cfg; }

//    // crouching: 지상에서 속도 감소
//    public void Tick(float inputX, bool movementLocked, bool crouching)
//    {
//        float max = cfg.maxSpeed;
//        if (crouching && motor.IsGrounded) max *= Mathf.Clamp01(cfg.crouchSpeedScale);

//        float target = inputX * max;
//        float cur = motor.Velocity.x;

//        float accel = motor.IsGrounded ? cfg.accel : cfg.accel * cfg.airControl;
//        float decel = motor.IsGrounded ? cfg.decel : cfg.decel * cfg.airControl;

//        float rate = Mathf.Abs(target) > 0.01f ? accel : decel;
//        float newVx = Mathf.MoveTowards(cur, target, rate * Time.fixedDeltaTime);

//        if (!movementLocked) motor.SetHorizontalVelocity(newVx);
//    }
//}

using UnityEngine;

[RequireComponent(typeof(CharacterMotor2D))]
public class MoveAbilityMB : MonoBehaviour
{
    public MovementConfig cfg;
    CharacterMotor2D motor;

    void Awake() { motor = GetComponent<CharacterMotor2D>(); }

    // PlayerController가 호출
    public void Tick(float inputX, bool movementLocked, bool crouching)
    {
        float max = cfg.maxSpeed;
        if (crouching && motor.IsGrounded) max *= Mathf.Clamp01(cfg.crouchSpeedScale);

        float target = inputX * max;
        float cur = motor.Velocity.x;

        float accel = motor.IsGrounded ? cfg.accel : cfg.accel * cfg.airControl;
        float decel = motor.IsGrounded ? cfg.decel : cfg.decel * cfg.airControl;

        float rate = Mathf.Abs(target) > 0.01f ? accel : decel;
        float newVx = Mathf.MoveTowards(cur, target, rate * Time.fixedDeltaTime);

        if (!movementLocked) motor.SetHorizontalVelocity(newVx);
    }
}
