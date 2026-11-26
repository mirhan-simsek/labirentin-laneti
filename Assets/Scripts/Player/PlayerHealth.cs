using UnityEngine;

// Oyuncunun canı (7) ve ekipmandan gelen kalkanı (Miğfer +1, Zırh +2).
public class PlayerHealth : MonoBehaviour
{
    public const int BaseMaxHp = 7;
    public int maxHp = BaseMaxHp;
    public int hp = BaseMaxHp;
    public int maxShield;
    public int shield;
    public float invulnTime = 1f;

    float invuln;
    SpriteRenderer sr;
    Rigidbody2D rb;
    PlayerController ctrl;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        ctrl = GetComponent<PlayerController>();
    }

    public void ResetHealth()
    {
        maxHp = BaseMaxHp;
        hp = maxHp;
        maxShield = ShopData.GetTotalShield();
        shield = maxShield;
        invuln = 0;
        sr.enabled = true;
    }

    public void RefreshShieldCap()
    {
        int newMax = ShopData.GetTotalShield();
        if (newMax > maxShield) shield += (newMax - maxShield);
        maxShield = newMax;
        shield = Mathf.Min(shield, maxShield);
    }

    // Can full olsa bile iksir yeni bir kalp (+1 maksimum can) ekler
    public void Heal(int amount)
    {
        hp += amount;
        if (hp > maxHp) maxHp = hp;
    }

    public void TakeDamage(int dmg, Vector2 from)
    {
        if (invuln > 0) return;

        // Önce kalkan hasarı emer
        if (shield > 0)
        {
            int absorbed = Mathf.Min(shield, dmg);
            shield -= absorbed;
            dmg -= absorbed;
        }
        hp -= dmg;

        invuln = invulnTime;
        ctrl.stun = 0.2f;
        float dir = Mathf.Sign(transform.position.x - from.x);
        rb.linearVelocity = new Vector2(dir * 7f, 6f);
        if (hp <= 0) GameBootstrap.Instance.PlayerDied();
        else Sfx.PlayPlayerHurt();
    }

    void Update()
    {
        if (invuln > 0)
        {
            invuln -= Time.deltaTime;
            sr.enabled = Mathf.Repeat(invuln, 0.15f) > 0.075f; // yanıp sönme
            if (invuln <= 0) sr.enabled = true;
        }
    }
}
