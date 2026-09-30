using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
namespace ProjectBootstrap
{
    public static class PackageInstaller
    {
        static AddAndRemoveRequest request;
        static double deadline;
        public static void Install()
        {
            request = Client.AddAndRemove(new[] {
                "com.unity.localization@1.5.8",
                "com.unity.render-pipelines.universal@17.3.0",
                "com.unity.2d.animation@13.0.6",
                "com.unity.2d.psdimporter@12.0.2",
                "com.unity.inputsystem@1.20.0",
                "com.unity.test-framework@1.6.0",
                "com.unity.ugui@2.0.0"
            }, new[] { "com.unity.multiplayer.center", "com.unity.collab-proxy", "com.unity.visualscripting" });
            deadline = EditorApplication.timeSinceStartup + 600;
            EditorApplication.update += Poll;
        }
        static void Poll()
        {
            if (!request.IsCompleted)
            {
                if (EditorApplication.timeSinceStartup < deadline) return;
                Debug.LogError("[PackageInstaller] Timeout");
                EditorApplication.Exit(2);
                return;
            }
            EditorApplication.update -= Poll;
            if (request.Status != StatusCode.Success)
            {
                Debug.LogError("[PackageInstaller] " + request.Error.message);
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log("[PackageInstaller] Resolved " + string.Join(", ", request.Result.Select(p => p.name + "@" + p.version)));
            EditorApplication.Exit(0);
        }
    }
}
