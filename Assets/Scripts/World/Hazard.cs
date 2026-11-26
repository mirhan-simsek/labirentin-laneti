using UnityEngine;

// Zemin tuzakları (Diken veya Lav). Üstünden zıplanarak veya dash ile geçilir.
public class Hazard : MonoBehaviour
{
    public int damage = 1;

    public static GameObject Spawn(Vector3 floorPos, bool isLava)
    {
        var go = GameBootstrap.CreateSprite(isLava ? "Lava" : "Spikes", isLava ? PixelArt.Lava : PixelArt.Spikes, 2);
        go.transform.position = floorPos + Vector3.up * 0.22f;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.75f, 0.35f);
        var h = go.AddComponent<Hazard>();
        h.damage = 1;
        if (isLava) LightHelper.AddPointLight(go, new Color(1f, 0.35f, 0.08f), 2.5f, 0.9f, true, 0.2f);
        return go;
    }

    void OnTriggerStay2D(Collider2D o)
    {
        var ph = o.GetComponent<PlayerHealth>();
        if (ph) ph.TakeDamage(damage, transform.position);
    }
}
