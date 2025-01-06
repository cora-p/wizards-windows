using System;
using Godot;
#pragma warning disable IDE0044
public class PhysicsAudioPlayer : RigidBody2D {
    AudioStreamPlayer player;
    [Export]
    float minSpeed, maxSpeed;
    float initialVolume;
    [Export]
    Curve impactFactorCurve;

    public override void _Ready() {
        player = GetNode<AudioStreamPlayer>("AudioStreamPlayer");
        Connect("body_entered", this, "OnCollision");
        initialVolume = player.VolumeDb;
    }

    public void OnCollision(Node n) {
        if (player.Playing) return;
        if (n is RigidBody2D rb) {

            var speed = (LinearVelocity - rb.LinearVelocity).Length();
            if (speed < minSpeed) return;

            var vT = impactFactorCurve.Interpolate(speed % maxSpeed / maxSpeed);
            player.VolumeDb = initialVolume + vT;
            GD.Print($"{Name}: {player.VolumeDb},{vT}");
            player.Play();
        }
    }
}
