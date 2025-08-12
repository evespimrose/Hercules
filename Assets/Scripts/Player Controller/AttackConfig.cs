using UnityEngine;

[CreateAssetMenu(menuName = "Game/Config/Attack")]
public class AttackConfig : ScriptableObject
{
    public float damage = 10f;

    public float startUp = 0.08f;   // �ߵ�
    public float active = 0.12f;    // ��ȿ
    public float recovery = 0.16f;  // ����
    public float cooldown = 0.10f;

    public float knockback = 6f;
    public Vector2 hitboxOffset = new Vector2(0.7f, 0f);
    public Vector2 hitboxSize = new Vector2(1.0f, 0.6f);
}
