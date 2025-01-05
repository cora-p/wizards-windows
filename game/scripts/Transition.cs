using System;
using Godot;

public class Transition : Node2D {
    [Export]
    float speed;

    [Export]
    float destroyAfter;
    public override void _Process(float delta) {
        MoveLocalY(-speed * delta);
        if (destroyAfter < 0) {
            QueueFree();
        } else {
            destroyAfter -= delta;
        }
    }
}