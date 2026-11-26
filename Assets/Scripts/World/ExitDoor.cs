using UnityEngine;

// Çıkış kapısı: anahtar yoksa kilitli. Anahtarla girince sonraki seviye.
public class ExitDoor : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D o)
    {
        if (!o.GetComponent<PlayerController>()) return;
        var gb = GameBootstrap.Instance;
        if (gb.hasKey) gb.NextLevel();
        else gb.ShowMessage("Kapı kilitli! Önce anahtarı bul");
    }
}

