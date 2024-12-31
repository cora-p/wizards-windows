using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
#pragma warning disable CS0649, IDE0044

public class BrushController : KinematicBody2D, Manager {
	public Cleaner cleaner;

	[Export]
	float moveLerpWeight;
	[Export]
	float rotateLerpWeight;

	[Export]
	float scrubCooldown;

	float scrubCooldownRemaining;

	[Export]
	Curve scrubPositionCurve;

	[Export]
	Curve scrubAngleCurve;

	[Export]
	float scrubDuration;
	float scrubDurationRemaining;

	float distanceFromCleaner;

	Node2D manipulatableNode;
	Node2D brushHeadPositionNode;

	public Area2D hitbox;

	private static BrushController _Instance;

	public static BrushController Instance {
		get { return _Instance; }
		private set {
			if (_Instance == null) {
				_Instance = value;
			} else {
				GD.PrintErr("A BrushController has already initialized!");
			}
		}
	}


	public override void _Ready() {
		Instance = this;
		distanceFromCleaner = Position.DistanceTo(Vector2.Zero);
		manipulatableNode = GetNode<Node2D>("Manipulatable");
		brushHeadPositionNode = GetNode<Node2D>("Manipulatable/Brush/BrushHeadPosition");
		hitbox = GetNode<Area2D>("Hitbox");
		ManagerManager.Instance.ReportReady(this);
		// GrimeManager.Instance.GenerateGrime(100);
	}

	void RecurseChildren(Node node, List<Node> collection) {
		for (var i = 0; i < node.GetChildCount(); i++) {
			var c = node.GetChild(i);
			collection.Add(c);
			RecurseChildren(c, collection);
		}
	}


	public override void _PhysicsProcess(float delta) {

		if (Position.DistanceTo(GetLocalMousePosition()) < distanceFromCleaner) return;

		var parent = GetParent<Node2D>();
		var targetPosition = parent.GetLocalMousePosition().Normalized() * distanceFromCleaner;
		Position = Position.LinearInterpolate(targetPosition, moveLerpWeight);

		var currentRot = Rotation;
		LookAt(GetGlobalMousePosition());
		Rotation = Mathf.LerpAngle(currentRot, Rotation, rotateLerpWeight);

		hitbox.GlobalPosition = brushHeadPositionNode.GlobalPosition;
	}

	public override void _Process(float delta) {
		if (Input.IsActionPressed("Scrub") && scrubCooldownRemaining == 0f) {
			scrubCooldownRemaining += scrubCooldown;
			scrubDurationRemaining = scrubDuration;
		}
		if (scrubCooldownRemaining > 0f) {
			scrubCooldownRemaining -= delta;
			if (scrubCooldownRemaining < 0f) scrubCooldownRemaining = 0f;
		}
		if (scrubDurationRemaining > 0f) {
			scrubDurationRemaining -= delta;
			if (scrubDurationRemaining < 0f) scrubDurationRemaining = 0f;
			var t = 1 - scrubDurationRemaining / scrubDuration;
			manipulatableNode.RotationDegrees = manipulatableNode.Scale.y * scrubAngleCurve.Interpolate(t);
			manipulatableNode.Position = Vector2.Right * scrubPositionCurve.Interpolate(t);
		} else {
			manipulatableNode.Rotation = 0f;
			manipulatableNode.Position = Vector2.Zero;
		}
	}

	public void OnAllReady() {
		// nothing to do
	}
}
