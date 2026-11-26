using UnityEngine;

// Canavarlar:
// - Slime: kör devriye (hiçbir şeyi görmez)
// - Bat: duvarların arasından geçer, yaklaşınca takip eder
// - Skeleton: kılıçlı + vurma animasyonlu, merdivenlerden inip çıkabilir
// - Mage: uzaktan güdümlü ateş topu atar, 1 can (tek vuruşta ölür)
// - Boss: her 3 seviyede bir, 2 fazlı, ölünce anahtar düşürür
public class Enemy : MonoBehaviour
{
    public enum Kind { Slime, Bat, Skeleton, Mage, Boss }
    public Kind kind;
    public int hp, maxHp, damage;
    public float speed;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Transform player;
    Color baseColor;
    int dir = 1;
    float hurtTimer, phase, shootTimer, roarTimer, bossJumpCd;
    int bossTier = 1, bossShotCount;

    // Merdiven (İskelet)
    int ladderCount;
    bool climbing;
    float defaultGravity = 3f;

    // Kılıç animasyonu (İskelet & Boss)
    Transform weaponPivot;
    SpriteRenderer weaponSr;
    float swingTimer, swingCd;
    const float swingDuration = 0.24f;

    public static GameObject Spawn(Kind kind, Vector3 pos, Transform player)
    {
        Sprite s; Vector2 size; float scale = 1f;
        switch (kind)
        {
            case Kind.Bat: s = PixelArt.BatUp; size = new Vector2(0.6f, 0.4f); break;
            case Kind.Skeleton: s = PixelArt.Skeleton; size = new Vector2(0.55f, 0.95f); break;
            case Kind.Mage: s = PixelArt.Mage; size = new Vector2(0.65f, 0.95f); break;
            case Kind.Boss: s = PixelArt.Boss; size = new Vector2(0.85f, 0.95f); scale = 1.9f; break;
            default: s = PixelArt.Slime; size = new Vector2(0.8f, 0.55f); break;
        }
        var go = GameBootstrap.CreateSprite(kind.ToString(), s);
        go.transform.localScale = Vector3.one * scale;
        go.transform.position = pos + Vector3.up * (kind == Kind.Bat ? 1.5f : size.y * scale / 2f);
        GameBootstrap.AddBody(go, kind == Kind.Bat ? 0 : 3, size);

        // Yarasalar duvarların arasından geçebilir
        if (kind == Kind.Bat && MazeGenerator.WallCollider != null)
            Physics2D.IgnoreCollision(go.GetComponent<Collider2D>(), MazeGenerator.WallCollider, true);

        if (kind == Kind.Boss)
            LightHelper.AddPointLight(go, new Color(1f, 0.25f, 0.15f), 5.5f, 1.4f, true, 0.2f);

        var e = go.AddComponent<Enemy>();
        e.Init(kind, player);
        return go;
    }

    void Init(Kind k, Transform p)
    {
        kind = k; player = p;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
        defaultGravity = rb.gravityScale;
        dir = Random.value < 0.5f ? -1 : 1;
        phase = Random.value * 10f;
        shootTimer = k == Kind.Boss ? 2.5f : 0.45f;

        switch (k)
        {
            case Kind.Slime: hp = 2; damage = 1; speed = 1.5f; break;
            case Kind.Bat: hp = 1; damage = 1; speed = 3.2f; break;
            case Kind.Skeleton:
                hp = 2; damage = 2; speed = 2.1f;
                SetupWeapon(0.25f, 0.34f);
                break;
            case Kind.Mage: hp = 1; damage = 1; speed = 2.4f; break; // tek vuruşta ölür
            case Kind.Boss:
                bossTier = Mathf.Clamp(GameBootstrap.Instance.level / 5, 1, 4);
                if (bossTier == 1)      { hp = 18; damage = 2; speed = 2.0f; }  // Bölüm 5: Orman Bekçisi
                else if (bossTier == 2) { hp = 20; damage = 2; speed = 2.45f; } // Bölüm 10: Kristal Bekçisi
                else if (bossTier == 3) { hp = 22; damage = 2; speed = 2.75f; } // Bölüm 15: Lav Bekçisi
                else                    { hp = 25; damage = 3; speed = 2.95f; } // Bölüm 20: Karanlık Kral (7 canı tam 3 vuruşta öldürür)
                SetupWeapon(0.44f, 0.28f);
                break;
        }
        maxHp = hp;
    }

    void SetupWeapon(float xOff, float yOff)
    {
        var pivotGo = new GameObject("WeaponPivot");
        pivotGo.transform.SetParent(transform, false);
        weaponPivot = pivotGo.transform;

        var bladeGo = GameBootstrap.CreateSprite("BoneSword", PixelArt.BoneSword, 6);
        bladeGo.transform.SetParent(weaponPivot, false);
        bladeGo.transform.localPosition = new Vector3(0f, yOff, 0f);
        weaponSr = bladeGo.GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (kind == Kind.Bat)
        {
            sr.sprite = Mathf.Repeat(Time.time + phase, 0.3f) < 0.15f ? PixelArt.BatUp : PixelArt.BatDown;
            sr.flipX = rb.linearVelocity.x < 0;
            return;
        }

        sr.flipX = dir < 0;

        if (kind == Kind.Slime)
        {
            // Genişliği büyütmeden hafif dikey esneme (collider duvar içine taşmaz)
            float s = Mathf.Abs(Mathf.Sin(Time.time * 6f + phase)) * 0.08f;
            transform.localScale = new Vector3(1f - s * 0.4f, 1f + s, 1f);
        }
        else if (weaponPivot != null)
        {
            // Boss yürüme ve kükreme animasyonu
            if (kind == Kind.Boss)
            {
                bool walking = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
                float walkTilt = walking ? Mathf.Sin(Time.time * 6f) * 4.5f : 0f;
                float roarPulse = 0f;
                if (roarTimer > 0f)
                {
                    roarTimer -= Time.deltaTime;
                    roarPulse = Mathf.Sin((roarTimer / 0.45f) * Mathf.PI) * 0.22f;
                }
                transform.localScale = Vector3.one * (1.9f + roarPulse);
                transform.localRotation = Quaternion.Euler(0, 0, walkTilt);
            }

            // Kılıç tam elin içine otursun (Boss'un eli y=-0.16, x=±0.44)
            if (kind == Kind.Boss)
                weaponPivot.localPosition = new Vector3(dir * 0.44f, -0.16f, 0f);
            else
                weaponPivot.localPosition = new Vector3(dir * 0.25f, -0.04f, 0f);

            float dur = kind == Kind.Boss ? 0.34f : swingDuration;
            weaponSr.flipX = dir < 0;
            if (swingTimer > 0f)
            {
                swingTimer -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(swingTimer / dur);
                float angle = Mathf.Lerp(65f, -100f, t) * dir;
                weaponPivot.localRotation = Quaternion.Euler(0, 0, angle);
            }
            else
            {
                float idleBob = Mathf.Sin(Time.time * 5f + phase) * 6f;
                weaponPivot.localRotation = Quaternion.Euler(0, 0, (-22f + idleBob) * dir);
            }
        }
    }

    void FixedUpdate()
    {
        if (!player) return;
        if (hurtTimer > 0)
        {
            hurtTimer -= Time.fixedDeltaTime;
            if (hurtTimer <= 0) sr.color = (kind == Kind.Boss && hp <= maxHp / 2) ? new Color(1f, 0.65f, 0.65f) : baseColor;
            return;
        }

        Vector2 toP = player.position - transform.position;
        float dist = toP.magnitude;

        // 1) SLIME: Hiçbir şeyi görmez, sadece kör devriye
        if (kind == Kind.Slime)
        {
            if (WallInDir(dir) || !GroundInDir(dir)) dir = -dir;
            rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
            return;
        }

        // 2) BAT: Duvarlardan geçer, yaklaşınca takip eder
        if (kind == Kind.Bat)
        {
            if (dist < 6.5f)
                rb.linearVelocity = toP.normalized * speed + Vector2.up * Mathf.Sin(Time.time * 6f + phase) * 1.2f;
            else
                rb.linearVelocity = new Vector2(0, Mathf.Sin(Time.time * 3f + phase) * 0.5f);
            return;
        }

        // 3) MAGE: Zeki menzilli, güdümlü ateş topu atar
        if (kind == Kind.Mage)
        {
            bool canSee = dist < 11f && !HitsWall(transform.position, toP.normalized, dist);
            if (canSee)
            {
                dir = toP.x > 0 ? 1 : -1;
                float vx = 0f;
                if (dist < 3.6f)
                {
                    int away = -dir;
                    if (!WallInDir(away) && GroundInDir(away)) vx = away * speed;
                }
                rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

                shootTimer -= Time.fixedDeltaTime;
                if (shootTimer <= 0f)
                {
                    shootTimer = 1.35f;
                    Sfx.PlayEnemyShoot();
                    // Oyuncuyu takip eden (homing) ateş topu
                    Projectile.Spawn(transform.position + Vector3.right * dir * 0.45f, toP, damage, false, 6.2f, player, 2.7f);
                }
                return;
            }
            shootTimer = Mathf.Min(shootTimer, 0.4f);
            if (WallInDir(dir) || !GroundInDir(dir)) dir = -dir;
            rb.linearVelocity = new Vector2(dir * speed * 0.75f, rb.linearVelocity.y);
            return;
        }

        // 4) BOSS: 2 fazlı Labirent Bekçisi
        if (kind == Kind.Boss)
        {
            UpdateBoss(toP, dist);
            return;
        }

        // 5) SKELETON: Merdivenlerden inip çıkabilir, görünce kovalayıp kılıç sallar
        UpdateSkeleton(toP, dist);
    }

    void UpdateSkeleton(Vector2 toP, float dist)
    {
        bool aware = dist < 8.5f;
        bool wantVertical = aware && Mathf.Abs(toP.y) > 0.9f;

        if (ladderCount > 0 && wantVertical) climbing = true;
        else if (ladderCount == 0 || !wantVertical) climbing = false;

        rb.gravityScale = climbing ? 0f : defaultGravity;

        if (climbing)
        {
            float vy = Mathf.Sign(toP.y) * 3.0f;
            float vx = Mathf.Abs(toP.x) > 0.25f ? Mathf.Sign(toP.x) * 1.4f : 0f;
            dir = toP.x >= 0 ? 1 : -1;
            rb.linearVelocity = new Vector2(vx, vy);
            return;
        }

        float sp = speed;
        if (aware && Mathf.Abs(toP.y) < 1.3f)
        {
            dir = toP.x > 0 ? 1 : -1;
            sp *= 1.8f;
            swingCd -= Time.fixedDeltaTime;
            if (dist < 1.55f && swingCd <= 0f)
            {
                swingCd = 0.85f;
                swingTimer = swingDuration;
                Sfx.PlaySlash();
                var ph = player.GetComponent<PlayerHealth>();
                if (ph && dist < 1.45f) ph.TakeDamage(damage, transform.position);
            }
        }
        else
        {
            if (WallInDir(dir)) dir = -dir;
            // Merdiven girişindeyse aşağı inmek için boşlukta dönmesin
            if (!GroundInDir(dir) && !(wantVertical && toP.y < 0 && ladderCount > 0)) dir = -dir;
        }

        rb.linearVelocity = new Vector2(dir * sp, rb.linearVelocity.y);
    }

    void UpdateBoss(Vector2 toP, float dist)
    {
        // Tüm parkur arenasında oyuncuyu takip eder
        dir = toP.x >= 0 ? 1 : -1;
        bossJumpCd -= Time.fixedDeltaTime;

        bool onGround = Mathf.Abs(rb.linearVelocity.y) < 0.08f &&
            HitsWall(transform.position, Vector2.down, Half.y + 0.25f);

        // Oyuncu üst platformdaysa veya arada duvar/engel varsa zıplayarak parkuru takip et
        if (onGround && bossJumpCd <= 0f && (toP.y > 1.8f || WallInDir(dir)))
        {
            bossJumpCd = bossTier == 1 ? 2.1f : (bossTier == 2 ? 1.65f : 1.5f);
            rb.linearVelocity = new Vector2(dir * speed * 1.2f, 11.8f);
        }
        else
        {
            float vx = (dist > 1.7f && !WallInDir(dir)) ? dir * speed : 0f;
            rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
        }

        // Yanına gidince yakın kılıç vuruşu
        swingCd -= Time.fixedDeltaTime;
        if (dist < 2.3f)
        {
            if (swingCd <= 0f)
            {
                swingCd = bossTier == 1 ? 1.55f : (bossTier == 2 ? 1.35f : 1.25f);
                swingTimer = 0.34f;
                Sfx.PlaySlash();
                var ph = player.GetComponent<PlayerHealth>();
                if (ph && dist < 2.2f) ph.TakeDamage(damage, transform.position);
            }
        }
        else if (dist < 28f)
        {
            // Uzaktayken kükreyerek atış yapar (Bölüm 9'da her 3. atışta 3'lü salvo!)
            shootTimer -= Time.fixedDeltaTime;
            if (shootTimer <= 0f && !HitsWall(transform.position, toP.normalized, dist))
            {
                shootTimer = bossTier == 1 ? 5.0f : (bossTier == 2 ? 4.2f : 3.8f);
                roarTimer = 0.45f;
                Sfx.PlayBossRoar();
                bossShotCount++;

                Vector2 aim = toP;
                var prb = player.GetComponent<Rigidbody2D>();
                if (prb != null) aim += prb.linearVelocity * Mathf.Clamp(dist / 14f, 0f, 0.3f);
                Vector3 muzzle = transform.position + Vector3.right * dir * 0.85f;
                float projSpd = bossTier == 1 ? 7.2f : 8.0f;

                if (bossTier >= 4 && bossShotCount % 3 == 0)
                {
                    // Bölüm 20 Final Boss: 3 atışta 1 üçlü salvo
                    Projectile.Spawn(muzzle, aim, damage, false, projSpd);
                    Projectile.Spawn(muzzle, Quaternion.Euler(0, 0, 16f) * aim, damage, false, projSpd);
                    Projectile.Spawn(muzzle, Quaternion.Euler(0, 0, -16f) * aim, damage, false, projSpd);
                }
                else if (bossTier == 3 && bossShotCount % 3 == 0)
                {
                    // Bölüm 15 Lav Bekçisi: 3 atışta 1 ikili atış
                    Projectile.Spawn(muzzle, Quaternion.Euler(0, 0, 10f) * aim, damage, false, projSpd);
                    Projectile.Spawn(muzzle, Quaternion.Euler(0, 0, -10f) * aim, damage, false, projSpd);
                }
                else
                {
                    Projectile.Spawn(muzzle, aim, damage, false, projSpd);
                }
            }
        }
    }

    bool HitsWall(Vector2 origin, Vector2 d, float len)
    {
        foreach (var h in Physics2D.RaycastAll(origin, d, len))
            if (h.collider == MazeGenerator.WallCollider) return true;
        return false;
    }

    Vector2 Half => GetComponent<BoxCollider2D>().size * transform.localScale.x / 2f;
    bool WallInDir(int d) => HitsWall(transform.position, Vector2.right * d, Half.x + 0.16f);
    bool GroundInDir(int d) => HitsWall((Vector2)transform.position + Vector2.right * d * (Half.x + 0.08f), Vector2.down, Half.y + 0.35f);

    public void TakeHit(int dmg, Vector2 from)
    {
        hp -= dmg;
        hurtTimer = kind == Kind.Boss ? 0.1f : 0.2f;
        sr.color = Color.red;
        float d = Mathf.Sign(transform.position.x - from.x);
        float knock = kind == Kind.Boss ? 2.5f : 6f;
        rb.linearVelocity = new Vector2(d * knock, kind == Kind.Boss ? 1.5f : 3f);
        if (hp <= 0)
        {
            Sfx.PlayEnemyDie();
            HitFx.Burst(transform.position, kind == Kind.Boss ? new Color(1f, 0.35f, 0.2f) : baseColor, kind == Kind.Boss ? 18 : 7);
            GameBootstrap.Instance.AddKill();
            if (kind == Kind.Boss) GameBootstrap.Instance.OnBossDefeated(transform.position);
            Destroy(gameObject);
        }
        else Sfx.PlayHitEnemy();
    }

    void OnTriggerEnter2D(Collider2D o) { if (o.GetComponent<Ladder>()) ladderCount++; }
    void OnTriggerExit2D(Collider2D o) { if (o.GetComponent<Ladder>()) ladderCount = Mathf.Max(0, ladderCount - 1); }

    void OnCollisionStay2D(Collision2D c)
    {
        var ph = c.collider.GetComponent<PlayerHealth>();
        if (ph) ph.TakeDamage(damage, transform.position);
    }
}
