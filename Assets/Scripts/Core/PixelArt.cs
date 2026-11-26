using UnityEngine;
using System.Collections.Generic;

// Koddan üretilen pixel-art sprite'lar. Harf = renk, '.' = şeffaf.
public static class PixelArt
{
    static readonly Dictionary<char, Color32> pal = new Dictionary<char, Color32>
    {
        {'K', C("1b1b2a")}, {'S', C("c0c8d8")}, {'s', C("7a8496")}, {'V', C("101018")},
        {'B', C("3d74e8")}, {'b', C("274a9c")}, {'R', C("e04848")}, {'r', C("9a2a2a")},
        {'L', C("8a5a2e")}, {'l', C("5e3a1a")}, {'Y', C("f4c430")}, {'y', C("b8860b")},
        {'W', C("f0eee0")}, {'w', C("b0ad98")}, {'G', C("5ad65a")}, {'g', C("2e8a3a")},
        {'H', C("c8ffc8")}, {'P', C("8a4ac8")}, {'p', C("52287a")}, {'E', C("ff3030")},
        {'e', C("ffffff")}, {'c', C("a0d0ff")},
    };

    static Color32 C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }

    static Texture2D NewTex(int w, int h)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Point; t.wrapMode = TextureWrapMode.Clamp;
        return t;
    }

    static Sprite ToSprite(Texture2D t) =>
        Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 16);

    public static Sprite FromRows(params string[] rows)
    {
        int h = rows.Length, w = 0;
        foreach (var r in rows) w = Mathf.Max(w, r.Length);
        var t = NewTex(w, h);
        for (int y = 0; y < h; y++)
        {
            var row = rows[h - 1 - y];
            for (int x = 0; x < w; x++)
            {
                char ch = x < row.Length ? row[x] : '.';
                t.SetPixel(x, y, pal.TryGetValue(ch, out var col) ? (Color)col : Color.clear);
            }
        }
        t.Apply();
        return ToSprite(t);
    }

    // ---------- Karakterler ----------
    static readonly string[] playerTemplate = {
        ".......RR.......",
        "......RRK.......",
        ".....KKKKKK.....",
        "....KSSSSSSK....",
        "....KSVVVVSK....",
        "....KSSSSSSK....",
        ".....KsssKK.....",
        "....KBBBBBBK....",
        "...KSBBYYBBSK...",
        "...KSBBBBBBSK...",
        "....KBBBBBBK....",
        "....KLLLLLLK....",
        "....KbbKKbbK....",
        "....KssK.KssK...",
        "....KKK...KKK...",
        "................"
    };

    public static readonly Sprite Player = FromRows(playerTemplate);

    static readonly Sprite[] playerSkins = {
        Player,
        FromRows(ReplaceChars(playerTemplate, ('B','R'), ('b','r'), ('R','Y'))), // Kızıl Şövalye
        FromRows(ReplaceChars(playerTemplate, ('B','Y'), ('b','y'), ('S','Y'), ('s','y'), ('R','e'))), // Altın Şövalye
        FromRows(ReplaceChars(playerTemplate, ('B','p'), ('b','V'), ('S','p'), ('s','V'), ('R','P'), ('Y','E'))) // Gölge Ninja
    };

    public static Sprite GetPlayerSkin(int index) => playerSkins[Mathf.Clamp(index, 0, playerSkins.Length - 1)];

    static string[] ReplaceChars(string[] rows, params (char from, char to)[] map)
    {
        var res = new string[rows.Length];
        for (int i = 0; i < rows.Length; i++)
        {
            var chars = rows[i].ToCharArray();
            for (int j = 0; j < chars.Length; j++)
            {
                foreach (var (from, to) in map)
                    if (chars[j] == from) { chars[j] = to; break; }
            }
            res[i] = new string(chars);
        }
        return res;
    }

    public static readonly Sprite Slime = FromRows(
        "......KKKK......",
        "....KKGGGGKK....",
        "...KGGHHGGGGK...",
        "..KGGHGGGGGGGK..",
        "..KGGeKGGeKGGK..",
        "..KGGKKGGKKGGK..",
        ".KGGGGGGGGGGGGK.",
        ".KgGGGGGGGGGGgK.",
        ".KggggggggggggK.",
        "..KKKKKKKKKKKK..");

    public static readonly Sprite BatUp = FromRows(
        "K..............K",
        "KK....K..K....KK",
        "KPK...KKKK...KPK",
        "KPPK.KPPPPK.KPPK",
        ".KPPKPEPPEPKPPK.",
        ".KPPPPPPPPPPPPK.",
        "..KPpPPPPPPpPK..",
        "...KK.KPPK.KK...",
        "......K..K......",
        "................");

    public static readonly Sprite BatDown = FromRows(
        "................",
        "......K..K......",
        "......KKKK......",
        ".....KPPPPK.....",
        "..KKKPEPPEPKKK..",
        ".KPPPPPPPPPPPPK.",
        "KPPpPPPPPPPPpPPK",
        "KPK.KKPPPPKK.KPK",
        "KK....KPPK....KK",
        "K......KK......K");

    public static readonly Sprite Skeleton = FromRows(
        "................",
        ".....KKKKKK.....",
        "....KWWWWWWK....",
        "....KWEKWEKK....",
        "....KWWWWWWK....",
        ".....KWKWKWK....",
        "......KKKKK.....",
        ".....KKWWWKK....",
        "....KWKWKWKWK...",
        "....KwKWWWKwK...",
        "....KWKwKwKWK...",
        ".....KKWWWKK....",
        "......KWKWK.....",
        "......KWKWK.....",
        ".....KWK.KWK....",
        ".....KKK.KKK....");

    public static readonly Sprite Mage = FromRows(
        "......KKKK......",
        ".....KPPPPK.....",
        "....KPPpPPPK....",
        "...KKKKKKKKKK...",
        "....KVVVVVVK....",
        "....KVEKVEVK....",
        "....KVVVVVVK..KY",
        ".....KKKKKK..KYK",
        "....KPPPPPPKKLK.",
        "...KPPpYYpPPKL..",
        "...KPPPPPPPPKL..",
        "...KPPpPPpPPKL..",
        "..KPPPPPPPPPKL..",
        "..KppppppppppK..",
        "..KKKKKKKKKKKK..",
        "................");

    public static readonly Sprite Fireball = FromRows(
        "..RRRR..",
        ".RYYYYR.",
        "RYeeeeYR",
        "RYeeeeYR",
        "RYYYYYYR",
        ".RRYRRR.",
        "..RRRR..",
        "........");

    public static readonly Sprite PlayerBullet = FromRows(
        "..KKKKKK..",
        ".KYYYYeeK.",
        "KYYYYeeeeK",
        ".KYYYYeeK.",
        "..KKKKKK..");

    static readonly string[] swordTemplate = {
        "....K...",
        "...KeK..",
        "...KSK..",
        "...KeK..",
        "...KSK..",
        "...KSK..",
        "...KsK..",
        "...KSK..",
        "...KsK..",
        ".KKYYKK.",
        "KYYYYYYK",
        ".KKLLKK.",
        "...KLK..",
        "...KYK..",
        "...KKK.."
    };

    public static readonly Sprite Sword = FromRows(swordTemplate);

    static readonly Sprite[] swordSkins = {
        Sword,
        FromRows(ReplaceChars(swordTemplate, ('S','R'), ('s','r'), ('e','Y'))), // Alev Kılıcı
        FromRows(ReplaceChars(swordTemplate, ('S','c'), ('s','B'), ('e','e'), ('Y','c'))), // Kristal Kılıç
        FromRows(ReplaceChars(swordTemplate, ('S','Y'), ('s','y'), ('e','e'), ('L','R'))) // Altın Kılıç
    };

    public static Sprite GetSwordSkin(int index) => swordSkins[Mathf.Clamp(index, 0, swordSkins.Length - 1)];

    public static readonly Sprite BoneSword = FromRows(
        "...KK...",
        "..KWWK..",
        "..KWWK..",
        "..KWwK..",
        "..KWWK..",
        "..KWwK..",
        "..KWWK..",
        "..KwwK..",
        ".KKKKKK.",
        ".KRRRRK.",
        "..KLLK..",
        "..KKKK..");

    public static readonly Sprite Torch = FromRows(
        "...YY...",
        "..RYYR..",
        "..RYYR..",
        "...RR...",
        "..KKKK..",
        "..KLLK..",
        "..KLlK..",
        "..KLLK..",
        "..KLlK..",
        "..KKKK..");

    public static readonly Sprite Boss = FromRows(
        ".KK..........KK.",
        "KYYK........KYYK",
        ".KYYK.KKKK.KYYK.",
        "..KKKrrrrrrKKK..",
        "..KrrrrrrrrrrK..",
        ".KrrKEKrrKEKrrK.",
        ".KrrKKKrrKKKrrK.",
        ".KrrrrrrrrrrrrK.",
        "..KrKWKWWKWKrK..",
        "..KKKKKKKKKKKK..",
        ".KsssKRRRRKsssK.",
        "KssKKRRRRRRKKssK",
        "KsK.KRRYYRRK.KsK",
        "KK..KRRRRRRK..KK",
        "....KrrKKrrK....",
        "...KKKK..KKKK...");

    public static readonly Sprite Helmet = FromRows(
        "......RR......",
        ".....RRRR.....",
        "....KKKKKK....",
        "...KSSSSSSK...",
        "..KSSSeSSSSK..",
        "..KSSSSSSSSK..",
        "..KSVVVVVVSK..",
        "..KSVKVVVKVK..",
        "..KSSKKKKSSK..",
        "..KssK..KssK..",
        "...KK....KK...");

    public static readonly Sprite Armor = FromRows(
        "..KKK....KKK..",
        ".KSSSKKKKSSSK.",
        "KSSsSSSSSSsSSK",
        "KSSKSSSSSSKSSK",
        ".KKKSSYYSSKKK.",
        "...KSSSSSSK...",
        "...KssYYssK...",
        "...KSSSSSSK...",
        "...KLLLLLLK...",
        "..KssKYYKssK..",
        "..KSSK..KSSK..",
        "...KK....KK...");

    public static readonly Sprite Princess = FromRows(
        ".....K.K.K......",
        ".....KYYYK......",
        "....KYYYYYK.....",
        "...KLLLLLLLK....",
        "...KLeeeeeLK....",
        "...KLeKeKeLK....",
        "...KLeeeeeLK....",
        "...KLLeeeLLK....",
        "....KKRRRKK.....",
        "...KRRRRRRRK....",
        "..KRRRYRYRRRK...",
        "..KRRRRRRRRRK...",
        "..KRrRRRRRrRK...",
        ".KRRRRRRRRRRRK..",
        ".KrrRRRRRRRrrK..",
        ".KKKKKKKKKKKKK..");

    public static readonly Sprite HeartIcon = FromRows(
        "..KKK..KKK..",
        ".KRRRK.KRRK.",
        "KRReRRKRRRRK",
        "KReRRRRRRRRK",
        "KRRRRRRRRRRK",
        ".KRRRRRRRRK.",
        "..KRRRRRRK..",
        "...KRRRRK...",
        "....KRRK....",
        ".....KK.....");

    public static readonly Sprite EmptyHeartIcon = FromRows(
        "..KKK..KKK..",
        ".KVVVK.KVVK.",
        "KVVVVVKVVVVK",
        "KVVVVVVVVVVK",
        "KVVVVVVVVVVK",
        ".KVVVVVVVVK.",
        "..KVVVVVVK..",
        "...KVVVVK...",
        "....KVVK....",
        ".....KK.....");

    public static readonly Sprite ShieldIcon = FromRows(
        ".KKKKKKKKK.",
        "KccBBBBBBBK",
        "KcBBBBBBBBK",
        "KBBBeeeeBBK",
        "KBBBBBBBBBK",
        ".KBBBBBBBK.",
        ".KbbbbbbbK.",
        "..KbbbbbK..",
        "...KbbbK...",
        "....KKK....");

    public static readonly Sprite Spikes = FromRows(
        "................",
        "................",
        "................",
        "..K....K....K...",
        ".KeK..KeK..KeK..",
        ".KSK..KSK..KSK..",
        "KSSsKKSSsKKSSsK.",
        "KKKKKKKKKKKKKKKK");

    public static readonly Sprite Lava = FromRows(
        "................",
        "................",
        ".YY..RRR..YY.RR.",
        "RYYRRYYYRRYRRYYR",
        "RRRRYYYYRRRRRYYR",
        "rRRRRRRRRrRRRRRr",
        "rrRRRRrrRRrrRRrr",
        "rrrrrrrrrrrrrrrr");

    public static readonly Sprite SpeedPotion = FromRows(
        "..KKKK..",
        "..KLLK..",
        "...KK...",
        "..KccK..",
        ".KccccK.",
        "KBBBBBBK",
        "KBeBBBBK",
        "KBBBBBbK",
        ".KbbbbK.",
        "..KKKK..");

    // ---------- Eşyalar ----------
    public static readonly Sprite Key = FromRows(
        ".KKK........",
        "KYYYK.......",
        "KY.YKKKKKKKK",
        "KY.YYYYYYYYK",
        "KYYYKKKYKYKK",
        ".KKK...K.K..");

    public static readonly Sprite Potion = FromRows(
        "..KKKK..",
        "..KLLK..",
        "...KK...",
        "..KccK..",
        ".KcRRcK.",
        "KRRRRRRK",
        "KReRRRRK",
        "KRRRRRrK",
        ".KrrrrK.",
        "..KKKK..");

    public static readonly Sprite Gold = FromRows(
        ".KKKK.",
        "KYYeYK",
        "KYyYYK",
        "KYYYyK",
        "KyYYYK",
        ".KKKK.");

    public static readonly Sprite Chest = FromRows(
        ".KKKKKKKKKKKK.",
        "KLLLLLLLLLLLLK",
        "KLlLLLLLLLLlLK",
        "KKKKKKYYKKKKKK",
        "KLLLLKYYKLLLLK",
        "KLlLLLKKLLLlLK",
        "KLLLLLLLLLLLLK",
        "KLlLLLLLLLLlLK",
        "KyLLLLLLLLLLyK",
        "KKKKKKKKKKKKKK");

    // ---------- Prosedürel ----------
    public static Sprite Door()
    {
        int w = 16, h = 24;
        var t = NewTex(w, h);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                float dx = x - 7.5f, dy = y - 16f;
                bool inside = y < 16 || dx * dx + dy * dy < 7.6f * 7.6f;
                bool inner = (x >= 2 && x <= 13) && (y < 16 || dx * dx + dy * dy < 5.6f * 5.6f) && y >= 1;
                Color c = Color.clear;
                if (inside) c = pal['y'];
                if (inside && !inner && (x == 0 || x == 15 || y == 0)) c = pal['K'];
                if (inner)
                {
                    c = (x % 4 == 1) ? pal['l'] : pal['L'];
                    if (y == 6 || y == 13) c = pal['K'];
                }
                if ((x == 11 || x == 12) && (y == 9 || y == 10)) c = pal['Y']; // kulp
                t.SetPixel(x, y, c);
            }
        t.Apply();
        return ToSprite(t);
    }

    public static Sprite Brick(int seed, int biome = 0)
    {
        var rnd = new System.Random(seed + biome * 97);
        var t = NewTex(16, 16);
        Color mortar = biome == 2 ? new Color32(0x28, 0x12, 0x10, 255) : new Color32(0x1c, 0x1e, 0x2a, 255);

        // 0: Orman Harabeleri, 1: Kristal Mağara, 2: Lav Zindanı, 3: Karanlık Kale
        Color baseCol = biome == 0 ? new Color(0.30f, 0.34f, 0.38f)
                      : biome == 1 ? new Color(0.22f, 0.34f, 0.50f)
                      : biome == 2 ? new Color(0.36f, 0.22f, 0.20f)
                                   : new Color(0.26f, 0.20f, 0.34f);
        Color accentCol = biome == 0 ? new Color(0.25f, 0.55f, 0.25f) // yosun
                        : biome == 1 ? new Color(0.35f, 0.85f, 1.00f) // kristal
                        : biome == 2 ? new Color(1.00f, 0.42f, 0.12f) // kor/lav damarı
                                     : new Color(0.65f, 0.30f, 0.85f); // mor rün

        for (int y = 0; y < 16; y++)
        {
            int row = y / 4;
            int offset = (row % 2) * 4;
            for (int x = 0; x < 16; x++)
            {
                int bx = (x + offset) % 16;
                bool isMortar = y % 4 == 3 || bx % 8 == 7;
                if (isMortar) { t.SetPixel(x, y, mortar); continue; }
                int brickId = row * 3 + (x + offset) / 8;
                float shade = 0.85f + (float)new System.Random(seed * 31 + brickId).NextDouble() * 0.3f;
                Color c = new Color(baseCol.r * shade, baseCol.g * shade, baseCol.b * shade);
                if (y % 4 == 0) c *= 1.15f;
                if (rnd.NextDouble() < 0.07) c = Color.Lerp(c, accentCol, 0.72f);
                c.a = 1;
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        return ToSprite(t);
    }

    public static Sprite Ladder()
    {
        var t = NewTex(16, 16);
        for (int x = 0; x < 16; x++)
            for (int y = 0; y < 16; y++)
            {
                Color c = Color.clear;
                if (x == 3 || x == 12) c = pal['L'];
                if (x == 2 || x == 13) c = pal['l'];
                if (x > 3 && x < 12 && (y % 5 == 2)) c = pal['L'];
                if (x > 3 && x < 12 && (y % 5 == 1)) c = pal['l'];
                t.SetPixel(x, y, c);
            }
        t.Apply();
        return ToSprite(t);
    }
}
