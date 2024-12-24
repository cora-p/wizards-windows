using System;
using Godot;

public class Cleaner : KinematicBody2D {
    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";
    public float speed = 20;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {

    }

    //  // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta) {
        MoveAndCollide(Vector2.Right * delta * speed);
    }
}
