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
//    // �� �߰�: ���� �������� Ƚ�� 
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
//            // ���� �߿��� �׻� ���� 
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
//        // 1) ����/�ڿ��� ���� �켱 
//        if (coyoteTimer > 0f) 
//        { 
//            DoJump(); 
//            bufferTimer = 0f; 
//            coyoteTimer = 0f; 
//            return; 
//        } 
//
//        // 2) ���� ���� (���� Ƚ�� ���� ����) 
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
//        // ���� ��: Ű�� ������ ��� ���̸� �ӵ��� ���� 
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

    // ���� ���� ��� �ݿ�
    Unit ownerUnit;

    void Awake()
    {
        motor = GetComponent<CharacterMotor2D>();
        ownerUnit = GetComponent<Unit>(); // ������ null ���
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

        // �켱: ���� ����
        if (coyoteTimer > 0f)
        {
            DoJump();
            bufferTimer = 0f; coyoteTimer = 0f;
            return;
        }

        // ���� ����
        if (remainingAirJumps > 0)
        {
            DoJump();
            remainingAirJumps--;
            bufferTimer = 0f;
        }
    }

    void DoJump()
    {
        // Exhaustion���� ������ ���� ���� ��� ����
        float jumpMul = (ownerUnit != null) ? ownerUnit.JumpHeightMultiplier : 1f;
        float jumpH = Mathf.Max(0f, (cfg ? cfg.jumpHeight : 0f) * jumpMul);

        float v0 = Mathf.Sqrt(2f * Mathf.Abs(motor.Gravity) * jumpH);
        motor.AddVerticalVelocity(v0);
        jumpHeld = true;
    }

    public void Tick()
    {
        // ���� ��: ��� �� & Ű ������ ��� �ӵ� ����
        if (!jumpHeld && motor.Velocity.y > 0f)
            motor.AddVerticalVelocity(motor.Velocity.y * 0.5f);
    }
}
