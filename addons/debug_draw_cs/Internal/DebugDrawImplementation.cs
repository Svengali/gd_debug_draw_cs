// FILE: DebugDrawInternal/DebugDrawImplementation.cs
#if DEBUG
using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GDArray = Godot.Collections.Array; // Keep alias for Obsolete method

namespace DebugDrawInternalFunctionality
{
	partial class FPSGraph : Node2D
	{
		float[] frameTimes = new float[1];
		int position = 0;
		int filled = 0;

		public void Update(float delta)
		{
			if (delta == 0)
				return;

			var length = Mathf.Clamp((int)DebugDraw.FPSGraphSize.X, 150, int.MaxValue);
			if (frameTimes.Length != length)
			{
				frameTimes = new float[length];
				frameTimes[0] = delta;
				// loop array
				frameTimes[length - 1] = delta;
				position = 1;
				filled = 1;
			}
			else
			{
				frameTimes[position] = delta;
				position = Mathf.PosMod(position + 1, frameTimes.Length);
				filled = Mathf.Clamp(filled + 1, 0, frameTimes.Length);
			}
		}

		public void Draw(Font font, Vector2 viewportSize)
		{
			var notZero = frameTimes.Where((f) => f > 0f).Select((f) => DebugDraw.FPSGraphFrameTimeMode ? f * 1000 : 1f / f).ToArray();

			// No elements. Leave
			if (notZero.Length == 0)
				return;

			var max = notZero.Max();
			var min = notZero.Min();
			var avg = notZero.Average();

			// Truncate for pixel perfect render
			var graphSize = new Vector2(frameTimes.Length, (int)DebugDraw.FPSGraphSize.Y);
			var graphOffset = new Vector2((int)DebugDraw.FPSGraphOffset.X, (int)DebugDraw.FPSGraphOffset.Y);
			var pos = graphOffset;

			switch (DebugDraw.FPSGraphPosition)
			{
				case DebugDraw.BlockPosition.LeftTop:
					break;
				case DebugDraw.BlockPosition.RightTop:
					pos = new Vector2(viewportSize.X - graphSize.X - graphOffset.X, graphOffset.Y);
					break;
				case DebugDraw.BlockPosition.LeftBottom:
					pos = new Vector2(graphOffset.X, viewportSize.Y - graphSize.Y - graphOffset.Y);
					break;
				case DebugDraw.BlockPosition.RightBottom:
					pos = new Vector2(viewportSize.X - graphSize.X - graphOffset.X, viewportSize.Y - graphSize.Y - graphOffset.Y);
					break;
			}

			var height_multiplier = graphSize.Y / max;
			var center_offset = DebugDraw.FPSGraphCenteredGraphLine ? (graphSize.Y - height_multiplier * (max - min)) * 0.5f : 0;
			float get_warped(int idx) => notZero[Mathf.PosMod(idx, notZero.Length)];
			float get_y_pos(int idx) => graphSize.Y - get_warped(idx) * height_multiplier + center_offset;

			var start = position - filled;
			var prev = new Vector2(0, get_y_pos(start)) + pos;
			var border_size = new Rect2(pos + Vector2.Up, graphSize + Vector2.Down);

			// Draw background
			DrawRect(border_size, DebugDraw.FPSGraphBackgroundColor, true);

			// Draw framerate graph
			for (int i = 1; i < filled; i++)
			{
				var idx = Mathf.PosMod(start + i, notZero.Length);
				var v = pos + new Vector2(i, (int)get_y_pos(idx));
				DrawLine(v, prev, DebugDraw.FPSGraphLineColor);
				prev = v;
			}

			// Draw border
			DrawRect(border_size, DebugDraw.FPSGraphBorderColor, false);

			// Draw text
			var suffix = (DebugDraw.FPSGraphFrameTimeMode ? "ms" : "fps");
			var min_text = $"min: {min:F1} {suffix}";
			var max_text = $"max: {max:F1} {suffix}";
			var max_height = font.GetHeight();
			var avg_text = $"avg: {avg:F1} {suffix}";
			var avg_height = font.GetHeight();
			var cur_text = $"{get_warped(position - 1):F1} {suffix} ";
			var cur_size = font.GetStringSize(cur_text);

			if ((DebugDraw.FPSGraphShowTextFlags & DebugDraw.FPSGraphTextFlags.Max) == DebugDraw.FPSGraphTextFlags.Max)
				DrawString(font, pos + new Vector2(4, max_height - 1),
						cur_text, modulate: DebugDraw.FPSGraphTextColor);

			if ((DebugDraw.FPSGraphShowTextFlags & DebugDraw.FPSGraphTextFlags.Avarage) == DebugDraw.FPSGraphTextFlags.Avarage)
				DrawString(font, pos + new Vector2(4, graphSize.Y * 0.5f + avg_height * 0.5f - 2),
						cur_text, modulate: DebugDraw.FPSGraphTextColor);

			if ((DebugDraw.FPSGraphShowTextFlags & DebugDraw.FPSGraphTextFlags.Min) == DebugDraw.FPSGraphTextFlags.Min)
				DrawString(font, pos + new Vector2(4, graphSize.Y - 3),
						cur_text, modulate: DebugDraw.FPSGraphTextColor);

			if ((DebugDraw.FPSGraphShowTextFlags & DebugDraw.FPSGraphTextFlags.Current) == DebugDraw.FPSGraphTextFlags.Current)
				DrawString(font, pos + new Vector2(graphSize.X - cur_size.X, graphSize.Y * 0.5f + cur_size.Y * 0.5f - 2),
						cur_text, modulate: DebugDraw.FPSGraphTextColor);
		}
	}

	class MultiMeshContainer
	{
		readonly Action<int> addRenderedObjects = null;

		readonly MultiMeshInstance3D _mmi_cubes = null;
		readonly MultiMeshInstance3D _mmi_cubes_centered = null;
		readonly MultiMeshInstance3D _mmi_arrowheads = null;
		readonly MultiMeshInstance3D _mmi_billboard_squares = null;
		readonly MultiMeshInstance3D _mmi_positions = null;
		readonly MultiMeshInstance3D _mmi_spheres = null;
		readonly MultiMeshInstance3D _mmi_cylinders = null;

		public HashSet<DelayedRendererInstance> Cubes { get => all_mmi_with_values[_mmi_cubes]; }
		public HashSet<DelayedRendererInstance> CubesCentered { get => all_mmi_with_values[_mmi_cubes_centered]; }
		public HashSet<DelayedRendererInstance> Arrowheads { get => all_mmi_with_values[_mmi_arrowheads]; }
		public HashSet<DelayedRendererInstance> BillboardSquares { get => all_mmi_with_values[_mmi_billboard_squares]; }
		public HashSet<DelayedRendererInstance> Positions { get => all_mmi_with_values[_mmi_positions]; }
		public HashSet<DelayedRendererInstance> Spheres { get => all_mmi_with_values[_mmi_spheres]; }
		public HashSet<DelayedRendererInstance> Cylinders { get => all_mmi_with_values[_mmi_cylinders]; }

		readonly Dictionary<MultiMeshInstance3D, HashSet<DelayedRendererInstance>> all_mmi_with_values =
				new Dictionary<MultiMeshInstance3D, HashSet<DelayedRendererInstance>>();

		public MultiMeshContainer(Node root, Action<int> onObjectRendered)
		{
			addRenderedObjects = onObjectRendered;

			// Create node with material and MultiMesh. Add to tree. Create array of instances
			_mmi_cubes = CreateMMI(root, nameof(_mmi_cubes));
			_mmi_cubes_centered = CreateMMI(root, nameof(_mmi_cubes_centered));
			_mmi_arrowheads = CreateMMI(root, nameof(_mmi_arrowheads));
			_mmi_billboard_squares = CreateMMI(root, nameof(_mmi_billboard_squares));
			_mmi_positions = CreateMMI(root, nameof(_mmi_positions));
			_mmi_spheres = CreateMMI(root, nameof(_mmi_spheres));
			_mmi_cylinders = CreateMMI(root, nameof(_mmi_cylinders));

			// Customize parameters
			var billboardMaterial = (_mmi_billboard_squares.MaterialOverride as StandardMaterial3D);
			billboardMaterial.BillboardMode = StandardMaterial3D.BillboardModeEnum.Enabled;
			billboardMaterial.BillboardKeepScale = true;

			// Create Meshes
			_mmi_cubes.Multimesh.Mesh = CreateMesh(
					Mesh.PrimitiveType.Lines, Geometry.CubeVertices, Geometry.CubeIndices);

			_mmi_cubes_centered.Multimesh.Mesh = CreateMesh(Mesh.PrimitiveType.Lines,
					Geometry.CenteredCubeVertices, Geometry.CubeIndices);

			_mmi_arrowheads.Multimesh.Mesh = CreateMesh(Mesh.PrimitiveType.Lines,
					Geometry.ArrowheadVertices, Geometry.ArrowheadIndices);

			_mmi_billboard_squares.Multimesh.Mesh = CreateMesh(Mesh.PrimitiveType.Triangles,
					Geometry.CenteredSquareVertices, Geometry.SquareIndices);

			_mmi_positions.Multimesh.Mesh = CreateMesh(Mesh.PrimitiveType.Lines,
					Geometry.PositionVertices, Geometry.PositionIndices);

			_mmi_spheres.Multimesh.Mesh = CreateMesh(Mesh.PrimitiveType.Lines,
					Geometry.CreateSphereLines(6, 6, 0.5f, Vector3.Zero));

			_mmi_cylinders.Multimesh.Mesh = CreateMesh(Mesh.PrimitiveType.Lines,
					Geometry.CreateCylinderLines(52, 0.5f, 1, Vector3.Zero, 4));
		}

		MultiMeshInstance3D CreateMMI(Node root, string name)
		{
			var mmi = new MultiMeshInstance3D()
			{
				Name = name,
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
				GIMode = GeometryInstance3D.GIModeEnum.Disabled,

				MaterialOverride = new StandardMaterial3D()
				{
					ShadingMode = StandardMaterial3D.ShadingModeEnum.Unshaded,
					VertexColorUseAsAlbedo = true
				}
			};
			mmi.Multimesh = new MultiMesh()
			{
				UseColors = true,
				TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			};
			mmi.Multimesh.UseCustomData = false;

			root.AddChild(mmi);
			all_mmi_with_values.Add(mmi, new HashSet<DelayedRendererInstance>());
			return mmi;
		}

		ArrayMesh CreateMesh(Mesh.PrimitiveType type, Vector3[] vertices, int[] indices = null, Color[] colors = null)
		{
			var mesh = new ArrayMesh();
			var a = new Godot.Collections.Array();
			a.Resize((int)Mesh.ArrayType.Max);

			a[(int)Mesh.ArrayType.Vertex] = vertices;
			if (indices != null)
				a[(int)Mesh.ArrayType.Index] = indices;
			if (colors != null)
				a[(int)Mesh.ArrayType.Color] = colors; // Corrected index

			mesh.AddSurfaceFromArrays(type, a);
			return mesh;
		}

		public void Deinit()
		{
			all_mmi_with_values.Clear();

			foreach (var p in all_mmi_with_values)
				p.Key?.QueueFree();
		}

		public void ClearInstances()
		{
			foreach (var item in all_mmi_with_values)
				item.Value.Clear();
		}

		public void RemoveExpired(Action<DelayedRendererInstance> returnFunc)
		{
			foreach (var item in all_mmi_with_values)
			{
				item.Value.RemoveWhere((o) =>
				{
					if (o == null || o.IsExpired())
					{
						returnFunc(o);
						return true;
					}
					return false;
				});
			}
		}

		public void UpdateVisibility(Plane[] frustum)
		{
			Parallel.ForEach(all_mmi_with_values, (item) => UpdateVisibilityInternal(item.Value, frustum));
		}

		public void UpdateInstances()
		{
			foreach (var item in all_mmi_with_values)
				UpdateInstancesInternal(item.Key, item.Value);
		}

		public void HideAll()
		{
			foreach (var item in all_mmi_with_values)
				item.Key.Multimesh.VisibleInstanceCount = 0;
		}

		void UpdateInstancesInternal(MultiMeshInstance3D mmi, HashSet<DelayedRendererInstance> instances)
		{
			if (instances.Count > 0)
			{
				if (mmi.Multimesh.InstanceCount < instances.Count)
					mmi.Multimesh.InstanceCount = instances.Count;

				var visibleInstances = instances.Where(inst => inst.IsVisible).ToList();
				mmi.Multimesh.VisibleInstanceCount = visibleInstances.Count;
				addRenderedObjects?.Invoke(mmi.Multimesh.VisibleInstanceCount);

				int i = 0;
				foreach (var d in visibleInstances)
				{
					d.IsUsedOneTime = true;
					mmi.Multimesh.SetInstanceTransform(i, d.InstanceTransform);
					mmi.Multimesh.SetInstanceColor(i, d.InstanceColor);
					i++;
				}
			}
			else
				mmi.Multimesh.VisibleInstanceCount = 0;
		}

		void UpdateVisibilityInternal(HashSet<DelayedRendererInstance> instances, Plane[] frustum)
		{
			foreach (var _mesh in instances)
				_mesh.IsVisible = Geometry.BoundsPartiallyInsideConvexShape(_mesh.Bounds, frustum);
		}
	}


	partial class DebugDrawImplementation : Node2D
	{
		// 2D

		public Node2D CanvasItemInternal { get; private set; } = null;
		CanvasLayer _canvasLayer = null;
		bool _canvasNeedUpdate = true;
		Font _font = null;

		// fps
		readonly FPSGraph fpsGraph = new FPSGraph();

		// Text
		readonly HashSet<TextGroup> _textGroups = new HashSet<TextGroup>();
		TextGroup _currentTextGroup = null;
		readonly TextGroup _defaultTextGroup = new TextGroup(null, 0, false, DebugDraw.TextForegroundColor);

		// 3D
		MeshInstance3D _immediateGeometryNode = null;
		ImmediateMesh _immediateGeometryMesh = null;
		MultiMeshContainer _mmc = null;
		readonly HashSet<DelayedRendererLine> _wireMeshes = new HashSet<DelayedRendererLine>();
		readonly ObjectPool<DelayedRendererLine> _poolWiredRenderers = null;
		readonly ObjectPool<DelayedRendererInstance> _poolInstanceRenderers = null;
		public int renderInstances = 0;
		public int renderWireframes = 0;

		// Misc

		readonly object dataLock = new object();
		readonly DebugDraw debugDraw = null;
		bool isReady = false;

		CanvasItem _customCanvas = null;
		public CanvasItem CustomCanvas
		{
			get => _customCanvas;
			set
			{
				var callable = Callable.From( debugDraw.OnCanvasItemDraw );
				var connected_internal = CanvasItemInternal.IsConnected(CanvasItem.SignalName.Draw, callable);
				var connected_custom = _customCanvas != null && _customCanvas.IsConnected(CanvasItem.SignalName.Draw, callable);

				if (value == null)
				{
					if (!connected_internal)
						CanvasItemInternal.Connect(CanvasItem.SignalName.Draw, callable, (uint)Node.ConnectFlags.ReferenceCounted);
					if (connected_custom)
						_customCanvas?.Disconnect(CanvasItem.SignalName.Draw, callable);
				}
				else
				{
					if (connected_internal)
						CanvasItemInternal.Disconnect(CanvasItem.SignalName.Draw, callable);
					if (!connected_custom)
						value.Connect(CanvasItem.SignalName.Draw, callable, (uint)Node.ConnectFlags.ReferenceCounted);
				}
				_customCanvas = value;
			}
		}

		public DebugDrawImplementation(DebugDraw dd)
		{
			debugDraw = dd;

			GD.PrintRich($"DD: {GetType().Name}" );

			_poolWiredRenderers = new ObjectPool<DelayedRendererLine>(() => new DelayedRendererLine());
			_poolInstanceRenderers = new ObjectPool<DelayedRendererInstance>(() => new DelayedRendererInstance());
		}

		/// <summary>
		/// Must be called only once be DebugDraw class
		/// </summary>
		public void Ready()
		{
			if (isReady) return;
			isReady = true;

			// Funny hack to get default font
			var c = new Control();
			debugDraw.AddChild(c);
			_font = c.GetThemeFont(new StringName("font"));
			c.QueueFree();

			// Setup default text group
			EndTextGroupInternal();

			// Create wireframe mesh drawer
			_immediateGeometryNode = new MeshInstance3D()
			{
				Name = nameof(_immediateGeometryNode),
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
				GIMode = GeometryInstance3D.GIModeEnum.Disabled,
			};

			_immediateGeometryMesh = new ImmediateMesh();
			_immediateGeometryNode.Mesh = _immediateGeometryMesh;

			_immediateGeometryNode.MaterialOverride = new StandardMaterial3D()
			{
				ShadingMode = StandardMaterial3D.ShadingModeEnum.Unshaded,
				VertexColorUseAsAlbedo = true
			};
			debugDraw.AddChild(_immediateGeometryNode);
			// Create MultiMeshInstance instances..
			_mmc = new MultiMeshContainer(debugDraw, (i) => renderInstances += i);

			// Create canvas item and canvas layer
			_canvasLayer = new CanvasLayer() { Layer = 64 };
			CanvasItemInternal = new Node2D();

			if (CustomCanvas == null)
			{
				var callable = Callable.From(debugDraw.OnCanvasItemDraw);
				CanvasItemInternal.Connect(CanvasItem.SignalName.Draw, callable, (uint)Node.ConnectFlags.ReferenceCounted);
			}

			debugDraw.AddChild(_canvasLayer);
			_canvasLayer.AddChild(CanvasItemInternal);
		}

		public void Dispose()
		{
			FinalizedClearAll();
		}

		void FinalizedClearAll()
		{
			lock (dataLock)
			{
				_textGroups.Clear();
				_wireMeshes.Clear();
				_mmc?.Deinit();
				_mmc = null;
			}

			_font = null; // Fonts are usually resources, not Disposed manually unless loaded

			if (CanvasItemInternal != null && CanvasItemInternal.IsConnected(CanvasItem.SignalName.Draw, Callable.From(debugDraw.OnCanvasItemDraw)))
				CanvasItemInternal.Disconnect(CanvasItem.SignalName.Draw, Callable.From(debugDraw.OnCanvasItemDraw));
			if (_customCanvas != null && _customCanvas.IsConnected(CanvasItem.SignalName.Draw, Callable.From(debugDraw.OnCanvasItemDraw)))
				_customCanvas.Disconnect(CanvasItem.SignalName.Draw, Callable.From(debugDraw.OnCanvasItemDraw));

			CanvasItemInternal?.QueueFree();
			CanvasItemInternal = null;

			_canvasLayer?.QueueFree();
			_canvasLayer = null;

			_immediateGeometryNode?.QueueFree();
			_immediateGeometryNode = null;

			//_immediateGeometryMesh?.===
			//_immediateGeometryMesh = null;

			// Clear editor canvas
			CustomCanvas?.QueueRedraw();
		}

		public void Update(float delta)
		{
			lock (dataLock)
			{
				// Clean texts
				_textGroups.RemoveWhere((g) => g.Texts.Count == 0);
				foreach (var g in _textGroups) g.CleanTexts(() => UpdateCanvas());

				// Clean lines
				_wireMeshes.RemoveWhere((o) =>
				{
					if (o == null || o.IsExpired())
					{
						_poolWiredRenderers.Return(o);
						return true;
					}
					return false;
				});

				// Clean instances
				_mmc.RemoveExpired((o) => _poolInstanceRenderers.Return(o));
			}

			// FPS Graph
			fpsGraph.Update(delta);

			// Update overlay
			if (_canvasNeedUpdate || DebugDraw.FPSGraphEnabled)
			{
				if (CustomCanvas == null)
					CanvasItemInternal.QueueRedraw();
				else
					CustomCanvas.QueueRedraw();

				// reset some values
				_canvasNeedUpdate = false;
				EndTextGroupInternal();
			}

			// Update 3D debug
			UpdateDebugGeometry();
		}

		void UpdateDebugGeometry()
		{
			// Don't clear geometry for debug this debug class
			if (DebugDraw.Freeze3DRender)
				return;

			// Clear first and then leave
			_immediateGeometryMesh.ClearSurfaces();

			renderInstances = 0;
			renderWireframes = 0;

			// Return if nothing to do
			if (!DebugDraw.DebugEnabled)
			{
				lock (dataLock)
					_mmc?.HideAll();
				return;
			}

			// Get camera frustum
			var camera = DebugDraw.CustomViewport == null || DebugDraw.ForceUseCameraFromScene ?
					debugDraw.GetViewport().GetCamera3D() :
					DebugDraw.CustomViewport.GetCamera3D();

			var frustumPlanes = camera?.GetFrustum();
			Plane[] f = frustumPlanes?.ToArray();

			// Check visibility of all objects
			lock (dataLock)
			{
				// Update visibility
				if (DebugDraw.UseFrustumCulling && f != null)
				{
					// Update immediate geometry
					foreach (var _lines in _wireMeshes)
						_lines.IsVisible = Geometry.BoundsPartiallyInsideConvexShape(_lines.Bounds, f);
					// Update meshes
					_mmc.UpdateVisibility(f);
				}

				_immediateGeometryMesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
				// Line drawing much faster with only one Begin/End call
				foreach (var m in _wireMeshes)
				{
					m.IsUsedOneTime = true;

					if (m.IsVisible)
					{
						renderWireframes++;
						_immediateGeometryMesh.SurfaceSetColor(m.LinesColor);
						foreach (var l in m.Lines)
						{
							_immediateGeometryMesh.SurfaceAddVertex(l);
						}
						;
					}
				}
				_immediateGeometryMesh.SurfaceEnd();

				// Update MultiMeshInstances
				_mmc.UpdateInstances();
			}
		}

		public void OnCanvasItemDraw()
		{
			if (!DebugDraw.DebugEnabled)
				return;

			var time = DateTime.Now;
			Vector2 vp_size = HasMeta("UseParentSize") ? GetParent<Control>().Size : GetViewportRect().Size;

			lock (dataLock)
			{ // Text drawing
				var count = _textGroups.Sum((g) => g.Texts.Count + (g.ShowTitle ? 1 : 0));

				const string separator = " : ";

				Vector2 ascent = new Vector2(0, _font.GetAscent());
				Vector2 font_offset = ascent + DebugDraw.TextPadding;
				float line_height = _font.GetHeight() + DebugDraw.TextPadding.Y * 2;
				Vector2 pos = Vector2.Zero;
				float size_mul = 0;

				switch (DebugDraw.TextBlockPosition)
				{
					case DebugDraw.BlockPosition.LeftTop:
						pos = DebugDraw.TextBlockOffset;
						size_mul = 0;
						break;
					case DebugDraw.BlockPosition.RightTop:
						pos = new Vector2(
								vp_size.X - DebugDraw.TextBlockOffset.X,
								DebugDraw.TextBlockOffset.Y);
						size_mul = -1;
						break;
					case DebugDraw.BlockPosition.LeftBottom:
						pos = new Vector2(
								DebugDraw.TextBlockOffset.X,
								vp_size.Y - DebugDraw.TextBlockOffset.Y - line_height * count);
						size_mul = 0;
						break;
					case DebugDraw.BlockPosition.RightBottom:
						pos = new Vector2(
								vp_size.X - DebugDraw.TextBlockOffset.X,
								vp_size.Y - DebugDraw.TextBlockOffset.Y - line_height * count);
						size_mul = -1;
						break;
				}

				foreach (var g in _textGroups.OrderBy(g => g.GroupPriority))
				{
					var a = g.Texts.OrderBy(t => t.Value.Priority).ThenBy(t => t.Key);

					foreach (var t in g.ShowTitle ? a.Prepend(new KeyValuePair<string, DelayedText>(g.Title ?? "", null)) : a)
					{
						var keyText = t.Key ?? "";
						var text = t.Value?.Text == null ? keyText : $"{keyText}{separator}{t.Value.Text}";
						var size = _font.GetStringSize(text);
						float size_right_revert = (size.X + DebugDraw.TextPadding.X * 2) * size_mul;
						DrawRect(
								new Rect2(new Vector2(pos.X + size_right_revert, pos.Y),
								new Vector2(size.X + DebugDraw.TextPadding.X * 2, line_height)),
								DebugDraw.TextBackgroundColor);

						// Draw colored string
						if (t.Value == null || t.Value.ValueColor == null || t.Value.Text == null)
						{
							DrawString(_font, new Vector2(pos.X + font_offset.X + size_right_revert, pos.Y + font_offset.Y), text, modulate: g.GroupColor);
						}
						else
						{
							var textSep = $"{keyText}{separator}";
							var _keyLength = textSep.Length;
							DrawString(_font,
									new Vector2(pos.X + font_offset.X + size_right_revert, pos.Y + font_offset.Y),
									text.Substring(0, _keyLength), modulate: g.GroupColor);
							DrawString(_font,
									new Vector2(pos.X + font_offset.X + size_right_revert + _font.GetStringSize(textSep).X, pos.Y + font_offset.Y),
									text.Substring(_keyLength), modulate: t.Value.ValueColor.Value);
						}
						pos.Y += line_height;
					}
				}
			}

			if (DebugDraw.FPSGraphEnabled)
				fpsGraph.Draw( _font, vp_size);
		}

		void UpdateCanvas()
		{
			_canvasNeedUpdate = true;
		}

		#region Local Draw Functions

		public void Clear3DObjectsInternal()
		{
			lock (dataLock)
			{
				_wireMeshes.Clear();
				_mmc?.ClearInstances();
			}
		}

		public void Clear2DObjectsInternal()
		{
			lock (dataLock)
			{
				_textGroups.Clear();
				UpdateCanvas();
			}
		}

		public void ClearAllInternal()
		{
			Clear2DObjectsInternal();
			Clear3DObjectsInternal();
		}

		#region 3D

		#region Spheres

		public void DrawSphereInternal(ref Vector3 position, float radius, ref Color? color, float duration)
		{
			if (!DebugDraw.DebugEnabled) return;

			var t = Transform3D.Identity;
			t.Origin = position;
			t.Basis = t.Basis.Scaled(Vector3.One * (radius * 2));

			DrawSphereInternal(ref t, ref color, duration);
		}

		public void DrawSphereInternal(ref Transform3D transform, ref Color? color, float duration)
		{
			if (!DebugDraw.DebugEnabled) return;

			lock (dataLock)
			{
				var inst = _poolInstanceRenderers.Get();
				inst.InstanceTransform = transform;
				inst.InstanceColor = color ?? Colors.Chartreuse;
				inst.Bounds.Position = transform.Origin; inst.Bounds.Radius = transform.Basis.Scale.Length() * 0.5f;
				inst.ExpirationTime = DateTime.Now + TimeSpan.FromSeconds(duration);

				_mmc?.Spheres.Add(inst);
			}
		}

		#endregion // Spheres

		#region Cylinders

		public void DrawCylinderInternal(ref Vector3 position, float radius, float height, ref Color? color, float duration)
		{
			if (!DebugDraw.DebugEnabled) return;

			var t = Transform3D.Identity;
			t.Origin = position;
			t.Basis = t.Basis.Scaled(new Vector3(radius * 2, height, radius * 2));

			DrawCylinderInternal(ref t, ref color, duration);
		}

		public void DrawCylinderInternal(ref Transform3D transform, ref Color? color, float duration)
		{
			if (!DebugDraw.DebugEnabled) return;

			lock (dataLock)
			{
				var inst = _poolInstanceRenderers.Get();
				inst.InstanceTransform = transform;
				inst.InstanceColor = color ?? Colors.Yellow;
				inst.Bounds.Position = transform.Origin; inst.Bounds.Radius = transform.Basis.Scale.Length() * 0.5f;
				inst.ExpirationTime = DateTime.Now + TimeSpan.FromSeconds(duration);

				_mmc?.Cylinders.Add(inst);
			}
		}

		#endregion // Cylinders

		#region Boxes

		public void DrawBoxInternal(ref Vector3 position, ref Vector3 size, ref Color? color, float duration, bool isBoxCentered)
		{
			if (!DebugDraw.DebugEnabled) return;

			var q = Quaternion.Identity;
			DrawBoxInternal(ref position, ref q, ref size, ref color, duration, isBoxCentered);
		}

		public void DrawBoxInternal(ref Vector3 position, ref Quaternion rotation, ref Vector3 size, ref Color? color, float duration, bool isBoxCentered)
		{
			if (!DebugDraw.DebugEnabled) return;

			lock (dataLock)
			{
				var t = new Transform3D(new Basis(rotation), position);
				t.Basis = t.Basis.Scaled(size);
				var radius = size.Length() * 0.5f;

				var inst = _poolInstanceRenderers.Get();
				inst.InstanceTransform = t;
				inst.InstanceColor = color ?? Colors.ForestGreen;
				inst.ExpirationTime = DateTime.Now + TimeSpan.FromSeconds(duration);
				inst.Bounds.Radius = radius;

				if (isBoxCentered)
					inst.Bounds.Position = t.Origin;
				else
					inst.Bounds.Position = t.Origin + size * 0.5f;

				if (isBoxCentered)
					_mmc?.CubesCentered.Add(inst);
				else
					_mmc?.Cubes.Add(inst);
			}
		}

		public void DrawBoxInternal(ref Transform3D transform, ref Color? color, float duration, bool isBoxCentered)
		{
			if (!DebugDraw.DebugEnabled) return;

			lock (dataLock)
			{
				var radius = transform.Basis.Scale.Length() * 0.5f;

				var inst = _poolInstanceRenderers.Get();
				inst.InstanceTransform = transform;
				inst.InstanceColor = color ?? Colors.ForestGreen;
				inst.ExpirationTime = DateTime.Now + TimeSpan.FromSeconds(duration);
				inst.Bounds.Radius = radius;

				if (isBoxCentered)
					inst.Bounds.Position = transform.Origin;
				else
					inst.Bounds.Position = transform.Origin + transform.Basis.Scale * 0.5f;

				if (isBoxCentered)
					_mmc?.CubesCentered.Add(inst);
				else
					_mmc?.Cubes.Add(inst);
			}
		}

		public void DrawAABBInternal(ref Aabb box, ref Color? color, float duration)
		{
			if (!DebugDraw.DebugEnabled) return;
			Geometry.GetDiagonalVectors(box.Position, box.End, out Vector3 bottom, out _, out Vector3 diag);
			DrawBoxInternal(ref bottom, ref diag, ref color, duration, false);
		}

		public void DrawAABBInternal(ref Vector3 a, ref Vector3 b, ref Color? color, float duration)
		{
			if (!DebugDraw.DebugEnabled) return;
			Geometry.GetDiagonalVectors(a, b, out Vector3 bottom, out _, out Vector3 diag);
			DrawBoxInternal(ref bottom, ref diag, ref color, duration, false);
		}

		#endregion // Boxes

		#region Lines

		public void DrawLine3DHitInternal(ref Vector3 a, ref Vector3 b, bool isHit, float unitOffsetOfHit, float hitSize, float duration, ref Color? hitColor, ref Color? afterHitColor)
		{
			if (!DebugDraw.DebugEnabled) return;

			lock (dataLock)
			{
				if (isHit && unitOffsetOfHit >= 0 && unitOffsetOfHit <= 1.0f)
				{
					var time = DateTime.Now + TimeSpan.FromSeconds(duration);
					var hit_pos = (b - a).Normalized() * a.DistanceTo(b) * unitOffsetOfHit + a;

					// Get lines from pool and setup
					var line_a = _poolWiredRenderers.Get();
					var line_b = _poolWiredRenderers.Get();

					line_a.Lines = new Vector3[] { a, hit_pos };
					line_a.LinesColor = hitColor ?? DebugDraw.LineHitColor;
					line_a.ExpirationTime = time;

					line_b.Lines = new Vector3[] { hit_pos, b };
					line_b.LinesColor = afterHitColor ?? DebugDraw.LineAfterHitColor;
					line_b.ExpirationTime = time;

					_wireMeshes.Add(line_a);
					_wireMeshes.Add(line_b);

					// Get instance from pool and setup
					var t = new Transform3D(Basis.Identity, hit_pos);
					t.Basis = t.Basis.Scaled(Vector3.One * hitSize);

					var inst = _poolInstanceRenderers.Get();
					inst.InstanceTransform = t;
					inst.InstanceColor = hitColor ?? DebugDraw.LineHitColor;
					inst.Bounds.Position = t.Origin; inst.Bounds.Radius = Geometry.CubeDiagonalLengthForSphere * hitSize;
					inst.ExpirationTime = time;

					_mmc?.BillboardSquares.Add(inst);
				}
				else
				{
					var line = _poolWiredRenderers.Get();

					line.Lines = new Vector3[] { a, b };
					line.LinesColor = hitColor ?? DebugDraw.LineHitColor;
					line.ExpirationTime = DateTime.Now + TimeSpan.FromSeconds(duration);

					_wireMeshes.Add(line);
				}
			}
		}

		#region Normal

		public void DrawLine3DInternal(ref Vector3 a, ref Vector3 b, ref Color? color, float duration)
		{
			if (!DebugDraw.DebugEnabled) return;

			lock (dataLock)
			{
				var line = _poolWiredRenderers.Get();

				line.Lines = new Vector3[] { a, b };
				line.LinesColor = color ?? Colors.LightGreen;
				line.ExpirationTime = DateTime.Now + TimeSpan.FromSeconds(duration);

				_wireMeshes.Add(line);
			}
		}

		public void DrawRay3DInternal(Vector3 origin, Vector3 direction, float length, Color? color, float duration)
		{
			if (!DebugDraw.DebugEnabled) return;

			var end = origin + direction * length;
			DrawLine3DInternal(ref origin, ref end, ref color, duration);
		}

		public void DrawLinePath3DInternal(IList<Vector3> path, Color? color, float duration = 0f)
		{
			if (!DebugDraw.DebugEnabled) return;

			if (path == null || path.Count < 2) return; // Changed to < 2

			lock (dataLock)
			{
				var line = _poolWiredRenderers.Get();

				line.Lines = Geometry.CreateLinesFromPath(path);
				line.LinesColor = color ?? Colors.LightGreen;
				line.ExpirationTime = DateTime.Now + TimeSpan.FromSeconds(duration);

				_wireMeshes.Add(line);
			}
		}

		public void DrawLinePath3DInternal(Color? color, float duration, params Vector3[] path)
		{
			if (!DebugDraw.DebugEnabled) return;

			DrawLinePath3DInternal(path, color, duration);
		}

		#endregion // Normal

		#region Arrows

		public void DrawArrowLine3DInternal(Vector3 a, Vector3 b, Color? color, float duration, float arrowSize, bool absoluteSize)
		{
			if (!DebugDraw.DebugEnabled) return;

			var line = _poolWiredRenderers.Get();

			line.Lines = new Vector3[] { a, b };
			line.LinesColor = color ?? Colors.LightGreen;
			line.ExpirationTime = DateTime.Now + TimeSpan.FromSeconds(duration);

			_wireMeshes.Add(line);

			GenerateArrowheadInstance(ref a, ref b, ref color, ref duration, ref arrowSize, ref absoluteSize);
		}

		public void DrawArrowRay3DInternal(Vector3 origin, Vector3 direction, float length, Color? color, float duration, float arrowSize, bool absoluteSize)
		{
			if (!DebugDraw.DebugEnabled) return;

			DrawArrowLine3DInternal(origin, origin + direction * length, color, duration, arrowSize, absoluteSize);
		}

		public void DrawArrowPath3DInternal(IList<Vector3> path, ref Color? color, float duration, float arrowSize, bool absoluteSize)
		{
			if (!DebugDraw.DebugEnabled) return;

			if (path == null || path.Count < 2) return;

			var line = _poolWiredRenderers.Get();
			line.Lines = Geometry.CreateLinesFromPath(path);
			line.LinesColor = color ?? Colors.LightGreen;
			line.ExpirationTime = DateTime.Now + TimeSpan.FromSeconds(duration);
			_wireMeshes.Add(line);

			for (int i = 0; i < path.Count - 1; i++)
			{
				Vector3 a = path[i], b = path[i + 1];
				GenerateArrowheadInstance(ref a, ref b, ref color, ref duration, ref arrowSize, ref absoluteSize);
			}
		}

		public void DrawArrowPath3DInternal(ref Color? color, float duration, float arrowSize, bool absoluteSize, params Vector3[] path)
		{
			if (!DebugDraw.DebugEnabled) return;

			DrawArrowPath3DInternal(path, ref color, duration, arrowSize, absoluteSize);
		}

		#endregion // Arrows
		#endregion // Lines

		#region Misc

		public void DrawBillboardSquareInternal(ref Vector3 position, float size, ref Color? color, float duration)
		{
			if (!DebugDraw.DebugEnabled) return;

			lock (dataLock)
			{
				var t = Transform3D.Identity;
				t.Origin = position;
				t.Basis = t.Basis.Scaled(Vector3.One * size);

				var inst = _poolInstanceRenderers.Get();
				inst.InstanceTransform = t;
				inst.InstanceColor = color ?? Colors.Red;
				inst.Bounds.Position = t.Origin; inst.Bounds.Radius = Geometry.CubeDiagonalLengthForSphere * size;
				inst.ExpirationTime = DateTime.Now + TimeSpan.FromSeconds(duration);

				_mmc?.BillboardSquares.Add(inst);
			}
		}

		#region Camera Frustum

		public void DrawCameraFrustumInternal(ref Camera3D camera, ref Color? color, float duration)
		{
			if (!DebugDraw.DebugEnabled) return;
			if (camera == null) return;

			DrawCameraFrustumInternal(ref camera, ref color, duration);
		}

		[Obsolete("GetFrustum() now returns Plane[]. This overload is no longer necessary.")]
		public void DrawCameraFrustumInternal(ref GDArray cameraFrustum, ref Color? color, float duration)
		{
			if (!DebugDraw.DebugEnabled) return;
			if (cameraFrustum.Count != 6) return;

			Plane[] f = new Plane[cameraFrustum.Count];
			for (int i = 0; i < cameraFrustum.Count; i++)
				f[i] = ((Plane)cameraFrustum[i]);

			DrawCameraFrustumInternal(ref f, ref color, duration);
		}

		public void DrawCameraFrustumInternal(ref Plane[] planes, ref Color? color, float duration)
		{
			if (!DebugDraw.DebugEnabled) return;
			if (planes.Length != 6) return;

			lock (dataLock)
			{
				var line = _poolWiredRenderers.Get();

				line.Lines = Geometry.CreateCameraFrustumLines(planes);
				line.LinesColor = color ?? Colors.DarkSalmon;
				line.ExpirationTime = DateTime.Now + TimeSpan.FromSeconds(duration);

				_wireMeshes.Add(line);
			}
		}

		#endregion // Camera frustum

		public void DrawPosition3DInternal(ref Transform3D transform, ref Color? color, float duration)
		{
			if (!DebugDraw.DebugEnabled) return;

			lock (dataLock)
			{
				var s = transform.Basis.Scale;

				var inst = _poolInstanceRenderers.Get();
				inst.InstanceTransform = transform;
				inst.InstanceColor = color ?? Colors.Crimson;
				inst.Bounds.Position = transform.Origin; inst.Bounds.Radius = Geometry.GetMaxValue(ref s) * 0.5f;
				inst.ExpirationTime = DateTime.Now + TimeSpan.FromSeconds(duration);

				_mmc?.Positions.Add(inst);
			}
		}

		public void DrawPosition3DInternal(ref Vector3 position, ref Quaternion rotation, ref Vector3 scale, ref Color? color, float duration)
		{
			if (!DebugDraw.DebugEnabled) return;

			var t = new Transform3D(new Basis(rotation), position);
			t.Basis = t.Basis.Scaled(scale);

			DrawPosition3DInternal(ref t, ref color, duration);
		}

		public void DrawPosition3DInternal(ref Vector3 position, ref Color? color, float scale, float duration)
		{
			if (!DebugDraw.DebugEnabled) return;

			var t = new Transform3D(Basis.Identity, position);
			t.Basis = t.Basis.Scaled(Vector3.One * scale);

			DrawPosition3DInternal(ref t, ref color, duration);
		}

		#endregion // Misc
		#endregion // 3D

		#region 2D

		public void BeginTextGroupInternal(string groupTitle, int groupPriority, ref Color? groupColor, bool showTitle)
		{
			lock (dataLock)
			{
				var newGroup = _textGroups.FirstOrDefault(g => g.Title == groupTitle);
				if (newGroup != null)
				{
					newGroup.ShowTitle = showTitle;
					newGroup.GroupPriority = groupPriority;
					newGroup.GroupColor = groupColor ?? DebugDraw.TextForegroundColor;
				}
				else
				{
					newGroup = new TextGroup(groupTitle, groupPriority, showTitle, groupColor ?? DebugDraw.TextForegroundColor);
					_textGroups.Add(newGroup);
				}
				_currentTextGroup = newGroup;
			}
		}

		public void EndTextGroupInternal()
		{
			lock (dataLock)
			{
				if (!_textGroups.Contains(_defaultTextGroup))
					_textGroups.Add(_defaultTextGroup);
				_currentTextGroup = _defaultTextGroup;

				// Update color 
				_defaultTextGroup.GroupColor = DebugDraw.TextForegroundColor;
			}
		}

		public void SetTextIntenal(ref string key, ref object value, int priority, ref Color? colorOfValue, float duration)
		{
			if (!DebugDraw.DebugEnabled) return;

			var _newTime = DateTime.Now + (duration < 0 ? DebugDraw.TextDefaultDuration : TimeSpan.FromSeconds(duration));
			var _strVal = value?.ToString();

			lock (dataLock)
			{
				if (_currentTextGroup.Texts.ContainsKey(key))
				{
					var t = _currentTextGroup.Texts[key];
					if (_strVal != t.Text)
						UpdateCanvas();
					t.Text = _strVal;
					t.Priority = priority;
					t.ExpirationTime = _newTime;
					t.ValueColor = colorOfValue;
				}
				else
				{
					_currentTextGroup.Texts[key] = new DelayedText(_newTime, _strVal, priority, colorOfValue);
					UpdateCanvas();
				}
			}
		}

		#endregion // 2D
		#endregion

		#region Utilities

		void DrawDebugBoundsForDebugLinePrimitives(DelayedRendererLine dr)
		{
			if (!dr.IsVisible)
				return;

			var _lines = Geometry.CreateCubeLines(dr.Bounds.Position, Quaternion.Identity, dr.Bounds.Size, false, true);

			renderWireframes++;
			_immediateGeometryMesh.SurfaceSetColor(Colors.Orange);
			foreach (var l in _lines)
			{
				_immediateGeometryMesh.SurfaceAddVertex(l);
			}
			;
		}

		void DrawDebugBoundsForDebugInstancePrimitives(DelayedRendererInstance dr)
		{
			if (!dr.IsVisible)
				return;

			renderInstances++;
			var p = dr.Bounds.Position;
			var r = dr.Bounds.Radius;
			Color? c = Colors.DarkOrange;
			DrawSphereInternal(ref p, r, ref c, 0);
		}

		void GenerateArrowheadInstance(ref Vector3 a, ref Vector3 b, ref Color? color, ref float duration, ref float arrowSize, ref bool absoluteSize)
		{
			lock (dataLock)
			{
				var offset = (b - a);
				if (offset.LengthSquared() < 0.0001f) return; // Avoid NaN

				var length = (absoluteSize ? arrowSize : offset.Length() * arrowSize);
				var offsetNorm = offset.Normalized();

				var t = new Transform3D(Basis.Identity, b - offsetNorm * length).LookingAt(b, Vector3.Up);
				t.Basis = t.Basis.Scaled(Vector3.One * length);
				var time = DateTime.Now + TimeSpan.FromSeconds(duration);

				var inst = _poolInstanceRenderers.Get();
				inst.InstanceTransform = t;
				inst.InstanceColor = color ?? Colors.LightGreen;
				inst.Bounds.Position = t.Origin - t.Basis.Z * 0.5f; inst.Bounds.Radius = Geometry.CubeDiagonalLengthForSphere * length;
				inst.ExpirationTime = time;

				_mmc?.Arrowheads.Add(inst);
			}
		}
		#endregion // Utilities
	}
}
#endif
