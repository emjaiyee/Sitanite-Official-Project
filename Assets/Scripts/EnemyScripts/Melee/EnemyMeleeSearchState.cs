using UnityEngine;

public class EnemyMeleeSearchState : EnemyMeleeState
{
    private float searchTimer;

    private const float DefaultSearchDuration = 5f;


    public EnemyMeleeSearchState(
        EnemyMelee enemy)
        : base(enemy)
    {
    }


    public override void Enter()
    {
        Enemy.StopMoving();

        searchTimer = 0f;

        Debug.Log(
            $"[EnemyMelee] {Enemy.name} is searching for the player."
        );
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
        // PLAYER FOUND AGAIN
        // -------------------------------------------------

        if (Enemy.IsPlayerDetected())
        {
            Enemy.ChangeState(
                EnemyMelee.EnemyState.Chase
            );

            return;
        }


        // -------------------------------------------------
        // SEARCH TIMER
        // -------------------------------------------------

        searchTimer += Time.deltaTime;

        if (searchTimer >= DefaultSearchDuration)
        {
            Enemy.ChangeState(
                EnemyMelee.EnemyState.Return
            );
        }
    }


    public override void Exit()
    {
        searchTimer = 0f;
    }
}