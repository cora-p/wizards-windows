using System;
using Godot;
#pragma warning disable CS0649, IDE0044
public class Cleaner : Node2D {



    [Export]
    float moveInputAcceleration, centeringAcceleration, stopAcceleration;
    [Export]
    float maxTravelDistance;
    float moveInputMoment = 0f;
    float travelPosition;
    [Export]
    float travelSpeed;

    [Export]
    float centerAfter;
    float idleTime;

    float shoveMomentumDistance = 0f;

    [Export]
    float shovePower;

    bool hasShovedRight, hasShovedLeft;

    RigidBody2D platform;

    public override void _Ready() {
        platform = GetParent<RigidBody2D>();
        hasShovedRight = false;
        hasShovedLeft = false;
    }

    public override void _PhysicsProcess(float delta) {
        var leftPressed = Input.IsActionPressed("Move Left");
        var rightPressed = Input.IsActionPressed("Move Right");
        if (leftPressed && !rightPressed) {
            moveInputMoment = Mathf.Lerp(moveInputMoment, -1f, moveInputAcceleration);
        }
        if (rightPressed && !leftPressed) {
            moveInputMoment = Mathf.Lerp(moveInputMoment, 1f, moveInputAcceleration);
        }
        if (!rightPressed) {
            hasShovedRight = false;
        }
        if (!leftPressed) {
            hasShovedLeft = false;
        }
        if (!(leftPressed || rightPressed)) {
            if (idleTime > centerAfter) {
                if (travelPosition > 0f) {
                    moveInputMoment = Mathf.Lerp(moveInputMoment, -1f, centeringAcceleration);
                } else {
                    moveInputMoment = Mathf.Lerp(moveInputMoment, 1f, centeringAcceleration);
                }
                if (Mathf.Abs(travelPosition) < 0.25f) {
                    moveInputMoment = 0f;
                    travelPosition = 0f;
                }
            } else {
                moveInputMoment = Mathf.Lerp(moveInputMoment, 0f, stopAcceleration);
                idleTime += delta;
            }
        } else {
            idleTime = 0;
        }
        if (Mathf.Abs(moveInputMoment) > 0.01f) {
            if (moveInputMoment > 0 && travelPosition >= maxTravelDistance) {
                if (!hasShovedRight) {
                    ShovePlatform(left: false);
                }
                travelPosition = maxTravelDistance;
            } else if (moveInputMoment < 0 && travelPosition <= -maxTravelDistance) {
                if (!hasShovedLeft) {
                    ShovePlatform(left: true);
                }
                travelPosition = -maxTravelDistance;
            } else {
                var positionDelta = moveInputMoment * travelSpeed * delta;
                travelPosition += positionDelta;
                shoveMomentumDistance += Mathf.Abs(positionDelta);
            }
        }
        Position = new Vector2(travelPosition, Position.y);
        Rotation = 0f;
    }

    void ShovePlatform(bool left) {
        Vector2 impulse;
        if (left) {
            impulse = Vector2.Left.Rotated(platform.GlobalRotation);
            hasShovedLeft = true;
        } else {
            impulse = Vector2.Right.Rotated(platform.GlobalRotation);
            hasShovedRight = true;
        }
        platform.ApplyCentralImpulse(impulse * shoveMomentumDistance * shovePower);
        shoveMomentumDistance = 0f;
    }
}
