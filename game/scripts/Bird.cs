using System;
using Godot;
#pragma warning disable CS0649, IDE0044,IDE0017
public class Bird : RigidBody2D {
    readonly static string Flying = "fly";
    readonly static string Turning = "turn";
    AnimatedSprite sprite;
    [Export]
    float speed;

    readonly string FLY = "fly";
    readonly string TURN = "turn";

    [Export]
    Curve turningSpeedNormalized, yBobCurve;

    [Export]
    float timeTilTurn;
    float timeTilTurnRemaining;

    float turnFrameCount, flyFrameCount;
    float TurnPercent { get => sprite.Frame / turnFrameCount; }
    float FlyPercent { get => sprite.Frame / flyFrameCount; }

    [Export]
    float rotateSpeed;

    Vector2 target;

    public override void _Ready() {
        sprite = GetNode<AnimatedSprite>("sprite");
        sprite.Animation = Flying;
        timeTilTurnRemaining = timeTilTurn;
        turnFrameCount = sprite.Frames.GetFrameCount(TURN);
        flyFrameCount = sprite.Frames.GetFrameCount(FLY);
        sprite.Connect("animation_finished", this, "OnAnimFinished");
        target = GetGlobalMousePosition();
    }

    float Forward { get => sprite.FlipH ? -1 : 1; }
    bool IsTurning { get => sprite.Animation == TURN; }

    public override void _PhysicsProcess(float delta) {
        // var oldRot = GlobalRotation;
        // LookAt(GetGlobalMousePosition());
        // var rotDelta = GlobalRotation - oldRot;
        // GlobalRotation = oldRot;

        GD.Print(Forward);
        if (IsTurning) {
            MoveLocalX(Forward * speed * turningSpeedNormalized.InterpolateBaked(TurnPercent) * delta);
        } else {
            MoveLocalX(Forward * speed * delta);
            sprite.Position = Vector2.Up * yBobCurve.InterpolateBaked(FlyPercent);
            // GlobalPosition += Forward * speed * delta;
            timeTilTurnRemaining -= delta;
            if (timeTilTurnRemaining <= 0) {
                Turn();
            }
        }
    }

    void Turn() {
        sprite.Animation = TURN;
    }

    public void OnAnimFinished() {
        if (sprite.Animation == Turning) {
            sprite.Animation = Flying;
            sprite.FlipH = !sprite.FlipH;
            timeTilTurnRemaining = timeTilTurn;
            target = GetGlobalMousePosition();
        }
    }
}
