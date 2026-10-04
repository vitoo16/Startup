using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Temporary Editor-only evidence utility. Removed after the build; changes no runtime behavior or saved data.
public static class M7RuntimeEvidenceBuild
{
    public static void Android()
    {
        var args = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(args, "-buildOutput");
        if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("Missing -buildOutput.");
        Debug.Log("M7 evidence: reviewed runtime head 57418f618cb2122ba8f71b6d82975184a14f7870; Development; " +
            PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android) + "; " + PlayerSettings.Android.targetArchitectures);
        var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            locationPathName = args[index + 1],
            target = BuildTarget.Android,
            options = BuildOptions.Development
        });
        Debug.Log("M7 evidence build: " + result.summary.result + "; warnings=" + result.summary.totalWarnings + "; errors=" + result.summary.totalErrors);
        EditorApplication.Exit(result.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}

