using System;
using Godot;

public class MenuController : Node2D {
    public override void _Process(float delta) {
        if (Input.IsActionJustPressed("Quit")) {
            GetTree().Quit();
        }
    }
}
