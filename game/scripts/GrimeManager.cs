using System;
using System.Collections.Generic;
using Godot;

public class GrimeManager : Node, Manager {
    static PackedScene grimePackedScene;

    FastNoiseLite noise = new FastNoiseLite();

    [Export]
    float threshold;

    [Export]
    float frequency;

    [Export]
    Color[] possibleGrimeColors;

    public float PercentGrimy {
        get {
            if (maxGrime == 0) return 0;
            return currentGrime / maxGrime;
        }
    }

    float maxGrime;
    float currentGrime;

    static List<GrimeRequest> grimeRequests = new List<GrimeRequest>();

    public static GrimeManager Instance { get; private set; }
    public override void _Ready() {
        Instance = this;
        ResetGrimeMeter();
        grimePackedScene = GD.Load<PackedScene>("res://scenes/grime/grime speck.tscn");
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetFrequency(frequency);
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        ManagerManager.Instance.ReportReady(this);
    }

    void ResetGrimeMeter() {
        maxGrime = 0f;
        currentGrime = 0f;
    }

    void GenerateGrime(GrimeRequest request) {
        var c1 = GetRandomGrimeColor(Colors.Transparent);
        var c2 = GetRandomGrimeColor(c1);
        var g = 0f;
        for (var x = 0; x < request.lowerBounds.Length; x++) {
            g += GrimePass(request.lowerBounds[x], request.upperBounds[x], c1);
            g += GrimePass(request.lowerBounds[x], request.upperBounds[x], c2);
        }
        maxGrime += g;
        currentGrime += g;
    }

    float GrimePass(Vector2 lowerBound, Vector2 upperBound, Color c) {
        noise.SetSeed((int)(GD.Randi() % int.MaxValue));
        var grimeCreated = 0f;
        for (var x = lowerBound.x; x <= upperBound.x; x++) {
            for (var y = lowerBound.y; y <= upperBound.y; y++) {
                var val = Mathf.Clamp(noise.GetNoise(x, y), 0, 1);
                if (val >= threshold) {
                    var g = grimePackedScene.Instance<Node2D>();
                    AddChild(g);
                    g.GlobalPosition = new Vector2(x, y);
                    g.Modulate = new Color(c.r, c.g, c.b, val);
                    grimeCreated += val;
                }
            }
        }
        return grimeCreated;
    }
    /// <summary>
    /// Queues grime grime generation within the given bounds.
    /// </summary>
    /// <param name="lowerBound">the lower bounds in global coordinates</param>
    /// <param name="upperBound">the upper bounds in global coordinates</param>
    public static void QueueGrime(Vector2[] lowerBounds, Vector2[] upperBounds) {
        grimeRequests.Add(new GrimeRequest(lowerBounds, upperBounds));
    }

    public Color GetRandomGrimeColor(Color excludedColor) {
        Color color;
        do {
            color = possibleGrimeColors[GD.Randi() % possibleGrimeColors.Length];
        } while (color == excludedColor);
        return color;
    }

    public void OnAllReady() {
        foreach (var b in BrushController.Instance.brushHitboxes) {
            b.Connect("area_entered", this, "OnClean");
        }
        for (var i = 0; i < grimeRequests.Count; i++) {
            var gr = grimeRequests[i];
            GenerateGrime(gr);
        }
    }

    public struct GrimeRequest {
        public Vector2[] lowerBounds;
        public Vector2[] upperBounds;

        public GrimeRequest(Vector2[] lowerBounds, Vector2[] upperBounds) {
            this.lowerBounds = lowerBounds;
            this.upperBounds = upperBounds;
        }
    }

    public void OnClean(Area2D area2D) {
        if (area2D.IsQueuedForDeletion()) return;
        var c = area2D.Modulate;
        var newVal = c.a - BrushController.Instance.scrubbingPower;
        var delta = newVal - c.a;
        if (newVal < threshold) {
            area2D.QueueFree();
            currentGrime -= c.a;
        } else {
            area2D.Modulate = new Color(c.r, c.g, c.b, newVal);
            currentGrime += delta;
        }
        if (currentGrime < 0) currentGrime = 0;
    }
}
