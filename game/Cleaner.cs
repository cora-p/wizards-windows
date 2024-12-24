using System;
using Godot;

public class Cleaner : KinematicBody2D {
    [Export]
    public float speed = 20;
    
    [Export]
    public float fallSpeed = 30;

    [Export]
    public float jumpPower;

    [Export]
    public float jumpDuration = 0.25f;

    float jumpTimeRemaining;


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
        MoveAndCollide(Vector2.Down * fallSpeed * delta);
        isGrounded = TestMove(Transform, Vector2.Down * fallSpeed * delta);
    }

    void ProcessMovement(float delta) {
        var horizontal =  (Input.IsActionPressed("Move Left") ? -1 : 0)
                        + (Input.IsActionPressed("Move Right") ? 1 : 0);
        var movementVector = new Vector2(horizontal * speed * delta, 0);
        MoveAndCollide(movementVector);

        if (isGrounded && Input.IsActionJustPressed("Jump")) {
            jumpTimeRemaining = jumpDuration;
        }

        if (jumpTimeRemaining > 0) {
            jumpTimeRemaining -= delta;
            MoveAndCollide(Vector2.Up * jumpPower);
        }
    }
}
