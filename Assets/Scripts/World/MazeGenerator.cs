using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

// Her çağrıldığında yeni, rastgele, yandan görünümlü bir labirent üretir.
public class MazeGenerator : MonoBehaviour
{
    [Header("Labirent boyutu (oda sayısı)")]
    public int width = 8;
    public int height = 6;
    [Header("Oda iç boyutu (tile)")]
    public int roomSize = 3;

    [Header("Renkler (sonra gerçek grafiklerle değişecek)")]
    public Color wallColor = new Color(0.25f, 0.3f, 0.25f);
    public Color ladderColor = new Color(0.6f, 0.4f, 0.2f);

    public Tilemap walls, ladders;
    public int TileWidth { get; private set; }
    public int TileHeight { get; private set; }
    public Vector3 StartPosition { get; private set; }
    public Vector3 ExitPosition { get; private set; }
    public Vector3 BossPosition { get; private set; }
    public int ExitRoomIndex { get; private set; }
    public List<Vector3> RoomCenters = new List<Vector3>(); // oda zemin ortaları (0 = başlangıç)
    public List<int> RoomDistances = new List<int>();       // başlangıçtan BFS adım mesafesi
    public static Collider2D WallCollider;

    Tile[][] biomeWallTiles;
    Tile[] wallTiles;
    Tile ladderTile;

    public static Sprite WhiteSprite()
    {
        var tex = new Texture2D(16, 16);
        var px = new Color[256];
        for (int i = 0; i < 256; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.filterMode = FilterMode.Point; tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
    }

    Tile MakeTile(Sprite s, Color c, Tile.ColliderType col)
    {
        var t = ScriptableObject.CreateInstance<Tile>();
        t.sprite = s; t.color = c; t.colliderType = col;
        return t;
    }

    public void Setup()
    {
        var grid = new GameObject("Grid").AddComponent<Grid>();

        // Duvarlar (katı)
        var wGo = new GameObject("Walls");
        wGo.transform.SetParent(grid.transform);
        walls = wGo.AddComponent<Tilemap>();
        wGo.AddComponent<TilemapRenderer>();
        var rb = wGo.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Static;
        var tc = wGo.AddComponent<TilemapCollider2D>();
        tc.compositeOperation = Collider2D.CompositeOperation.Merge;
        var comp = wGo.AddComponent<CompositeCollider2D>();
        comp.geometryType = CompositeCollider2D.GeometryType.Polygons; // içi dolu katı duvar (Slime içinden geçemez)
        WallCollider = comp;

        // Merdivenler (tetikleyici)
        var lGo = new GameObject("Ladders");
        lGo.transform.SetParent(grid.transform);
        ladders = lGo.AddComponent<Tilemap>();
        lGo.AddComponent<TilemapRenderer>().sortingOrder = -1;
        lGo.AddComponent<TilemapCollider2D>().isTrigger = true;
        lGo.AddComponent<Ladder>();

        biomeWallTiles = new Tile[4][];
        for (int b = 0; b < 4; b++)
        {
            biomeWallTiles[b] = new Tile[6];
            for (int i = 0; i < 6; i++)
                biomeWallTiles[b][i] = MakeTile(PixelArt.Brick(i + 1, b), Color.white, Tile.ColliderType.Grid);
        }
        wallTiles = biomeWallTiles[0];
        ladderTile = MakeTile(PixelArt.Ladder(), Color.white, Tile.ColliderType.Grid);
    }

    public bool HasLadderNear(Vector3 worldPos, int radius = 1)
    {
        int cx = Mathf.FloorToInt(worldPos.x);
        int cy = Mathf.FloorToInt(worldPos.y);
        for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -1; dy <= 2; dy++)
                if (ladders.HasTile(new Vector3Int(cx + dx, cy + dy, 0))) return true;
        return false;
    }

    public void Generate(int bossTier = 0, int biome = 0)
    {
        wallTiles = biomeWallTiles[Mathf.Clamp(biome, 0, biomeWallTiles.Length - 1)];
        walls.ClearAllTiles(); ladders.ClearAllTiles();
        RoomCenters.Clear();
        RoomDistances.Clear();

        if (bossTier > 0)
        {
            GenerateBossParkourArena(bossTier);
            return;
        }

        int step = roomSize + 1;
        int tw = width * step + 1, th = height * step + 1;
        TileWidth = tw; TileHeight = th;

        for (int x = 0; x < tw; x++)
            for (int y = 0; y < th; y++)
                walls.SetTile(new Vector3Int(x, y, 0), wallTiles[Random.Range(0, wallTiles.Length)]);

        // Odaları oy
        for (int cx = 0; cx < width; cx++)
            for (int cy = 0; cy < height; cy++)
            {
                Carve(cx * step + 1, cy * step + 1, roomSize, roomSize);
                RoomCenters.Add(new Vector3(cx * step + 1 + roomSize / 2f, cy * step + 1, 0));
                RoomDistances.Add(0);
            }

        // Recursive backtracker + oda komşuluk grafı (BFS mesafesi için)
        var visited = new bool[width, height];
        var adj = new List<Vector2Int>[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                adj[x, y] = new List<Vector2Int>();

        var stack = new Stack<Vector2Int>();
        visited[0, 0] = true; stack.Push(Vector2Int.zero);
        Vector2Int[] dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        while (stack.Count > 0)
        {
            var c = stack.Peek();
            var options = new List<Vector2Int>();
            foreach (var d in dirs)
            {
                var n = c + d;
                if (n.x >= 0 && n.y >= 0 && n.x < width && n.y < height && !visited[n.x, n.y]) options.Add(n);
            }
            if (options.Count == 0) { stack.Pop(); continue; }
            var next = options[Random.Range(0, options.Count)];
            Connect(c, next, step);
            adj[c.x, c.y].Add(next);
            adj[next.x, next.y].Add(c);
            visited[next.x, next.y] = true;
            stack.Push(next);
        }

        // Başlangıçtan her odaya gerçek labirent mesafesini (BFS) hesapla
        var dist = new int[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                dist[x, y] = -1;

        var q = new Queue<Vector2Int>();
        dist[0, 0] = 0;
        q.Enqueue(Vector2Int.zero);
        Vector2Int farthest = Vector2Int.zero;
        int maxDist = 0;

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            int cd = dist[cur.x, cur.y];
            if (cd > maxDist) { maxDist = cd; farthest = cur; }
            foreach (var nb in adj[cur.x, cur.y])
            {
                if (dist[nb.x, nb.y] != -1) continue;
                dist[nb.x, nb.y] = cd + 1;
                q.Enqueue(nb);
            }
        }

        for (int cx = 0; cx < width; cx++)
            for (int cy = 0; cy < height; cy++)
                RoomDistances[cx * height + cy] = dist[cx, cy];

        ExitRoomIndex = farthest.x * height + farthest.y;
        StartPosition = new Vector3(1 + roomSize / 2f, 1.5f, 0);
        // Kapı, labirentin en son durağı olan en uzak odada
        ExitPosition = RoomCenters[ExitRoomIndex] + new Vector3(0f, 0.5f, 0f);
        BossPosition = ExitPosition;
    }

    void GenerateBossParkourArena(int tier)
    {
        int tw = tier == 4 ? 38 : (tier == 3 ? 34 : 32);
        int th = 15;
        TileWidth = tw; TileHeight = th;

        // Dış duvar çerçevesi
        for (int x = 0; x < tw; x++)
        {
            walls.SetTile(new Vector3Int(x, 0, 0), wallTiles[Random.Range(0, wallTiles.Length)]);
            walls.SetTile(new Vector3Int(x, th - 1, 0), wallTiles[Random.Range(0, wallTiles.Length)]);
        }
        for (int y = 0; y < th; y++)
        {
            walls.SetTile(new Vector3Int(0, y, 0), wallTiles[Random.Range(0, wallTiles.Length)]);
            walls.SetTile(new Vector3Int(tw - 1, y, 0), wallTiles[Random.Range(0, wallTiles.Length)]);
        }

        if (tier == 1) // Bölüm 5: Orman Bekçisi - Klasik 3 katlı parkur
        {
            BuildPlatform(4, 4, 6); BuildPlatform(13, 4, 6); BuildPlatform(22, 4, 6);
            BuildPlatform(8, 8, 6); BuildPlatform(18, 8, 6);
            BuildPlatform(13, 11, 6);
            BuildLadder(6, 1, 4); BuildLadder(25, 1, 4);
            BuildLadder(11, 5, 8); BuildLadder(20, 5, 8);
            RoomCenters.Add(new Vector3(3.5f, 1f, 0));
            RoomCenters.Add(new Vector3(11f, 9f, 0));
            RoomCenters.Add(new Vector3(21f, 9f, 0));
        }
        else if (tier == 2) // Bölüm 10: Kristal Bekçisi - Kule parkuru
        {
            BuildPlatform(3, 4, 5); BuildPlatform(11, 6, 4); BuildPlatform(17, 6, 4); BuildPlatform(24, 4, 5);
            BuildPlatform(5, 9, 6); BuildPlatform(21, 9, 6);
            BuildPlatform(13, 11, 6);
            BuildLadder(5, 1, 4); BuildLadder(26, 1, 4);
            BuildLadder(8, 5, 9); BuildLadder(23, 5, 9);
            RoomCenters.Add(new Vector3(3.5f, 1f, 0));
            RoomCenters.Add(new Vector3(8f, 10f, 0));
            RoomCenters.Add(new Vector3(24f, 10f, 0));
        }
        else if (tier == 3) // Bölüm 15: Lav Bekçisi - Köprülü Lav Arenası
        {
            BuildPlatform(4, 4, 7); BuildPlatform(14, 5, 6); BuildPlatform(23, 4, 7);
            BuildPlatform(8, 8, 5); BuildPlatform(21, 8, 5);
            BuildPlatform(13, 11, 8);
            BuildLadder(7, 1, 4); BuildLadder(26, 1, 4);
            BuildLadder(10, 5, 8); BuildLadder(23, 5, 8);
            RoomCenters.Add(new Vector3(3.5f, 1f, 0));
            RoomCenters.Add(new Vector3(10.5f, 9f, 0));
            RoomCenters.Add(new Vector3(23.5f, 9f, 0));
        }
        else // Bölüm 20: Karanlık Kral - Devasa Final Arenası
        {
            BuildPlatform(4, 4, 5); BuildPlatform(12, 4, 14); BuildPlatform(29, 4, 5);
            BuildPlatform(7, 8, 6); BuildPlatform(16, 9, 6); BuildPlatform(25, 8, 6);
            BuildPlatform(15, 12, 8);
            BuildLadder(6, 1, 4); BuildLadder(19, 1, 4); BuildLadder(31, 1, 4);
            BuildLadder(10, 5, 8); BuildLadder(27, 5, 8);
            RoomCenters.Add(new Vector3(3.5f, 1f, 0));
            RoomCenters.Add(new Vector3(10f, 9f, 0));
            RoomCenters.Add(new Vector3(28f, 9f, 0));
        }

        StartPosition = new Vector3(3f, 2f, 0);
        BossPosition = new Vector3(tw - 9f, 1f, 0);
        ExitPosition = new Vector3(tw - 3f, 1.5f, 0);
    }

    void BuildPlatform(int x0, int y, int len)
    {
        for (int x = x0; x < x0 + len; x++)
            walls.SetTile(new Vector3Int(x, y, 0), wallTiles[Random.Range(0, wallTiles.Length)]);
    }

    void BuildLadder(int x, int yBottom, int yTop)
    {
        for (int y = yBottom; y <= yTop; y++)
        {
            walls.SetTile(new Vector3Int(x, y, 0), null);
            ladders.SetTile(new Vector3Int(x, y, 0), ladderTile);
        }
    }

    void Carve(int x, int y, int w, int h)
    {
        for (int i = 0; i < w; i++)
            for (int j = 0; j < h; j++)
                walls.SetTile(new Vector3Int(x + i, y + j, 0), null);
    }

    void Connect(Vector2Int a, Vector2Int b, int step)
    {
        if (a.y == b.y) // yatay geçiş: aradaki duvarı tamamen aç
        {
            int wx = Mathf.Max(a.x, b.x) * step;
            Carve(wx, a.y * step + 1, 1, roomSize);
        }
        else // dikey geçiş: 1 tile delik + merdiven
        {
            var low = a.y < b.y ? a : b;
            int lx = low.x * step + 1 + Random.Range(0, roomSize);
            int wy = (low.y + 1) * step;
            Carve(lx, wy, 1, 1);
            for (int y = low.y * step + 1; y <= wy + 1; y++)
                ladders.SetTile(new Vector3Int(lx, y, 0), ladderTile);
        }
    }
}
