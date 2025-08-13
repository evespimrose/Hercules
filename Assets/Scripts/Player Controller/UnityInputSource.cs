using UnityEngine;

public class UnityInputSource : MonoBehaviour, IInputSource
{
    public float MoveX => Input.GetAxisRaw("Horizontal"); // A/D, ��/��

    // ����: Space
    public bool JumpDown => Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space);
    public bool JumpUp => Input.GetButtonUp("Jump") || Input.GetKeyUp(KeyCode.Space);

    public bool DashDown => Input.GetKeyDown(KeyCode.LeftShift);

    // ����: ���콺 ��Ŭ�� (���ϸ� || Input.GetKeyDown(KeyCode.J) �߰�)
    public bool AttackDown => Input.GetMouseButtonDown(0);

    // S: ��ũ����/������
    public bool DownHeld => Input.GetKey(KeyCode.S) || Input.GetAxisRaw("Vertical") < -0.5f;
    public bool DownDown => Input.GetKeyDown(KeyCode.S);
}
