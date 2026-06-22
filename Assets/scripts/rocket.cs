using System.Globalization;
using UnityEngine;
using Unity.Netcode;

public class rocket : NetworkBehaviour
{
    [Header("Moving")]
    public float speed;
    public Rigidbody2D rb;

    [Header("exploding")]
    public gun spawner;
    private void FixedUpdate()
    {
        rb.linearVelocity = transform.right * speed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsOwner == false || IsSpawned == false) return;

        spawner.shootServerRpc(transform.position, true);
        if (IsServer)
        {
            NetworkObject.Despawn();
        }
    }
}
