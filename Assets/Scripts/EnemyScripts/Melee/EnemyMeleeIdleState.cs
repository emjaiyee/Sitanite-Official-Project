public class EnemyMeleeIdleState : EnemyMeleeState
{
    public EnemyMeleeIdleState(
        EnemyMelee enemy)
        : base(enemy)
    {
    }


    public override void Enter()
    {
        Enemy.StopMoving();
    }


    public override void Tick()
    {
        // -------------------------------------------------
        // DEATH CHECK
        // -------------------------------------------------

        if (Enemy.IsDead())
        {
            Enemy.ChangeState(
                EnemyMelee.EnemyState.Death
            );

            return;
        }


        // -------------------------------------------------
        // PLAYER DETECTION
        // -------------------------------------------------

        if (Enemy.IsPlayerDetected())
        {
            Enemy.ChangeState(
                EnemyMelee.EnemyState.Chase
            );
        }
    }


    public override void Exit()
    {
    }
}