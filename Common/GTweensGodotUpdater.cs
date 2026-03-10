using Godot;
using System;
using GTweensGodot.Contexts;

public partial class GTweensGodotUpdater : Node
{
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Process(double delta)
    {
        var deltaTime = (float)delta;
        if (!GetTree().Paused)
        {
            GodotGTweensContext.Instance.PausableContext.Tick(deltaTime);
        }

        GodotGTweensContext.Instance.UnpausableContext.Tick(deltaTime);
    }
}