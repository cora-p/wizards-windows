using System;
using Godot;
#pragma warning disable CS0649, IDE0044
public class RopeAnchor : RigidBody2D {
    [Export]
    float moveLerp;

    [Export]
    float moveSpeed;

    public Vector2 targetPosition;
    public Vector2 targetOffset;

    public Vector2 moveRange;
    public override void _PhysicsProcess(float delta) {
        GlobalPosition = GlobalPosition.LinearInterpolate(targetPosition + targetOffset, moveLerp);
        var leftPressed = Input.IsActionPressed("Move Platform Left");
        var rightPressed = Input.IsActionPressed("Move Platform Right");
        Vector2 moveVector = Vector2.Zero;
        if (leftPressed) {
            moveVector = Vector2.Left * moveSpeed * delta;
        }
        if (rightPressed) {
            moveVector = Vector2.Right * moveSpeed * delta;
        }
        if (leftPressed ^ rightPressed) {
            targetOffset += moveVector;
        }
    }
}
