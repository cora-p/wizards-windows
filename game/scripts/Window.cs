using System;
using Godot;

public class Window : Node2D {

	[Export]
	float griminess;

	public override void _Ready() {
		foreach (Node2D c in GetChildren()) {
			if (c is CollisionShape2D) {
				var shape = (c as CollisionShape2D).Shape;
				if (shape is RectangleShape2D) {
					var rect = shape as RectangleShape2D;
					var lowerBound = c.GlobalPosition - rect.Extents;
					var upperBound = c.GlobalPosition + rect.Extents;
					var vol = rect.Extents * 2f;
					var area = (int)(vol.x * vol.y);
					var grimePoints = griminess / 100f * area;
					GrimeManager.QueueGrime((int)grimePoints, lowerBound, upperBound);
				} else {
					GD.PrintErr($"Window {Name} has grime bounds shape of type {shape.GetType().Name}. Ignoring...");
				}
			}
		}
	}
}
