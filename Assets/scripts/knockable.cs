using Unity.Netcode;
using UnityEngine;

public class knockable : NetworkBehaviour
{
    public Vector2 targetVel;

    [Header("self KnockBack")]
    public Vector2 knockBackFallof;

    public void KnockBack(Vector2 dir, playerMovement movement)
    {
        targetVel += dir;
        movement.gPull = 0;
    }
    private void FixedUpdate()
    {
        if (IsOwner == false || IsSpawned == false) return;

        targetVel.x = shrinkX(targetVel.x, knockBackFallof.x);
        targetVel.y = shrinkX(targetVel.y, knockBackFallof.y);
    }
    float shrinkX(float x, float multiplier)
    {
        return x > 0 ? Mathf.Max(x - multiplier, 0) : Mathf.Min(x + multiplier, 0);
    }
}
