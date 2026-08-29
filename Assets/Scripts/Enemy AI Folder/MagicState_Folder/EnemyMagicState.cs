public abstract class EnemyMagicState
{
    protected readonly EnemyMagic Enemy;

    protected EnemyMagicState(
        EnemyMagic enemy)
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
