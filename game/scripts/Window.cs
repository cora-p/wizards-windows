using System;
using System.Collections.Generic;
using Godot;

public class Window : Node2D {
    public override void _Ready() {
        List<Vector2> lowerBounds = new List<Vector2>();
        List<Vector2> upperBounds = new List<Vector2>();
        foreach (Node2D c in GetChildren()) {
            if (c is CollisionShape2D) {
                var shape = (c as CollisionShape2D).Shape;
                if (shape is RectangleShape2D) {
                    var rect = shape as RectangleShape2D;
                    lowerBounds.Add(c.GlobalPosition - rect.Extents);
                    upperBounds.Add(c.GlobalPosition + rect.Extents);

                } else {
                    GD.PrintErr($"Window {Name} has grime bounds shape of type {shape.GetType().Name}. Ignoring...");
                }
            }
        }
        GrimeManager.QueueGrime(lowerBounds.ToArray(), upperBounds.ToArray());
    }
}
