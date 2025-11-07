// FILE: debug_draw_cs/DebugDrawEditor.cs
#if TOOLS
using Godot;
using System;

[Tool]
public partial class DebugDrawEditor : EditorPlugin
{
    public static string PluginDir = "res://addons/debug_draw_cs/";

	Control spatial_editor_viewport = null;

	public DebugDrawEditor()
	{
		GD.PrintRich($"DD: {GetType().Name}" );

	}

	public override void _EnterTree()
	{
		CreateAutoFind();

		if (!IsConnected(EditorPlugin.SignalName.SceneChanged, Callable.From(OnSceneChanged)))
			Connect(EditorPlugin.SignalName.SceneChanged, Callable.From(OnSceneChanged));
	}

	public override void _ExitTree()
	{
		RemovePrevNode();

		if (IsConnected(EditorPlugin.SignalName.SceneChanged, Callable.From(OnSceneChanged)))
			Disconnect(EditorPlugin.SignalName.SceneChanged, Callable.From(OnSceneChanged));
	}
		


    public override void _DisablePlugin()
    {
        RemovePrevNode();
    }

    public override void _Process(double delta)
    {
        // Dirty workaround for reloading of DebugDraw after project rebuild
        CreateAutoFind();
    }

    void OnSceneChanged()
	{
		var node = GetTree().CurrentScene;

        CreateNewNode(node);
    }

	#region Utilities

	void FindViewportControl()
	{
		// This gets the Node3DEditorViewport, which is a Control
		spatial_editor_viewport = EditorInterface.Singleton.GetEditorMainScreen();

		if (spatial_editor_viewport != null)
		{
			spatial_editor_viewport.SetMeta("UseParentSize", true);
			spatial_editor_viewport.QueueRedraw();
		}
	}


	void RemovePrevNode()
	{
		DebugDraw.Instance?.QueueFree();
		spatial_editor_viewport?.QueueRedraw();

		var root = EditorInterface.Singleton?.GetEditedSceneRoot();
		if (root != null)
		{
			if (root != null)
			{
				var nodes = root.GetChildren();
				foreach (Node n in nodes)
				{
					if (n.Owner == null && n.HasMeta(nameof(DebugDraw)) && !n.IsQueuedForDeletion())
					{
						n.QueueFree();
					}
				}
			}
		}
	}

	void CreateNewNode(Node parent)
	{
		RemovePrevNode();
		if (DebugDraw.Instance == null)
		{
			FindViewportControl();
			if (spatial_editor_viewport == null)
			{
				GD.PushWarning("DebugDrawEditor: Could not find 3D editor viewport.");
				return;
			}

			var d = new DebugDraw();
			parent.AddChild(d);

			DebugDraw.CustomViewport = spatial_editor_viewport.GetViewport();
			DebugDraw.CustomCanvas = spatial_editor_viewport;
		}
	}


	void CreateAutoFind()
	{
		if (DebugDraw.Instance == null)
		{
			Node node = EditorInterface.Singleton?.GetEditedSceneRoot();
			if (node != null)
				CreateNewNode(node);
		}
	}
		
		    #endregion
}
#endif