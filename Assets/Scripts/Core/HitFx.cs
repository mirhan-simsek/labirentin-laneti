using UnityEngine;

// Canavar ölümü ve sandık açılışında dağılan küçük pixel parçacıkları.
public class HitFx : MonoBehaviour
{
    Vector2 vel;
    float life;

    public static void Burst(Vector3 pos, Color color, int count = 7)
    {
        for (int i = 0; i < count; i++)
        {
            var go = GameBootstrap.CreateBox("Px", color, Vector2.one * Random.Range(0.10f, 0.18f));
            go.transform.position = pos;
            go.GetComponent<SpriteRenderer>().sortingOrder = 9;
            var fx = go.AddComponent<HitFx>();
            fx.vel = new Vector2(Random.Range(-4.5f, 4.5f), Random.Range(1.5f, 6.5f));
            fx.life = Random.Range(0.25f, 0.45f);
        }
    }

    void Update()
    {
        vel.y -= 14f * Time.deltaTime;
        transform.position += (Vector3)(vel * Time.deltaTime);
        life -= Time.deltaTime;
        if (life <= 0f) Destroy(gameObject);
    }
}
