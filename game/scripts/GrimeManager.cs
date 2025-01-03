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
        "IMMACULATE",
        "HOT!",
        "WELL DONE",
        "SPARKLY"
    };

    public float PercentGrimy {
        get {
            if (maxGrimes == 0) return 0;
            return (float)currentGrimes / maxGrimes;
        }
    }

    int maxGrimes;
    int currentGrimes;

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
        winLabel = GetNode<Label>("Win Label");
        Overseer.Instance.ReportReady(this);
    }

    public override void _Process(float delta) {
        if (currentGrimes == 0 && maxGrimes != 0) {
            if (!winLabel.Visible) {
                winLabel.Text = winTextOptions[GD.Randi() % winTextOptions.Length];
                winLabel.Visible = true;
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
        maxGrimes = 0;
        currentGrimes = 0;
    }

    void GenerateGrime(Window w) {
        noise.SetSeed((int)(GD.Randi() % int.MaxValue));
        var c1 = GetRandomGrimeColor(Colors.Transparent);
        var c2 = GetRandomGrimeColor(c1);
        var g = 0;
        for (var x = 0; x < w.BoundsCount; x++) {
            g += GrimePass(w, w.LowerBounds[x], w.UpperBounds[x], c1);
            g += GrimePass(w, w.LowerBounds[x], w.UpperBounds[x], c2);
        }
        maxGrimes += g;
        currentGrimes += g;
    }

    int GrimePass(Window w, Vector2 lowerBound, Vector2 upperBound, Color c) {
        var grimesCreated = 0;
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
                    grimesCreated += 1;
                }
            }
        }
        return grimesCreated;
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
        if (newVal < threshold) {
            g.QueueFree();
            perWindowGrimeCount[g.window] = perWindowGrimeCount[g.window] - 1;
            currentGrimes -= 1;
        } else {
            g.Modulate = new Color(c.r, c.g, c.b, newVal);
        }

        if (perWindowGrimeCount[g.window] < 1) {
            g.window.OnCleaned();
            perWindowGrimeCount.Remove(g.window);
        }
        if (currentGrimes < 0) currentGrimes = 0;
    }
}
