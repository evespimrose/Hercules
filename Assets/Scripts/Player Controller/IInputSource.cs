public interface IInputSource
{
    float MoveX { get; }     // -1..+1 (A/D, ��/��)
    bool JumpDown { get; }   // Space ����
    bool JumpUp { get; }     // Space ��
    bool DashDown { get; }
    bool AttackDown { get; }

    // S �Է� (��ũ����/��Ӵٿ�)
    bool DownHeld { get; }   // S ����
    bool DownDown { get; }   // S ����(�̹� ������ ����)
}
