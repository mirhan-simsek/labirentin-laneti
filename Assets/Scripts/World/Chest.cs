using UnityEngine;
using UnityEngine.InputSystem;

// Sandık: yanındayken E'ye bas, içinden iksir veya altın çıkar.
public class Chest : MonoBehaviour
{
    bool playerNear;

    public static GameObject Spawn(Vector3 floorPos)
    {
        var go = GameBootstrap.CreateSprite("Chest", PixelArt.Chest);
        go.transform.position = floorPos + Vector3.up * 0.3125f;
        go.GetComponent<SpriteRenderer>().sortingOrder = 2;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true; col.size = new Vector2(2f, 2f); // biraz geniş algılama alanı
        go.AddComponent<Chest>();
        return go;
    }

    void Update()
    {
        bool openDown = (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) || TouchControls.InteractPressed;
        if (Time.timeScale == 0f || !playerNear || !openDown) return;
        Sfx.PlayChest();
        var pos = transform.position + Vector3.up * 0.6f;
        HitFx.Burst(transform.position + Vector3.up * 0.2f, new Color(1f, 0.85f, 0.3f), 8);
        float r = Random.value;
        if (r < 0.45f) GameBootstrap.Instance.Track(Pickup.Spawn(Pickup.Type.Potion, pos));
        else if (r < 0.65f) GameBootstrap.Instance.Track(Pickup.Spawn(Pickup.Type.SpeedBoost, pos));
        else
            for (int i = 0; i < 3; i++)
                GameBootstrap.Instance.Track(Pickup.Spawn(Pickup.Type.Gold, pos + Vector3.right * (i - 1) * 0.35f));
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D o)
    {
        if (!o.GetComponent<PlayerController>()) return;
        playerNear = true;
        GameBootstrap.Instance.ShowMessage("Sandığı açmak için E");
    }

    void OnTriggerExit2D(Collider2D o) { if (o.GetComponent<PlayerController>()) playerNear = false; }
}
