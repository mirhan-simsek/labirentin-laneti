#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

// Tek tıkla Windows (.exe) build alır: Tools -> Build Windows (.exe)
public static class BuildGame
{
    [MenuItem("Tools/Build Windows (.exe)")]
    public static void BuildWindows()
    {
        PlayerSettings.productName = "Labirentin Laneti";
        PlayerSettings.companyName = "MirhanGames";
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;

        string buildDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds");
        Directory.CreateDirectory(buildDir);
        string exePath = Path.Combine(buildDir, "LabirentinLaneti.exe");

        var scenes = new[] { "Assets/Scenes/SampleScene.unity" };
        var opts = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildGame] BAŞARILI! Oyun oluşturuldu: {exePath}");
            EditorUtility.RevealInFinder(exePath);
        }
        else
        {
            Debug.LogError($"[BuildGame] Build başarısız: {report.summary.result}");
        }
    }

    [MenuItem("Tools/Build WebGL (Telefon & itch.io)")]
    public static void BuildWebGL()
    {
        PlayerSettings.productName = "Labirentin Laneti";
        PlayerSettings.companyName = "MirhanGames";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;

        string buildDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds", "WebGL");
        Directory.CreateDirectory(buildDir);

        var opts = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            locationPathName = buildDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(opts);
        if (report.summary.result == BuildResult.Succeeded)
        {
            WriteMobileFullscreenIndex(buildDir);
            string zipPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds", "labirentin-laneti-mobil.zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);
            System.IO.Compression.ZipFile.CreateFromDirectory(buildDir, zipPath);
            Debug.Log($"[BuildGame] WebGL BAŞARILI! Hazır ZIP: {zipPath}");
            EditorUtility.RevealInFinder(zipPath);
        }
        else
        {
            Debug.LogError($"[BuildGame] WebGL Build başarısız: {report.summary.result}");
        }
    }

    static void WriteMobileFullscreenIndex(string buildDir)
    {
        string html = @"<!DOCTYPE html>
<html lang=""tr"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, height=device-height, initial-scale=1.0, maximum-scale=1.0, user-scalable=no, viewport-fit=cover"">
  <meta name=""apple-mobile-web-app-capable"" content=""yes"">
  <meta name=""mobile-web-app-capable"" content=""yes"">
  <title>Labirentin Laneti</title>
  <style>
    * { margin: 0; padding: 0; box-sizing: border-box; }
    html, body { width: 100%; height: 100%; overflow: hidden; background: #08080e; touch-action: none; user-select: none; -webkit-user-select: none; }
    #unity-canvas { position: fixed; inset: 0; width: 100vw; height: 100dvh; display: block; background: #08080e; }
    #loading { position: fixed; inset: 0; display: flex; flex-direction: column; align-items: center; justify-content: center; background: #08080e; color: #f4d03f; font-family: sans-serif; z-index: 10; }
    #bar-bg { width: 240px; height: 14px; background: #1e2030; border-radius: 7px; overflow: hidden; margin-top: 14px; }
    #bar { width: 0%; height: 100%; background: #f4d03f; transition: width 0.1s; }
    #rotate-overlay { position: fixed; inset: 0; background: #0a0a14; color: #fff; font-family: sans-serif; display: none; flex-direction: column; align-items: center; justify-content: center; text-align: center; padding: 24px; z-index: 999; }
    #rotate-overlay .ico { font-size: 56px; margin-bottom: 14px; }
    #rotate-overlay h2 { color: #f4d03f; margin-bottom: 8px; }
    @media screen and (orientation: portrait) and (max-width: 920px) { #rotate-overlay { display: flex; } }
  </style>
</head>
<body>
  <canvas id=""unity-canvas"" tabindex=""-1""></canvas>
  <div id=""loading""><h2>LABİRENTİN LANETİ</h2><div id=""bar-bg""><div id=""bar""></div></div></div>
  <div id=""rotate-overlay""><div class=""ico"">&#x1F4F1;&#x27A1;</div><h2>Lütfen Telefonu YAN Çevirin</h2><p>Oyun yatay (tam ekran) modda oynanır.</p></div>
  <script>
    var canvas = document.querySelector('#unity-canvas'), unityInstanceRef = null;
    function goFullscreen() {
      var el = document.documentElement, req = el.requestFullscreen || el.webkitRequestFullscreen || el.msRequestFullscreen;
      if (req) { try { var p = req.call(el); if (p && p.then) p.then(function(){ if (screen.orientation && screen.orientation.lock) screen.orientation.lock('landscape').catch(function(){}); }).catch(function(){}); } catch(e){} }
      else if (unityInstanceRef) unityInstanceRef.SetFullscreen(1);
    }
    var triedAutoFs = false;
    window.addEventListener('touchstart', function(){ if (triedAutoFs) return; triedAutoFs = true; if (/Android/i.test(navigator.userAgent)) goFullscreen(); }, { passive: true });
    var buildUrl = 'Build', loaderUrl = buildUrl + '/WebGL.loader.js';
    var config = { arguments: [], dataUrl: buildUrl + '/WebGL.data.unityweb', frameworkUrl: buildUrl + '/WebGL.framework.js.unityweb', codeUrl: buildUrl + '/WebGL.wasm.unityweb', streamingAssetsUrl: 'StreamingAssets', companyName: 'MirhanGames', productName: 'Labirentin Laneti', productVersion: '1.0' };
    var s = document.createElement('script'); s.src = loaderUrl;
    s.onload = function(){ createUnityInstance(canvas, config, function(p){ document.querySelector('#bar').style.width = (100*p)+'%'; }).then(function(u){ unityInstanceRef = u; document.querySelector('#loading').style.display = 'none'; }).catch(function(m){ alert(m); }); };
    document.body.appendChild(s);
  </script>
</body>
</html>";
        File.WriteAllText(Path.Combine(buildDir, "index.html"), html, new System.Text.UTF8Encoding(false));
    }
}
#endif
