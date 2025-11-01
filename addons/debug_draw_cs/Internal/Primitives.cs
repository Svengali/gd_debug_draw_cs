// FILE: DebugDrawInternal/Primitives.cs
#if DEBUG
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DebugDrawInternalFunctionality
{
    #region Renderable Primitives

    public class SphereBounds
    {
        public Vector3 Position;
        public float Radius;
    }

    class TextGroup
    {
        public string Title;
        public int GroupPriority;
        public Color GroupColor;
        public bool ShowTitle;
        public Dictionary<string, DelayedText> Texts = new Dictionary<string, DelayedText>();

        public TextGroup(string title, int priority, bool showTitle, Color groupColor)
        {
            Title = title;
            GroupPriority = priority;
            ShowTitle = showTitle;
            GroupColor = groupColor;
        }

        public void CleanTexts(Action update)
        {
            var keysToRemove = Texts
                .Where(p => p.Value.IsExpired())
                .Select(p => p.Key).ToArray();

            foreach (var k in keysToRemove)
                Texts.Remove(k);

            if (keysToRemove.Length > 0)
                update?.Invoke();
        }
    }

    class DelayedText
    {
        public DateTime ExpirationTime;
        public string Text;
        public int Priority;
        public Color? ValueColor = null;
        public DelayedText(DateTime expirationTime, string text, int priority, Color? color)
        {
            ExpirationTime = expirationTime;
            Text = text;
            Priority = priority;
            ValueColor = color;
        }

        public bool IsExpired()
        {
            return !DebugDraw.DebugEnabled || (DateTime.Now - ExpirationTime).TotalMilliseconds > 0;
        }
    }

    class DelayedRenderer : IPoolable
    {
        public DateTime ExpirationTime;
        public bool IsUsedOneTime = false;
        public bool IsVisible = true;

        public bool IsExpired()
        {
            return !DebugDraw.DebugEnabled || ((DateTime.Now - ExpirationTime).TotalMilliseconds > 0 && IsUsedOneTime);
        }

        public void Returned()
        {
            IsUsedOneTime = false;
            IsVisible = true;
        }
    }

    class DelayedRendererInstance : DelayedRenderer
    {
        public Transform3D InstanceTransform;
        public Color InstanceColor;
        public SphereBounds Bounds = new SphereBounds();
    }

    class DelayedRendererLine : DelayedRenderer
    {
        public Aabb Bounds { get; set; }
        public Color LinesColor;
        protected Vector3[] _lines = Array.Empty<Vector3>();
        public virtual Vector3[] Lines
        {
            get => _lines;
            set
            {
                _lines = value;
                Bounds = CalculateBoundsBasedOnLines(ref _lines);
            }
        }

        protected Aabb CalculateBoundsBasedOnLines(ref Vector3[] lines)
        {
            if (lines.Length > 0)
            {
               var b = new Aabb(lines[0], Vector3.Zero);
               foreach (var v in lines)
                    b = b.Expand(v);

                return b;
            }
            else
            {
                return new Aabb();
            }
        }
    }

    interface IPoolable
    {
        void Returned();
    }

    #endregion // Renderable Primitives
}
#endif // DebugDrawInternalFunctionality