using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Hierarchy;

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
    public float horiznotalCheckLength;

    [Header("animation")]
    public Animator anim;
    public float flyingThreshold;

    public GameObject bodySprite;
    public float flipSpeed;
    float sizeX;
    private void Start()
    {
        name = "player " + OwnerClientId;

        pointDir = Vector2.right;
        moveDir = Vector2.right;
        sizeX = -1;
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

        grounded(length * 1.4f, out RaycastHit2D hit, true);

        anim.SetBool("running", pointDir.x != 0 && hit);
        anim.SetBool("flying", !hit);

        bodySprite.transform.localScale = new Vector3(sizeX, 1, 1);
        if (sizeX != -moveDir.x)
        {
            sizeX = Mathf.Clamp(sizeX + (flipSpeed * -moveDir.x * Time.deltaTime), -1, 1);
        }

        transform.up = grounded(length * 1.5f) ? hit.normal : rb.linearVelocity;
    }
    private void FixedUpdate()
    {
        float moveValue = moveDir.x * speed * speedMultiplier;
        grounded(length, out RaycastHit2D hit, true);

        Vector2 normal = hit.normal;
        rb.position -= normal * Vector3.Dot((Vector2)transform.position - (length * (Vector2)transform.up) - hit.point, normal);

        RaycastHit2D forwardHit = Physics2D.Raycast(transform.position + length * -transform.up, new Vector2(moveDir.x, 0), horiznotalCheckLength, groundMask);
        RaycastHit2D downhillHit = Physics2D.Raycast(transform.position + length * -transform.up, new Vector2(-moveDir.x, 0), horiznotalCheckLength, groundMask);

        bool usingForwardHit = (forwardHit) && moveDir.x != 0;
        if (usingForwardHit) hit = forwardHit;

        if (hit || downhillHit)
        {
            gPull = 0;

            rb.linearVelocity = (Vector2)transform.right * moveValue + knockBackinfo.targetVel;

            Debug.DrawRay(transform.position, -transform.up * length, hit.normal == Vector2.up ? Color.red : (usingForwardHit ? Color.blue : Color.yellow));
        }
        else
        {
            gPull = Mathf.Max(gPull - gravityStrength, -gravityClamp);
            rb.linearVelocity = new Vector2(moveValue, gPull) + knockBackinfo.targetVel;
        }
    }

    bool grounded(float length, out RaycastHit2D hit, bool useLocalDown = false)
    {
        return hit = Physics2D.Raycast(transform.position, useLocalDown ? -transform.up : Vector2.down, length, groundMask);
    }
    bool grounded(float length, bool useLocalDown = false)
    {
        return Physics2D.Raycast(transform.position, useLocalDown ? -transform.up : Vector2.down, length, groundMask);
    }
}