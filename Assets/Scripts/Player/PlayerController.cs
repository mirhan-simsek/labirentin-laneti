using UnityEngine;
using UnityEngine.InputSystem;

// Yandan görünüm oyuncu: A/D koş, Space zıpla, W/S merdiven, Shift dash.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float jumpForce = 11f;
    public float climbSpeed = 4f;
    public float dashSpeed = 16f;
    public float dashTime = 0.15f;
    public float dashCooldown = 0.6f;

    Rigidbody2D rb;
    ContactFilter2D groundFilter;
    float gravity;
    int ladderCount;
    bool climbing;
    float dashTimer, dashCdTimer;
    int facing = 1;
    public int Facing => facing;
    [HideInInspector] public float stun;
    float speedBoostTimer;

    public void GiveSpeedBoost(float duration) { speedBoostTimer = Mathf.Max(speedBoostTimer, duration); }
    public float SpeedBoostRemaining => Mathf.Max(0f, speedBoostTimer);

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        gravity = rb.gravityScale;
        groundFilter.useNormalAngle = true;
        groundFilter.SetNormalAngle(45, 135); // yukarı bakan yüzeyler = zemin
        groundFilter.useTriggers = false;
    }

    bool Grounded => rb.IsTouching(groundFilter);

    void Update()
    {
        if (Time.timeScale == 0f) return;
        var k = Keyboard.current;
        if (stun > 0) { stun -= Time.deltaTime; return; } // hasar alınca kısa sersemleme
        if (speedBoostTimer > 0) speedBoostTimer -= Time.deltaTime;
        float curSpeed = speedBoostTimer > 0 ? moveSpeed * 1.45f : moveSpeed;

        float kx = k != null ? (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0) : 0;
        float ky = k != null ? (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0) : 0;
        float x = Mathf.Clamp(kx + TouchControls.MoveX, -1f, 1f);
        float y = Mathf.Clamp(ky + TouchControls.MoveY, -1f, 1f);
        if (x != 0) { facing = (int)Mathf.Sign(x); GetComponent<SpriteRenderer>().flipX = facing < 0; }

        dashCdTimer -= Time.deltaTime;
        if (dashTimer > 0)
        {
            dashTimer -= Time.deltaTime;
            rb.linearVelocity = new Vector2(facing * dashSpeed, 0);
            if (dashTimer <= 0) rb.gravityScale = climbing ? 0 : gravity;
            return;
        }
        bool dashDown = (k != null && k.leftShiftKey.wasPressedThisFrame) || TouchControls.DashPressed;
        if (dashDown && dashCdTimer <= 0)
        {
            dashTimer = dashTime; dashCdTimer = dashCooldown;
            rb.gravityScale = 0; Sfx.PlayDash(); return;
        }

        // Merdiven: Joystick yukarı/aşağı VEYA Zıpla'ya basılı tutunca yukarı tırmanır
        bool jumpDown = (k != null && k.spaceKey.wasPressedThisFrame) || TouchControls.JumpPressed;
        bool jumpHeld = (k != null && k.spaceKey.isPressed) || TouchControls.JumpHeld;

        if (ladderCount > 0 && (Mathf.Abs(y) > 0.15f || jumpHeld)) climbing = true;
        if (ladderCount == 0) climbing = false;
        rb.gravityScale = climbing ? 0 : gravity;

        if (climbing)
        {
            float climbY = jumpHeld ? 1f : y;
            rb.linearVelocity = new Vector2(x * moveSpeed * 0.65f, climbY * climbSpeed);
            return;
        }

        rb.linearVelocity = new Vector2(x * curSpeed, rb.linearVelocity.y);
        if (jumpDown && Grounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            Sfx.PlayJump();
        }
    }

    void OnTriggerEnter2D(Collider2D o) { if (o.GetComponent<Ladder>()) ladderCount++; }
    void OnTriggerExit2D(Collider2D o) { if (o.GetComponent<Ladder>()) ladderCount = Mathf.Max(0, ladderCount - 1); }
}
