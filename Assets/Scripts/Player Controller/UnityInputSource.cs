using UnityEngine;

public class UnityInputSource : MonoBehaviour, IInputSource
{
    public float MoveX => Input.GetAxisRaw("Horizontal"); // A/D, ←/→

    // 점프: Space
    public bool JumpDown => Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space);
    public bool JumpUp => Input.GetButtonUp("Jump") || Input.GetKeyUp(KeyCode.Space);

    public bool DashDown => Input.GetKeyDown(KeyCode.LeftShift);

    // 공격: 마우스 좌클릭 (원하면 || Input.GetKeyDown(KeyCode.J) 추가)
    public bool AttackDown => Input.GetMouseButtonDown(0);

    // S: 웅크리기/더블탭
    public bool DownHeld => Input.GetKey(KeyCode.S) || Input.GetAxisRaw("Vertical") < -0.5f;
    public bool DownDown => Input.GetKeyDown(KeyCode.S);
}
