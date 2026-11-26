using UnityEngine;

// Oyuncu tabanca mermisi ve düşman (güdümlü/düz) ateş topları.
public class Projectile : MonoBehaviour
{
    Vector2 dir;
    float speed;
    int damage;
    bool fromPlayer;
    bool spin;
    Transform homingTarget;
    float turnSpeed;

    public static GameObject Spawn(Vector3 pos, Vector2 direction, int dmg, bool isPlayer, float spd = 8.5f, Transform homing = null, float turn = 2.5f)
    {
        var sprite = isPlayer ? PixelArt.PlayerBullet : PixelArt.Fireball;
        var go = GameBootstrap.CreateSprite(isPlayer ? "Bullet" : "Fireball", sprite, 6);
        go.transform.position = pos;
        if (isPlayer)
            go.GetComponent<SpriteRenderer>().flipX = direction.x < 0;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = isPlayer ? 0.18f : 0.22f;

        var p = go.AddComponent<Projectile>();
        p.dir = direction.normalized;
        p.damage = dmg;
        p.fromPlayer = isPlayer;
        p.speed = spd;
        p.spin = !isPlayer;
        p.homingTarget = homing;
        p.turnSpeed = turn;

        Color lightCol = isPlayer ? new Color(1f, 0.92f, 0.45f) : new Color(1f, 0.42f, 0.15f);
        LightHelper.AddPointLight(go, lightCol, isPlayer ? 2.4f : 2.8f, 1.0f, false, 0.2f);

        Destroy(go, homing ? 5.5f : 4f);
        GameBootstrap.Instance.Track(go);
        return go;
    }

    void FixedUpdate()
    {
        // Büyücü / boss ateş topu oyuncuyu yumuşakça takip eder
        if (homingTarget != null)
        {
            Vector2 desired = ((Vector2)homingTarget.position - (Vector2)transform.position).normalized;
            dir = Vector2.Lerp(dir, desired, turnSpeed * Time.fixedDeltaTime).normalized;
        }
        transform.position += (Vector3)(dir * speed * Time.fixedDeltaTime);
        if (spin) transform.Rotate(0, 0, 360f * Time.fixedDeltaTime);
    }

    void OnTriggerEnter2D(Collider2D o)
    {
        if (o == MazeGenerator.WallCollider)
        {
            Destroy(gameObject);
            return;
        }

        if (fromPlayer)
        {
            var e = o.GetComponent<Enemy>();
            if (e)
            {
                e.TakeHit(damage, transform.position);
                Destroy(gameObject);
            }
        }
        else
        {
            var ph = o.GetComponent<PlayerHealth>();
            if (ph)
            {
                ph.TakeDamage(damage, transform.position);
                Destroy(gameObject);
            }
        }
    }
}
