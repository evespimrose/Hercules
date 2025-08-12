using UnityEngine;

public class JumpAbility
{
    readonly CharacterMotor2D motor;
    readonly JumpConfig cfg;

    float coyoteTimer;
    float bufferTimer;
    bool jumpHeld;

    public JumpAbility(CharacterMotor2D motor, JumpConfig cfg)
    { this.motor = motor; this.cfg = cfg; }

    public void UpdateTimers(float dt, bool grounded)
    {
        coyoteTimer = grounded ? cfg.coyoteTime : Mathf.Max(0, coyoteTimer - dt);
        bufferTimer = Mathf.Max(0, bufferTimer - dt);
    }

    public void OnJumpPressed() => bufferTimer = cfg.bufferTime;
    public void OnJumpReleased() => jumpHeld = false;

    public void TryConsume()
    {
        if (bufferTimer > 0f && coyoteTimer > 0f)
        {
            float v0 = Mathf.Sqrt(2f * Mathf.Abs(motor.Gravity) * cfg.jumpHeight);
            motor.AddVerticalVelocity(v0);
            jumpHeld = true;
            bufferTimer = 0f;
            coyoteTimer = 0f;
        }
    }

    public void Tick()
    {
        // 점프 컷: 키를 떼었고 상승 중이면 속도 절삭
        if (!jumpHeld && motor.Velocity.y > 0f)
            motor.AddVerticalVelocity(motor.Velocity.y * 0.5f);
    }
}
