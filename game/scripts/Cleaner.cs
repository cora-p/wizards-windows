using System;
using Godot;
#pragma warning disable CS0649, IDE0044
public class Cleaner : Node2D, Manager {
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

    [Export]
    float maxWindup, windupDecayRate;
    [Export]
    Curve windUpCurve;

    float windup = 0f;

    [Export]
    float shovePower;

    bool hasShovedRight, hasShovedLeft;

    public RigidBody2D platform;

    public static Cleaner Instance { get; private set; }
    [Export]
    Curve windUpColorCurve;

    Sprite sprite;

    [Export]
    float shoveUpFactor, popUpPixels, shovePopThreshold;

    public override void _Ready() {
        platform = GetParent<RigidBody2D>();
        hasShovedRight = false;
        hasShovedLeft = false;
        Instance = this;
        Overseer.Instance.ReportReady(this);
        sprite = GetNode<Sprite>("Sprite");
    }

    public override void _PhysicsProcess(float delta) {
        var leftPressed = Input.IsActionPressed("Move Left");
        var rightPressed = Input.IsActionPressed("Move Right");
        var windupNormalized = windup / maxWindup;
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
            windup -= windupDecayRate * delta;
        } else {
            idleTime = 0;
        }
        windup = Mathf.Clamp(windup, 0, maxWindup);
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
                windup += Mathf.Abs(positionDelta) * windUpCurve.Interpolate(windupNormalized);
            }
        }
        var ct = windUpColorCurve.Interpolate(windupNormalized);
        sprite.Modulate = new Color(1, ct, ct, 1);

        Position = new Vector2(travelPosition, Position.y);
        Rotation = 0f;
    }

    void ShovePlatform(bool left) {
        if (windup == 0) return;
        Vector2 impulse;
        windup = Mathf.Min(windup, maxWindup);
        GD.Print($"Windup: {windup}");
        if (left) {
            impulse = (Vector2.Left + Vector2.Up * shoveUpFactor).Normalized().Rotated(platform.GlobalRotation);
            hasShovedLeft = true;
        } else {
            impulse = (Vector2.Right + Vector2.Up * shoveUpFactor).Normalized().Rotated(platform.GlobalRotation);
            hasShovedRight = true;
        }
        if (windup > shovePopThreshold) {
            platform.MoveLocalY(-popUpPixels);
        }
        platform.ApplyCentralImpulse(impulse * windup * shovePower);
        windup = 0f;
    }

    public void OnAllReady() {
    }
    public bool Reset() {
        Instance = null;
        QueueFree();
        return true;
    }
    public PackedScene GetPackedScene() => null;

}
