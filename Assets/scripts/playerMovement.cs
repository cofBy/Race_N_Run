using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class playerMovement : NetworkBehaviour
{
    [Header("Input")]
    public InputActionReference moving;

    Vector2 moveDir;
    Vector2 pointDir;

    [Header("moving")]
    public Rigidbody2D rb;

    public float speedMultiplier;
    float speed;

    public float timeToMaxSpeed;
    float timer;
    bool setTimer;

    public AnimationCurve accCurve;

    [Header("gravity")]
    public float gravityStrength;
    public float gPull;

    public float gravityClamp;

    [Header("knockBack")]
    public knockable knockBackinfo;

    [Header("check if grounded")]
    public LayerMask groundMask;
    public float length;

    private void Start()
    {
        name = "player " + OwnerClientId;
    }
    private void OnEnable()
    {
        moving.action.Enable();
    }
    private void Update()
    {
        if (IsOwner == false || IsSpawned == false) return;

        pointDir = moving.action.ReadValue<Vector2>();

        if (pointDir.x != 0)
        {
            moveDir = moving.action.ReadValue<Vector2>();

            if (setTimer == false)
            {
                timer = 0;
                setTimer = true;
            }

            timer = Mathf.Min(timer + Time.deltaTime, timeToMaxSpeed);
        }
        else
        {
            setTimer = false;
            timer = Mathf.Max(timer - Time.deltaTime, 0);
        }

        float dt = timer / timeToMaxSpeed;
        speed = accCurve.Evaluate(dt);
    }
    private void FixedUpdate()
    {

        if (grounded())
        {
            gPull = 0;
        }
        else
        {
            gPull = Mathf.Max(gPull - gravityStrength, -gravityClamp);
        }

        rb.linearVelocity = new Vector2(moveDir.x * speed * speedMultiplier, gPull) + knockBackinfo.targetVel;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, Vector2.down * length);
    }

    bool grounded()
    {
        return Physics2D.Raycast(transform.position, Vector2.down, length, groundMask);
    }
}
