using UnityEngine;

[RequireComponent(typeof(NewFighter), typeof(FighterCommandBuffer))]
public class AIInputProvider : MonoBehaviour
{
    private NewFighter _fighter;
    private FighterCommandBuffer _buffer;

    private void Awake()
    {
        _fighter = GetComponent<NewFighter>();
        _buffer = GetComponent<FighterCommandBuffer>();
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

        // Build 1–9 direction codes.
        // Down directions should work WITHOUT jump (for crouch + quarter-circles).
        // Up directions should only be produced when JumpPressed is true (so the agent doesn't "float" direction 8 constantly).
        input.direction = BuildDirection(cmd.MoveX, cmd.MoveY, cmd.JumpPressed, _fighter.IsOnLeftSide);

        return input;
    }

    private int BuildDirection(float moveX, float moveY, bool jumpPressed, bool isOnLeftSide)
    {
        // Convert world left/right into forward/back relative to side
        int xDir = 0; // -1 back, 0 neutral, +1 forward
        if (moveX > 0f) xDir = isOnLeftSide ? +1 : -1;
        else if (moveX < 0f) xDir = isOnLeftSide ? -1 : +1;

        
        // (crouch + inputs)
        if (moveY < 0f)
        {
            if (xDir < 0) return 1; // down-back
            if (xDir > 0) return 3; // down-forward
            return 2;               // down
        }

        // Up should only show up when jump is pressed (matches your game's jump button design)
        if (jumpPressed)
        {
            if (xDir < 0) return 7; // up-back
            if (xDir > 0) return 9; // up-forward
            return 8;               // up
        }

        // Neutral standing directions
        else if (xDir < 0) return 4; // back
        else if (xDir > 0) return 6; // forward
        else return 5;               // neutral
    }
}