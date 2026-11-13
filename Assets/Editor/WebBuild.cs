using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering.Universal;

// Versión web (WebGL) para publicar en GitHub Pages.
// El navegador no soporta Forward+, así que durante la compilación el renderer pasa a Forward y luego se restaura.
public static class WebBuild
{
    const string RendererPath = "Assets/_Project/Configuracion/Render/PC_Renderer.asset";

    public static void Build()
    {
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        var previousMode = renderer.renderingMode;
        renderer.renderingMode = RenderingMode.Forward;
        EditorUtility.SetDirty(renderer);
        AssetDatabase.SaveAssets();

        // Sin compresión: GitHub Pages no envía los encabezados que necesitan Brotli o Gzip
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.bundleVersion = "3.0";

        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Build/Web",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });

        renderer.renderingMode = previousMode;
        EditorUtility.SetDirty(renderer);
        AssetDatabase.SaveAssets();
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
