using System;
using Godot;

public class Cleaner : KinematicBody2D {
    [Export]
    public float speed = 20;

    [Export]
    public float fallAcceleration = 60;

    [Export]
    public float jumpPower;

    float verticalVelocity = 0;

    bool isGrounded;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {

    }

    //  // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta) {
        // MoveAndCollide(Vector2.Right * delta * speed);
        ProcessFalling(delta);
        ProcessMovement(delta);
    }

    void ProcessFalling(float delta) {
        verticalVelocity -= fallAcceleration * delta;
        MoveAndCollide(Vector2.Up * verticalVelocity * delta);
        isGrounded = TestMove(Transform, Vector2.Down);

        if (isGrounded) {
            verticalVelocity = Mathf.Clamp(verticalVelocity,0,200);
        }
        if (TestMove(Transform, Vector2.Up)) {
            verticalVelocity = Mathf.Clamp(verticalVelocity,-200,0);
        }
    }

    void ProcessMovement(float delta) {
        var horizontal = (Input.IsActionPressed("Move Left") ? -1 : 0)
                        + (Input.IsActionPressed("Move Right") ? 1 : 0);
        var movementVector = new Vector2(horizontal * speed * delta, 0);
        MoveAndCollide(movementVector);

        if (isGrounded && Input.IsActionPressed("Jump")) {
            verticalVelocity += jumpPower;
        }

        // if (jumpPowerRemaining > 0) {
        //     GD.Print(jumpPowerRemaining);
        //     MoveAndCollide(Vector2.Up * jumpPowerRemaining * delta);
        //     // jumpTimeRemaining -= delta;
        //     jumpPowerRemaining -= jumpDecay * delta;
        // }
    }
}
