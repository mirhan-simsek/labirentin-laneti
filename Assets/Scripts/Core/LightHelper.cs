using UnityEngine;
using UnityEngine.Rendering.Universal;

// URP 2D ışık yardımcısı: atmosferik ortam ışığı, meşale ve mermi ışıkları.
public static class LightHelper
{
    public static void SetupGlobalLight(int biome = 0)
    {
        Light2D global = null;
        foreach (var l in Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
        {
            if (l.lightType == Light2D.LightType.Global) { global = l; break; }
        }
        if (global == null)
        {
            var go = new GameObject("Global Light 2D");
            global = go.AddComponent<Light2D>();
            global.lightType = Light2D.LightType.Global;
        }
        global.intensity = 0.28f;
        global.color = biome == 0 ? new Color(0.58f, 0.68f, 0.88f)
                     : biome == 1 ? new Color(0.45f, 0.72f, 1.00f)
                     : biome == 2 ? new Color(0.95f, 0.52f, 0.40f)
                                  : new Color(0.70f, 0.48f, 0.92f);
    }

    public static Light2D AddPointLight(GameObject parent, Color color, float outerRadius, float intensity, bool flicker = false, float innerRatio = 0.25f)
    {
        var go = new GameObject("Light2D");
        go.transform.SetParent(parent.transform, false);
        var l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = color;
        l.pointLightOuterRadius = outerRadius;
        l.pointLightInnerRadius = outerRadius * innerRatio;
        l.intensity = intensity;
        l.falloffIntensity = 0.65f;
        if (flicker)
        {
            var f = go.AddComponent<LightFlicker>();
            f.baseIntensity = intensity;
            f.baseRadius = outerRadius;
        }
        return l;
    }

    public static GameObject SpawnTorch(Vector3 pos)
    {
        var go = GameBootstrap.CreateSprite("Torch", PixelArt.Torch, 1);
        go.transform.position = pos;
        AddPointLight(go, new Color(1f, 0.68f, 0.30f), 4.8f, 1.1f, true, 0.2f);
        return go;
    }
}

public class LightFlicker : MonoBehaviour
{
    public float baseIntensity = 1.1f;
    public float baseRadius = 5f;
    Light2D light2D;
    float seed;

    void Awake()
    {
        light2D = GetComponent<Light2D>();
        seed = Random.value * 100f;
    }

    void Update()
    {
        if (!light2D) return;
        float n = Mathf.PerlinNoise(seed, Time.time * 5.5f) - 0.5f;
        light2D.intensity = baseIntensity + n * 0.22f;
        light2D.pointLightOuterRadius = baseRadius + n * 0.35f;
    }
}
