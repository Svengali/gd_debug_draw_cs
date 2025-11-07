// FILE: DebugDraw.cs
// This is the primary Autoload script.

using Godot;
using System;
using System.Collections.Generic;
using GDArray = Godot.Collections.Array; // This alias is kept for the one remaining API, but could be removed.

/// <summary>
/// Single-file autoload for debug drawing and printing.
/// Draw and print on screen from anywhere in a single line of code.
/// 
/// You can use only this file by adding it to autoload.
/// Also you can use it in editor by enabling 'Debug Draw For Editor' plugin.
/// 
/// No need to remove any code associated with this class in the release build.
/// Canvas placed on layer 64.
/// All positions in global space.
/// Thread-safe (I hope).
/// "Game Camera Override" is not supports, because no one in the Godot Core Team 
/// exposes methods to support this (but you can just disable culling see <see cref="UseFrustumCulling"/>).
/// </summary>
public partial class DebugDraw : Node2D
{
    public enum BlockPosition
    {
        LeftTop,
        RightTop,
        LeftBottom,
        RightBottom,
    }

    [Flags]
    public enum FPSGraphTextFlags
    {
        None = 0,
        Current = 1 << 0,
        Avarage = 1 << 1,
        Max = 1 << 2,
        Min = 1 << 3,
        All = Current | Avarage | Max | Min
    }

    public struct RenderCountData
    {
        public int Instances;
        public int Wireframes;
        public int Total;
        public RenderCountData(int instances, int wireframes)
        {
            Instances = instances;
            Wireframes = wireframes;
            Total = instances + wireframes;
        }
    }

    // GENERAL

    /// <summary>
    /// Enable or disable all debug draw.
    /// </summary>
    public static bool DebugEnabled { get; set; } = true;

    /// <summary>
    /// Debug for debug...
    /// </summary>
    public static bool Freeze3DRender { get; set; } = false;

    /// <summary>
    /// Geometry culling based on camera frustum
    /// Change to false to disable it
    /// </summary>
    public static bool UseFrustumCulling { get; set; } = true;

    /// <summary>
    /// Force use camera placed on edited scene. Usable for editor.
    /// </summary>
    public static bool ForceUseCameraFromScene { get; set; } = false;

    // TEXT

    /// <summary>
    /// Position of text block
    /// </summary>
    public static BlockPosition TextBlockPosition { get; set; } = BlockPosition.LeftTop;

    /// <summary>
    /// Offset from the corner selected in <see cref="TextBlockPosition"/>
    /// </summary>
    public static Vector2 TextBlockOffset { get; set; } = new Vector2(8, 8);

    /// <summary>
    /// Text padding for each line
    /// </summary>
    public static Vector2 TextPadding { get; set; } = new Vector2(2, 1);

    /// <summary>
    /// How long HUD text lines remain shown after being invoked.
    /// </summary>
    public static TimeSpan TextDefaultDuration { get; set; } = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// Color of the text drawn as HUD
    /// </summary>
    public static Color TextForegroundColor { get; set; } = new Color(1, 1, 1);

    /// <summary>
    /// Background color of the text drawn as HUD
    /// </summary>
    public static Color TextBackgroundColor { get; set; } = new Color(0.3f, 0.3f, 0.3f, 0.8f);

    // FPS GRAPH

    /// <summary>
    /// Is FPSGraph enabled
    /// </summary>
    public static bool FPSGraphEnabled { get; set; } = true;

    /// <summary>
    /// Switch between frame time and FPS modes
    /// </summary>
    public static bool FPSGraphFrameTimeMode { get; set; } = true;

    /// <summary>
    /// Draw a graph line aligned vertically in the center
    /// </summary>
    public static bool FPSGraphCenteredGraphLine { get; set; } = true;

    /// <summary>
    /// Sets the text visibility
    /// </summary>
    public static FPSGraphTextFlags FPSGraphShowTextFlags { get; set; } = FPSGraphTextFlags.All;

    /// <summary>
    /// Size of the FPS Graph. The width is equal to the number of stored frames.
    /// </summary>
    public static Vector2 FPSGraphSize { get; set; } = new Vector2(256, 64);

    /// <summary>
    /// Offset from the corner selected in <see cref="FPSGraphPosition"/>
    /// </summary>
    public static Vector2 FPSGraphOffset { get; set; } = new Vector2(8, 8);

    /// <summary>
    /// FPS Graph position
    /// </summary>
    public static BlockPosition FPSGraphPosition { get; set; } = BlockPosition.RightTop;

    /// <summary>
    /// Graph line color
    /// </summary>
    public static Color FPSGraphLineColor { get; set; } = Colors.OrangeRed;

    /// <summary>
    /// Color of the info text
    /// </summary>
    public static Color FPSGraphTextColor { get; set; } = Colors.WhiteSmoke;

    /// <summary>
    /// Background color
    /// </summary>
    public static Color FPSGraphBackgroundColor { get; set; } = new Color(0.2f, 0.2f, 0.2f, 0.6f);

    /// <summary>
    /// Border color
    /// </summary>
    public static Color FPSGraphBorderColor { get; set; } = Colors.Black;

    // GEOMETRY

    public static RenderCountData RenderCount
    {
#if DEBUG
        get
        {
            if (internalInstance != null)
                return new RenderCountData(internalInstance.renderInstances, internalInstance.renderWireframes);
            else
                return default;
        }
#else
        get => default;
#endif
    }

    /// <summary>
    /// Color of line with hit
    /// </summary>
    public static Color LineHitColor { get; set; } = Colors.Red;

    /// <summary>
    /// Color of line after hit
    /// </summary>
    public static Color LineAfterHitColor { get; set; } = Colors.Green;

    // Misc

    /// <summary>
    /// Custom <see cref="Viewport"/> to use for frustum culling.
    /// Usually used in editor.
    /// </summary>
    public static Viewport CustomViewport { get; set; } = null;

    /// <summary>
    /// Custom <see cref="CanvasItem"/> to draw on it. Set to <see langword="null"/> to disable.
    /// </summary>
    public static CanvasItem CustomCanvas
    {
#if DEBUG
        get => internalInstance?.CustomCanvas;
        set { if (internalInstance != null) internalInstance.CustomCanvas = value; }
#else
        get; set;
#endif
    }

#if DEBUG

    static DebugDrawInternalFunctionality.DebugDrawImplementation internalInstance = null;
    static DebugDraw instance = null;

    /// <summary>
    /// Do not use it directly. This property will not be available without debug
    /// </summary>
    public static DebugDraw Instance
    {
        get => instance;
    }

#endif

    #region Node Functions

#if DEBUG

    public DebugDraw()
    {
        GD.PrintRich($"DD: {GetType().Name}" );


        if (instance == null)
            instance = this;
        else
            throw new Exception("Only 1 instance of DebugDraw is allowed");

        Name = nameof(DebugDraw);
        internalInstance = new DebugDrawInternalFunctionality.DebugDrawImplementation(this);
    }

    public override void _EnterTree()
    {
        SetMeta(nameof(DebugDraw), true);

        // Specific for editor settings
        if (Engine.IsEditorHint())
        {
            TextBlockPosition = BlockPosition.LeftBottom;
            FPSGraphOffset = new Vector2(12, 72);
            FPSGraphPosition = BlockPosition.LeftTop;
        }
    }

    protected override void Dispose(bool disposing)
    {
        internalInstance?.Dispose();
        internalInstance = null;
        instance = null;

        if (NativeInstance != IntPtr.Zero && !IsQueuedForDeletion())
            QueueFree();
        base.Dispose(disposing);
    }

    public override void _ExitTree()
    {
        internalInstance?.Dispose();
        internalInstance = null;
    }

    public override void _Ready()
    {
        ProcessPriority = int.MaxValue;
        internalInstance.Ready();
    }

    public override void _Process(double delta)
    {
        internalInstance?.Update((float)delta);
    }

#endif

#pragma warning disable CA1822 // Mark members as static
    public void OnCanvasItemDraw()
#pragma warning restore CA1822 // Mark members as static
    {
#if DEBUG
        internalInstance?.OnCanvasItemDraw();
#endif
    }

    #endregion // Node Functions

    #region Static Draw Functions

    /// <summary>
    /// Clear all 3D objects
    /// </summary>
    public static void Clear3DObjects()
    {
#if DEBUG
        internalInstance?.Clear3DObjectsInternal();
#endif
    }

    /// <summary>
    /// Clear all 2D objects
    /// </summary>
    public static void Clear2DObjects()
    {
#if DEBUG
        internalInstance?.Clear2DObjectsInternal();
#endif
    }

    /// <summary>
    /// Clear all debug objects
    /// </summary>
    public static void ClearAll()
    {
#if DEBUG
        internalInstance?.ClearAllInternal();
#endif
    }

    #region 3D

    #region Spheres

    /// <summary>
    /// Draw sphere
    /// </summary>
    /// <param name="position">Position of the sphere center</param>
    /// <param name="radius">Sphere radius</param>
    /// <param name="color">Sphere color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawSphere(Vector3 position, float radius, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawSphereInternal(ref position, radius, ref color, duration);
#endif
    }

    /// <summary>
    /// Draw sphere
    /// </summary>
    /// <param name="transform">Transform of the sphere</param>
    /// <param name="color">Sphere color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawSphere(Transform3D transform, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawSphereInternal(ref transform, ref color, duration);
#endif
    }

    #endregion // Spheres

    #region Cylinders

    /// <summary>
    /// Draw vertical cylinder
    /// </summary>
    /// <param name="position">Center position</param>
    /// <param name="radius">Cylinder radius</param>
    /// <param name="height">Cylinder height</param>
    /// <param name="color">Cylinder color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawCylinder(Vector3 position, float radius, float height, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawCylinderInternal(ref position, radius, height, ref color, duration);
#endif
    }

    /// <summary>
    /// Draw vertical cylinder
    /// </summary>
    /// <param name="transform">Cylinder transform</param>
    /// <param name="color">Cylinder color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawCylinder(Transform3D transform, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawCylinderInternal(ref transform, ref color, duration);
#endif
    }

    #endregion // Cylinders

    #region Boxes

    /// <summary>
    /// Draw box
    /// </summary>
    /// <param name="position">Position of the box</param>
    /// <param name="size">Box size</param>
    /// <param name="color">Box color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    /// <param name="isBoxCentered">Use <paramref name="position"/> as center of the box</param>
    public static void DrawBox(Vector3 position, Vector3 size, Color? color = null, float duration = 0f, bool isBoxCentered = true)
    {
#if DEBUG
        internalInstance?.DrawBoxInternal(ref position, ref size, ref color, duration, isBoxCentered);
#endif
    }

    /// <summary>
    /// Draw rotated box
    /// </summary>
    /// <param name="position">Position of the box</param>
    /// <param name="rotation">Box rotation</param>
    /// <param name="size">Box size</param>
    /// <param name="color">Box color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    /// <param name="isBoxCentered">Use <paramref name="position"/> as center of the box</param>
    public static void DrawBox(Vector3 position, Quaternion rotation, Vector3 size, Color? color = null, float duration = 0f, bool isBoxCentered = true)
    {
#if DEBUG
        internalInstance?.DrawBoxInternal(ref position, ref rotation, ref size, ref color, duration, isBoxCentered);
#endif
    }

    /// <summary>
    /// Draw rotated box
    /// </summary>
    /// <param name="transform">Box transform</param>
    /// <param name="color">Box color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    /// <param name="isBoxCentered">Use <paramref name="position"/> as center of the box</param>
    public static void DrawBox(Transform3D transform, Color? color = null, float duration = 0f, bool isBoxCentered = true)
    {
#if DEBUG
        internalInstance?.DrawBoxInternal(ref transform, ref color, duration, isBoxCentered);
#endif
    }

    /// <summary>
    /// Draw Aaabb from <paramref name="a"/> to <paramref name="b"/>
    /// </summary>
    /// <param name="a">Firts corner</param>
    /// <param name="b">Second corner</param>
    /// <param name="color">Box color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawAABB(Vector3 a, Vector3 b, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawAABBInternal(ref a, ref b, ref color, duration);
#endif
    }

    /// <summary>
    /// Draw Aaabb
    /// </summary>
    /// <param name="aabb">Aaabb</param>
    /// <param name="color">Box color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawAABB(Aabb aabb, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawAABBInternal(ref aabb, ref color, duration);
#endif
    }

    #endregion // Boxes

    #region Lines

    /// <summary>
    /// Draw line separated by hit point (billboard square) or not separated if <paramref name="is_hit"/> = <see langword="false"/>
    /// </summary>
    /// <param name="a">Start point</param>
    /// <param name="b">End point</param>
    /// <param name="is_hit">Is hit</param>
    /// <param name="unitOffsetOfHit">Unit offset on the line where the hit occurs</param>
    /// <param name="duration">Duration of existence in seconds</param>
    /// <param name="hitColor">Color of the hit point and line before hit</param>
    /// <param name="afterHitColor">Color of line after hit position</param>
    public static void DrawLine3DHit(Vector3 a, Vector3 b, bool is_hit, float unitOffsetOfHit = 0.5f, float hitSize = 0.25f, float duration = 0f, Color? hitColor = null, Color? afterHitColor = null)
    {
#if DEBUG
        internalInstance?.DrawLine3DHitInternal(ref a, ref b, is_hit, unitOffsetOfHit, hitSize, duration, ref hitColor, ref afterHitColor);
#endif
    }

    #region Normal

    /// <summary>
    /// Draw line
    /// </summary>
    /// <param name="a">Start point</param>
    /// <param name="b">End point</param>
    /// <param name="color">Line color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawLine3D(Vector3 a, Vector3 b, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawLine3DInternal(ref a, ref b, ref color, duration);
#endif
    }

    /// <summary>
    /// Draw ray
    /// </summary>
    /// <param name="origin">Origin</param>
    /// <param name="direction">Direction</param>
    /// <param name="length">Length</param>
    /// <param name="color">Ray color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawRay3D(Vector3 origin, Vector3 direction, float length, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawRay3DInternal(origin, direction, length, color, duration);
#endif
    }

    /// <summary>
    /// Draw a sequence of points connected by lines
    /// </summary>
    /// <param name="path">Sequence of points</param>
    /// <param name="color">Color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawLinePath3D(IList<Vector3> path, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawLinePath3DInternal(path, color, duration);
#endif
    }

    /// <summary>
    /// Draw a sequence of points connected by lines
    /// </summary>
    /// <param name="color">Color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    /// <param name="path">Sequence of points</param>
    public static void DrawLinePath3D(Color? color = null, float duration = 0f, params Vector3[] path)
    {
#if DEBUG
        internalInstance?.DrawLinePath3DInternal(color, duration, path);
#endif
    }

    #endregion // Normal

    #region Arrows

    /// <summary>
    /// Draw line with arrow
    /// </summary>
    /// <param name="a">Start point</param>
    /// <param name="b">End point</param>
    /// <param name="color">Line color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    /// <param name="arrowSize">Size of the arrow</param>
    /// <param name="absoluteSize">Is the <paramref name="arrowSize"/> absolute or relative to the length of the line?</param>
    public static void DrawArrowLine3D(Vector3 a, Vector3 b, Color? color = null, float duration = 0f, float arrowSize = 0.15f, bool absoluteSize = false)
    {
#if DEBUG
        internalInstance?.DrawArrowLine3DInternal(a, b, color, duration, arrowSize, absoluteSize);
#endif
    }

    /// <summary>
    /// Draw ray with arrow
    /// </summary>
    /// <param name="origin">Origin</param>
    /// <param name="direction">Direction</param>
    /// <param name="length">Length</param>
    /// <param name="color">Ray color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    /// <param name="arrowSize">Size of the arrow</param>
    /// <param name="absoluteSize">Is the <paramref name="arrowSize"/> absolute or relative to the length of the line?</param>
    public static void DrawArrowRay3D(Vector3 origin, Vector3 direction, float length, Color? color = null, float duration = 0f, float arrowSize = 0.15f, bool absoluteSize = false)
    {
#if DEBUG
        internalInstance?.DrawArrowRay3DInternal(origin, direction, length, color, duration, arrowSize, absoluteSize);
#endif
    }

    /// <summary>
    /// Draw a sequence of points connected by lines with arrows
    /// </summary>
    /// <param name="path">Sequence of points</param>
    /// <param name="color">Color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    /// <param name="arrowSize">Size of the arrow</param>
    /// <param name="absoluteSize">Is the <paramref name="arrowSize"/> absolute or relative to the length of the line?</param>
    public static void DrawArrowPath3D(IList<Vector3> path, Color? color = null, float duration = 0f, float arrowSize = 0.75f, bool absoluteSize = true)
    {
#if DEBUG
        internalInstance?.DrawArrowPath3DInternal(path, ref color, duration, arrowSize, absoluteSize);
#endif
    }

    /// <summary>
    /// Draw a sequence of points connected by lines with arrows
    /// </summary>
    /// <param name="color">Color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    /// <param name="path">Sequence of points</param>
    /// <param name="arrowSize">Size of the arrow</param>
    /// <param name="absoluteSize">Is the <paramref name="arrowSize"/> absolute or relative to the length of the line?</param>
    public static void DrawArrowPath3D(Color? color = null, float duration = 0f, float arrowSize = 0.75f, bool absoluteSize = true, params Vector3[] path)
    {
#if DEBUG
        internalInstance?.DrawArrowPath3DInternal(ref color, duration, arrowSize, absoluteSize, path);
#endif
    }

    #endregion // Arrows
    #endregion // Lines

    #region Misc

    /// <summary>
    /// Draw a square that will always be turned towards the camera
    /// </summary>
    /// <param name="position">Center position of square</param>
    /// <param name="color">Color</param>
    /// <param name="size">Unit size</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawBillboardSquare(Vector3 position, float size = 0.2f, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawBillboardSquareInternal(ref position, size, ref color, duration);
#endif
    }

    #region Camera Frustum

    /// <summary>
    /// Draw camera frustum area
    /// </summary>
    /// <param name="camera">Camera node</param>
    /// <param name="color">Color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawCameraFrustum(Camera3D camera, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawCameraFrustumInternal(ref camera, ref color, duration);
#endif
    }

    /// <summary>
    /// Draw camera frustum area
    /// </summary>
    /// <param name="cameraFrustum">Array of frustum planes</param>
    /// <param name="color">Color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    [Obsolete("GetFrustum() now returns Plane[]. This overload is no longer necessary.")]
    public static void DrawCameraFrustum(GDArray cameraFrustum, Color? color = null, float duration = 0f)
    {
#if DEBUG
        if (cameraFrustum.Count != 6) return;
        Plane[] f = new Plane[cameraFrustum.Count];
        for (int i = 0; i < cameraFrustum.Count; i++)
            f[i] = ((Plane)cameraFrustum[i]);
        internalInstance?.DrawCameraFrustumInternal(ref f, ref color, duration);
#endif
    }

    /// <summary>
    /// Draw camera frustum area
    /// </summary>
    /// <param name="planes">Array of frustum planes</param>
    /// <param name="color">Color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawCameraFrustum(Plane[] planes, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawCameraFrustumInternal(ref planes, ref color, duration);
#endif
    }

    #endregion // Camera Frustum

    /// <summary>
    /// Draw 3 intersecting lines with the given transformations
    /// </summary>
    /// <param name="transform">Transform</param>
    /// <param name="color">Color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawPosition3D(Transform3D transform, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawPosition3DInternal(ref transform, ref color, duration);
#endif
    }

    /// <summary>
    /// Draw 3 intersecting lines with the given transformations
    /// </summary>
    /// <param name="position">Center position</param>
    /// <param name="rotation">Rotation</param>
    /// <param name="scale">Scale</param>
    /// <param name="color">Color</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawPosition3D(Vector3 position, Quaternion rotation, Vector3 scale, Color? color = null, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawPosition3DInternal(ref position, ref rotation, ref scale, ref color, duration);
#endif
    }

    /// <summary>
    /// Draw 3 intersecting lines with the given transformations
    /// </summary>
    /// <param name="position">Center position</param>
    /// <param name="color">Color</param>
    /// <param name="scale">Uniform scale</param>
    /// <param name="duration">Duration of existence in seconds</param>
    public static void DrawPosition3D(Vector3 position, Color? color = null, float scale = 0.25f, float duration = 0f)
    {
#if DEBUG
        internalInstance?.DrawPosition3DInternal(ref position, ref color, scale, duration);
#endif
    }

    #endregion // Misc
    #endregion // 3D

    #region 2D

    /// <summary>
    /// Begin text group
    /// </summary>
    /// <param name="groupTitle">Group title and ID</param>
    /// <param name="groupPriority">Group priority</param>
    /// <param name="showTitle">Whether to show the title</param>
    public static void BeginTextGroup(string groupTitle, int groupPriority = 0, Color? groupColor = null, bool showTitle = true)
    {
#if DEBUG
        internalInstance?.BeginTextGroupInternal(groupTitle, groupPriority, ref groupColor, showTitle);
#endif
    }

    /// <summary>
    /// End text group. Should be called after <see cref="BeginTextGroup(string, int, bool)"/> if you don't need more than one group.
    /// If you need to create 2+ groups just call again <see cref="BeginTextGroup(string, int, bool)"/>
    /// and this function in the end.
    /// </summary>
    /// <param name="groupTitle">Group title and ID</param>
    /// <param name="groupPriority">Group priority</param>
    /// <param name="showTitle">Whether to show the title</param>
    public static void EndTextGroup()
    {
#if DEBUG
        internalInstance?.EndTextGroupInternal();
#endif
    }

    /// <summary>
    /// Add or update text in overlay
    /// </summary>
    /// <param name="key">Name of field if <paramref name="value"/> exists, otherwise whole line will equal <paramref name="key"/>.</param>
    /// <param name="value">Value of field</param>
    /// <param name="priority">Priority of this line. Lower value is higher position.</param>
    /// <param name="duration">Expiration time</param>
    public static void SetText(string key, object value = null, int priority = 0, Color? colorOfValue = null, float duration = -1f)
    {
#if DEBUG
        internalInstance?.SetTextIntenal(ref key, ref value, priority, ref colorOfValue, duration);
#endif
    }

    #endregion // 2D

    #endregion
}