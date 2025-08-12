using UnityEngine;

[CreateAssetMenu(menuName = "Game/Config/Jump")]
public class JumpConfig : ScriptableObject
{
    public float jumpHeight = 4f;    // ��ǥ �ְ���(���� ����)
    public float coyoteTime = 0.1f;  // �� ���� ���
    public float bufferTime = 0.1f;  // �Է� ����

    // �� �߰�: ���� �߰� ����(��������=1)
    [Range(0, 3)] public int extraAirJumps = 1;
}
