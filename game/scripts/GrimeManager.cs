using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class GrimeManager : Node, Manager {
	static PackedScene grimePackedScene;

	[Export]
	Color[] possibleGrimeColors;

	static List<GrimeRequest> grimeRequests = new List<GrimeRequest>();

	private static GrimeManager _Instance;

	public static GrimeManager Instance {
		get {
			return _Instance;
		}
		private set {
			if (_Instance == null) {
				_Instance = value;
			} else {
				GD.PrintErr("A GrimeManager has already initialized!");
			}
		}
	}
	public override void _Ready() {
		grimePackedScene = GD.Load<PackedScene>("res://scenes/grime/grime speck.tscn");
		ManagerManager.Instance.ReportReady(this);
	}

	void GenerateGrime(GrimeRequest request) {
		for (var i = 0; i < request.count; i++) {
			var g = grimePackedScene.Instance<Node2D>();
			AddChild(g);
			g.GlobalPosition = new Vector2(
				(float)GD.RandRange(request.lowerBound.x, request.upperBound.x),
				(float)GD.RandRange(request.lowerBound.y, request.upperBound.y));

			g.Modulate = getRandomGrimeColor();
		}
	}
	/// <summary>
	/// Queues grime grime generation within the given bounds.
	/// </summary>
	/// <param name="count">Number of grime dots to generate</param>
	/// <param name="lowerBound">the lower bounds in global coordinates</param>
	/// <param name="upperBound">the upper bounds in global coordinates</param>
	public static void QueueGrime(int count, Vector2 lowerBound, Vector2 upperBound) {
		grimeRequests.Add(new GrimeRequest(count, lowerBound, upperBound));
	}

	Color getRandomGrimeColor() => possibleGrimeColors[GD.Randi() % possibleGrimeColors.Length];
	public void OnClean(Area2D area2D) {
		area2D.QueueFree();
	}

	public void OnAllReady() {
		foreach (var b in BrushController.Instance.brushHitboxes) {
			b.Connect("area_entered", this, "OnClean");
		}
		// BrushController.Instance.hitbox.Connect("area_entered", this, "OnClean");
		for (var i = 0; i < grimeRequests.Count; i++) {
			var gr = grimeRequests[i];
			GenerateGrime(gr);
		}
	}

	struct GrimeRequest {
		public int count;
		public Vector2 lowerBound;
		public Vector2 upperBound;

		public GrimeRequest(int count, Vector2 lowerBound, Vector2 upperBound) {
			this.count = count;
			this.lowerBound = lowerBound;
			this.upperBound = upperBound;
		}
	}
}
