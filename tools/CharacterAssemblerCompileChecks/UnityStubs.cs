#nullable enable
using System;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SerializeField : Attribute { }

    public enum FilterMode { Point, Bilinear }

    public sealed class Transform
    {
        public Transform? Parent { get; set; }
        public bool IsChildOf(Transform owner)
        {
            for (var current = this; current != null; current = current.Parent)
                if (ReferenceEquals(current, owner))
                    return true;
            return false;
        }
    }

    public class Object
    {
        public static implicit operator bool(Object? target) => target != null;
    }

    public sealed class GameObject : Object
    {
        public bool activeSelf { get; private set; } = true;
        public void SetActive(bool active) { activeSelf = active; }
    }

    public sealed class TextAsset : Object
    {
        public string text { get; }
        public TextAsset(string content) { text = content; }
    }

    public class Component : Object
    {
        public Transform transform { get; set; } = new Transform();
        public GameObject gameObject { get; set; } = new GameObject();
        public T? GetComponent<T>() where T : Component => null;
        public T? GetComponentInParent<T>() where T : Component => null;
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : Component =>
            Array.Empty<T>();
    }

    public class MonoBehaviour : Component
    {
        public bool isActiveAndEnabled { get; set; } = true;
    }

    public sealed class Canvas : Component { public float scaleFactor = 1f; }
    public static class Screen
    {
        public static int width = 360;
        public static float dpi = 160f;
    }

    public static class JsonUtility
    {
        // Stubs are never a substitute for Unity's actual JsonUtility semantics.
        public static T? FromJson<T>(string value) =>
            System.Text.Json.JsonSerializer.Deserialize<T>(value,
                new System.Text.Json.JsonSerializerOptions { IncludeFields = true });
    }

    public sealed class Texture2D
    {
        public FilterMode filterMode { get; set; }
        public int mipmapCount { get; set; } = 1;
    }

    public sealed class Sprite
    {
        public Texture2D texture { get; set; } = new Texture2D();
        public float pixelsPerUnit { get; set; } = 32f;
    }

    public sealed class SpriteRenderer : Component
    {
        public Sprite? sprite { get; set; }
        public bool enabled { get; set; } = true;
        public int sortingOrder { get; set; }
    }

    public static class Mathf
    {
        public static float Abs(float value) => Math.Abs(value);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
    }
}

namespace TMPro
{
    public class TMP_Text : UnityEngine.Component { public string text = ""; }
    public sealed class TMP_InputField : UnityEngine.Component { public string text = ""; }
}

namespace UnityEngine.UI
{
    public sealed class LayoutElement : UnityEngine.Component
    {
        public float minHeight, preferredHeight;
    }
    public sealed class HorizontalLayoutGroup : UnityEngine.Component { }
    public sealed class Button : UnityEngine.Component { public bool interactable = true; }
    public sealed class ScrollRect : UnityEngine.Component { public float verticalNormalizedPosition = 1f; }
}

namespace StartupLife.Presentation
{
    // Only the documented tiny external surface needed by UI controller.
    public sealed class LocalizedKeyLabel : UnityEngine.Component
    {
        public static LocalizedKeyLabel EnsureFor(TMPro.TMP_Text target) => new LocalizedKeyLabel();
        public void SetKey(string key) { }
    }
}

namespace StartupLife.Presentation
{
    public sealed class MobileTheme : UnityEngine.Object
    {
        public int touchTarget = 96;
    }
}
