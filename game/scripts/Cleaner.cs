using System;
using Godot;

public class Cleaner : RigidBody2D {
    [Export]
    float speed;

    public override void _IntegrateForces(Physics2DDirectBodyState state) {
        var move = Input.IsActionPressed("Move Left") ? -1f : 0f;
        move += Input.IsActionPressed("Move Right") ? 1f : 0f;
        AppliedForce = (Vector2.Right * move * speed).Rotated(Rotation);
        Rotation = 0f;
    }
}
