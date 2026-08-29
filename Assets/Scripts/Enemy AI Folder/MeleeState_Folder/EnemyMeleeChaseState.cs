using System.Collections.Generic;
using UnityEngine;

public class EnemyMeleeChaseState : EnemyMeleeState
{
    #region Variable Part


    private bool isAttacking = false;
    private float attackAnimationDelay = 1.1f; //Delay can be change when animation is Given
    private float attackAnimationTimer = 0f;

    private CircleCollider2D attackCollider;

    private float repathTimer;

    private const float RepathInterval = 0.15f;


    public EnemyMeleeChaseState(
        EnemyMelee enemy)
        : base(enemy)
    {
    }
    #endregion


    // =========================================================
    // ENTER
    // =========================================================

    public override void Enter()
    {
        repathTimer = 0f;

        Debug.Log(
            $"[Chase] {Enemy.name} started chasing."
        );

        CalculatePath();

        attackCollider = Enemy.GetComponentInChildren<CircleCollider2D>();
    }


    // =========================================================
    // TICK
    // =========================================================

    public override void Tick()
    {

        // -----------------------------------------------------
        // ATTACK ANIMATION LOCK
        // -----------------------------------------------------

        if (isAttacking)
        {
            Enemy.StopMoving();

            attackAnimationTimer -= Time.deltaTime;
            if (attackAnimationTimer <= 0)
            {
                isAttacking = false;
                attackCollider.enabled = false;
            }
            return;
        }

        // -----------------------------------------------------
        // PLAYER EXISTS?
        // -----------------------------------------------------

        if (Enemy.Player == null)
        {
            Enemy.StopMoving();
            return;
        }


        // -----------------------------------------------------
        // PLAYER STILL DETECTED?
        // -----------------------------------------------------

        if (!Enemy.IsPlayerDetected())
        {
            Debug.Log(
                $"[Chase] {Enemy.name}: " +
                "Lost the player. Entering Search."
            );

            Enemy.ChangeState(
                EnemyMelee.EnemyState.Search
            );

            return;
        }


        // -----------------------------------------------------
        // ATTACK 
        // -----------------------------------------------------

        if (Enemy.IsPlayerWithinAttackRange() && Enemy.IsPlayerRayCasted())
        {


            Debug.Log(
                $"[Chase] {Enemy.name}: " +
                "Player is within attack range. Entering Attack."
            );

            Enemy.StopMoving();

            isAttacking = true;
            attackAnimationTimer = attackAnimationDelay;
            attackCollider.enabled = true;


            Debug.Log(
                  $"[Chase] {Enemy.name}: " +
                  "Attacking player."
            );

            return;
        }

        // -----------------------------------------------------
        // REPATH
        // -----------------------------------------------------

        repathTimer += Time.deltaTime;

        if (repathTimer >= RepathInterval)
        {
            repathTimer = 0f;

            CalculatePath();
        }


        // -----------------------------------------------------
        // FOLLOW PATH
        // -----------------------------------------------------

        Enemy.FollowCurrentPath();
    }


    // =========================================================
    // CALCULATE PATH
    // =========================================================

    private void CalculatePath()
    {
        if (AStarManager.Instance == null)
            return;

        if (Enemy.Player == null)
            return;


        List<Vector3> path =
            AStarManager.Instance.FindPath(
                Enemy.transform.position,
                Enemy.Player.position
            );


        if (path == null ||
            path.Count == 0)
        {
            Enemy.StopMoving();

            Debug.LogWarning(
                $"[Chase] {Enemy.name}: " +
                "Could not find path to player."
            );

            return;
        }


        Enemy.SetPath(path);
    }


    // =========================================================
    // EXIT
    // =========================================================

    public override void Exit()
    {
        Enemy.StopMoving();

        Debug.Log(
            $"[Chase] {Enemy.name} stopped chasing."
        );
    }
}