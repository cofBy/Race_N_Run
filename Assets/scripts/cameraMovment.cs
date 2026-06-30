using Unity.Netcode;
using UnityEngine;

public class cameraMovment : NetworkBehaviour
{
    [Header("following the player")]
    public Vector3 offset;
    [HideInInspector] public Rigidbody2D target;
    [HideInInspector] public bool spawned;
    bool oldSpawned;
    gun playerGun;

    [Range(0, 1)] public float followBehavior;
    public float velocityCab;

    Vector3 vel;
    public float speed;

    [Header("zooming out")]
    public Camera cam;
    public float minVel, maxVel;
    public float minSize, maxSize;
    public float sizeChangingSpeed;
    float sizeVel;

    [Header("followVars")]
    Vector3 lookAhead;
    Vector3 awayFromMouse;

    private void Update()
    {
        if (oldSpawned == false && spawned == true)
        {
            playerGun = target.GetComponent<gun>();
        }
        oldSpawned = spawned;

        if (IsOwner == false || IsSpawned == false || spawned == false) return;

        lookAhead = target.transform.position + Vector3.ClampMagnitude(target.linearVelocity, velocityCab);
        awayFromMouse = target.transform.position + Vector3.ClampMagnitude(target.transform.position - cam.ScreenToWorldPoint(playerGun.aiming.action.ReadValue<Vector2>()), velocityCab);

        Vector3 follow = Vector3.Lerp(lookAhead, awayFromMouse, followBehavior);
        transform.position = Vector3.SmoothDamp(transform.position - offset, follow, ref vel, speed) + offset;

        float size = Mathf.InverseLerp(minVel, maxVel, Mathf.Abs(target.linearVelocity.magnitude));
        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, Mathf.Lerp(minSize, maxSize, size), ref sizeVel, sizeChangingSpeed);
    }

    private void OnDrawGizmos()
    {
        if (IsOwner == false || IsSpawned == false || spawned == false) return;

        Gizmos.color = Color.blue;
        Gizmos.DrawCube(lookAhead, Vector3.one);

        Gizmos.color = Color.red;
        Gizmos.DrawCube(awayFromMouse, Vector3.one);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(Vector3.Lerp(lookAhead, awayFromMouse, followBehavior), 1);
    }
}
