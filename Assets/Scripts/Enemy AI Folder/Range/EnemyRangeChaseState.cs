using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR.Haptics;

public class EnemyRangeChaseState : EnemyRangeState
{
    #region Variable Part


    private bool isAttacking = false;
    private float attackAnimationDelay = 1.1f; //Delay can be change when animation is Given
    private float attackAnimationTimer = 0f;

    private CircleCollider2D attackCollider;

    private float repathTimer;

    private const float RepathInterval = 0.15f;
    public EnemyRangeChaseState(
      EnemyRange enemy)
      : base(enemy)
    {
    }
    #endregion


    public override void Enter()
    {
        repathTimer = 0f;

        Debug.Log(
            $"[Chase] {Enemy.name} started chasing."
        );

        CalculatePath();

        attackCollider = Enemy.GetComponentInChildren<CircleCollider2D>();
    }


    public override void Tick()
    {
        // -----------------------------------------------------
        // ATTACK ANIMATION LOCK
        // -----------------------------------------------------

        if (isAttacking)
        { 
            Enemy.PauseMovement(true);

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
                EnemyRange.EnemyState.Search
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

            Enemy.PauseMovement(true);

            isAttacking = true;
            attackAnimationTimer = attackAnimationDelay;
            attackCollider.enabled = true;


            Debug.Log(
                  $"[Chase] {Enemy.name}: " +
                  "Attacking player."
            );

            return;
        }

        Enemy.PauseMovement(false);

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


    public override void Exit()
    {
        Enemy.StopMoving();

        Debug.Log(
            $"[Chase] {Enemy.name} stopped chasing."
        );
    }
}
