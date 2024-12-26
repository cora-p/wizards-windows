using System;
using Godot;

public class CableSegment : Node2D {
    [Export]
    NodePath nextNode;

    Sprite sprite;
    public override void _Ready() {
        GD.Print(nextNode);
        GetNode<PinJoint2D>("PinJoint2D").NodeB = nextNode;
        sprite = GetNode<Sprite>("body/Sprite");
    }
    
    public override void _Process(float delta) {
        
        // sprite.Scale = new Vector2(1, GlobalPosition.DistanceTo(GetNode<Node2D>(nextNode).GlobalPosition));
    }
}
