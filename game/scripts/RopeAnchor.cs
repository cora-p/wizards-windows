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

    public bool IsTravelling { get; private set; }

    [Export]
    float maxTravel;
    public float travel;

    public Vector2 moveRange;
    public override void _PhysicsProcess(float delta) {

        var leftPressed = Input.IsActionPressed("Move Platform Left");
        var rightPressed = Input.IsActionPressed("Move Platform Right");
        float travelDelta = 0;
        if (leftPressed && !rightPressed) {
            travelDelta = -1;
        } else if (rightPressed && !leftPressed) {
            travelDelta = 1;
        }
        travelDelta *= moveSpeed * delta;
        if (travelDelta != 0) {
            if (travelDelta < 0 && travel > -maxTravel) {
                travel += travelDelta;
                IsTravelling = true;
            } else if (travelDelta > 0 && travel < maxTravel) {
                travel += travelDelta;
                IsTravelling = true;
            } else {
                IsTravelling = false;
            }
        } else {
            IsTravelling = false;
        }
        travel = Mathf.Clamp(travel, -maxTravel, maxTravel);

        GlobalPosition = GlobalPosition.LinearInterpolate(targetPosition + targetOffset + Vector2.Right * travel, moveLerp);
    }
}
