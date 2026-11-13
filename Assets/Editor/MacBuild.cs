using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class MacBuild
{
    public static void Build()
    {
        PlayerSettings.bundleVersion = "3.0";
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Build/Mac/ArenaDelGarfio.app",
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        });
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
