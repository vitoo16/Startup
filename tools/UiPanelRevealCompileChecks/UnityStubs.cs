using System;
using System.Collections;
using System.Collections.Generic;

// This namespace intentionally collides with the unqualified Unity
// Application type as it does in Startup Life's production source tree.
namespace StartupLife.Application
{
    public sealed class NameCollisionFixture { }
}

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DisallowMultipleComponent : Attribute { }

    public class Object
    {
        public static implicit operator bool(Object instance) => instance != null;
    }

    public class Component : Object
    {
        public GameObject gameObject { get; internal set; }
        public T GetComponent<T>() where T : Component => gameObject.GetComponent<T>();
    }

    public class MonoBehaviour : Component
    {
        protected Coroutine StartCoroutine(IEnumerator iterator) => new Coroutine(iterator);
        protected void StopCoroutine(Coroutine coroutine) { }
    }

    public sealed class Coroutine
    {
        public Coroutine(IEnumerator iterator) { }
    }

    public sealed class GameObject : Object
    {
        private readonly Dictionary<Type, Component> components = new Dictionary<Type, Component>();
        public bool activeSelf { get; private set; }
        public bool activeInHierarchy => activeSelf;

        public void SetActive(bool active) { activeSelf = active; }
        public T GetComponent<T>() where T : Component
        {
            return components.TryGetValue(typeof(T), out var item) ? (T)item : null;
        }
        public T AddComponent<T>() where T : Component, new()
        {
            var created = new T { gameObject = this };
            components.Add(typeof(T), created);
            return created;
        }
    }

    public sealed class CanvasGroup : Component
    {
        public float alpha = 1f;
    }

    public static class PlayerPrefs
    {
        private static readonly Dictionary<string, int> values = new Dictionary<string, int>();
        public static int GetInt(string key, int fallback) =>
            values.TryGetValue(key, out var value) ? value : fallback;
        public static void SetInt(string key, int value) => values[key] = value;
    }

    public static class Application
    {
        public static bool isPlaying;
    }

    public static class Time
    {
        public static float unscaledDeltaTime = 0.016f;
    }

    public static class Mathf
    {
        public static float Clamp01(float value) => Math.Max(0f, Math.Min(1f, value));
    }
}
