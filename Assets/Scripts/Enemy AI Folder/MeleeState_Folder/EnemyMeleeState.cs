public abstract class EnemyMeleeState
{
    protected readonly EnemyMelee Enemy;


    protected EnemyMeleeState(
        EnemyMelee enemy)
    {
        Enemy = enemy;
    }


    /// <summary>
    /// Called once when entering this state.
    /// </summary>
    public virtual void Enter()
    {
    }


    /// <summary>
    /// Called every frame while this state is active.
    /// </summary>
    public virtual void Tick()
    {
    }


    /// <summary>
    /// Called once when leaving this state.
    /// </summary>
    public virtual void Exit()
    {
    }
}