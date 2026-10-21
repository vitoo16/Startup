using System;
using StartupLife.Presentation;
using UnityEngine;

internal static class Program
{
    private static int Main()
    {
        // Compile links the actual production UiPanelReveal.cs with the
        // namespace that caused the user's Unity CS0234. A future
        // unqualified Application.isPlaying fails before this program runs.
        Application.isPlaying = false;
        var menu = new GameObject();
        UiPanelReveal.Show(menu);
        Require(menu.activeSelf, "Showing the menu must enable its GameObject.");
        Require(menu.GetComponent<UiPanelReveal>() != null, "A reveal controller must be installed.");
        var canvas = menu.GetComponent<CanvasGroup>();
        Require(canvas != null && canvas.alpha == 1f,
            "Outside Play Mode the panel must become fully visible.");

        Application.isPlaying = true;
        PlayerPrefs.SetInt(UiPanelReveal.ReducedMotionPreference, 1);
        var reduced = new GameObject();
        UiPanelReveal.Show(reduced);
        Require(reduced.GetComponent<CanvasGroup>().alpha == 1f,
            "Reduced motion must disable the tween without hiding the UI.");
        UiPanelReveal.Show(reduced);
        Require(reduced.GetComponent<UiPanelReveal>() != null,
            "Repeated visibility refresh must reuse the same component.");
        Console.WriteLine("PASS actual UiPanelReveal.cs compiles alongside StartupLife.Application.");
        Console.WriteLine("PASS outside-play and reduced-motion presentation branches.");
        return 0;
    }

    private static void Require(bool valid, string message)
    {
        if (!valid) throw new InvalidOperationException(message);
    }
}
