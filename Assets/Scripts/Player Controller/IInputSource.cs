public interface IInputSource
{
    float MoveX { get; }     // -1..+1 (A/D, ←/→)
    bool JumpDown { get; }   // Space 눌림
    bool JumpUp { get; }     // Space 뗌
    bool DashDown { get; }
    bool AttackDown { get; }

    // S 입력 (웅크리기/드롭다운)
    bool DownHeld { get; }   // S 유지
    bool DownDown { get; }   // S 엣지(이번 프레임 눌림)
}
