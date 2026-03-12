public abstract class FighterStateOld
{
    protected NewFighterOld fighter;

    public FighterStateOld(NewFighterOld fighter)
    {
        this.fighter = fighter;
    }

    public virtual void OnStateEnter() { }

    public virtual void Update(InputData currentInput) { }

    public virtual void OnStateExit() { }
}
