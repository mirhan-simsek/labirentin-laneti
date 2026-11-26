#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

// Tüm 20 bölümü 10'ar kez (toplam 200 çalıştırma) üretip yol bulma (BFS), sabitlik ve obje doğrulaması yapar.
public static class LevelStressTest
{
    [MenuItem("Tools/Test All 20 Levels (10x = 200 Runs)")]
    public static void RunAll20Levels10Times()
    {
        int totalRuns = 0, passed = 0, failed = 0;
        var root = new GameObject("TestRunner");
        var maze = root.AddComponent<MazeGenerator>();
        maze.Setup();

        try
        {
            for (int lv = 1; lv <= GameBootstrap.TotalLevels; lv++)
            {
                int biome = GameBootstrap.GetBiome(lv);
                bool isBoss = GameBootstrap.IsBossLevel(lv);
                string firstHash = null;

                for (int run = 1; run <= 10; run++)
                {
                    totalRuns++;
                    Random.InitState(lv * 10007 + 42);
                    maze.width = Mathf.Min(13, 6 + (lv - 1) / 2);
                    maze.height = Mathf.Min(8, 4 + (lv - 1) / 4);
                    maze.Generate(isBoss ? lv / 5 : 0, biome);

                    // 1) Başlangıç ve Çıkış boş alanda mı?
                    var sCell = new Vector3Int(Mathf.FloorToInt(maze.StartPosition.x), Mathf.FloorToInt(maze.StartPosition.y), 0);
                    var eCell = new Vector3Int(Mathf.FloorToInt(maze.ExitPosition.x), Mathf.FloorToInt(maze.ExitPosition.y), 0);
                    bool startFree = !maze.walls.HasTile(sCell);
                    bool exitFree = !maze.walls.HasTile(eCell);

                    // 2) Başlangıçtan çıkışa BFS ile ulaşılabilir mi?
                    bool reachable = CanReach(maze, sCell, eCell);

                    // 3) Deterministik (10 tekrarda da birebir aynı harita) mi?
                    string hash = HashMaze(maze);
                    if (firstHash == null) firstHash = hash;
                    bool deterministic = (hash == firstHash);

                    if (startFree && exitFree && reachable && deterministic && maze.RoomCenters.Count >= 3)
                    {
                        passed++;
                    }
                    else
                    {
                        failed++;
                        Debug.LogError($"[FAIL] Bölüm {lv} (Tur {run}): startFree={startFree}, exitFree={exitFree}, reachable={reachable}, det={deterministic}");
                    }
                }
            }
        }
        finally
        {
            var grid = Object.FindFirstObjectByType<Grid>();
            if (grid) Object.DestroyImmediate(grid.gameObject);
            Object.DestroyImmediate(root);
        }

        string summary = $"[LevelStressTest] Toplam: {totalRuns} çalıştırma (20 bölüm x 10 tur) | BAŞARILI: {passed} | HATALI: {failed}";
        if (failed == 0) Debug.Log(summary);
        else Debug.LogError(summary);
    }

    static bool CanReach(MazeGenerator maze, Vector3Int start, Vector3Int goal)
    {
        var q = new Queue<Vector3Int>();
        var vis = new HashSet<Vector3Int>();
        q.Enqueue(start);
        vis.Add(start);
        Vector3Int[] dirs = { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down };
        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            if (cur == goal) return true;
            foreach (var d in dirs)
            {
                var n = cur + d;
                if (n.x < 0 || n.y < 0 || n.x >= maze.TileWidth || n.y >= maze.TileHeight) continue;
                if (vis.Contains(n) || maze.walls.HasTile(n)) continue;
                vis.Add(n);
                q.Enqueue(n);
            }
        }
        return false;
    }

    static string HashMaze(MazeGenerator maze)
    {
        int h = 17;
        for (int x = 0; x < maze.TileWidth; x++)
            for (int y = 0; y < maze.TileHeight; y++)
            {
                var c = new Vector3Int(x, y, 0);
                int v = maze.walls.HasTile(c) ? 1 : (maze.ladders.HasTile(c) ? 2 : 0);
                h = h * 31 + v;
            }
        return $"{maze.TileWidth}x{maze.TileHeight}:{h}:{maze.StartPosition}:{maze.ExitPosition}:{maze.BossPosition}";
    }
}
#endif
