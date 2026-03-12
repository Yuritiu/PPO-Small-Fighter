using UnityEngine;

public struct FighterCommand
{
    public float MoveX;       // held: -1, 0, +1
    public float MoveY;       // held: -1, 0, +1 (down/neutral/up intent)
    public bool JumpPressed;  // one-shot
    public bool APressed;     // one-shot
    public bool BPressed;     // one-shot
    public bool CPressed;     // one-shot
}

public class FighterCommandBuffer : MonoBehaviour
{
    public float MoveX { get; private set; }
    public float MoveY { get; private set; }

    private bool _jump, _a, _b, _c;

    private int _lastJump;
    private int _lastButton;

    /// <summary>
    /// Discrete branches:
    /// horiz: 0 neutral, 1 left, 2 right
    /// vert : 0 neutral, 1 down, 2 up
    /// btn  : 0 none, 1 A, 2 B, 3 C
    /// jump : 0 no, 1 yes
    /// </summary>
    public void ApplyDiscrete(int horiz, int vert, int btn, int jump)
    {
        // Held movement
        MoveX = horiz == 1 ? -1f : horiz == 2 ? +1f : 0f;
        MoveY = vert == 1 ? -1f : vert == 2 ? +1f : 0f;

        // Jump edge-trigger
        if (jump == 1 && _lastJump == 0) _jump = true;
        _lastJump = jump;

        // Button edge-trigger (only reward the transition none->button)
        if (btn != 0 && _lastButton == 0)
        {
            if (btn == 1) _a = true;
            else if (btn == 2) _b = true;
            else if (btn == 3) _c = true;
        }
        _lastButton = btn;
    }

    public FighterCommand Consume()
    {
        var cmd = new FighterCommand
        {
            MoveX = MoveX,
            MoveY = MoveY,
            JumpPressed = _jump,
            APressed = _a,
            BPressed = _b,
            CPressed = _c,
        };

        _jump = _a = _b = _c = false;
        return cmd;
    }

    public void ClearAll()
    {
        MoveX = 0f;
        MoveY = 0f;
        _jump = _a = _b = _c = false;
        _lastJump = 0;
        _lastButton = 0;
    }
}