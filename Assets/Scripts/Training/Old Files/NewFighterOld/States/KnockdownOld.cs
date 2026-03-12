using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnockdownOld : FighterStateOld
{
    private const int KnockdownLength = 65;
    private int knockdownTimer;

    public KnockdownOld(NewFighterOld fighter) : base(fighter)
    {
    }

    public override void OnStateEnter()
    {
        fighter.ClearHitboxes();
        fighter.ClearHurtboxes();
        fighter.velocity = Vector3.zero;
        fighter.shouldKnockdown = false;
        fighter.model.layer = NewFighter.BackLayer;
    }

    public override void Update(InputData currentInput)
    {
        knockdownTimer += 1;

        if (currentInput.direction == 1 || currentInput.direction == 4)
            fighter.blocking = true;
        else
            fighter.blocking = false;

        if (knockdownTimer >= KnockdownLength)
        {
            fighter.SwitchState(new WalkingOld(fighter));
        }
    }

    public override void OnStateExit()
    {
        fighter.SetModelLayer(fighter.IsOnLeftSide ? NewFighter.FrontLayer : NewFighter.BackLayer);
    }
}
