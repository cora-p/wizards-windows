using System;
using System.Collections.Generic;
using Godot;
#pragma warning disable IDE0044,IDE0051,IDE0060
public class SpellCollectible : Node2D {
    [Export]
    Curve revealCurve, wiggleCurve;
    [Export]
    float revealDuration, wiggleDuration, wiggleAmplitude, rotateSpeed;

    float t;

    AnimatedSprite sprite;

    Label label;

    bool isRevealed, isLevelComplete;

    float Normalize(float d) => Mathf.Clamp(t % d / d, 0, 1);

    public override void _Ready() {
        sprite = GetNode<AnimatedSprite>("AnimatedSprite");
        label = GetNode<Label>("Label");
        GetNode<Area2D>("Area2D").Connect("body_entered", this, "Reveal");
        sprite.Modulate = Colors.Transparent;
        label.Modulate = Colors.Transparent;
        isRevealed = false;
        t = 0;
    }

    public override void _Process(float delta) {
        if (!isRevealed) return;

        t += delta;
        // don't do anything until we've been found
        if (isLevelComplete) {
            if (label.Modulate.a < 0.99f) {
                var aTime = Normalize(revealDuration);
                sprite.Modulate = new Color(1, 1, 1, revealCurve.Interpolate(1 - aTime));
                label.Modulate = new Color(1, 1, 1, revealCurve.Interpolate(aTime));
            }
            label.RectRotation = wiggleCurve.Interpolate(Normalize(wiggleDuration)) * wiggleAmplitude;
        } else {
            sprite.Rotate(Mathf.Deg2Rad(-delta * rotateSpeed));
            if (sprite.Modulate.a < 1.0) {
                t += delta;
                sprite.Modulate = new Color(1, 1, 1, revealCurve.Interpolate(t % revealDuration));
            }
        }

        if (!isLevelComplete && GrimeManager.Instance != null && GrimeManager.Instance.PercentGrimy == 0) {
            isLevelComplete = true;
            t = 0f;
        }
    }

    private void Reveal(Node n) => isRevealed = true;
}
