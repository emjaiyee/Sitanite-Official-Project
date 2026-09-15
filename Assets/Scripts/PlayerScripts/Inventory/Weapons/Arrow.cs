using UnityEngine;

public class Arrow : ProjectileBase
{
    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Arrow-specific initialization can go here later.
        // ProjectileBase already handles:
        // - movement
        // - collision
        // - damage
        // - Pierce damage type
        // - projectile destruction
    }
}