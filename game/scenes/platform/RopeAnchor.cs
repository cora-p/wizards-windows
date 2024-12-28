using System;
using Godot;

public class RopeAnchor : RigidBody2D
{
    [Export]
    float moveLerp;
    public Vector2 targetPosition;
    public override void _PhysicsProcess(float delta) {
        GlobalPosition = GlobalPosition.LinearInterpolate(targetPosition, moveLerp);
        // var moveAxis = Input.IsActionPressed("Raise Platform") ? -1f: 0f;
        // moveAxis += Input.IsActionPressed("Lower Platform") ? 1f : 0f;
        // Vector2 moveVector = Vector2.Zero;
        // if (moveAxis < 0f) {
        //     moveVector = Vector2.Up * raiseSpeed * delta;
        // }
        // if (moveAxis > 0f) {
        //     moveVector = Vector2.Down * lowerSpeed * delta;
        // }
        // if (moveAxis != 0) {
        //     GlobalPosition += moveVector;
        // }
    }
}
