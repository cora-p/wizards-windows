using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
#pragma warning disable CS0649, IDE0044
public class RopeController : Node2D {

    enum Side {
        Left,
        Right
    }

    const Side L = Side.Left;
    const Side R = Side.Right;

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

    [Export]
    float minimumSlack;

    RigidBody2D platform;

    Dictionary<Side, RopeAnchor> anchors;

    Node2D leftRope, rightRope;

    Dictionary<Side, List<RigidBody2D>> segments;
    Dictionary<Side, List<PinJoint2D>> joints;

    [Export]
    int minimumRopeSegments;

    int sn, jn;

    float timeTilNextSegment;

    PackedScene anchorPackedScene;

    Vector2 GetStartPosition(Side s) => anchors[s].GlobalPosition;
    Vector2 GetEndPosition(Side s) => s == L ? platform.ToGlobal(leftPlatformOffset) : platform.ToGlobal(rightPlatformOffset);
    Vector2 GetSegmentPosition(Vector2 start, Vector2 end, int index) => start + start.DirectionTo(end) * index * segmentLength;
    float GetAnchorDistance(Side s) => GetStartPosition(s).DistanceTo(GetEndPosition(s));
    float GetActualRopeLength(Side s) => segments[s].Count * segmentLength;
    float GetSlackFactor(Side s) => GetActualRopeLength(s) / GetAnchorDistance(s);

    public override void _Ready() {
        anchorPackedScene = GD.Load<PackedScene>("res://_scenes/platform/rope anchor.tscn");
        leftAnchorPosition = GetNode<Node2D>("Left Anchor Position");
        rightAnchorPosition = GetNode<Node2D>("Right Anchor Position");
        platformPosition = GetNode<Node2D>("Platform Position");

        segments = new Dictionary<Side, List<RigidBody2D>>();
        segments[L] = new List<RigidBody2D>();
        segments[R] = new List<RigidBody2D>();
        joints = new Dictionary<Side, List<PinJoint2D>>();
        joints[L] = new List<PinJoint2D>();
        joints[R] = new List<PinJoint2D>();
        anchors = new Dictionary<Side, RopeAnchor>();

        ConstructRopedBodies();
        ConstructRope(L);
        ConstructRope(R);
    }

    public override void _Draw() {
        foreach (Side s in Enum.GetValues(typeof(Side))) {
            for (var i = 0; i < segments[s].Count - 1; i++) {
                DrawLine(ToLocal(segments[s][i].GlobalPosition), ToLocal(segments[s][i + 1].GlobalPosition), ropeColor, ropeVisualWidth);
                DrawLine(ToLocal(segments[s].Last().GlobalPosition), ToLocal(GetEndPosition(s)), ropeColor, ropeVisualWidth);
            }
        }
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

    Vector2 GetTargetOffset() => anchors[L].targetOffset;
    void SetTargetOffsets(Vector2 offset) {
        anchors[L].targetOffset = offset;
        anchors[R].targetOffset = offset;
    }



    void ShortenRopes() {
        if (segments[L].Count <= minimumRopeSegments || segments[R].Count <= minimumRopeSegments) return;
        if (GetSlackFactor(L) < minimumSlack || GetSlackFactor(R) < minimumSlack) { GD.Print($"Ropes too short already! L:{GetSlackFactor(L)}, R:{GetSlackFactor(R)}"); return; };
        var targetOffset = GetTargetOffset();
        anchors[L].QueueFree();
        segments[L][0].QueueFree();
        segments[L].RemoveAt(0);
        joints[L].RemoveAt(0);
        anchors[R].QueueFree();
        segments[R][0].QueueFree();
        segments[R].RemoveAt(0);
        joints[R].RemoveAt(0);
        ConstructAnchors(segments[L][0].GlobalPosition, segments[R][0].GlobalPosition);
        SetTargetOffsets(targetOffset);
        ConstructJoint(anchors[L].GetPath(), segments[L][0].GetPath(), Side.Left, anchors[L].Position);
        ConstructJoint(anchors[R].GetPath(), segments[R][0].GetPath(), Side.Right, anchors[R].Position);
    }

    void LengthenRopes() {
        var targetOffset = GetTargetOffset();
        anchors[L].QueueFree();
        anchors[R].QueueFree();
        var newLeftAnchorPosition = leftAnchorPosition.GlobalPosition
            + Vector2.Up * segmentLength;
        var newRightAnchorPosition = rightAnchorPosition.GlobalPosition
            + Vector2.Up * segmentLength;
        ConstructAnchors(
            newLeftAnchorPosition + targetOffset,
            newRightAnchorPosition + targetOffset);
        SetTargetOffsets(targetOffset);
        ConstructSegment(GetStartPosition(Side.Left), segmentLength, Side.Left, true);
        ConstructSegment(GetStartPosition(Side.Right), segmentLength, Side.Right, true);
        segments[L][0].LookAt(segments[L][1].GlobalPosition);
        segments[R][0].LookAt(segments[R][1].GlobalPosition);
        ConstructJoint(
            anchors[L].GetPath(),
            segments[L][0].GetPath(),
            Side.Left,
            anchors[L].Position);
        ConstructJoint(
            anchors[R].GetPath(),
            segments[R][0].GetPath(),
            Side.Right,
            anchors[R].Position);
        ConstructJoint(
            segments[L][0].GetPath(),
            segments[L][1].GetPath(),
            Side.Left,
            segments[L][1].GlobalPosition);
        ConstructJoint(
            segments[R][0].GetPath(),
            segments[R][1].GetPath(),
            Side.Right,
            segments[R][1].GlobalPosition);
    }

    void ConstructAnchors(Vector2 leftPosition, Vector2 rightPosition) {
        anchors[L] = anchorPackedScene.Instance<RopeAnchor>();
        anchors[L].GlobalPosition = leftPosition;
        anchors[R] = anchorPackedScene.Instance<RopeAnchor>();
        anchors[R].GlobalPosition = rightPosition;
        anchors[L].targetPosition = leftAnchorPosition.GlobalPosition;
        anchors[R].targetPosition = rightAnchorPosition.GlobalPosition;
        AddChild(anchors[L]);
        AddChild(anchors[R]);
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
        sn = 1;
        jn = 1;
        Vector2 startPosition = GetStartPosition(side);
        Vector2 endPosition = GetEndPosition(side);
        if (side == Side.Left) {
            rope.Name = "Left Rope";
            rope.GlobalPosition = anchors[L].GlobalPosition;
            leftRope = rope;
        } else {
            rope.Name = "Right Rope";
            rope.GlobalPosition = anchors[L].GlobalPosition;
            rightRope = rope;
        }
        AddChild(rope);
        var ropeLength = startPosition.DistanceTo(endPosition);
        var distanceRemainder = ropeLength % segmentLength;
        if (distanceRemainder != 0) {
            anchors[L].Position -= startPosition.DirectionTo(endPosition) * distanceRemainder;
        }
        var segmentsNeeded = (int)(ropeLength / segmentLength);
        RigidBody2D segment = null;
        for (var i = 0; i < segmentsNeeded; i++) {
            segment = ConstructSegment(
                            GetSegmentPosition(startPosition, endPosition, i),
                            segmentLength,
                            side
                        );
        }
        segment.LookAt(endPosition);
        for (var i = 0; i < segments[side].Count; i++) {
            NodePath a, b;
            Vector2? position;
            if (i == 0) {
                a = anchors[L].GetPath();
                position = startPosition;
            } else {
                a = segments[side][i - 1].GetPath();
                position = null;
            }
            b = segments[side][i].GetPath();
            ConstructJoint(a, b, side, position);
        }
        ConstructJoint(
            segments[side].Last().GetPath(),
            platform.GetPath(),
            side,
            endPosition);
    }

    PinJoint2D ConstructJoint(NodePath a, NodePath b, Side side, Vector2? globalPosition, bool insertAtStart = false) {
        var joint = new PinJoint2D();
        if (side == Side.Left) {
            joint.Name = $"j-L-{jn++}";
        } else {
            joint.Name = $"j-R-{jn++}";
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
            joints[side].Insert(0, joint);
        } else {
            joints[side].Add(joint);
        }
        joint.NodeB = b;
        return joint;
    }

    RigidBody2D ConstructSegment(Vector2 gPosition, float length, Side side, bool insertAtStart = false) {
        var segment = new RigidBody2D();
        Vector2 lookPosition = GetEndPosition(side);
        if (side == Side.Left) {
            segment.Name = $"sL{sn++}";
            leftRope.AddChild(segment);
        } else {
            segment.Name = $"sR{sn++}";
            rightRope.AddChild(segment);
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
            segments[side].Insert(0, segment);
        } else {
            segments[side].Add(segment);
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
