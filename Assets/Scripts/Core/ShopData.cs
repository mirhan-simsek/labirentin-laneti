using UnityEngine;

// Kalıcı altın, mağaza ürünleri, kostüm/kılıç skinleri ve zırh/miğfer kalkan verileri.
public static class ShopData
{
    public struct Item
    {
        public string id, name, desc;
        public int price;
        public Item(string id, string name, string desc, int price)
        { this.id = id; this.name = name; this.desc = desc; this.price = price; }
    }

    public static readonly Item[] Costumes = {
        new Item("skin_0", "Mavi Şövalye", "Varsayılan (Görsel)", 0),
        new Item("skin_1", "Kızıl Şövalye", "Sadece görsel", 10),
        new Item("skin_2", "Altın Şövalye", "Sadece görsel", 20),
        new Item("skin_3", "Gölge Ninja", "Sadece görsel", 25),
    };

    public static readonly Item[] Swords = {
        new Item("sword_0", "Demir Kılıç", "Varsayılan (Görsel)", 0),
        new Item("sword_1", "Alev Kılıcı", "Sadece görsel", 12),
        new Item("sword_2", "Kristal Kılıç", "Sadece görsel", 18),
        new Item("sword_3", "Altın Kılıç", "Sadece görsel", 25),
    };

    public static readonly Item Helmet = new Item("gear_helmet", "Çelik Miğfer", "+1 Kalkan", 15);
    public static readonly Item Armor = new Item("gear_armor", "Şövalye Zırhı", "+2 Kalkan", 30);

    public static int Gold
    {
        get => PlayerPrefs.GetInt("TotalGold", 0);
        set { PlayerPrefs.SetInt("TotalGold", Mathf.Max(0, value)); PlayerPrefs.Save(); }
    }

    public static int EquippedSkin
    {
        get => PlayerPrefs.GetInt("EqSkin", 0);
        set { PlayerPrefs.SetInt("EqSkin", value); PlayerPrefs.Save(); }
    }

    public static int EquippedSword
    {
        get => PlayerPrefs.GetInt("EqSword", 0);
        set { PlayerPrefs.SetInt("EqSword", value); PlayerPrefs.Save(); }
    }

    public static bool HasHelmet => IsOwned(Helmet.id);
    public static bool HasArmor => IsOwned(Armor.id);

    public static bool IsOwned(string id)
    {
        if (id == "skin_0" || id == "sword_0") return true;
        return PlayerPrefs.GetInt("Own_" + id, 0) == 1;
    }

    public static bool TryBuy(Item item)
    {
        if (IsOwned(item.id)) return true;
        if (Gold < item.price) return false;
        Gold -= item.price;
        PlayerPrefs.SetInt("Own_" + item.id, 1);
        PlayerPrefs.Save();
        return true;
    }

    public static int GetTotalShield()
    {
        int s = 0;
        if (HasHelmet) s += 1;
        if (HasArmor) s += 2;
        return s;
    }
}
