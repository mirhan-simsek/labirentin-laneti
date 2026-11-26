using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

// Oyunu kurar: labirent, oyuncu, çıkış, canavarlar, kamera, ışık, minimap, sesler ve menüler.
public class GameBootstrap : MonoBehaviour
{
    public enum GameState { Menu, Playing, Paused, GameOver, Victory, Shop }
    public const int TotalLevels = 20;
    public static GameBootstrap Instance;
    public int level = 1;
    public int unlockedLevel = 1;
    public GameState state = GameState.Menu;
    GameState shopReturnState = GameState.Menu;
    float victoryTimer;

    static readonly string[] biomeNames = { "Orman Harabeleri", "Kristal Mağara", "Lav Zindanı", "Karanlık Kale" };
    public static int GetBiome(int lv) => Mathf.Clamp((lv - 1) / 5, 0, 3);
    public static bool IsBossLevel(int lv) => lv % 5 == 0;

    static readonly string[] storyLines = {
        "Bir zamanlar huzur içinde yaşayan krallıkta, cesur şövalye Mirhan ile güzel Prenses Azra'nın büyük bir aşkı vardı.",
        "Ancak karanlık zindanların efendisi korkunç Karanlık Kral, Prenses Azra'yı kaçırıp dört diyarın ötesindeki kuleye hapsetti.",
        "Mirhan, sevdiği Azra'yı kurtarmak için tereddüt etmeden lanetli labirente adım attı.",
        "Orman Harabeleri'nden Kristal Mağara'ya, Lav Zindanı'ndan Karanlık Kale'ye kadar nice canavar ve tuzakla savaştı.",
        "Yirmi zorlu bölümü ve dört korkunç Bekçiyi tek tek alt eden Mirhan, sonunda Karanlık Kral'ı da yenerek altın kapıyı açtı.",
        "Karanlık dağıldı, lanet kırıldı... Ve Mirhan sonunda sevgili Prensesi Azra'ya kavuştu."
    };

    MazeGenerator maze;
    Transform player, exitDoor;
    PlayerHealth playerHealth;
    PlayerCombat playerCombat;
    Enemy activeBoss;
    Light2D exitLight;
    Minimap minimap;
    Camera cam;
    Texture2D overlayTex, barBgTex, barFillTex;
    readonly List<GameObject> enemies = new List<GameObject>();

    // Sahneye elle eklenmemişse oyun başlarken kendini otomatik oluşturur.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (FindFirstObjectByType<GameBootstrap>() == null)
            new GameObject("Game").AddComponent<GameBootstrap>();
    }

    void Awake()
    {
        Instance = this;
        unlockedLevel = Mathf.Clamp(PlayerPrefs.GetInt("UnlockedLevel", 1), 1, TotalLevels);
        cam = Camera.main;
        cam.orthographicSize = 6;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f);

        maze = gameObject.AddComponent<MazeGenerator>();
        maze.Setup();
        SetupBackground();
        LightHelper.SetupGlobalLight();

        player = CreateSprite("Player", PixelArt.GetPlayerSkin(ShopData.EquippedSkin)).transform;
        AddBody(player.gameObject, 3, new Vector2(0.55f, 0.9f));
        player.gameObject.AddComponent<PlayerController>();
        playerHealth = player.gameObject.AddComponent<PlayerHealth>();
        playerCombat = player.gameObject.AddComponent<PlayerCombat>();
        LightHelper.AddPointLight(player.gameObject, new Color(1f, 0.86f, 0.62f), 7.2f, 1.25f, true, 0.3f);

        exitDoor = CreateSprite("Exit", PixelArt.Door(), 1).transform;
        exitDoor.gameObject.AddComponent<BoxCollider2D>().isTrigger = true;
        exitDoor.gameObject.AddComponent<ExitDoor>();
        exitLight = LightHelper.AddPointLight(exitDoor.gameObject, new Color(0.55f, 0.55f, 0.7f), 3.2f, 0.4f, false, 0.2f);

        minimap = gameObject.AddComponent<Minimap>();
        gameObject.AddComponent<TouchControls>();

        overlayTex = MakeTex(new Color(0.04f, 0.04f, 0.08f, 0.78f));
        barBgTex = MakeTex(new Color(0.1f, 0.08f, 0.12f, 0.9f));
        barFillTex = MakeTex(new Color(0.88f, 0.18f, 0.22f, 1f));

        BuildLevel();
        Sfx.StartMusic();
        SetState(GameState.Menu);
    }

    static Texture2D MakeTex(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    public void TogglePause()
    {
        if (state == GameState.Playing) SetState(GameState.Paused);
        else if (state == GameState.Paused) SetState(GameState.Playing);
    }

    void SetState(GameState s)
    {
        state = s;
        Time.timeScale = s == GameState.Playing ? 1f : 0f;
        UpdateMusicForState();
    }

    void UpdateMusicForState()
    {
        if (state == GameState.Menu || state == GameState.Shop)
        {
            Sfx.SetMusic(Sfx.MusicTrack.Menu);
        }
        else if (state == GameState.Victory)
        {
            Sfx.SetMusic(Sfx.MusicTrack.Victory);
        }
        else if (state == GameState.Playing)
        {
            if (IsBossLevel(level)) Sfx.SetMusic(Sfx.MusicTrack.Boss);
            else
            {
                int b = GetBiome(level);
                Sfx.SetMusic(b == 0 ? Sfx.MusicTrack.Forest
                           : b == 1 ? Sfx.MusicTrack.Crystal
                           : b == 2 ? Sfx.MusicTrack.Lava
                                    : Sfx.MusicTrack.Castle);
            }
        }
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k == null) return;
        if (k.escapeKey.wasPressedThisFrame)
        {
            if (state == GameState.Playing) SetState(GameState.Paused);
            else if (state == GameState.Paused) SetState(GameState.Playing);
            else if (state == GameState.Shop) SetState(shopReturnState);
        }
        if (k.mKey.wasPressedThisFrame)
        {
            Sfx.Muted = !Sfx.Muted;
            ShowMessage(Sfx.Muted ? "Ses kapatıldı" : "Ses açıldı");
        }
    }

    void SetupBackground()
    {
        var tex = Resources.Load<Texture2D>("dungeon_bg");
        if (!tex) return;
        tex.filterMode = FilterMode.Point;
        var bg = new GameObject("Background");
        bg.transform.SetParent(cam.transform, false);
        bg.transform.localPosition = new Vector3(0, 0, 10);
        var sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
        sr.color = new Color(0.55f, 0.55f, 0.65f, 1f);
        sr.sortingOrder = -10;
        float h = cam.orthographicSize * 2.2f;
        float w = h * Mathf.Max(cam.aspect, 16f / 9f);
        float scale = Mathf.Max(w / sr.sprite.bounds.size.x, h / sr.sprite.bounds.size.y);
        bg.transform.localScale = Vector3.one * scale;
    }

    public static GameObject CreateSprite(string name, Sprite s, int order = 5)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s; sr.sortingOrder = order;
        return go;
    }

    public static GameObject CreateBox(string name, Color c, Vector2 size)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MazeGenerator.WhiteSprite(); sr.color = c; sr.sortingOrder = 5;
        go.transform.localScale = new Vector3(size.x, size.y, 1);
        return go;
    }

    public static Rigidbody2D AddBody(GameObject go, float gravity, Vector2? boxSize = null)
    {
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = gravity; rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var col = go.AddComponent<BoxCollider2D>();
        if (boxSize.HasValue) col.size = boxSize.Value;
        col.sharedMaterial = new PhysicsMaterial2D { friction = 0, bounciness = 0 };
        return rb;
    }

    void BuildLevel()
    {
        // Her seviye için sabit seed: ölünce veya yeniden başlayınca 20 bölümün hiçbiri değişmez
        Random.InitState(level * 10007 + 42);
        int biome = GetBiome(level);
        bool isBoss = IsBossLevel(level);
        LightHelper.SetupGlobalLight(biome);

        // Labirent boyutu kontrollü büyür (çok devasa olup bunaltmasın)
        maze.width = Mathf.Min(13, 6 + (level - 1) / 2);
        maze.height = Mathf.Min(8, 4 + (level - 1) / 4);
        maze.Generate(isBoss ? level / 5 : 0, biome);

        player.position = maze.StartPosition;
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        playerHealth.ResetHealth();
        playerCombat.ResetAmmo();
        exitDoor.position = maze.ExitPosition + Vector3.up * 0.2f;
        cam.transform.position = new Vector3(player.position.x, player.position.y, -10);
        SpawnEnemies();
        minimap.Rebuild(maze, player, exitDoor);
        Random.InitState(System.Environment.TickCount);
    }

    void SpawnEnemies()
    {
        foreach (var e in enemies) if (e) Destroy(e);
        enemies.Clear();
        activeBoss = null;
        hasKey = false;
        exitDoor.GetComponent<SpriteRenderer>().color = lockedColor;
        if (exitLight) { exitLight.color = new Color(0.55f, 0.55f, 0.7f); exitLight.intensity = 0.4f; exitLight.pointLightOuterRadius = 3.2f; }

        var rooms = maze.RoomCenters;
        int biome = GetBiome(level);
        bool isBossLevel = IsBossLevel(level);

        if (isBossLevel)
        {
            // Bölüm 5, 10, 15, 20: SADECE Boss + parkur platformlarında sandıklar ve meşaleler
            var bossGo = Enemy.Spawn(Enemy.Kind.Boss, maze.BossPosition, player);
            activeBoss = bossGo.GetComponent<Enemy>();
            enemies.Add(bossGo);

            float[] torchX = { 4f, 10f, 16f, 22f, 28f };
            foreach (float tx in torchX)
                Track(LightHelper.SpawnTorch(new Vector3(tx, 3.2f, 0f)));
            Track(LightHelper.SpawnTorch(new Vector3(11f, 10.5f, 0f)));
            Track(LightHelper.SpawnTorch(new Vector3(21f, 10.5f, 0f)));

            // Bölüm 15 Lav Arenasında merdivenlerden uzak güvenli noktalara lav tuzağı
            if (level == 15)
            {
                var lav1 = new Vector3(13f, 1f, 0f);
                var lav2 = new Vector3(20f, 1f, 0f);
                if (!maze.HasLadderNear(lav1, 2)) Track(Hazard.Spawn(lav1, true));
                if (!maze.HasLadderNear(lav2, 2)) Track(Hazard.Spawn(lav2, true));
            }

            if (rooms.Count >= 3)
            {
                Track(Chest.Spawn(rooms[1]));
                Track(Chest.Spawn(rooms[2]));
            }

            if (level == TotalLevels)
            {
                var azra = CreateSprite("Azra", PixelArt.Princess, 5);
                azra.transform.position = maze.ExitPosition + new Vector3(-1.3f, 0.0f, 0f);
                azra.GetComponent<SpriteRenderer>().flipX = true;
                LightHelper.AddPointLight(azra, new Color(1f, 0.72f, 0.85f), 4.2f, 1.15f, true, 0.25f);
                Track(azra);
                ShowMessage("BÖLÜM 20 - FİNAL! Karanlık Kral Prenses Azra'yı kaçırmaya çalışıyor! Onu kurtar Mirhan!");
            }
            else
            {
                ShowMessage($"BÖLÜM {level} - {biomeNames[biome].ToUpper()} BOSS ARENASI!");
            }
            return;
        }

        // Normal seviyeler: odalara meşale, anahtar (ortada), sandıklar, güvenli tuzaklar ve kapıdan önceki canavarlar
        for (int i = 0; i < rooms.Count; i++)
        {
            if (i == 0 || Random.value < 0.35f)
                Track(LightHelper.SpawnTorch(rooms[i] + new Vector3(0f, 1.55f, 0f)));
        }

        // Kapı en son durakta (ExitRoomIndex). Başlangıç (0) ve Kapı odası hariç tüm ara odalar:
        int exitIdx = maze.ExitRoomIndex;
        int maxDist = maze.RoomDistances.Count > exitIdx ? maze.RoomDistances[exitIdx] : 1;
        var midRooms = new List<int>();
        var validRooms = new List<int>();
        for (int i = 1; i < rooms.Count; i++)
        {
            if (i == exitIdx) continue; // Kapı odasında ve ötesinde canavar/tuzak yok
            validRooms.Add(i);
            int d = maze.RoomDistances[i];
            if (d >= maxDist * 0.35f && d <= maxDist * 0.65f) midRooms.Add(i);
        }
        if (midRooms.Count == 0) midRooms = validRooms;

        // Anahtarı labirentin ortalarındaki bir odaya sakla
        int keyRoom = midRooms[Random.Range(0, midRooms.Count)];
        Track(Pickup.Spawn(Pickup.Type.Key, rooms[keyRoom] + Vector3.up * 0.6f));

        // Sandıklar (kapı odası hariç)
        int chests = Mathf.Min(6, 2 + level / 3);
        for (int i = 0; i < chests; i++)
            Track(Chest.Spawn(rooms[validRooms[Random.Range(0, validRooms.Count)]]));

        // Bölüm 6'dan itibaren zemin tuzakları: merdiven başında/sonunda ASLA olmaz!
        if (level >= 6)
        {
            var safeHazardRooms = new List<int>();
            foreach (int idx in validRooms)
                if (!maze.HasLadderNear(rooms[idx], 1)) safeHazardRooms.Add(idx);

            int hazardCount = Mathf.Min(safeHazardRooms.Count, Mathf.Min(8, (level - 4) / 2));
            bool isLava = biome == 2;
            for (int i = 0; i < hazardCount; i++)
            {
                int pick = Random.Range(0, safeHazardRooms.Count);
                Track(Hazard.Spawn(rooms[safeHazardRooms[pick]], isLava));
                safeHazardRooms.RemoveAt(pick);
            }
        }

        // Canavarlar sadece kapıya ulaşmadan önceki odalarda doğar (kapı son durak!)
        int count = Mathf.Min(22, 4 + level);
        for (int i = 0; i < count; i++)
        {
            var pos = rooms[validRooms[Random.Range(0, validRooms.Count)]];
            float r = Random.value;
            Enemy.Kind kind = r < 0.35f ? Enemy.Kind.Slime : (r < 0.60f ? Enemy.Kind.Bat : (r < 0.80f ? Enemy.Kind.Skeleton : Enemy.Kind.Mage));
            enemies.Add(Enemy.Spawn(kind, pos, player));
        }
    }

    public void NextLevel()
    {
        Sfx.PlayLevelClear();
        if (level >= TotalLevels)
        {
            victoryTimer = 0f;
            SetState(GameState.Victory);
            return;
        }
        level++;
        if (level > unlockedLevel)
        {
            unlockedLevel = level;
            PlayerPrefs.SetInt("UnlockedLevel", unlockedLevel);
            PlayerPrefs.Save();
        }
        BuildLevel();
        UpdateMusicForState();
        ShowMessage($"Bölüm {level}/{TotalLevels}");
    }
    public void PlayerDied() { Sfx.PlayGameOver(); SetState(GameState.GameOver); }
    public void AddKill() { kills++; }

    public void OnBossDefeated(Vector3 pos)
    {
        activeBoss = null;
        gold += 10;
        ShopData.Gold += 10;
        Sfx.PlayLevelClear();
        Track(Pickup.Spawn(Pickup.Type.Key, pos + Vector3.up * 0.5f));
        ShowMessage("LABİRENT BEKÇİSİ YENİLDİ! +10 Altın & Anahtar düştü!");
    }

    public void ApplyPlayerCosmetics()
    {
        if (player) player.GetComponent<SpriteRenderer>().sprite = PixelArt.GetPlayerSkin(ShopData.EquippedSkin);
        if (playerCombat) playerCombat.ApplySwordSkin();
        if (playerHealth) playerHealth.RefreshShieldCap();
    }

    void OpenShop(GameState from)
    {
        shopReturnState = from;
        SetState(GameState.Shop);
    }

    // --- Eşyalar ve İstatistik ---
    public bool hasKey;
    public int gold, kills;
    readonly Color lockedColor = new Color(0.45f, 0.45f, 0.55f);
    readonly Color openColor = Color.white;
    string message; float messageTimer;

    public void Track(GameObject go) { enemies.Add(go); } // seviye değişince silinsin
    public void ShowMessage(string m) { message = m; messageTimer = 2.5f; }

    public void Collect(Pickup.Type t)
    {
        switch (t)
        {
            case Pickup.Type.Key:
                hasKey = true;
                exitDoor.GetComponent<SpriteRenderer>().color = openColor;
                if (exitLight) { exitLight.color = new Color(1f, 0.85f, 0.25f); exitLight.intensity = 1.6f; exitLight.pointLightOuterRadius = 5.5f; }
                Sfx.PlayKey();
                ShowMessage("Anahtarı buldun! Çıkış kapısı açıldı");
                break;
            case Pickup.Type.Potion: playerHealth.Heal(1); Sfx.PlayPickup(); ShowMessage("+1 Can!"); break;
            case Pickup.Type.SpeedBoost: player.GetComponent<PlayerController>().GiveSpeedBoost(8f); Sfx.PlayKey(); ShowMessage("HIZ İKSİRİ! (8 sn)"); break;
            case Pickup.Type.Gold: gold++; ShopData.Gold++; Sfx.PlayPickup(); break;
        }
    }

    void StartNewGame() { StartLevel(1); }
    void StartLevel(int lv) { level = Mathf.Clamp(lv, 1, TotalLevels); gold = 0; kills = 0; BuildLevel(); SetState(GameState.Playing); }
    void RetryLevel() { BuildLevel(); SetState(GameState.Playing); }
    void UnlockAllLevels()
    {
        unlockedLevel = TotalLevels;
        PlayerPrefs.SetInt("UnlockedLevel", unlockedLevel);
        PlayerPrefs.Save();
    }

    void LateUpdate()
    {
        var target = new Vector3(player.position.x, player.position.y, -10);
        cam.transform.position = Vector3.Lerp(cam.transform.position, target, 8f * Time.unscaledDeltaTime);
    }

    void OnGUI()
    {
        int hudFont = Mathf.Clamp((int)(Screen.height * 0.042f), 20, 32);
        var style = new GUIStyle(GUI.skin.label) { fontSize = hudFont, fontStyle = FontStyle.Bold };
        if (state == GameState.Playing || state == GameState.Paused)
        {
            // Can barı (gerçek pixel-art kalpler — WebGL, iPhone, Android ve PC'de %100 görünür!)
            float heartSz = Mathf.Clamp(Screen.height * 0.048f, 22f, 34f);
            float hGap = heartSz * 0.16f;
            var fullHeart = PixelArt.HeartIcon.texture;
            var emptyHeart = PixelArt.EmptyHeartIcon.texture;
            for (int i = 0; i < playerHealth.maxHp; i++)
            {
                Rect hr = new Rect(18f + i * (heartSz + hGap), 12f, heartSz, heartSz);
                GUI.DrawTexture(hr, i < playerHealth.hp ? fullHeart : emptyHeart, ScaleMode.ScaleToFit);
            }

            float nextY = 12f + heartSz + 6f;
            if (playerHealth.maxShield > 0)
            {
                float shSz = heartSz * 0.88f;
                var shTex = PixelArt.ShieldIcon.texture;
                for (int i = 0; i < playerHealth.maxShield; i++)
                {
                    var prevC = GUI.color;
                    GUI.color = i < playerHealth.shield ? Color.white : new Color(0.35f, 0.35f, 0.45f, 0.65f);
                    GUI.DrawTexture(new Rect(18f + i * (shSz + hGap), nextY, shSz, shSz), shTex, ScaleMode.ScaleToFit);
                    GUI.color = prevC;
                }
                nextY += shSz + 6f;
            }

            var infoStyle = new GUIStyle(style) { fontSize = Mathf.Max(15, hudFont - 8) };
            infoStyle.normal.textColor = Color.white;
            string gunText = playerCombat.GunCooldownRemaining <= 0f
                ? "Tabanca: HAZIR"
                : $"Tabanca: {playerCombat.GunCooldownRemaining:0.0}s";
            GUI.Label(new Rect(18, nextY, Screen.width * 0.55f, infoStyle.fontSize + 10),
                $"Bölüm {level}/{TotalLevels} · Altın: {ShopData.Gold} · {gunText}" + (hasKey ? " · [Anahtar]" : ""), infoStyle);

            if (messageTimer > 0 && state == GameState.Playing)
            {
                messageTimer -= Time.unscaledDeltaTime * 0.5f;
                var ms = new GUIStyle(style) { alignment = TextAnchor.MiddleCenter };
                ms.normal.textColor = new Color(1f, 0.9f, 0.4f);
                GUI.Label(new Rect(0, Screen.height * 0.22f, Screen.width, 50), message, ms);
            }

            // Boss can barı
            if (activeBoss != null && activeBoss.hp > 0)
            {
                float bw = Mathf.Min(520f, Screen.width * 0.52f);
                float bx = (Screen.width - bw) * 0.5f;
                float by = Screen.height - 54f;
                GUI.DrawTexture(new Rect(bx - 4, by - 4, bw + 8, 26), barBgTex);
                float ratio = Mathf.Clamp01((float)activeBoss.hp / Mathf.Max(1, activeBoss.maxHp));
                GUI.DrawTexture(new Rect(bx, by, bw * ratio, 18), barFillTex);
                var bs = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                bs.normal.textColor = new Color(1f, 0.85f, 0.85f);
                string bossTitle = level == 5 ? "ORMAN BEKÇİSİ" : (level == 10 ? "KRİSTAL BEKÇİSİ" : (level == 15 ? "LAV BEKÇİSİ" : "KARANLIK KRAL"));
                GUI.Label(new Rect(bx, by - 24, bw, 22), $"{bossTitle}  {activeBoss.hp}/{activeBoss.maxHp}", bs);
            }
        }

        if (state != GameState.Playing) DrawOverlayMenu();
    }

    void DrawOverlayMenu()
    {
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), overlayTex);

        float bw = 320, bh = 52, cx = Screen.width / 2f;
        var title = new GUIStyle(GUI.skin.label) { fontSize = 52, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        var sub = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
        sub.normal.textColor = new Color(0.85f, 0.85f, 0.92f);
        var btn = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold };

        float y = Screen.height * 0.25f;

        if (state == GameState.Menu)
        {
            float my = Screen.height * 0.06f;
            title.normal.textColor = new Color(1f, 0.85f, 0.3f);
            GUI.Label(new Rect(0, my, Screen.width, 58), "LABİRENTİN LANETİ", title);
            GUI.Label(new Rect(0, my + 58, Screen.width, 28), $"Toplam {TotalLevels} Bölüm (4 Diyar) · Açık Bölüm: {unlockedLevel}/{TotalLevels}", sub);

            // 5x4 Bölüm Seçme Izgarası (20 Bölüm)
            int cols = 5, rows = 4;
            float cellW = 132f, cellH = 54f, gap = 10f;
            float gridW = cols * cellW + (cols - 1) * gap;
            float startX = cx - gridW * 0.5f;
            float startY = my + 98f;

            var lvlBtn = new GUIStyle(GUI.skin.button) { fontSize = 15, fontStyle = FontStyle.Bold };
            for (int i = 1; i <= TotalLevels; i++)
            {
                int row = (i - 1) / cols;
                int col = (i - 1) % cols;
                Rect r = new Rect(startX + col * (cellW + gap), startY + row * (cellH + gap), cellW, cellH);

                bool unlocked = i <= unlockedLevel;
                bool isBoss = IsBossLevel(i);
                string label = unlocked
                    ? (isBoss ? $"Bölüm {i}\n[BOSS]" : $"Bölüm {i}")
                    : $"Bölüm {i}\nKİLİTLİ";

                int b = GetBiome(i);
                Color biomeCol = b == 0 ? new Color(0.55f, 0.90f, 0.65f)
                               : b == 1 ? new Color(0.55f, 0.82f, 1.00f)
                               : b == 2 ? new Color(1.00f, 0.65f, 0.45f)
                                        : new Color(0.82f, 0.60f, 1.00f);

                bool prevEn = GUI.enabled;
                Color prevBg = GUI.backgroundColor;
                GUI.enabled = unlocked;
                GUI.backgroundColor = !unlocked ? new Color(0.45f, 0.45f, 0.5f) : (isBoss ? new Color(1f, 0.45f, 0.40f) : biomeCol);
                if (GUI.Button(r, label, lvlBtn)) StartLevel(i);
                GUI.backgroundColor = prevBg;
                GUI.enabled = prevEn;
            }

            float by = startY + rows * (cellH + gap) + 14f;
            float halfW = (bw - 10f) * 0.5f;
            if (GUI.Button(new Rect(cx - bw / 2, by, bw, 44), $"DEVAM ET (BÖLÜM {unlockedLevel})", btn)) StartLevel(unlockedLevel);
            by += 52f;
            Color prevC = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.85f, 0.35f);
            if (GUI.Button(new Rect(cx - bw / 2, by, bw, 44), $"MAĞAZA  (Altın: {ShopData.Gold})", btn)) OpenShop(GameState.Menu);
            GUI.backgroundColor = prevC;
            by += 52f;

            if (GUI.Button(new Rect(cx - bw / 2, by, halfW, 40), "SES: " + (Sfx.Muted ? "KAPALI" : "AÇIK"), lvlBtn)) Sfx.Muted = !Sfx.Muted;
            if (unlockedLevel < TotalLevels)
            {
                if (GUI.Button(new Rect(cx - bw / 2 + halfW + 10f, by, halfW, 40), "HEPSİNİ AÇ", lvlBtn)) UnlockAllLevels();
                by += 48f;
                if (GUI.Button(new Rect(cx - bw / 2, by, bw, 40), "ÇIKIŞ", lvlBtn)) QuitGame();
            }
            else
            {
                if (GUI.Button(new Rect(cx - bw / 2 + halfW + 10f, by, halfW, 40), "ÇIKIŞ", lvlBtn)) QuitGame();
            }

            // Kontroller yalnızca oyunun başında (Ana Menü altında) gösterilir
            var ctrlStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            ctrlStyle.normal.textColor = new Color(0.82f, 0.86f, 0.95f);
            GUI.Label(new Rect(10, Screen.height - 34, Screen.width - 20, 26),
                "Kontroller — PC: A/D Koş · Space Zıpla/Tırman · J Kılıç · K Tabanca · Shift Dash · E Sandık · Tab Harita  |  Mobil: Joystick + Butonlar (Zıpla basılı tut = Tırman)", ctrlStyle);
        }
        else if (state == GameState.Paused)
        {
            title.normal.textColor = Color.white;
            GUI.Label(new Rect(0, y, Screen.width, 70), "DURAKLATILDI", title);
            y += 100;
            if (GUI.Button(new Rect(cx - bw / 2, y, bw, bh), "DEVAM ET", btn)) SetState(GameState.Playing);
            y += 62;
            if (GUI.Button(new Rect(cx - bw / 2, y, bw, bh), $"MAĞAZA (Altın: {ShopData.Gold})", btn)) OpenShop(GameState.Paused);
            y += 62;
            if (GUI.Button(new Rect(cx - bw / 2, y, bw, bh), "YENİDEN BAŞLA", btn)) StartNewGame();
            y += 62;
            if (GUI.Button(new Rect(cx - bw / 2, y, bw, bh), "SES: " + (Sfx.Muted ? "KAPALI" : "AÇIK"), btn)) Sfx.Muted = !Sfx.Muted;
            y += 62;
            if (GUI.Button(new Rect(cx - bw / 2, y, bw, bh), "ANA MENÜ", btn)) SetState(GameState.Menu);
        }
        else if (state == GameState.Shop)
        {
            DrawShop(cx, title, sub, btn);
        }
        else if (state == GameState.GameOver)
        {
            title.normal.textColor = new Color(1f, 0.3f, 0.3f);
            GUI.Label(new Rect(0, y, Screen.width, 70), "ÖLDÜN!", title);
            GUI.Label(new Rect(0, y + 72, Screen.width, 36), $"Bölüm: {level}/{TotalLevels}   ·   Altın: {gold}   ·   Yenilen Canavar: {kills}", sub);
            y += 140;
            if (GUI.Button(new Rect(cx - bw / 2, y, bw, bh), "BÖLÜMÜ TEKRAR DENE", btn)) RetryLevel();
            y += 66;
            if (GUI.Button(new Rect(cx - bw / 2, y, bw, bh), "BAŞTAN BAŞLA", btn)) StartNewGame();
            y += 66;
            if (GUI.Button(new Rect(cx - bw / 2, y, bw, bh), "ANA MENÜ", btn)) SetState(GameState.Menu);
        }
        else if (state == GameState.Victory)
        {
            DrawVictoryEnding(cx, title, sub, btn);
        }
    }

    void DrawVictoryEnding(float cx, GUIStyle title, GUIStyle sub, GUIStyle btn)
    {
        if (Event.current.type == EventType.Repaint)
            victoryTimer += Time.unscaledDeltaTime;

        float topY = Screen.height * 0.08f;

        // Mirhan + Kalp + Azra kavuşma sahnesi
        float spriteSize = 96f;
        float heartPulse = 1f + Mathf.Sin(victoryTimer * 4f) * 0.12f;
        float heartSize = 54f * heartPulse;

        var mirhanTex = PixelArt.GetPlayerSkin(ShopData.EquippedSkin).texture;
        var azraTex = PixelArt.Princess.texture;
        var heartTex = PixelArt.HeartIcon.texture;

        GUI.DrawTexture(new Rect(cx - spriteSize - 42f, topY, spriteSize, spriteSize), mirhanTex, ScaleMode.ScaleToFit);
        GUI.DrawTexture(new Rect(cx - heartSize * 0.5f, topY + (spriteSize - heartSize) * 0.5f, heartSize, heartSize), heartTex, ScaleMode.ScaleToFit);
        GUI.DrawTexture(new Rect(cx + 42f, topY, spriteSize, spriteSize), azraTex, ScaleMode.ScaleToFit);

        // Mirhan ♥ Azra & THE END (WebGL font uyumu için ortada pixel-art kalp zaten büyük görünüyor)
        title.fontSize = 48;
        title.normal.textColor = new Color(1f, 0.45f, 0.6f);
        GUI.Label(new Rect(0, topY + spriteSize + 10f, Screen.width, 58), "Mirhan  &  Azra", title);

        var endStyle = new GUIStyle(title) { fontSize = 34 };
        endStyle.normal.textColor = new Color(1f, 0.88f, 0.3f);
        GUI.Label(new Rect(0, topY + spriteSize + 68f, Screen.width, 44), "THE END", endStyle);

        // Aşağıda yavaş, daktilo + yumuşak belirme (fade) animasyonlu hikaye kutusu
        float boxW = Mathf.Min(780f, Screen.width - 60f);
        float boxH = 128f;
        float boxX = cx - boxW * 0.5f;
        float boxY = topY + spriteSize + 118f;

        GUI.DrawTexture(new Rect(boxX, boxY, boxW, boxH), barBgTex);

        // Her hikaye cümlesi yavaşça harf harf yazılır, okunması için 3.6 sn bekler, yumuşakça geçer
        const float slideDur = 7.8f;
        int idx = ((int)(victoryTimer / slideDur)) % storyLines.Length;
        float localT = victoryTimer % slideDur;
        string fullLine = storyLines[idx];

        // Daktilo hızı: ~22 karakter/sn (rahat okunur)
        int chars = Mathf.Clamp((int)(localT * 24f), 0, fullLine.Length);
        string shown = fullLine.Substring(0, chars);

        // Yumuşak fade-in (0.4 sn) ve fade-out (son 0.7 sn)
        float alpha = 1f;
        if (localT < 0.4f) alpha = localT / 0.4f;
        else if (localT > slideDur - 0.7f) alpha = Mathf.Clamp01((slideDur - localT) / 0.7f);

        var storyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.Clamp((int)(Screen.height * 0.032f), 16, 22),
            wordWrap = true,
            alignment = TextAnchor.MiddleCenter
        };
        storyStyle.normal.textColor = new Color(0.98f, 0.94f, 0.86f, alpha);

        // Hafif yukarı süzülme animasyonu
        float liftY = (1f - Mathf.Clamp01(localT / 0.5f)) * 10f;
        GUI.Label(new Rect(boxX + 24f, boxY + 8f + liftY, boxW - 48f, boxH - 34f), shown, storyStyle);

        // Sayfa göstergesi (1 / 6)
        var pageStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
        pageStyle.normal.textColor = new Color(0.75f, 0.75f, 0.85f, 0.85f);
        GUI.Label(new Rect(boxX, boxY + boxH - 24f, boxW, 20f), $"Hikaye {idx + 1} / {storyLines.Length}", pageStyle);

        // Alt istatistik ve butonlar
        float btnY = boxY + boxH + 18f;
        GUI.Label(new Rect(0, btnY, Screen.width, 30), $"Toplam Altın: {ShopData.Gold}   ·   Yenilen Canavar: {kills}", sub);
        btnY += 42f;
        if (GUI.Button(new Rect(cx - 270f, btnY, 250f, 48f), "TEKRAR OYNA", btn)) StartNewGame();
        if (GUI.Button(new Rect(cx + 20f, btnY, 250f, 48f), "ANA MENÜ", btn)) SetState(GameState.Menu);
    }

    void DrawShop(float cx, GUIStyle title, GUIStyle sub, GUIStyle btn)
    {
        float sy = Screen.height * 0.08f;
        title.normal.textColor = new Color(1f, 0.85f, 0.3f);
        GUI.Label(new Rect(0, sy, Screen.width, 64), "MAĞAZA", title);
        GUI.Label(new Rect(0, sy + 64, Screen.width, 32), $"Altın: {ShopData.Gold}   ·   Aktif Kalkan: +{ShopData.GetTotalShield()}", sub);

        float colW = 260f, gap = 24f;
        float totalW = colW * 3 + gap * 2;
        float left = cx - totalW * 0.5f;
        float top = sy + 115f;

        var hdr = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        var itemBtn = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(68, 10, 4, 4)
        };

        // Sütun 1: Kostüm Skinleri (Sadece görsel)
        hdr.normal.textColor = new Color(0.6f, 0.9f, 1f);
        GUI.Label(new Rect(left, top, colW, 30), "KOSTÜMLER (Görsel)", hdr);
        for (int i = 0; i < ShopData.Costumes.Length; i++)
        {
            var it = ShopData.Costumes[i];
            bool owned = ShopData.IsOwned(it.id);
            bool eq = ShopData.EquippedSkin == i;
            Rect r = new Rect(left, top + 38 + i * 74, colW, 64);
            string txt = eq ? $"{it.name}\n[KUŞANILDI]" : (owned ? $"{it.name}\nKUŞAN" : $"{it.name}\nSATIN AL ({it.price} Altın)");
            if (ShopButton(r, txt, PixelArt.GetPlayerSkin(i).texture, owned, eq, ShopData.Gold >= it.price, itemBtn))
            {
                if (owned || ShopData.TryBuy(it))
                {
                    ShopData.EquippedSkin = i;
                    ApplyPlayerCosmetics();
                    Sfx.PlayKey();
                }
            }
        }

        // Sütun 2: Kılıç Skinleri (Sadece görsel)
        float col2 = left + colW + gap;
        hdr.normal.textColor = new Color(1f, 0.75f, 0.55f);
        GUI.Label(new Rect(col2, top, colW, 30), "KILIÇ SKİNLERİ (Görsel)", hdr);
        for (int i = 0; i < ShopData.Swords.Length; i++)
        {
            var it = ShopData.Swords[i];
            bool owned = ShopData.IsOwned(it.id);
            bool eq = ShopData.EquippedSword == i;
            Rect r = new Rect(col2, top + 38 + i * 74, colW, 64);
            string txt = eq ? $"{it.name}\n[KUŞANILDI]" : (owned ? $"{it.name}\nKUŞAN" : $"{it.name}\nSATIN AL ({it.price} Altın)");
            if (ShopButton(r, txt, PixelArt.GetSwordSkin(i).texture, owned, eq, ShopData.Gold >= it.price, itemBtn))
            {
                if (owned || ShopData.TryBuy(it))
                {
                    ShopData.EquippedSword = i;
                    ApplyPlayerCosmetics();
                    Sfx.PlayKey();
                }
            }
        }

        // Sütun 3: Zırh & Miğfer (Kalkan verir)
        float col3 = col2 + colW + gap;
        hdr.normal.textColor = new Color(0.55f, 1f, 0.65f);
        GUI.Label(new Rect(col3, top, colW, 30), "ZIRH & MİĞFER", hdr);
        var gears = new[] { ShopData.Helmet, ShopData.Armor };
        var gearIcons = new[] { PixelArt.Helmet.texture, PixelArt.Armor.texture };
        for (int i = 0; i < gears.Length; i++)
        {
            var it = gears[i];
            bool owned = ShopData.IsOwned(it.id);
            Rect r = new Rect(col3, top + 38 + i * 90, colW, 76);
            string txt = owned
                ? $"{it.name} ({it.desc})\n[AKTİF]"
                : $"{it.name} ({it.desc})\nSATIN AL ({it.price} Altın)";
            if (ShopButton(r, txt, gearIcons[i], owned, owned, ShopData.Gold >= it.price, itemBtn))
            {
                if (!owned && ShopData.TryBuy(it))
                {
                    ApplyPlayerCosmetics();
                    Sfx.PlayKey();
                }
            }
        }

        float backY = top + 38 + 4 * 74 + 16f;
        if (GUI.Button(new Rect(cx - 140, backY, 280, 48), "GERİ DÖN", btn)) SetState(shopReturnState);
    }

    bool ShopButton(Rect r, string label, Texture2D icon, bool owned, bool equipped, bool canAfford, GUIStyle style)
    {
        bool prevEn = GUI.enabled;
        Color prevC = GUI.backgroundColor;
        GUI.enabled = owned || canAfford;
        GUI.backgroundColor = equipped ? new Color(0.45f, 0.95f, 0.55f) : (owned ? new Color(0.75f, 0.85f, 1f) : (canAfford ? new Color(1f, 0.85f, 0.4f) : new Color(0.45f, 0.45f, 0.5f)));
        bool clicked = GUI.Button(r, label, style);
        GUI.backgroundColor = prevC;
        GUI.enabled = prevEn;

        if (icon != null)
        {
            float pad = 8f;
            float size = r.height - pad * 2f;
            Rect iconRect = new Rect(r.x + 10f, r.y + pad, size, size);
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
        }
        return clicked;
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

