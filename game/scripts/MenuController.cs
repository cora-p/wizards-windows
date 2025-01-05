using System;
using Godot;
#pragma warning disable CS0649, IDE0044
public class MenuController : Node2D {

    [Export]
    float resetHoldTime;
    float resetTime;

    public override void _Process(float delta) {
        if (Input.IsActionPressed("Reset")) {
            resetTime += delta;
            if (resetHoldTime < resetTime) {
                Overseer.Instance.Reset();
            }
        }
    }
}
