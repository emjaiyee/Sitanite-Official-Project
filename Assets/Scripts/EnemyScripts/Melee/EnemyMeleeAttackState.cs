using UnityEngine;

public class EnemyMeleeAttackState : EnemyMeleeState
{
    [Header("Attack")]
    private float attackCooldown = 1f;

    private float attackTimer;

    private bool hasAttacked;


    public EnemyMeleeAttackState(
        EnemyMelee enemy)
        : base(enemy)
    {
    }


    public override void Enter()
    {
        Enemy.StopMoving();

        attackTimer = 0f;
        hasAttacked = false;

        Debug.Log(
            $"[EnemyMelee] {Enemy.name} entered Attack state."
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
        // PLAYER CHECK
        // -------------------------------------------------

        if (Enemy.Player == null)
        {
            Enemy.ChangeState(
                EnemyMelee.EnemyState.Search
            );

            return;
        }


        // -------------------------------------------------
        // PLAYER LEFT ATTACK RANGE
        // -------------------------------------------------

        if (!Enemy.IsPlayerInAttackRange())
        {
            Enemy.ChangeState(
                EnemyMelee.EnemyState.Chase
            );

            return;
        }


        // -------------------------------------------------
        // ATTACK TIMER
        // -------------------------------------------------

        attackTimer += Time.deltaTime;


        if (
            !hasAttacked &&
            attackTimer >= attackCooldown
        )
        {
            PerformAttack();

            hasAttacked = true;
        }
    }


    private void PerformAttack()
    {
        Debug.Log(
            $"[EnemyMelee] {Enemy.name} performs melee attack!"
        );

        // -------------------------------------------------
        // DAMAGE WILL GO HERE
        // -------------------------------------------------
        //
        // We are intentionally NOT putting damage logic
        // here yet.
        //
        // Later this can connect to:
        //
        // - an animation event
        // - a hitbox
        // - weapon damage
        // - IDamageable
        // - attack cooldowns
        // - knockback
        //
    }


    public override void Exit()
    {
        attackTimer = 0f;
        hasAttacked = false;
    }
}