using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static Utility;
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

    List<Grime> registeredGrimes;

    Dictionary<Window, int> perWindowGrimeCount;

    string[] winTextOptions = new string[] {
        "PRISTINE",
        "SPOTLESS",
        "OOH SHINY",
        "FABULOUS",
        "IMPECCABLE",
        "PERFECTION",
        "ULTRACLEAN",
        "GLEAMING",
        "IMMACULATE",
        "HOT!",
        "WELL DONE",
        "SPARKLY",
        "SPIC AND SPAN",
        "FLAWLESS",
        "WOW!",
        "TOOK YOU\nLONG ENOUGH",
        "C-L-E-A-N"
    };

    public float PercentGrimy {
        get {
            if (maxGrimes == 0) return 0;
            return (float)currentGrimes / maxGrimes;
        }
    }
    public bool IsBlinking { get; private set; }

    [Export]
    float grimeFreqJitter;

    int maxGrimes;
    int currentGrimes;

    bool hasConnectedBrush;

    static List<Window> grimeRequests = new List<Window>();

    public static GrimeManager Instance { get; private set; }
    public override void _Ready() {
        hasConnectedBrush = false;
        registeredGrimes = new List<Grime>();
        perWindowGrimeCount = new Dictionary<Window, int>();
        Instance = this;
        maxGrimes = 0;
        currentGrimes = 0;
        grimePackedScene = GD.Load<PackedScene>("res://_scenes/grime/grime.tscn");
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetFrequency(frequency);
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        winLabel = GetNode<Label>("Win Label");
        Overseer.Instance.ReportReady(this);
    }

    public override void _Process(float delta) {
        if (BrushController.Instance != null && !hasConnectedBrush) {
            foreach (var b in BrushController.Instance.brushHitboxes) {
                b.Connect("area_entered", this, "OnClean");
            }
            hasConnectedBrush = true;
        }
        if (grimeRequests.Count > 0) {
            var gr = grimeRequests.First();
            grimeRequests.RemoveAt(0);
            GenerateGrime(gr);
        }
    }
    void GenerateGrime(Window w) {
        var j1 = GetJitter(grimeFreqJitter);
        var j2 = GetJitter(grimeFreqJitter);
        var s1 = (int)(GD.Randi() % int.MaxValue);
        var s2 = (int)(GD.Randi() % int.MaxValue);

        var c1 = GetRandomGrimeColor(Colors.Transparent);
        var c2 = GetRandomGrimeColor(c1);
        var g = 0;
        for (var x = 0; x < w.BoundsCount; x++) {
            noise.SetSeed(s1);
            noise.SetFrequency(frequency + j1);
            g += GrimePass(w, w.LowerBounds[x], w.UpperBounds[x], c1);
            noise.SetSeed(s2);
            noise.SetFrequency(frequency + j2);
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
                    w.AddChild(g);
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
        // nothing to do
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
        if (currentGrimes < 1) {
            currentGrimes = 0;
            LevelManager.Instance.AllowProgression();
        }
    }

    public void ShowWinLabel(string message = null) {
        GD.Print(message);
        if (message == null) {
            winLabel.Text = winTextOptions[GD.Randi() % winTextOptions.Length];
        } else {
            winLabel.Text = message;
        }
        winLabel.Visible = true;
        IsBlinking = true;
        winLabel.Modulate = win1;
        Blink();
    }

    void Blink() {
        if (!IsBlinking) return;
        ToSignal(GetTree().CreateTimer(winBlinkTime), "timeout").OnCompleted(() => {
            if (winLabel.Modulate.Equals(win1)) winLabel.Modulate = win2; else winLabel.Modulate = win1;
            Blink();
        });
    }

    public bool Reset() {
        maxGrimes = 0;
        currentGrimes = 0;
        winLabel.Visible = false;
        hasConnectedBrush = false;
        IsBlinking = false;
        while (registeredGrimes.Count > 0) {
            var g = registeredGrimes[0];
            g.QueueFree();
            registeredGrimes.RemoveAt(0);
        }
        perWindowGrimeCount = new Dictionary<Window, int>();
        return false;
    }
    public PackedScene GetPackedScene() => null;
}
