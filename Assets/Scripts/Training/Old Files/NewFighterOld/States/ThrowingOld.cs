using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowingOld : FighterStateOld
{
    public ThrowingOld(NewFighterOld fighter) : base(fighter)
    {
    }

    public override void OnStateEnter()
    {
        fighter.currentFrame = 1;
        fighter.ClearHitboxes();
        fighter.ClearHurtboxes();
        fighter.velocity = Vector3.zero;
        fighter.gameObject.layer = 2;

        if (fighter.currentThrow != null)
        {
            if (!fighter.beingThrown)
            {
                fighter.SetModelLayer(NewFighterOld.FrontLayer);
                fighter.animator.Play($"Base Layer.{fighter.currentThrow.animationName}", -1, 0f);
            }                
            else
            {
                fighter.SetModelLayer(NewFighterOld.BackLayer);
                fighter.animator.Play($"Base Layer.{fighter.currentThrow.animationName}Dmg", -1, 0f);
            }
        }
    }

    public override void Update(InputData currentInput)
    {
        if (fighter.currentThrow == null || (!fighter.beingThrown && fighter.currentFrame >= fighter.currentThrow.numberOfFramesThrowing) || (fighter.beingThrown && fighter.currentFrame >= fighter.currentThrow.numberOfFramesThrown))
        {
            if (fighter.beingThrown)
            {
                if (fighter.currentThrow.tossSpeed < 0f)
                    fighter.SwitchSide(!fighter.IsOnLeftSide, false);
                fighter.animator.Play("Rising", -1, 0f);
                FightManagerOld.instance.PlaySound(SoundType.Impact, fighter.audioSource);
                fighter.SwitchState(new KnockdownOld(fighter));
            }
            else
            {
                fighter.SwitchState(new WalkingOld(fighter));
            }
        }
        else
        {
            if (fighter.beingThrown)
            {
                if (fighter.currentFrame <= 3 && fighter.CanBreakThrow())
                {
                    fighter.BreakThrow.Invoke(fighter, fighter.throwOpponent);
                    return;
                }

                if (fighter.currentFrame == fighter.currentThrow.tossFrame)
                {
                    int side = fighter.IsOnLeftSide ? -1 : 1;
                    Vector2 offset = new Vector2(fighter.currentThrow.opponentTargetOffset.x * side, fighter.currentThrow.opponentTargetOffset.y);
                    fighter.controller.Move(offset);

                    fighter.velocity = new Vector2(fighter.currentThrow.tossSpeed * side, 0f);
                }

                foreach (DamageData data in fighter.currentThrow.damageFrames)
                {
                    if (data.frame == fighter.currentFrame)
                    {
                        fighter.currentHealth -= data.damage;
                        fighter.TookDamage.Invoke(fighter);
                        FightManagerOld.instance.StartCoroutine(FightManagerOld.instance.ShakeCamera(5, 0.03f));
                    }
                }
            }

            fighter.currentFrame += 1;
        }
    }

    public override void OnStateExit()
    {
        fighter.SetModelLayer(fighter.IsOnLeftSide ? NewFighterOld.FrontLayer : NewFighterOld.BackLayer);
        fighter.currentAction = null;
        fighter.actionHasHit = false;
        fighter.currentThrow = null;
        fighter.beingThrown = false;
        fighter.throwOpponent = null;
        fighter.gameObject.layer = 6;
    }
}
