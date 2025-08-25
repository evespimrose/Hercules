using UnityEngine;

/// <summary>
/// 피격 인터페이스(프로젝트 전역에서 사용).
/// </summary>
public interface IHitReceiver
{
    /// <param name="dmg">가해지는 데미지(최종값)</param>
    /// <param name="knockback">넉백 벡터(방향*세기)</param>
    /// <param name="hitPoint">피격 지점</param>
    void ReceiveHit(float dmg, Vector2 knockback, Vector2 hitPoint);
}
