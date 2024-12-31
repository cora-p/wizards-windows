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
		GD.Randomize();
		grimePackedScene = GD.Load<PackedScene>("res://scenes/grime/grime speck.tscn");
		noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
		noise.SetFrequency(frequency);
		noise.SetFractalType(FastNoiseLite.FractalType.FBm);
		ManagerManager.Instance.ReportReady(this);
	}

	void GenerateGrime(GrimeRequest request) {
		var c1 = GetRandomGrimeColor(Colors.Transparent);
		var c2 = GetRandomGrimeColor(c1);
		for (var x = 0; x < request.lowerBounds.Length; x++) {
			GrimePass(request.lowerBounds[x], request.upperBounds[x], c1);
			GrimePass(request.lowerBounds[x], request.upperBounds[x], c2);
		}
	}

	void GrimePass(Vector2 lowerBound, Vector2 upperBound, Color c) {
		noise.SetSeed((int)(GD.Randi() % int.MaxValue));
		for (var x = lowerBound.x; x <= upperBound.x; x++) {
			for (var y = lowerBound.y; y <= upperBound.y; y++) {
				var val = noise.GetNoise(x, y);
				if (val >= threshold) {
					var g = grimePackedScene.Instance<Node2D>();
					AddChild(g);
					g.GlobalPosition = new Vector2(x, y);
					g.Modulate = new Color(c.r, c.g, c.b, val);
				}
			}
		}
	}
	/// <summary>
	/// Queues grime grime generation within the given bounds.
	/// </summary>
	/// <param name="lowerBound">the lower bounds in global coordinates</param>
	/// <param name="upperBound">the upper bounds in global coordinates</param>
	public static void QueueGrime(Vector2[] lowerBounds, Vector2[] upperBounds) {
		grimeRequests.Add(new GrimeRequest(lowerBounds, upperBounds));
	}

	Color GetRandomGrimeColor(Color excludedColor) {
		Color color;
		do {
			color = possibleGrimeColors[GD.Randi() % possibleGrimeColors.Length];
		} while (color == excludedColor);
		return color;
	}

	public void OnClean(Area2D area2D) {
		area2D.QueueFree();
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
}
