using System;
using Godot;

public class Torch : AnimatedSprite {

    public override void _Ready() {
        Frame = (int)(GD.Randi() % Frames.GetFrameCount("default"));
    }

}
