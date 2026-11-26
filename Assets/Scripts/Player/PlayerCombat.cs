using UnityEngine;
using UnityEngine.InputSystem;

// Elde tutulan animasyonlu kılıç (J / Sol tık) ve 4 sn beklemeli tabanca (K / Sağ tık).
public class PlayerCombat : MonoBehaviour
{
    public int damage = 1;
    public float cooldown = 0.35f;
    public float range = 0.85f;

    public int gunDamage = 2;
    public float gunCooldown = 2f;
    public float bulletSpeed = 16f;

    float cd, gunCd;
    float swingTimer;
    const float swingDuration = 0.18f;

    PlayerController ctrl;
    Transform swordPivot;
    SpriteRenderer swordSr;

    public float GunCooldownRemaining => Mathf.Max(0f, gunCd);
    public void ResetAmmo() { gunCd = 0f; }

    void Awake()
    {
        ctrl = GetComponent<PlayerController>();

        // Karakterin elindeki kılıç pivotu
        var pivotGo = new GameObject("SwordPivot");
        pivotGo.transform.SetParent(transform, false);
        swordPivot = pivotGo.transform;

        var bladeGo = GameBootstrap.CreateSprite("Sword", PixelArt.GetSwordSkin(ShopData.EquippedSword), 6);
        bladeGo.transform.SetParent(swordPivot, false);
        bladeGo.transform.localPosition = new Vector3(0f, 0.42f, 0f);
        swordSr = bladeGo.GetComponent<SpriteRenderer>();
    }

    public void ApplySwordSkin()
    {
        if (swordSr) swordSr.sprite = PixelArt.GetSwordSkin(ShopData.EquippedSword);
    }

    void Update()
    {
        UpdateSwordVisual();
        if (Time.timeScale == 0f) return;

        cd -= Time.deltaTime;
        gunCd = Mathf.Max(0f, gunCd - Time.deltaTime);

        var k = Keyboard.current; var m = Mouse.current;

        bool slashPressed = (k != null && k.jKey.wasPressedThisFrame) || (!TouchControls.Visible && m != null && m.leftButton.wasPressedThisFrame) || TouchControls.SlashPressed;
        if (slashPressed && cd <= 0f) DoSlash();

        bool gunPressed = (k != null && k.kKey.wasPressedThisFrame) || (!TouchControls.Visible && m != null && m.rightButton.wasPressedThisFrame) || TouchControls.GunPressed;
        if (gunPressed && gunCd <= 0f) DoGunShot();
    }

    void UpdateSwordVisual()
    {
        int f = ctrl.Facing;
        swordPivot.localPosition = new Vector3(f * 0.28f, -0.05f, 0f);
        swordSr.flipX = f < 0;

        if (swingTimer > 0f)
        {
            swingTimer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(swingTimer / swingDuration);
            // Yukarıdan öne doğru keskin savurma yayı
            float angle = Mathf.Lerp(55f, -105f, t) * f;
            swordPivot.localRotation = Quaternion.Euler(0, 0, angle);
        }
        else
        {
            // Elde hazır duruş açısı
            swordPivot.localRotation = Quaternion.Euler(0, 0, -25f * f);
        }
    }

    void DoSlash()
    {
        cd = cooldown;
        swingTimer = swingDuration;
        Sfx.PlaySlash();

        Vector2 center = (Vector2)transform.position + Vector2.right * ctrl.Facing * range;
        Vector2 size = new Vector2(1.05f, 1.0f);
        foreach (var hit in Physics2D.OverlapBoxAll(center, size, 0))
        {
            var e = hit.GetComponent<Enemy>();
            if (e) e.TakeHit(damage, transform.position);
        }
    }

    void DoGunShot()
    {
        gunCd = gunCooldown;
        Sfx.PlayGun();

        Vector2 dir = Vector2.right * ctrl.Facing;
        Vector3 spawnPos = transform.position + (Vector3)(dir * 0.55f);
        Projectile.Spawn(spawnPos, dir, gunDamage, true, bulletSpeed);
    }
}
