public abstract class EnemyLichState 
{
     protected readonly EnemyLich Enemy;    
    
    protected EnemyLichState(EnemyLich enemy)
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
