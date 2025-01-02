using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
#pragma warning disable CS0649, IDE0044

public class SparkleManager : Node2D, Manager {

    PackedScene sparkleScene;

    Dictionary<Window, float> sparklingWindows;

    [Export]
    Vector2 sparkleDelayRange;

    [Export]
    Vector2 sparkleScaleRange;

    float NewSparkleDelay {
        get =>
            sparkleDelayRange.x
            + GD.Randf() * (sparkleDelayRange.y - sparkleDelayRange.x);
    }

    public static SparkleManager Instance { get; private set; }
    public override void _Ready() {
        Instance = this;
        sparklingWindows = new Dictionary<Window, float>();
        sparkleScene = GD.Load<PackedScene>("res://_scenes/fx/Sparkle.tscn");
        ManagerManager.Instance.ReportReady(this);
    }

    public void StartSparkling(Window w) {
        sparklingWindows.Add(w, NewSparkleDelay);
    }

    public void StopSparkling(Window w) {
        sparklingWindows.Remove(w);
    }

    public override void _Process(float delta) {
        var wandows = sparklingWindows.Keys.ToArray();
        foreach (var w in wandows) {
            var t = sparklingWindows[w];
            t -= delta;
            sparklingWindows[w] = t;
            if (t > 0) continue;
            var luckyPane = (int)(GD.Randi() % w.BoundsCount);
            CreateSparkle(w.LowerBounds[luckyPane], w.UpperBounds[luckyPane]);
            sparklingWindows[w] = NewSparkleDelay;
        }
    }

    private void CreateSparkle(Vector2 lowerBound, Vector2 upperBound) {
        var x = (float)GD.RandRange(lowerBound.x, upperBound.x);
        var y = (float)GD.RandRange(lowerBound.y, upperBound.y);
        var s = sparkleScene.Instance<Sparkle>();
        s.GlobalPosition = new Vector2(x, y);
        s.Scale = Vector2.One * (float)GD.RandRange(sparkleScaleRange.x, sparkleScaleRange.y);
        AddChild(s);
    }

    public void OnAllReady() {
        // nothing to do
    }
}
