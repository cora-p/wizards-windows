using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class CleanerSpawner : Node2D {

    enum Side {
        Left,
        Right
    }

    [Export]
    NodePath LeftAnchorPosition, RightAnchorPosition, PlatformPosition;

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

    RigidBody2D platform, leftAnchor, rightAnchor;

    Node2D leftRope, rightRope;

    List<RigidBody2D> leftSegments, rightSegments;
    List<PinJoint2D> leftJoints, rightJoints;

    int sn, jn;

    float leftFirstSegmentLength;
    float rightFirstSegmentLength;

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

    Vector2 LeftRopeDirection {
        get {
            return (GLeftEndPosition - GLeftStartPosition).Normalized();
        }
    }

    Vector2 RightRopeDirection {
        get {
            return (GRightEndPosition - GRightStartPosition).Normalized();
        }
    }

    Vector2 GetSegmentGPositionOfSegment(Side side, int index) {
        if (side == Side.Left) {
            return GLeftStartPosition + LeftRopeDirection * (leftFirstSegmentLength + index * segmentLength);
        } else {
            return GRightStartPosition + RightRopeDirection * (rightFirstSegmentLength + index * segmentLength);
        }
    }

    public override void _Ready() {
        anchorPackedScene = GD.Load<PackedScene>("res://scenes/platform/rope anchor.tscn");
        ConstructRopedBodies();
        ConstructRopes();
    }

    public override void _Draw() {
        // DrawCircle(ToLocal(GLeftStartPosition), 1, Colors.Red);
        // DrawCircle(ToLocal(GRightStartPosition), 1, Colors.DarkRed);
        // DrawCircle(ToLocal(GLeftEndPosition), 1, Colors.Blue);
        // DrawCircle(ToLocal(GRightEndPosition), 1, Colors.DarkBlue);
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

    void ShortenRopes() {
        leftAnchor.QueueFree();
        leftSegments[0].QueueFree();
        leftSegments.RemoveAt(0);
        leftJoints.RemoveAt(0);
        rightAnchor.QueueFree();
        rightSegments[0].QueueFree();
        rightSegments.RemoveAt(0);
        rightJoints.RemoveAt(0);
        ConstructAnchors(leftSegments[0].GlobalPosition, rightSegments[0].GlobalPosition);
        ConstructJoint(leftAnchor.GetPath(), leftSegments[0].GetPath(), Side.Left, leftAnchor.Position);
        ConstructJoint(rightAnchor.GetPath(), rightSegments[0].GetPath(), Side.Right, rightAnchor.Position);
    }

    void LengthenRopes() {
        leftAnchor.QueueFree();
        rightAnchor.QueueFree();
        var newLeftAnchorPosition = ((Node2D)GetNode(LeftAnchorPosition)).GlobalPosition
            + Vector2.Up * segmentLength;
        var newRightAnchorPosition = ((Node2D)GetNode(RightAnchorPosition)).GlobalPosition
            + Vector2.Up * segmentLength;
        ConstructAnchors(
            newLeftAnchorPosition,
            newRightAnchorPosition);
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
        leftAnchor = anchorPackedScene.Instance<RigidBody2D>();
        leftAnchor.GlobalPosition = leftPosition;//((Node2D)GetNode(LeftAnchorPosition)).GlobalPosition;
        rightAnchor = anchorPackedScene.Instance<RigidBody2D>();
        rightAnchor.GlobalPosition = rightPosition;//((Node2D)GetNode(RightAnchorPosition)).GlobalPosition;
        (leftAnchor as RopeAnchor).targetPosition = ((Node2D)GetNode(LeftAnchorPosition)).GlobalPosition;
        (rightAnchor as RopeAnchor).targetPosition = ((Node2D)GetNode(RightAnchorPosition)).GlobalPosition;
        AddChild(leftAnchor);
        AddChild(rightAnchor);
    }

    void ConstructRopedBodies() {
        ConstructAnchors(
            ((Node2D)GetNode(LeftAnchorPosition)).GlobalPosition,
            ((Node2D)GetNode(RightAnchorPosition)).GlobalPosition
        );
        var platformPackedScene = GD.Load<PackedScene>("res://scenes/platform/Platform.tscn");
        platform = platformPackedScene.Instance<RigidBody2D>();
        platform.GlobalPosition = ((Node2D)GetNode(PlatformPosition)).GlobalPosition;
        AddChild(platform);
    }

    void ConstructRopes() {
        // Construct the left-side rope
        leftRope = new Node2D() {
            Name = "Left Rope",
            GlobalPosition = leftAnchor.GlobalPosition
        };

        AddChild(leftRope);
        leftSegments = new List<RigidBody2D>();
        leftJoints = new List<PinJoint2D>();

        var leftRopeLength = GLeftStartPosition.DistanceTo(GLeftEndPosition);
        leftFirstSegmentLength = leftRopeLength % segmentLength;
        var leftSegmentsNeeded = (int)(leftRopeLength / segmentLength);
        if (leftFirstSegmentLength == 0) {
            leftFirstSegmentLength = segmentLength;
            leftSegmentsNeeded++;
        }
        sn = 1;
        ConstructSegment(GLeftStartPosition, leftFirstSegmentLength, Side.Left);
        RigidBody2D segment = null;
        for (var i = 0; i < leftSegmentsNeeded; i++) {
            segment = ConstructSegment(
                            GetSegmentGPositionOfSegment(Side.Left, i),
                            segmentLength,
                            Side.Left
                        );
        }
        segment.LookAt(GLeftEndPosition);
        for (var i = 0; i < leftSegments.Count; i++) {
            NodePath a, b;
            Vector2? position = null;
            switch (i) {
                case 0:
                    a = leftAnchor.GetPath();
                    position = GLeftStartPosition;
                    break;
                case 1:
                    a = leftSegments[i - 1].GetPath();
                    position = GLeftStartPosition + LeftRopeDirection * leftFirstSegmentLength;
                    break;
                default:
                    a = leftSegments[i - 1].GetPath();
                    break;
            }
            b = leftSegments[i].GetPath();
            ConstructJoint(a, b, Side.Left, position);
        }
        ConstructJoint(
            leftSegments.Last().GetPath(),
            platform.GetPath(),
            Side.Left,
            GLeftEndPosition);

        // Now construct the right-side rope
        rightRope = new Node2D() {
            Name = "Right Rope"
        };
        AddChild(rightRope);
        rightSegments = new List<RigidBody2D>();
        rightJoints = new List<PinJoint2D>();

        var rightRopeLength = GRightStartPosition.DistanceTo(GRightEndPosition);
        rightFirstSegmentLength = rightRopeLength % segmentLength;
        var rightSegmentsNeeded = (int)(rightRopeLength / segmentLength);
        if (rightFirstSegmentLength == 0) {
            rightFirstSegmentLength = segmentLength;
            rightSegmentsNeeded++;
        }
        sn = 1;
        ConstructSegment(GRightStartPosition, rightFirstSegmentLength, Side.Right);
        segment = null;
        for (var i = 0; i < rightSegmentsNeeded; i++) {
            segment = ConstructSegment(
                            GetSegmentGPositionOfSegment(Side.Right, i),
                            segmentLength,
                            Side.Right
                        );
        }
        segment.LookAt(GRightEndPosition);
        for (var i = 0; i < rightSegments.Count; i++) {
            NodePath a, b;
            Vector2? position = null;
            switch (i) {
                case 0:
                    a = rightAnchor.GetPath();
                    position = GRightStartPosition;
                    break;
                case 1:
                    a = rightSegments[i - 1].GetPath();
                    position = GRightStartPosition + RightRopeDirection * rightFirstSegmentLength;
                    break;
                default:
                    a = rightSegments[i - 1].GetPath();
                    break;
            }
            b = rightSegments[i].GetPath();
            ConstructJoint(a, b, Side.Right, position);
        }
        ConstructJoint(
            rightSegments.Last().GetPath(),
            platform.GetPath(),
            Side.Right,
            GRightEndPosition);
    }

    PinJoint2D ConstructJoint(NodePath a, NodePath b, Side side, Vector2? globalPosition, bool insertAtStart = false) {
        if (side == Side.Left) {
            var joint = new PinJoint2D() {
                Name = $"j-L-{jn++}",
                Bias = bias,
                Softness = softness,
                NodeA = a
            };
            GetNode(a).AddChild(joint);
            if (globalPosition == null) {
                joint.Position = Vector2.Right * segmentLength;
            } else {
                joint.GlobalPosition = (Vector2)globalPosition;
            }
            if (insertAtStart) {
                leftJoints.Insert(0, joint);
            } else {
                leftJoints.Add(joint);
            }
            joint.NodeB = b;
            return joint;
        } else {
            var joint = new PinJoint2D() {
                Name = $"j-R-{jn++}",
                Bias = bias,
                Softness = softness,
                NodeA = a
            };
            GetNode(a).AddChild(joint);
            if (globalPosition == null) {
                joint.Position = Vector2.Right * segmentLength;
            } else {
                joint.GlobalPosition = (Vector2)globalPosition;
            }
            if (insertAtStart) {
                rightJoints.Insert(0, joint);
            } else {
                rightJoints.Add(joint);
            }
            joint.NodeB = b;
            return joint;
        }
    }

    RigidBody2D ConstructSegment(Vector2 gPosition, float length, Side side, bool insertAtStart = false) {
        if (side == Side.Left) {
            var segment = new RigidBody2D() {
                Name = $"s-L-{sn++}",
                Mass = ropeDensity * length,
                CollisionLayer = 4
            };
            leftRope.AddChild(segment);
            segment.GlobalPosition = gPosition;
            segment.LookAt(GLeftEndPosition);
            segment.PhysicsMaterialOverride = ropePhysicsMaterial;
            ConstructCollider(segment, 0.5f, length);
            if (insertAtStart) {
                leftSegments.Insert(0, segment);
            } else {
                leftSegments.Add(segment);
            }
            return segment;
        } else {
            var segment = new RigidBody2D() {
                Name = $"s-R-{sn++}",
                Mass = ropeDensity * length,
                CollisionLayer = 4
            };
            rightRope.AddChild(segment);
            segment.GlobalPosition = gPosition;
            segment.LookAt(GRightEndPosition);
            ConstructCollider(segment, 0.5f, length);
            if (insertAtStart) {
                rightSegments.Insert(0, segment);
            } else {
                rightSegments.Add(segment);
            }
            return segment;
        }
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
