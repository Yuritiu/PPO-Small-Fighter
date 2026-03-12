using UnityEngine;

[RequireComponent(typeof(NewFighterOld), typeof(FighterCommandBufferOld))]
public class AIInputProviderOld : MonoBehaviour
{
    private NewFighterOld _fighter;
    private FighterCommandBufferOld _buffer;

    private void Awake()
    {
        _fighter = GetComponent<NewFighterOld>();
        _buffer = GetComponent<FighterCommandBufferOld>();
    }

    public InputData GetInput()
    {
        FighterCommand cmd = _buffer.Consume();
        InputData input = default;

        // Buttons
        input.aPressed = cmd.APressed;
        input.bPressed = cmd.BPressed;
        input.cPressed = cmd.CPressed;

        // Jump button
        input.jumpPressed = cmd.JumpPressed;

        // Direction (match InputHelper style: 1-9)
        input.direction = BuildDirection(cmd.MoveX, cmd.JumpPressed, _fighter.IsOnLeftSide);

        return input;
    }

    private int BuildDirection(float moveX, bool jumpPressed, bool isOnLeftSide)
    {
        // Horizontal intent as "forward/back" relative to facing
        bool wantsRight = moveX > 0f;
        bool wantsLeft = moveX < 0f;

        // Convert world left/right into forward/back depending on side
        // Forward direction code is 6, back is 4.
        int horiz = 5; // neutral
        if (wantsRight)
            horiz = isOnLeftSide ? 6 : 4;   // +X is forward if on left, else back
        else if (wantsLeft)
            horiz = isOnLeftSide ? 4 : 6;   // -X is back if on left, else forward

        // If jump is pressed this frame, use up directions (7/8/9)
        // so Walking state will actually enter Jumping.
        if (jumpPressed)
        {
            if (horiz == 4) return 7; // up-back
            if (horiz == 6) return 9; // up-forward
            return 8;                 // up
        }

        return horiz; // 4/5/6
    }
}
