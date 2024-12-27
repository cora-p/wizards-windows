using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class Rope : Node2D {
    [Export]
    float segmentLength;
    float firstSegmentLength;
    [Export]
    float ropeLength;
    [Export]
    Vector2 startPoint;
    [Export]
    Vector2 endPoint;

    PhysicsBody2D topConnectedBody;
    RigidBody2D bottomConnectedBody;

    [Export]
    float ropeDensity = 0.1f;

    [Export]
    Vector2 topOffset;
    [Export]
    Vector2 bottomOffset;
    [Export]
    Color ropeColor;

    [Export]
    float softness;
    [Export]
    float bias;

    Node2D ropeContainer;
    List<RigidBody2D> segments;
    List<PinJoint2D> joints;
    List<CollisionShape2D> colliders;

    int jn = 1;
    int sn = 1;

    Font font;

    Vector2 GlobalStartPosition {
        get {
            return topConnectedBody.ToGlobal(topOffset);
        }
    }

    Vector2 GlobalEndPosition {
        get {
            return bottomConnectedBody.ToGlobal(bottomOffset);
        }
    }

    Vector2 RopeDirection {
        get {
            return (GlobalEndPosition - GlobalStartPosition).Normalized();
        }
    }

    Vector2 GetGlobalPositionOfSegmentIndex(int index) {
        return GlobalStartPosition + RopeDirection * (firstSegmentLength + index * segmentLength);
    }

    public override void _Ready() {
        font = new Control().GetFont("font");
        var anchor_scene =  GD.Load<PackedScene>("res://platform/rope anchor.tscn");
        var platform_scene =  GD.Load<PackedScene>("res://platform/Platform.tscn");
        topConnectedBody = anchor_scene.Instance<RigidBody2D>();
        topConnectedBody.Position = startPoint;
        AddChild(topConnectedBody);
        bottomConnectedBody = platform_scene.Instance<RigidBody2D>();
        bottomConnectedBody.Position = endPoint;
        AddChild(bottomConnectedBody);
        GenerateRope();
        GD.Print("generated rope!");
        foreach (var j in joints) {
            GD.Print($"{j.Name}:{GetNode(j.NodeA).Name},{GetNode(j.NodeB).Name}");
        }
    }

    public override void _Draw() {
        DrawCircle(GlobalStartPosition, 0.75f, Colors.Red);
        DrawCircle(GlobalEndPosition, 0.75f, Colors.Blue);
        for (var x = 0; x < segments.Count - 1; x++) {
            // DrawString(font, segments[x].GlobalPosition + Vector2.Right * 30f, segments[x].Name, modulate: Colors.Red);
            DrawLine(segments[x].GlobalPosition, segments[x + 1].GlobalPosition, ropeColor);
        }
        DrawLine(segments.Last().GlobalPosition, GlobalEndPosition, ropeColor);
        // foreach (var c in colliders) {
        //     DrawLine(c.GlobalPosition, c.ToGlobal(Vector2.Right*segmentLength), Colors.DarkOrange);
        // }
        // foreach (var s in segments) {
        //     DrawCircle(s.GlobalPosition,0.65f,Colors.Aquamarine);
        // }
        // foreach (var j in joints) {
        //     DrawString(font, j.GlobalPosition, j.Name);
        //     // DrawCircle(j.GlobalPosition,0.5f,Colors.Yellow);
        // }

    }

    public override void _Process(float delta) {
        Update();
    }

    void GenerateRope(bool calculateRopeLength = true) {
        ropeContainer = new Node2D();
        segments = new List<RigidBody2D>();
        joints = new List<PinJoint2D>();
        colliders = new List<CollisionShape2D>();
        AddChild(ropeContainer);
        if (calculateRopeLength) {
            ropeLength = GlobalStartPosition.DistanceTo(GlobalEndPosition);
            GD.Print($"calculate rope length: {ropeLength}");
        }
        // first segment is shorter if the ropeLength isn't a multiple of segmentLength
        firstSegmentLength = ropeLength % segmentLength;
        var segmentsNeeded = (int)(ropeLength / segmentLength)-1;
        if (firstSegmentLength != 0) {
            segmentsNeeded++;
        } else {
            firstSegmentLength = segmentLength;
        }

        var segment = CreateSegment(GlobalStartPosition, firstSegmentLength);
        // var segment = new RigidBody2D();
        // segments.Add(segment);
        // segment.CollisionLayer = 4;
        // ropeContainer.AddChild(segment);
        // var joint = new PinJoint2D();
        // segment.AddChild(joint);
        // segment.GlobalPosition = GlobalStartPosition;
        // segment.LookAt(GlobalEndPosition);
        // segment.Mass = ropeDensity * segmentLength;
        // CreateCollider(segment,0.5f,segmentLength);
        // joint.GlobalPosition = segment.ToGlobal(new Vector2(firstSegmentLength, 0));
        // joint.NodeA = segment.GetPath();
        // joint.Softness = softness;
        // joint.Bias = bias;
        // joints.Add(joint);
        // var workingPosition = joint.GlobalPosition;
        // var firstSegment = segment;

        // while (segmentsNeeded > 0) {
        RigidBody2D s = null;
        for (var x = 0; x < segmentsNeeded; x++) {
            s = CreateSegment(
                    GetGlobalPositionOfSegmentIndex(x),
                    segmentLength);
            // segment = new RigidBody2D {
            //     CollisionLayer = 4,
            //     Name = $"seg{sn++}"
            // };
            // segments.Add(segment);
            // ropeContainer.AddChild(segment);
            // segment.GlobalPosition = joint.GlobalPosition;
            // segment.LookAt(GlobalEndPosition);
            // joint.NodeB = segment.GetPath();
            // joint = new PinJoint2D();
            // joints.Add(joint);
            // segment.AddChild(joint);
            // segment.Mass = ropeDensity * segmentLength;
            // CreateCollider(segment,0.5f,segmentLength);
            // joint.GlobalPosition = segment.ToGlobal(new Vector2(segmentLength, 0));
            // joint.NodeA = segment.GetPath();
            // joint.Softness = softness;
            // joint.Bias = bias;
            // joint.Name = $"j{jn++}";
            // workingPosition = joint.GlobalPosition;
            // segmentsNeeded--;
        }
        s.LookAt(GlobalEndPosition);
        for (var i = 0; i < segments.Count; i++) {
            NodePath a, b;
            Vector2? position = null;
            if (i == 0) {
                a = topConnectedBody.GetPath();;
                position = GlobalStartPosition;
            } else if (i == 1) {
                a = segments[i - 1].GetPath();
                position = GlobalStartPosition + RopeDirection * firstSegmentLength;
            } else {
                a = segments[i - 1].GetPath();
            }
            b = segments[i].GetPath();
            CreateJoint(a, b, position);
        }
        CreateJoint(segments.Last().GetPath(), bottomConnectedBody.GetPath());
        // joint.NodeB = bottomConnectedBodyPath;
        // var firstJoint = new PinJoint2D{
        //     GlobalPosition = GlobalStartPosition,
        //     NodeA = topConnectedBodyPath,
        //     NodeB = firstSegment.GetPath(),
        //     Softness = softness,
        //     Bias = bias
        // };
        // joints.Add(firstJoint);
        // ropeContainer.AddChild(firstJoint);
        GD.Print($"Generated rope from [{GlobalStartPosition}] to [{GlobalEndPosition}].");
    }

    RigidBody2D CreateSegment(Vector2 globalPosition, float length) {
        var segment = new RigidBody2D() {
            Name = $"s{sn++}",
            GlobalPosition = globalPosition,
            Mass = ropeDensity * length,
            CollisionLayer = 4
        };
        ropeContainer.AddChild(segment);
        segment.LookAt(GlobalEndPosition);
        segments.Add(segment);
        CreateCollider(segment, 0.5f, length);
        return segment;
    }

    PinJoint2D CreateJoint(NodePath a, NodePath b, Vector2? globalPosition = null) {
        var joint = new PinJoint2D() {
            Name = $"j{jn++}",
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
        joints.Add(joint);
        joint.NodeB = b;
        return joint;
    }

    void CreateCollider(Node segment, float width, float length) {
        var collider = new CollisionShape2D();
        var shape = new CapsuleShape2D {
            Radius = width,
            Height = length
        };
        collider.Shape = shape;
        segment.AddChild(collider);

        collider.Position = Vector2.Right * length / 2;
        collider.Rotate(Mathf.Pi / 2f);
        colliders.Add(collider);
    }
}
