using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
#pragma warning disable CS0649, IDE0044

public class BrushController : KinematicBody2D {
	// [Export]
	// float distanceFromCleaner;

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

	float scrubProgress;

	float distanceFromCleaner;

	Node2D manipulatableNode;
	Node2D brushHeadPositionNode;
	Node2D brushHeadColliderNode;
	
	RigidBody2D[] scrubHeadBodies;

	public override void _Ready() {
		distanceFromCleaner = Position.DistanceTo(Vector2.Zero);
		manipulatableNode = GetNode<Node2D>("Manipulatable");
		brushHeadPositionNode = GetNode<Node2D>("Manipulatable/Brush/BrushHeadPosition");
		brushHeadColliderNode = GetNode<Node2D>("CollisionShape2D");

		var children = new List<Node>();
		RecurseChildren(this, children);
		scrubHeadBodies = (
			from c in children
			where c is RigidBody2D
			select c as RigidBody2D
			).ToArray();
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
		// Position = targetPosition.Rotated(Rotation);
		Position = Position.LinearInterpolate(targetPosition, moveLerpWeight);

		var currentRot = Rotation;
		LookAt(GetGlobalMousePosition());
		Rotation = Mathf.LerpAngle(currentRot, Rotation, rotateLerpWeight);

		if (Position.x < 0) {
			manipulatableNode.Scale = new Vector2(1, -1);
		} else {
			manipulatableNode.Scale = new Vector2(1, 1);
		}

		// var scrubHeadOffset = brushHeadPositionNode.GlobalPosition - brushHeadColliderNode.GlobalPosition;
		brushHeadColliderNode.GlobalPosition = brushHeadPositionNode.GlobalPosition;
		
		// for (var i = 0; i < scrubHeadBodies.Length; i++) {
		// 	scrubHeadBodies[i].GlobalPosition += scrubHeadOffset;
		// }
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
}
