using UnityEngine;
using System.Collections;

public class DashAbility
{
    readonly MonoBehaviour runner; // 内风凭 角青 林眉
    readonly CharacterMotor2D motor;
    readonly DashConfig cfg;

    bool _cooling, _dashing;
    public bool IsDashing => _dashing;

    public DashAbility(MonoBehaviour runner, CharacterMotor2D motor, DashConfig cfg)
    { this.runner = runner; this.motor = motor; this.cfg = cfg; }

    public void TryStart(float inputX)
    {
        if (_cooling || _dashing) return;
        float dir = Mathf.Abs(inputX) > 0.01f ? Mathf.Sign(inputX) : (motor.FacingRight ? 1f : -1f);
        runner.StartCoroutine(DashRoutine(dir));
    }

    IEnumerator DashRoutine(float dir)
    {
        _dashing = true; _cooling = true;

        Vector2 savedVel = motor.Velocity;
        float t = 0f;
        while (t < cfg.dashDuration)
        {
            motor.SetVelocity(new Vector2(cfg.dashSpeed * dir, 0f));
            t += Time.deltaTime;
            yield return null;
        }

        motor.SetVelocity(new Vector2(savedVel.x, motor.Velocity.y));
        _dashing = false;

        yield return new WaitForSeconds(cfg.cooldown);
        _cooling = false;
    }
}
