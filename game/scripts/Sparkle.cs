using System;
using Godot;

public class Sparkle : AnimatedSprite {
    public void OnFinished() {
        QueueFree();
    }
}
