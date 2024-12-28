using System;
using Godot;
#pragma warning disable CS0649, IDE0044
public class Cleaner : RigidBody2D {
    
    [Export]
    float speed;

    [Export]
    float centeringSpeed;

    [Export]
    float centeringFactor;

    Vector2 startPosition;

    float moveInputMoment = 0f;

    [Export]
    float moveInputAcceleration;

    [Export]
    float velocitySyncLerpWeight;

    public override void _Ready() {
        startPosition = Position;
    }

    public override void _Process(float delta) {
        var leftPressed = Input.IsActionPressed("Move Left");
        var rightPressed = Input.IsActionPressed("Move Right");

        if (leftPressed && moveInputMoment > 0f) moveInputMoment = 0f;
        if (rightPressed && moveInputMoment < 0f) moveInputMoment = 0f;
        if (!leftPressed && moveInputMoment < 0f) moveInputMoment = 0f;
        if (!rightPressed && moveInputMoment > 0f) moveInputMoment = 0f;
        if (leftPressed && !rightPressed) {
            moveInputMoment = Mathf.Lerp(moveInputMoment, -1f, moveInputAcceleration);
        }
        if (rightPressed && !leftPressed) {
            moveInputMoment = Mathf.Lerp(moveInputMoment, 1f, moveInputAcceleration);
        }
        if (!leftPressed && !rightPressed) {
            moveInputMoment = 0f;
        }
    }


    public override void _PhysicsProcess(float delta) {
        if (Mathf.Abs(moveInputMoment) > 0.1f) {
            ApplyCentralImpulse(Vector2.Right.Rotated(GlobalRotation) * moveInputMoment * speed * Mass * delta);
        } else {
            var centeringVector = startPosition - Position;
            var centerDistance = centeringVector.Length();
            Position += centeringVector.Normalized() * centeringSpeed * delta * Mathf.Pow(centerDistance, centeringFactor);
            LinearVelocity = LinearVelocity.LinearInterpolate(
                 GetParent<RigidBody2D>().LinearVelocity, velocitySyncLerpWeight);
        }

        Rotation = 0f;
    }
}
