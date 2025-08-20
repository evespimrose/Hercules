//using UnityEngine; 
//public class JumpAbility 
//{ 
//    readonly CharacterMotor2D motor; 
//    readonly JumpConfig cfg; 
//
//    float coyoteTimer; 
//    float bufferTimer; 
//    bool jumpHeld; 
//
//    // ↓ 추가: 남은 공중점프 횟수 
//    int remainingAirJumps = 0; 
//
//    public JumpAbility(CharacterMotor2D motor, JumpConfig cfg) 
//    { 
//        this.motor = motor; 
//        this.cfg = cfg; 
//        remainingAirJumps = cfg.extraAirJumps; 
//    } 
//
//    public void UpdateTimers(float dt, bool grounded) 
//    { 
//        if (grounded) 
//        { 
//            // 착지 중에는 항상 리셋 
//            coyoteTimer = cfg.coyoteTime; 
//            remainingAirJumps = cfg.extraAirJumps; 
//        } 
//        else 
//        { 
//            coyoteTimer = Mathf.Max(0, coyoteTimer - dt); 
//        } 
//        bufferTimer = Mathf.Max(0, bufferTimer - dt); 
//    } 
//
//    public void OnJumpPressed() => bufferTimer = cfg.bufferTime; 
//    public void OnJumpReleased() => jumpHeld = false; 
//
//    public void TryConsume() 
//    { 
//        if (bufferTimer <= 0f) return; 
//
//        // 1) 지상/코요테 점프 우선 
//        if (coyoteTimer > 0f) 
//        { 
//            DoJump(); 
//            bufferTimer = 0f; 
//            coyoteTimer = 0f; 
//            return; 
//        } 
//
//        // 2) 공중 점프 (남은 횟수 있을 때만) 
//        if (remainingAirJumps > 0) 
//        { 
//            DoJump(); 
//            remainingAirJumps--; 
//            bufferTimer = 0f; 
//        } 
//    } 
//
//    void DoJump() 
//    { 
//        float v0 = Mathf.Sqrt(2f * Mathf.Abs(motor.Gravity) * cfg.jumpHeight); 
//        motor.AddVerticalVelocity(v0); 
//        jumpHeld = true; 
//    } 
//
//    public void Tick() 
//    { 
//        // 점프 컷: 키를 떼었고 상승 중이면 속도를 절삭 
//        if (!jumpHeld && motor.Velocity.y > 0f) 
//            motor.AddVerticalVelocity(motor.Velocity.y * 0.5f); 
//    } 
//}

using UnityEngine;

[RequireComponent(typeof(CharacterMotor2D))]
public class JumpAbilityMB : MonoBehaviour
{
    public JumpConfig cfg;

    CharacterMotor2D motor;
    float coyoteTimer, bufferTimer;
    bool jumpHeld;
    int remainingAirJumps;

    // 점프 높이 배수 반영
    Unit ownerUnit;

    void Awake()
    {
        motor = GetComponent<CharacterMotor2D>();
        ownerUnit = GetComponent<Unit>(); // 없으면 null 허용
        remainingAirJumps = cfg ? cfg.extraAirJumps : 0;
    }

    public void UpdateTimers(float dt)
    {
        if (motor.IsGrounded)
        {
            coyoteTimer = cfg.coyoteTime;
            remainingAirJumps = cfg.extraAirJumps;
        }
        else
        {
            coyoteTimer = Mathf.Max(0, coyoteTimer - dt);
        }
        bufferTimer = Mathf.Max(0, bufferTimer - dt);
    }

    public void OnJumpPressed() { bufferTimer = cfg.bufferTime; }
    public void OnJumpReleased() { jumpHeld = false; }

    public void TryConsume()
    {
        if (bufferTimer <= 0f) return;

        // 우선: 지상 점프
        if (coyoteTimer > 0f)
        {
            DoJump();
            bufferTimer = 0f; coyoteTimer = 0f;
            return;
        }

        // 공중 점프
        if (remainingAirJumps > 0)
        {
            DoJump();
            remainingAirJumps--;
            bufferTimer = 0f;
        }
    }

    void DoJump()
    {
        // Exhaustion으로 조정된 점프 높이 배수 적용
        float jumpMul = (ownerUnit != null) ? ownerUnit.JumpHeightMultiplier : 1f;
        float jumpH = Mathf.Max(0f, (cfg ? cfg.jumpHeight : 0f) * jumpMul);

        float v0 = Mathf.Sqrt(2f * Mathf.Abs(motor.Gravity) * jumpH);
        motor.AddVerticalVelocity(v0);
        jumpHeld = true;
    }

    public void Tick()
    {
        // 점프 컷: 상승 중 & 키 뗐으면 상승 속도 절반
        if (!jumpHeld && motor.Velocity.y > 0f)
            motor.AddVerticalVelocity(motor.Velocity.y * 0.5f);
    }
}
