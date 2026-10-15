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

    public class Component
    {
        public Transform transform { get; set; } = new Transform();
    }

    public class MonoBehaviour : Component { }

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
    }
}
