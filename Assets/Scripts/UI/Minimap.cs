using UnityEngine;
using UnityEngine.InputSystem;

// Sağ üst köşede keşfetmeli (Fog of War) labirent minimap'i. Tab ile büyütülür/küçültülür.
public class Minimap : MonoBehaviour
{
    MazeGenerator maze;
    Transform player, exitDoor;
    Texture2D mapTex, bgTex, dotTex;
    bool[,] explored;
    int tw, th;
    bool bigMode;
    float refreshTimer;

    static readonly Color32 colUnexplored = new Color32(10, 12, 20, 235);
    static readonly Color32 colWall = new Color32(72, 82, 108, 255);
    static readonly Color32 colFloor = new Color32(26, 30, 44, 255);
    static readonly Color32 colLadder = new Color32(215, 145, 55, 255);

    void Awake()
    {
        bgTex = MakeSolid(new Color(0.03f, 0.04f, 0.08f, 0.85f));
        dotTex = MakeSolid(Color.white);
    }

    static Texture2D MakeSolid(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    public void Rebuild(MazeGenerator m, Transform p, Transform exit)
    {
        maze = m; player = p; exitDoor = exit;
        tw = maze.TileWidth; th = maze.TileHeight;
        explored = new bool[tw, th];

        if (mapTex != null) Destroy(mapTex);
        mapTex = new Texture2D(tw, th, TextureFormat.RGBA32, false);
        mapTex.filterMode = FilterMode.Point;
        mapTex.wrapMode = TextureWrapMode.Clamp;

        var clear = new Color32[tw * th];
        for (int i = 0; i < clear.Length; i++) clear[i] = colUnexplored;
        mapTex.SetPixels32(clear);
        mapTex.Apply();

        RevealAroundPlayer();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            bigMode = !bigMode;

        if (!maze || !player || Time.timeScale == 0f) return;
        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = 0.12f;
            RevealAroundPlayer();
        }
    }

    void RevealAroundPlayer()
    {
        if (explored == null || mapTex == null) return;
        int px = Mathf.FloorToInt(player.position.x);
        int py = Mathf.FloorToInt(player.position.y);
        const int r = 6;
        bool dirty = false;

        for (int x = Mathf.Max(0, px - r); x <= Mathf.Min(tw - 1, px + r); x++)
        {
            for (int y = Mathf.Max(0, py - r); y <= Mathf.Min(th - 1, py + r); y++)
            {
                if (explored[x, y]) continue;
                if ((x - px) * (x - px) + (y - py) * (y - py) > r * r) continue;
                explored[x, y] = true;
                dirty = true;
                var cell = new Vector3Int(x, y, 0);
                Color32 c = maze.walls.HasTile(cell) ? colWall : (maze.ladders.HasTile(cell) ? colLadder : colFloor);
                mapTex.SetPixel(x, y, c);
            }
        }
        if (dirty) mapTex.Apply();
    }

    bool IsExplored(Vector3 worldPos)
    {
        if (explored == null) return false;
        int x = Mathf.Clamp(Mathf.FloorToInt(worldPos.x), 0, tw - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(worldPos.y), 0, th - 1);
        return explored[x, y];
    }

    void OnGUI()
    {
        if (mapTex == null || GameBootstrap.Instance == null) return;
        if (GameBootstrap.Instance.state != GameBootstrap.GameState.Playing) return;

        float maxW = bigMode ? 380f : 210f;
        float aspect = (float)tw / Mathf.Max(1, th);
        float w = maxW;
        float h = w / aspect;
        if (h > Screen.height * 0.45f) { h = Screen.height * 0.45f; w = h * aspect; }

        float pad = 14f;
        var panel = new Rect(Screen.width - w - pad - 6, pad, w + 12, h + 28);
        var mapRect = new Rect(panel.x + 6, panel.y + 6, w, h);

        GUI.DrawTexture(panel, bgTex);
        // Texture2D y=th-1 üstte, y=0 altta -> GUI.DrawTexture doğrudan doğru yönle çizer!
        GUI.DrawTexture(mapRect, mapTex);

        // Keşfedilen canavarlar (kırmızı noktalar)
        foreach (var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (IsExplored(e.transform.position))
                DrawDot(mapRect, e.transform.position, new Color(1f, 0.25f, 0.25f), bigMode ? 7f : 4.5f);
        }

        // Keşfedilen anahtar (parlak sarı)
        if (!GameBootstrap.Instance.hasKey)
        {
            foreach (var p in FindObjectsByType<Pickup>(FindObjectsSortMode.None))
            {
                if (p.type == Pickup.Type.Key && IsExplored(p.transform.position))
                    DrawDot(mapRect, p.transform.position, new Color(1f, 0.95f, 0.2f), bigMode ? 9f : 6f);
            }
        }

        // Çıkış kapısı (keşfedildiyse veya anahtar alındıysa görünür)
        if (exitDoor && (IsExplored(exitDoor.position) || GameBootstrap.Instance.hasKey))
        {
            Color exitCol = GameBootstrap.Instance.hasKey ? new Color(1f, 0.85f, 0.15f) : new Color(0.65f, 0.6f, 0.4f);
            DrawDot(mapRect, exitDoor.position, exitCol, bigMode ? 10f : 6.5f);
        }

        // Oyuncu (parlak mavi)
        if (player)
            DrawDot(mapRect, player.position, new Color(0.25f, 0.75f, 1f), bigMode ? 10f : 6.5f);

        var label = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
        label.normal.textColor = new Color(0.78f, 0.82f, 0.92f);
        GUI.Label(new Rect(panel.x, mapRect.yMax + 2, panel.width, 18), "HARİTA [Tab]", label);
    }

    void DrawDot(Rect mapRect, Vector3 worldPos, Color c, float size)
    {
        float nx = Mathf.Clamp01(worldPos.x / Mathf.Max(1, tw));
        float ny = Mathf.Clamp01(worldPos.y / Mathf.Max(1, th));
        float sx = mapRect.x + nx * mapRect.width - size * 0.5f;
        float sy = mapRect.y + (1f - ny) * mapRect.height - size * 0.5f;
        var prev = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(new Rect(sx, sy, size, size), dotTex);
        GUI.color = prev;
    }
}
