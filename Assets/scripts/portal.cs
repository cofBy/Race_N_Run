using UnityEngine;

public class portal : MonoBehaviour
{
    [Header("detecing player touch")]
    public CapsuleCollider2D[] portals;
    public LayerMask playerMask;

    [Header("teleporting players")]
    public float distance;
    public float portalStrength;
    private void Update()
    {
        for (int i = 0; i < portals.Length; i++)
        {
            ColliderArray2D others = portals[i].GetContactColliders(new ContactFilter2D { layerMask = playerMask });
            foreach (Collider2D other in others)
            {
                playerMovement player = other.GetComponent<playerMovement>();
                CapsuleCollider2D gotoPortal = portals[(i + 1) % portals.Length];

                player.transform.position = gotoPortal.transform.position + (distance * gotoPortal.transform.right);
                player.knockBackinfo.KnockBack(gotoPortal.transform.right * portalStrength, player);
            }
        }
    }
}
