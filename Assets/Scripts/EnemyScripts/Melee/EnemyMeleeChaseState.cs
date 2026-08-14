public class EnemyMeleeChaseState : EnemyMeleeState
{
    public EnemyMeleeChaseState(
        EnemyMelee enemy)
        : base(enemy)
    {
    }


    public override void Enter()
    {
        Enemy.ResumeMoving();
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
        // PLAYER CHECK
        // -------------------------------------------------

        if (Enemy.Player == null)
        {
            Enemy.StopMoving();

            Enemy.ChangeState(
                EnemyMelee.EnemyState.Search
            );

            return;
        }


        // -------------------------------------------------
        // ATTACK RANGE
        // -------------------------------------------------

        if (Enemy.IsPlayerInAttackRange())
        {
            Enemy.StopMoving();

            Enemy.ChangeState(
                EnemyMelee.EnemyState.Attack
            );

            return;
        }


        // -------------------------------------------------
        // DETECTION RANGE
        // -------------------------------------------------

        if (!Enemy.IsPlayerDetected())
        {
            Enemy.StopMoving();

            Enemy.ChangeState(
                EnemyMelee.EnemyState.Search
            );

            return;
        }


        // -------------------------------------------------
        // CHASE PLAYER
        // -------------------------------------------------

        Enemy.MoveTo(
            Enemy.Player.position
        );
    }


    public override void Exit()
    {
        Enemy.StopMoving();
    }
}