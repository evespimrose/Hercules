using UnityEngine;

[CreateAssetMenu(menuName = "Game/Config/Attack")]
public class AttackConfig : ScriptableObject
{
    public float damage = 10f;

    public float startUp = 0.08f;   // 발동
    public float active = 0.12f;    // 유효
    public float recovery = 0.16f;  // 경직
    public float cooldown = 0.10f;

    public float knockback = 6f;
    public Vector2 hitboxOffset = new Vector2(0.7f, 0f); // 스폰 위치(좌우 자동 반전)
    public Vector2 hitboxSize = new Vector2(1.0f, 0.6f);
}
