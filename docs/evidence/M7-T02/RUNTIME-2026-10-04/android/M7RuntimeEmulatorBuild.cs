using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Temporary Editor-only emulator build utility; removed after the build.
public static class M7RuntimeEvidenceBuild
{
    public static void Android()
    {
        var args = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(args, "-buildOutput");
        if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("Missing -buildOutput.");
        // Native x86_64 avoids the emulator's crashing ARM translation; production ARM64 remains unchanged.
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.X86_64;
        Debug.Log("M7 emulator evidence: Development; " +
            PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android) + "; " + PlayerSettings.Android.targetArchitectures);
        var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            locationPathName = args[index + 1],
            target = BuildTarget.Android,
            options = BuildOptions.Development
        });
        Debug.Log("M7 emulator evidence build: " + result.summary.result + "; warnings=" + result.summary.totalWarnings + "; errors=" + result.summary.totalErrors);
        EditorApplication.Exit(result.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
