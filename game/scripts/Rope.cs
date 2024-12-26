using System;
using System.Collections.Generic;
using Godot;

public class Rope : Node2D {
    [Export]
    float segmentLength;
    [Export]
    float ropeLength;
    [Export]
    NodePath topConnectedBodyPath;
    [Export]
    NodePath bottomConnectedBodyPath;

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

    public override void _Ready() {
        topConnectedBody = GetNode<PhysicsBody2D>(topConnectedBodyPath);
        bottomConnectedBody = GetNode<RigidBody2D>(bottomConnectedBodyPath);
        GenerateRope();
        GD.Print("generated rope!");
        foreach (var j in joints) {
            GD.Print($"{j.Name}:{GetNode(j.NodeA).Name},{GetNode(j.NodeB).Name}");
        }
    }

    public override void _Draw() {
        DrawCircle(GlobalStartPosition,0.75f,Colors.Red);
        DrawCircle(GlobalEndPosition,0.75f,Colors.Blue);
        for (var x = 0; x < segments.Count - 1; x++) {
            DrawLine(segments[x].GlobalPosition, segments[x + 1].GlobalPosition, ropeColor);
        }
        // foreach (var c in colliders) {
        //     DrawLine(c.GlobalPosition, c.ToGlobal(Vector2.Right*segmentLength), Colors.DarkOrange);
        // }
        // foreach (var s in segments) {
        //     DrawCircle(s.GlobalPosition,0.65f,Colors.Aquamarine);
        // }
        // foreach (var j in joints) {
        //     DrawCircle(j.GlobalPosition,0.5f,Colors.Yellow);
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
        var firstSegmentLength = ropeLength % segmentLength;
        var segmentsNeeded = (int)(ropeLength / segmentLength);
        if (firstSegmentLength != 0) {
            segmentsNeeded++;
        } else {
            firstSegmentLength = segmentLength;
        }


        var segment = new RigidBody2D();
        segments.Add(segment);
        segment.CollisionLayer = 4;
        ropeContainer.AddChild(segment);
        var joint = new PinJoint2D();
        segment.AddChild(joint);
        segment.GlobalPosition = GlobalStartPosition;
        segment.LookAt(GlobalEndPosition);
        segment.Mass = ropeDensity * segmentLength;
        CreateCollider(segment,0.5f,segmentLength);
        joint.GlobalPosition = segment.ToGlobal(new Vector2(firstSegmentLength, 0));
        joint.NodeA = segment.GetPath();
        joint.Softness = softness;
        joint.Bias = bias;
        joints.Add(joint);
        // var workingPosition = joint.GlobalPosition;
        var firstSegment = segment;

        while (segmentsNeeded > 0) {
            segment = new RigidBody2D {
                CollisionLayer = 4,
                Name = $"seg{sn++}"
            };
            segments.Add(segment);
            ropeContainer.AddChild(segment);
            segment.GlobalPosition = joint.GlobalPosition;
            segment.LookAt(GlobalEndPosition);
            joint.NodeB = segment.GetPath();
            joint = new PinJoint2D();
            joints.Add(joint);
            segment.AddChild(joint);
            segment.Mass = ropeDensity * segmentLength;
            CreateCollider(segment,0.5f,segmentLength);
            joint.GlobalPosition = segment.ToGlobal(new Vector2(segmentLength, 0));
            joint.NodeA = segment.GetPath();
            joint.Softness = softness;
            joint.Bias = bias;
            joint.Name = $"j{jn++}";
            // workingPosition = joint.GlobalPosition;
            segmentsNeeded--;
        }
        joint.NodeB = bottomConnectedBodyPath;
        var firstJoint = new PinJoint2D{
            GlobalPosition = GlobalStartPosition,
            NodeA = topConnectedBodyPath,
            NodeB = firstSegment.GetPath(),
            Softness = softness,
            Bias = bias
        };
        joints.Add(firstJoint);
        ropeContainer.AddChild(firstJoint);
        GD.Print($"Generated rope from [{GlobalStartPosition}] to [{GlobalEndPosition}].");
    }

    void CreateCollider(Node segment, float width, float length) {
        var collider = new CollisionShape2D();
        var shape = new CapsuleShape2D {
            Radius = width,
            Height = length
        };
        collider.Shape = shape;
        segment.AddChild(collider);
        
        collider.Position = Vector2.Right * length/2;
        collider.Rotate(Mathf.Pi/2f);
        colliders.Add(collider);
    }
}
