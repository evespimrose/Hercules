using UnityEngine;

/// <summary>
/// �ǰ� �������̽�(������Ʈ �������� ���).
/// </summary>
public interface IHitReceiver
{
    /// <param name="dmg">�������� ������(������)</param>
    /// <param name="knockback">�˹� ����(����*����)</param>
    /// <param name="hitPoint">�ǰ� ����</param>
    void ReceiveHit(float dmg, Vector2 knockback, Vector2 hitPoint);
}
