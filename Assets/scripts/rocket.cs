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
        if (IsServer)
        {
            explodeServerRpc(transform.position);
            NetworkObject.Despawn();
        }
    }

    [ServerRpc]
    void explodeServerRpc(Vector3 pos)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, spawner.shootSize, spawner.playersMask);
        foreach (Collider2D hit in hits)
        {
            Vector2 knockDir = ((Vector2)(hit.transform.position - pos)).normalized * spawner.gunStrength;
            spawner.applyKnockbackClientRpc(hit.GetComponent<NetworkObject>().NetworkObjectId, knockDir);
        }
    }
}
