public abstract class EnemyRangeState
{
    protected readonly EnemyRange Enemy;

    protected EnemyRangeState(
        EnemyRange enemy)
    {
        Enemy = enemy;
    }


    public virtual void Enter()
    {
    }

    public virtual void Tick()
    {
    }

    public virtual void Exit()
    {
    }
}
