using System;
using System.Collections.Generic;
using Godot;
#pragma warning disable CS0649, IDE0044

public class Window : Node2D {

    public Vector2[] LowerBounds { get; private set; }
    public Vector2[] UpperBounds { get; private set; }

    [Export]
    List<Window> sisterWindows;

    [Export]
    public bool IsClean { get; private set; }

    public int BoundsCount { get => LowerBounds.Length; }

    public override void _Ready() {
        Connect("tree_exiting", this, "OnWindowDestroyed");
        var lowerBoundsList = new List<Vector2>();
        var upperBoundsList = new List<Vector2>();
        foreach (Node2D c in GetChildren()) {
            if (c is CollisionShape2D) {
                var shape = (c as CollisionShape2D).Shape;
                if (shape is RectangleShape2D) {
                    var rect = shape as RectangleShape2D;
                    lowerBoundsList.Add(c.GlobalPosition - rect.Extents);
                    upperBoundsList.Add(c.GlobalPosition + rect.Extents);
                } else {
                    GD.PrintErr($"Window {Name} has grime bounds shape of type {shape.GetType().Name}. Ignoring...");
                }
            }
        }
        LowerBounds = lowerBoundsList.ToArray();
        UpperBounds = upperBoundsList.ToArray();
        IsClean = false;
        GrimeManager.QueueGrime(this);
    }

    public void OnCleaned() {
        IsClean = true;
        if (sisterWindows == null) FindSisters();
        var areAllSistersClean = true;

        foreach (var sw in sisterWindows) {
            areAllSistersClean = areAllSistersClean && sw.IsClean;
        }
        if (areAllSistersClean) {
            SparkleManager.Instance.StartSparkling(this);
            foreach (var sw in sisterWindows) {
                sw.OnAllSistersCleaned();
            }
        }
    }

    void FindSisters() {
        sisterWindows = new List<Window>();
        for (var i = 0; i < GetParent().GetChildCount(); i++) {
            if (GetParent().GetChild<Node>(i) is Window sister) {
                if (sister == this) continue;
                sisterWindows.Add(sister);
            }
        }
    }

    protected void OnAllSistersCleaned() => SparkleManager.Instance.StartSparkling(this);

    public void OnWindowDestroyed() {
        SparkleManager.Instance.StopSparkling(this);
    }
}
