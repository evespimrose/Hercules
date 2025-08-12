using UnityEngine;

[CreateAssetMenu(menuName = "Game/Config/Dash")]
public class DashConfig : ScriptableObject
{
    public float dashSpeed = 18f;
    public float dashDuration = 0.18f;
    public float cooldown = 0.6f;
}
