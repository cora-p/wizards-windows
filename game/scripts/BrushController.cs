using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
#pragma warning disable CS0649, IDE0044

public class BrushController : KinematicBody2D, Manager {
    public Cleaner cleaner;

    [Export]
    public float scrubbingPower;

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
    Node2D headPositionNode;

    [Export]
    Color guideColor;

    [Export]
    float guideDashLength, guideGapLength;

    Node2D head;

    AudioStreamPlayer sfx;
    public List<Area2D> brushHitboxes;

    public static BrushController Instance { get; private set; }
    float T { get => 1 - scrubDurationRemaining / scrubDuration; }

    public override void _Ready() {
        Instance = this;
        distanceFromCleaner = Position.DistanceTo(Vector2.Zero);
        manipulatableNode = GetNode<Node2D>("Manipulatable");
        headPositionNode = GetNode<Node2D>("Manipulatable/Brush/BrushHeadPosition");
        sfx = GetNode<AudioStreamPlayer>("sfx");
        head = GetNode<Node2D>("Head");

        var hitboxKids = new List<Node>();
        RecurseChildren(head, hitboxKids);
        brushHitboxes = (from k in hitboxKids
                         where k is Area2D
                         select k as Area2D)
                           .ToList();

        Overseer.Instance.ReportReady(this);
    }

    // public override void _Draw() {
    //     Utility.DrawDashedRay(
    //         this,
    //         ToLocal(headPositionNode.GlobalPosition),
    //         Vector2.Right,
    //         1000f,
    //         guideColor,
    //         guideDashLength,
    //         guideGapLength,
    //         1.1f);
    // }
    void RecurseChildren(Node node, List<Node> collection) {
        for (var i = 0; i < node.GetChildCount(); i++) {
            var c = node.GetChild(i);
            collection.Add(c);
            RecurseChildren(c, collection);
        }
    }

    public override void _PhysicsProcess(float delta) {



        var parent = GetParent<Node2D>();
        var targetPosition = parent.GetLocalMousePosition().Normalized() * distanceFromCleaner;
        Position = Position.LinearInterpolate(targetPosition, moveLerpWeight);

        // only look at the mouse if we are far enough away
        if (Position.DistanceTo(GetLocalMousePosition()) > distanceFromCleaner + scrubPositionCurve.Interpolate(T)) {
            var currentRot = Rotation;
            LookAt(GetGlobalMousePosition());
            Rotation = Mathf.LerpAngle(currentRot, Rotation, rotateLerpWeight);
        }

        head.GlobalPosition = headPositionNode.GlobalPosition;
        head.ResetPhysicsInterpolation();
    }

    public override void _Process(float delta) {
        // Update();
        if (Input.IsActionPressed("Scrub") && scrubCooldownRemaining == 0f) {
            scrubCooldownRemaining += scrubCooldown;
            scrubDurationRemaining = scrubDuration;
            sfx.Play();
        }
        if (scrubDurationRemaining == 0f && scrubCooldownRemaining > 0f) {
            scrubCooldownRemaining -= delta;
            if (scrubCooldownRemaining < 0f) scrubCooldownRemaining = 0f;
        }
        if (scrubDurationRemaining > 0f) {
            scrubDurationRemaining -= delta;
            if (scrubDurationRemaining < 0f) scrubDurationRemaining = 0f;
            manipulatableNode.RotationDegrees = manipulatableNode.Scale.y * scrubAngleCurve.Interpolate(T);
            manipulatableNode.Position = Vector2.Right * scrubPositionCurve.Interpolate(T);
        } else {
            manipulatableNode.Rotation = 0f;
            manipulatableNode.Position = Vector2.Zero;
        }
    }

    public void OnAllReady() {
        // nothing to do
    }

    public bool Reset() {
        Instance = null;
        QueueFree();
        return true;
    }
    public PackedScene GetPackedScene() => null;
}
