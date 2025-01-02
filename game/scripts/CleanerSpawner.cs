using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
#pragma warning disable CS0649, IDE0044
public class CleanerSpawner : Node2D {

    enum Side {
        Left,
        Right
    }

    Node2D leftAnchorPosition, rightAnchorPosition, platformPosition;

    [Export]
    float segmentLength;

    [Export]
    Vector2 leftPlatformOffset, rightPlatformOffset;

    [Export]
    float softness;

    [Export]
    float bias;

    [Export]
    float ropeVisualWidth;
    [Export]
    PhysicsMaterial ropePhysicsMaterial;

    [Export]
    Color ropeColor;

    [Export]
    float ropeDensity;

    [Export]
    float raiseSecondsPerLink;

    [Export]
    float lowerSecondsPerLink;

    RigidBody2D platform;
    RopeAnchor leftAnchor, rightAnchor;

    Node2D leftRope, rightRope;

    List<RigidBody2D> leftSegments, rightSegments;
    List<PinJoint2D> leftJoints, rightJoints;

    [Export]
    int minimumRopeSegments;

    int sn, jn;

    float timeTilNextSegment;

    PackedScene anchorPackedScene;

    Vector2 GLeftStartPosition {
        get {
            return leftAnchor.GlobalPosition;
        }
    }

    Vector2 GLeftEndPosition {
        get {
            return platform.ToGlobal(leftPlatformOffset);
        }
    }

    Vector2 GRightStartPosition {
        get {
            return rightAnchor.GlobalPosition;
        }
    }

    Vector2 GRightEndPosition {
        get {
            return platform.ToGlobal(rightPlatformOffset);
        }
    }

    Vector2 GetSegmentPosition(Vector2 start, Vector2 end, int index) => start + start.DirectionTo(end) * index * segmentLength;

    public override void _Ready() {
        anchorPackedScene = GD.Load<PackedScene>("res://_scenes/platform/rope anchor.tscn");
        leftAnchorPosition = GetNode<Node2D>("Left Anchor Position");
        rightAnchorPosition = GetNode<Node2D>("Right Anchor Position");
        platformPosition = GetNode<Node2D>("Platform Position");

        ConstructRopedBodies();
        ConstructRope(Side.Left);
        ConstructRope(Side.Right);
    }

    public override void _Draw() {
        for (var i = 0; i < leftSegments.Count - 1; i++) {
            DrawLine(ToLocal(leftSegments[i].GlobalPosition), ToLocal(leftSegments[i + 1].GlobalPosition), ropeColor, ropeVisualWidth);
        }
        DrawLine(ToLocal(leftSegments.Last().GlobalPosition), ToLocal(GLeftEndPosition), ropeColor, ropeVisualWidth);
        for (var i = 0; i < rightSegments.Count - 1; i++) {
            DrawLine(ToLocal(rightSegments[i].GlobalPosition), ToLocal(rightSegments[i + 1].GlobalPosition), ropeColor, ropeVisualWidth);
        }
        DrawLine(ToLocal(rightSegments.Last().GlobalPosition), ToLocal(GRightEndPosition), ropeColor, ropeVisualWidth);
    }

    public override void _Process(float delta) {
        Update();
    }

    public override void _PhysicsProcess(float delta) {
        var moveAxis = Input.IsActionPressed("Raise Platform") ? -1f : 0f;
        moveAxis += Input.IsActionPressed("Lower Platform") ? 1f : 0f;
        if (timeTilNextSegment > 0) {
            timeTilNextSegment = Mathf.Clamp(timeTilNextSegment - delta, 0, 1);
        }
        if (timeTilNextSegment == 0) {
            if (moveAxis < 0) {
                ShortenRopes();
                timeTilNextSegment = raiseSecondsPerLink;
            } else if (moveAxis > 0) {
                LengthenRopes();
                timeTilNextSegment = lowerSecondsPerLink;
            }
        }
    }

    Vector2 GetTargetOffset() => leftAnchor.targetOffset;
    void SetTargetOffsets(Vector2 offset) {
        leftAnchor.targetOffset = offset;
        rightAnchor.targetOffset = offset;
    }
    void ShortenRopes() {
        if (leftSegments.Count <= minimumRopeSegments || rightSegments.Count <= minimumRopeSegments) return;
        var targetOffset = GetTargetOffset();
        leftAnchor.QueueFree();
        leftSegments[0].QueueFree();
        leftSegments.RemoveAt(0);
        leftJoints.RemoveAt(0);
        rightAnchor.QueueFree();
        rightSegments[0].QueueFree();
        rightSegments.RemoveAt(0);
        rightJoints.RemoveAt(0);
        ConstructAnchors(leftSegments[0].GlobalPosition, rightSegments[0].GlobalPosition);
        SetTargetOffsets(targetOffset);
        ConstructJoint(leftAnchor.GetPath(), leftSegments[0].GetPath(), Side.Left, leftAnchor.Position);
        ConstructJoint(rightAnchor.GetPath(), rightSegments[0].GetPath(), Side.Right, rightAnchor.Position);
    }

    void LengthenRopes() {
        var targetOffset = GetTargetOffset();
        leftAnchor.QueueFree();
        rightAnchor.QueueFree();
        var newLeftAnchorPosition = leftAnchorPosition.GlobalPosition
            + Vector2.Up * segmentLength;
        var newRightAnchorPosition = rightAnchorPosition.GlobalPosition
            + Vector2.Up * segmentLength;
        ConstructAnchors(
            newLeftAnchorPosition + targetOffset,
            newRightAnchorPosition + targetOffset);
        SetTargetOffsets(targetOffset);
        ConstructSegment(GLeftStartPosition, segmentLength, Side.Left, true);
        ConstructSegment(GRightStartPosition, segmentLength, Side.Right, true);
        leftSegments[0].LookAt(leftSegments[1].GlobalPosition);
        rightSegments[0].LookAt(rightSegments[1].GlobalPosition);
        ConstructJoint(
            leftAnchor.GetPath(),
            leftSegments[0].GetPath(),
            Side.Left,
            leftAnchor.Position);
        ConstructJoint(
            rightAnchor.GetPath(),
            rightSegments[0].GetPath(),
            Side.Right,
            rightAnchor.Position);
        ConstructJoint(
            leftSegments[0].GetPath(),
            leftSegments[1].GetPath(),
            Side.Left,
            leftSegments[1].GlobalPosition);
        ConstructJoint(
            rightSegments[0].GetPath(),
            rightSegments[1].GetPath(),
            Side.Right,
            rightSegments[1].GlobalPosition);
    }

    void ConstructAnchors(Vector2 leftPosition, Vector2 rightPosition) {
        leftAnchor = anchorPackedScene.Instance<RopeAnchor>();
        leftAnchor.GlobalPosition = leftPosition;
        rightAnchor = anchorPackedScene.Instance<RopeAnchor>();
        rightAnchor.GlobalPosition = rightPosition;
        leftAnchor.targetPosition = leftAnchorPosition.GlobalPosition;
        rightAnchor.targetPosition = rightAnchorPosition.GlobalPosition;
        AddChild(leftAnchor);
        AddChild(rightAnchor);
    }

    void ConstructRopedBodies() {
        ConstructAnchors(
            leftAnchorPosition.GlobalPosition,
            rightAnchorPosition.GlobalPosition
        );
        var platformPackedScene = GD.Load<PackedScene>("res://_scenes/platform/Platform.tscn");
        platform = platformPackedScene.Instance<RigidBody2D>();
        platform.GlobalPosition = platformPosition.GlobalPosition;
        AddChild(platform);
    }

    void ConstructRope(Side side) {
        var rope = new Node2D();
        var segments = new List<RigidBody2D>();
        var joints = new List<PinJoint2D>();
        sn = 1;
        jn = 1;
        RopeAnchor anchor;
        Vector2 startPosition, endPosition;
        if (side == Side.Left) {
            rope.Name = "Left Rope";
            rope.GlobalPosition = leftAnchor.GlobalPosition;
            leftSegments = segments;
            leftJoints = joints;
            leftRope = rope;
            startPosition = GLeftStartPosition;
            endPosition = GLeftEndPosition;
            anchor = leftAnchor;
        } else {
            rope.Name = "Right Rope";
            rope.GlobalPosition = rightAnchor.GlobalPosition;
            rightSegments = segments;
            rightJoints = joints;
            rightRope = rope;
            startPosition = GRightStartPosition;
            endPosition = GRightEndPosition;
            anchor = rightAnchor;
        }
        AddChild(rope);
        var ropeLength = startPosition.DistanceTo(endPosition);
        var distanceRemainder = ropeLength % segmentLength;
        if (distanceRemainder != 0) {
            anchor.Position -= startPosition.DirectionTo(endPosition) * distanceRemainder;
        }
        var segmentsNeeded = (int)(ropeLength / segmentLength);
        RigidBody2D segment = null;
        GD.Print(segments);
        for (var i = 0; i < segmentsNeeded; i++) {
            segment = ConstructSegment(
                            GetSegmentPosition(startPosition, endPosition, i),
                            segmentLength,
                            side
                        );
        }
        segment.LookAt(endPosition);
        for (var i = 0; i < segments.Count; i++) {
            NodePath a, b;
            Vector2? position;
            if (i == 0) {
                a = anchor.GetPath();
                position = startPosition;
            } else {
                a = segments[i - 1].GetPath();
                position = null;
            }
            b = segments[i].GetPath();
            ConstructJoint(a, b, side, position);
        }
        ConstructJoint(
            segments.Last().GetPath(),
            platform.GetPath(),
            side,
            endPosition);
    }

    PinJoint2D ConstructJoint(NodePath a, NodePath b, Side side, Vector2? globalPosition, bool insertAtStart = false) {
        var joint = new PinJoint2D();
        List<PinJoint2D> joints;
        if (side == Side.Left) {
            joint.Name = $"j-L-{jn++}";
            joints = leftJoints;
        } else {
            joint.Name = $"j-R-{jn++}";
            joints = rightJoints;
        }
        joint.Bias = bias;
        joint.Softness = softness;
        joint.NodeA = a;
        GetNode(a).AddChild(joint);
        if (globalPosition == null) {
            joint.Position = Vector2.Right * segmentLength;
        } else {
            joint.GlobalPosition = (Vector2)globalPosition;
        }
        if (insertAtStart) {
            joints.Insert(0, joint);
        } else {
            joints.Add(joint);
        }
        joint.NodeB = b;
        return joint;
    }

    RigidBody2D ConstructSegment(Vector2 gPosition, float length, Side side, bool insertAtStart = false) {
        var segment = new RigidBody2D();
        Vector2 lookPosition;
        List<RigidBody2D> segments;
        if (side == Side.Left) {
            segment.Name = $"sL{sn++}";
            leftRope.AddChild(segment);
            lookPosition = GLeftEndPosition;
            segments = leftSegments;
        } else {
            segment.Name = $"sR{sn++}";
            rightRope.AddChild(segment);
            lookPosition = GRightEndPosition;
            segments = rightSegments;
        }
        ConstructCollider(segment, 0.5f, length);
        segment.Mass = ropeDensity * length;
        segment.GlobalPosition = gPosition;
        segment.PhysicsMaterialOverride = ropePhysicsMaterial;
        segment.CollisionLayer = Layer.Rope;
        segment.CollisionMask = Layer.None;
        segment.ZIndex = ZIndices.Rope;
        segment.LookAt(lookPosition);
        if (insertAtStart) {
            segments.Insert(0, segment);
        } else {
            segments.Add(segment);
        }
        return segment;
    }

    void ConstructCollider(Node segment, float width, float length) {
        var collider = new CollisionShape2D();
        var shape = new CapsuleShape2D {
            Radius = width,
            Height = length
        };
        collider.Shape = shape;
        segment.AddChild(collider);

        collider.Position = Vector2.Right * length / 2;
        collider.Rotate(Mathf.Pi / 2f);
    }

}
