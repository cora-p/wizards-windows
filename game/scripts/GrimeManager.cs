using System;
using System.Collections.Generic;
using Godot;
#pragma warning disable CS0649, IDE0044

public class GrimeManager : Node, Manager {
    static PackedScene grimePackedScene;

    FastNoiseLite noise = new FastNoiseLite();

    [Export]
    float threshold;

    [Export]
    float frequency;

    [Export]
    Color[] possibleGrimeColors;

    Node2D winLabelHolder;
    Label winLabel;

    [Export]
    Color win1, win2;
    [Export]
    float winBlinkTime;
    float remainingTime;

    Dictionary<Window, int> perWindowGrimeCount;

    string[] winTextOptions = new string[] {
        "PRISTINE",
        "SPOTLESS",
        "OOH SHINY",
        "FABULOUS",
        "YOU WIN",
        "IMPECCABLE",
        "PERFECTION",
        "ULTRACLEAN",
        "GLEAMING",
        "IMMACULATE"
    };

    public float PercentGrimy {
        get {
            if (maxGrime == 0) return 0;
            return currentGrime / maxGrime;
        }
    }

    float maxGrime;
    float currentGrime;

    static List<Window> grimeRequests = new List<Window>();

    public static GrimeManager Instance { get; private set; }
    public override void _Ready() {
        perWindowGrimeCount = new Dictionary<Window, int>();
        Instance = this;
        ResetGrimeMeter();
        grimePackedScene = GD.Load<PackedScene>("res://_scenes/grime/grime.tscn");
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetFrequency(frequency);
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        winLabelHolder = GetNode<Node2D>("Win Label Holder");
        winLabel = winLabelHolder.GetChild<Label>(0);
        ManagerManager.Instance.ReportReady(this);
    }

    public override void _Process(float delta) {
        if (currentGrime == 0 && maxGrime != 0) {
            if (!winLabelHolder.Visible) {
                winLabel.Text = winTextOptions[GD.Randi() % winTextOptions.Length];
                winLabelHolder.Visible = true;
            }
            HandleBlinking(delta);
        }
    }

    void HandleBlinking(float delta) {
        if (remainingTime > 0) {
            remainingTime -= delta;
        } else {
            if (winLabel.Modulate.Equals(win1)) winLabel.Modulate = win2; else winLabel.Modulate = win1;
            remainingTime = winBlinkTime;
        }
    }

    void ResetGrimeMeter() {
        maxGrime = 0f;
        currentGrime = 0f;
    }

    void GenerateGrime(Window w) {
        var c1 = GetRandomGrimeColor(Colors.Transparent);
        var c2 = GetRandomGrimeColor(c1);
        var g = 0f;
        for (var x = 0; x < w.BoundsCount; x++) {
            g += GrimePass(w, w.LowerBounds[x], w.UpperBounds[x], c1);
            g += GrimePass(w, w.LowerBounds[x], w.UpperBounds[x], c2);
        }
        maxGrime += g;
        currentGrime += g;
    }

    float GrimePass(Window w, Vector2 lowerBound, Vector2 upperBound, Color c) {
        noise.SetSeed((int)(GD.Randi() % int.MaxValue));
        var grimeCreated = 0f;
        if (!perWindowGrimeCount.ContainsKey(w)) {
            perWindowGrimeCount.Add(w, 0);
        }
        for (var x = lowerBound.x; x <= upperBound.x; x++) {
            for (var y = lowerBound.y; y <= upperBound.y; y++) {
                var val = Mathf.Clamp(noise.GetNoise(x, y), 0, 1);
                if (val >= threshold) {
                    var g = grimePackedScene.Instance<Grime>();
                    AddChild(g);
                    g.window = w;
                    perWindowGrimeCount[w] = perWindowGrimeCount[w] + 1;
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
    public static void QueueGrime(Window window) {
        grimeRequests.Add(window);
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

    public void OnClean(Grime g) {
        if (g.IsQueuedForDeletion() || !perWindowGrimeCount.ContainsKey(g.window)) return;
        var c = g.Modulate;
        var newVal = c.a - BrushController.Instance.scrubbingPower;
        var delta = newVal - c.a;
        if (newVal < threshold) {
            g.QueueFree();
            perWindowGrimeCount[g.window] = perWindowGrimeCount[g.window] - 1;
            currentGrime -= c.a;
        } else {
            g.Modulate = new Color(c.r, c.g, c.b, newVal);
            currentGrime += delta;
        }

        if (perWindowGrimeCount[g.window] < 1) {
            g.window.OnCleaned();
            perWindowGrimeCount.Remove(g.window);
        }
        if (currentGrime < 0) currentGrime = 0;
    }
}
