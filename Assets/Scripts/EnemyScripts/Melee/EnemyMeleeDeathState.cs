using UnityEngine;

public class EnemyMeleeDeathState : EnemyMeleeState
{
    private bool deathStarted;


    public EnemyMeleeDeathState(
        EnemyMelee enemy)
        : base(enemy)
    {
    }


    public override void Enter()
    {
        if (deathStarted)
            return;

        deathStarted = true;


        Debug.Log(
            $"[EnemyMelee] {Enemy.name} entered Death state."
        );


        // -------------------------------------------------
        // STOP MOVEMENT
        // -------------------------------------------------

        Enemy.StopMoving();


        // -------------------------------------------------
        // FUTURE DEATH BEHAVIOR
        // -------------------------------------------------
        //
        // This is where we can eventually add:
        //
        // - Death animation
        // - Death VFX
        // - Sound
        // - Loot
        // - XP
        // - Delayed destruction
        //
        // EnemyHealth currently owns the actual
        // destruction of the GameObject.
    }


    public override void Tick()
    {
        // Death is currently a terminal state.
        //
        // EnemyHealth already destroys the enemy
        // after firing OnEnemyDied.
    }


    public override void Exit()
    {
        // Death is terminal.
    }
}