using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace StartupLife.Editor
{
    // One authored pixel camera contract for scene *generation*. This cannot
    // retrofit already serialized scenes without opening and saving in Unity.
    // The art validator checks these fields against the canonical art-spec.
    internal static class PixelWorldCameraSetup
    {
        internal const int AssetsPpu = 32;
        internal const int ReferenceWidth = 360;
        internal const int ReferenceHeight = 640;

        internal static void Configure(Camera camera)
        {
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
                throw new InvalidOperationException("The production pixel camera requires URP.");

            camera.orthographic = true;
            camera.orthographicSize = ReferenceHeight / (2f * AssetsPpu);
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.allowDynamicResolution = false;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;

            // URP owns this component. Do not add the incompatible Built-in
            // UnityEngine.U2D.PixelPerfectCamera package/type.
            var pixel = camera.GetComponent<PixelPerfectCamera>() ??
                camera.gameObject.AddComponent<PixelPerfectCamera>();
            pixel.assetsPPU = AssetsPpu;
            pixel.refResolutionX = ReferenceWidth;
            pixel.refResolutionY = ReferenceHeight;
            pixel.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;
            pixel.cropFrame = PixelPerfectCamera.CropFrame.Windowbox;

            // World sprites require point filtering and atlas settings when
            // they are imported. Those are NOT proven by camera configuration.
        }
    }
}
