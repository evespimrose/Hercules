using UnityEngine;

[CreateAssetMenu(menuName = "Game/Config/Jump")]
public class JumpConfig : ScriptableObject
{
    public float jumpHeight = 4f;    // 목표 최고점(월드 유닛)
    public float coyoteTime = 0.1f;  // 발 떼고도 허용
    public float bufferTime = 0.1f;  // 입력 버퍼

    // ↓ 추가: 공중 추가 점프(더블점프=1)
    [Range(0, 3)] public int extraAirJumps = 1;
}
