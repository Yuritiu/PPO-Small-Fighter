using UnityEngine;

public struct FighterCommandOld
{
    public float MoveX;       // held: -1, 0, +1
    public bool JumpPressed;  // one-shot
    public bool LightPressed; // one-shot
    public bool HeavyPressed; // one-shot
    public bool ThrowPressed; // one-shot
}

public class FighterCommandBufferOld : MonoBehaviour
{
    public float MoveX { get; private set; }

    // Latched one-shots; cleared when consumed.
    private bool _jump, _light, _heavy, _throw;

    // For edge-triggering across decisions.
    private int _lastJump;
    private int _lastAttack;

    /// <summary>
    /// Discrete branches:
    /// move: 0 none, 1 left, 2 right
    /// jump: 0 no, 1 yes
    /// atk : 0 none, 1 light, 2 heavy, 3 throw
    /// </summary>
    public void ApplyDiscrete(int move, int jump, int atk)
    {
        // Move is held
        MoveX = move == 1 ? -1f : move == 2 ? +1f : 0f;

        // Jump is edge-triggered
        if (jump == 1 && _lastJump == 0) _jump = true;
        _lastJump = jump;

        // Attack is edge-triggered
        if (atk != 0 && _lastAttack == 0)
        {
            if (atk == 1) _light = true;
            else if (atk == 2) _heavy = true;
            else if (atk == 3) _throw = true;
        }
        _lastAttack = atk;
    }

    /// <summary>Call once per physics step by your driver/fighter.</summary>
    public FighterCommand Consume()
    {
        var cmd = new FighterCommand
        {
            MoveX = MoveX,
            JumpPressed = _jump,
            APressed = _light,
            BPressed = _heavy,
            CPressed = _throw
        };

        _jump = _light = _heavy = _throw = false;
        return cmd;
    }

    public void ClearAll()
    {
        MoveX = 0f;
        _jump = _light = _heavy = _throw = false;
        _lastJump = 0;
        _lastAttack = 0;
    }
}