using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
#pragma warning disable CS0649, IDE0044
public class RopeController : Node2D, Manager {

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
    float ropeVisualWidth, ropePhysicalWidth;
    [Export]
    PhysicsMaterial ropePhysicsMaterial;

    [Export]
    Color ropeColor;

    [Export]
    float ropeDensity;

    [Export]
    float raiseSpeed, lowerSpeed;

    [Export]
    float minimumSlack;

    RigidBody2D platform;

    Dictionary<Side, RopeAnchor> anchors;

    Node2D leftRope, rightRope;

    Dictionary<Side, List<RigidBody2D>> segments;
    Dictionary<Side, List<PinJoint2D>> joints;

    [Export]
    float mainMenuYOffset;

    [Export]
    int minimumRopeSegments;

    int sn, jn;

    float timeTilNextSegment;

    PackedScene anchorPackedScene;

    AudioStreamPlayer raiseSfx, lowerSfx, horizontalSfx;

    Vector2 GetStartPosition(Side s) => anchors[s].GlobalPosition;
    Vector2 GetEndPosition(Side s) => s == L ? platform.ToGlobal(leftPlatformOffset) : platform.ToGlobal(rightPlatformOffset);
    Vector2 GetSegmentPosition(Vector2 start, Vector2 end, int index) => start + start.DirectionTo(end) * index * segmentLength;
    float GetAnchorDistance(Side s) => GetStartPosition(s).DistanceTo(GetEndPosition(s));
    float GetActualRopeLength(Side s) => segments[s].Count * segmentLength;
    float GetSlackFactor(Side s) => GetActualRopeLength(s) / GetAnchorDistance(s);
    bool CanShorten { get => segments[L].Count > minimumRopeSegments && GetSlackFactor(L) > minimumSlack && GetSlackFactor(R) > minimumSlack; }
    public override void _Ready() {
        anchorPackedScene = GD.Load<PackedScene>("res://_scenes/platform/rope anchor.tscn");
        leftAnchorPosition = GetNode<Node2D>("Left Anchor Position");
        rightAnchorPosition = GetNode<Node2D>("Right Anchor Position");
        platformPosition = GetNode<Node2D>("Platform Position");
        raiseSfx = GetNode<AudioStreamPlayer>("raise sfx");
        lowerSfx = GetNode<AudioStreamPlayer>("lower sfx");
        horizontalSfx = GetNode<AudioStreamPlayer>("horizontal sfx");

        segments = new Dictionary<Side, List<RigidBody2D>> {
            [L] = new List<RigidBody2D>(),
            [R] = new List<RigidBody2D>()
        };
        joints = new Dictionary<Side, List<PinJoint2D>> {
            [L] = new List<PinJoint2D>(),
            [R] = new List<PinJoint2D>()
        };
        anchors = new Dictionary<Side, RopeAnchor>();

        if (LevelManager.Instance.IsOnMainMenu) {
            var o = new Vector2(0, mainMenuYOffset);
            leftAnchorPosition.GlobalPosition += o;
            rightAnchorPosition.GlobalPosition += o;
            platformPosition.GlobalPosition += o;
        }

        ConstructRopedBodies();
        ConstructRope(L);
        ConstructRope(R);
        Overseer.Instance.ReportReady(this);
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
        if (anchors[L].IsTravelling) {
            if (!horizontalSfx.Playing) horizontalSfx.Play();
        } else {
            if (horizontalSfx.Playing) horizontalSfx.Stop();
        }
        var isRaisePressed = Input.IsActionPressed("Raise Platform");
        var isLowerPressed = Input.IsActionPressed("Lower Platform");
        var moveAxis = isLowerPressed ? 1 : 0;
        moveAxis += isRaisePressed ? -1 : 0;
        if (timeTilNextSegment > 0) {
            timeTilNextSegment = Mathf.Clamp(timeTilNextSegment - delta, 0, 1);
        }
        if (moveAxis < 0 && CanShorten) {
            if (!raiseSfx.Playing) raiseSfx.Play();
        } else {
            if (raiseSfx.Playing) raiseSfx.Stop();
        }
        if (moveAxis > 0) { //TODO CanLengthen
            if (!lowerSfx.Playing) lowerSfx.Play();
        } else {
            if (lowerSfx.Playing) lowerSfx.Stop();
        }
        if (timeTilNextSegment == 0) {
            if (moveAxis < 0) {
                ShortenRopes();
                timeTilNextSegment = 1f / (raiseSpeed / segmentLength);
            } else if (moveAxis > 0) {
                LengthenRopes();
                timeTilNextSegment = 1f / (lowerSpeed / segmentLength);
            }
        }
    }

    void ShortenRopes() {
        if (!CanShorten) return;
        segments[L][0].QueueFree();
        segments[L].RemoveAt(0);
        segments[R][0].QueueFree();
        segments[R].RemoveAt(0);
        joints[L].RemoveAt(0);
        joints[R].RemoveAt(0);
        ReconstructAnchors(
            segments[L][0].GlobalPosition,
            segments[R][0].GlobalPosition);
        ConstructJoint(anchors[L].GetPath(), segments[L][0].GetPath(), Side.Left, anchors[L].Position);
        ConstructJoint(anchors[R].GetPath(), segments[R][0].GetPath(), Side.Right, anchors[R].Position);
    }

    void LengthenRopes() {
        var newLeftPos = anchors[L].GlobalPosition
            + Vector2.Up * segmentLength;
        var newRightPos = anchors[R].GlobalPosition
            + Vector2.Up * segmentLength;
        ReconstructAnchors(
            newLeftPos,
            newRightPos);
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
        anchors[L].targetPosition = leftPosition;
        anchors[R] = anchorPackedScene.Instance<RopeAnchor>();
        anchors[R].GlobalPosition = rightPosition;
        anchors[R].targetPosition = rightPosition;
        AddChild(anchors[L]);
        AddChild(anchors[R]);
    }

    void ReconstructAnchors(Vector2 leftPos, Vector2 rightPos) {
        var oldLeft = anchors[L];
        var oldRight = anchors[R];
        ConstructAnchors(leftPos, rightPos);
        anchors[L].targetOffset = oldLeft.targetOffset;
        anchors[L].targetPosition = oldLeft.targetPosition;
        anchors[L].travel = oldLeft.travel;
        anchors[R].targetOffset = oldRight.targetOffset;
        anchors[R].targetPosition = oldRight.targetPosition;
        anchors[R].travel = oldRight.travel;
        oldLeft.QueueFree();
        oldRight.QueueFree();
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
        ConstructCollider(segment, length);
        segment.Mass = ropeDensity * length;
        segment.GlobalPosition = gPosition;
        segment.PhysicsMaterialOverride = ropePhysicsMaterial;
        segment.CollisionLayer = Layer.Rope;
        segment.CollisionMask = Layer.Deep;
        segment.ZIndex = ZIndices.Rope;
        segment.ContinuousCd = RigidBody2D.CCDMode.CastShape;
        segment.LookAt(lookPosition);
        if (insertAtStart) {
            segments[side].Insert(0, segment);
        } else {
            segments[side].Add(segment);
        }
        return segment;
    }

    void ConstructCollider(Node segment, float length) {
        var collider = new CollisionShape2D();
        var shape = new CapsuleShape2D {
            Radius = ropePhysicalWidth,
            Height = length
        };
        collider.Shape = shape;
        segment.AddChild(collider);

        collider.Position = Vector2.Right * length / 2;
        collider.Rotate(Mathf.Pi / 2f);
    }

    public void OnAllReady() {
        // nothing to do
    }
    public PackedScene GetPackedScene() => GD.Load<PackedScene>("res://_scenes/managers/RopeController.tscn");
    public bool Reset() {
        QueueFree();
        return true;
    }
}
