using UnityEngine;

[CreateAssetMenu(menuName = "Game/Config/Movement")]
public class MovementConfig : ScriptableObject
{
    [Header("Run")]
    public float maxSpeed = 7f;
    public float accel = 60f;
    public float decel = 70f;
    [Range(0f, 1f)] public float airControl = 0.5f;

    [Header("Crouch")]
    [Range(0.1f, 1f)] public float crouchSpeedScale = 0.5f; // 웅크릴 때 지상 속도 배수

    [Header("Drop-through")]
    [Range(0.05f, 0.5f)] public float dropDoubleTapWindow = 0.25f; // S 더블탭 허용 시간(초)
}
