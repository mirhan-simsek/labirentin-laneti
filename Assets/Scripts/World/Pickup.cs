using UnityEngine;

// Yerden alınan eşyalar: Anahtar, İksir (can), Altın, Hız İksiri.
public class Pickup : MonoBehaviour
{
    public enum Type { Key, Potion, Gold, SpeedBoost }
    public Type type;
    Vector3 basePos;

    public static GameObject Spawn(Type t, Vector3 pos)
    {
        Sprite s = t == Type.Key ? PixelArt.Key
                 : t == Type.Potion ? PixelArt.Potion
                 : t == Type.SpeedBoost ? PixelArt.SpeedPotion
                 : PixelArt.Gold;
        var go = GameBootstrap.CreateSprite(t.ToString(), s);
        go.transform.position = pos;
        go.AddComponent<BoxCollider2D>().isTrigger = true;
        var p = go.AddComponent<Pickup>();
        p.type = t; p.basePos = pos;
        return go;
    }

    void Update() // havada süzülme efekti
    {
        transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * 3f + basePos.x) * 0.1f;
    }

    void OnTriggerEnter2D(Collider2D o)
    {
        if (!o.GetComponent<PlayerController>()) return;
        GameBootstrap.Instance.Collect(type);
        Destroy(gameObject);
    }
}
