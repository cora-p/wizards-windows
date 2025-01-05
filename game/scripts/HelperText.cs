using System;
using Godot;
#pragma warning disable CS0649, IDE0044

public class HelperText : Area2D {
    [Export]
    Curve visibilityCurve;

    [Export]
    float fadeDuration, visibleDuration, delay;
    float fadePosition, visibleTimeRemaining, delayRemaining;

    float FadePositionNormalized { get => fadePosition / fadeDuration; }

    bool isCleanerPresent = false;

    readonly Color W = Colors.White;

    public override void _Ready() {
        Modulate = Colors.Transparent;
        delayRemaining = delay;
    }

    public override void _Process(float delta) {
        if (Cleaner.Instance == null) return;

        isCleanerPresent = OverlapsBody(Cleaner.Instance.platform);
        if (isCleanerPresent && delayRemaining > 0) {
            delayRemaining -= delta;
            return;
        }
        if (isCleanerPresent) {
            visibleTimeRemaining = visibleDuration;
            if (fadePosition < fadeDuration) fadePosition += delta;
        } else if (visibleTimeRemaining > 0f) {
            visibleTimeRemaining -= delta;
            if (fadePosition < fadeDuration) fadePosition += delta;
        } else {
            delayRemaining = delay;
            if (fadePosition > 0f) fadePosition -= delta;
        }
        Modulate = new Color(W.r, W.g, W.b, visibilityCurve.InterpolateBaked(FadePositionNormalized));
    }
}
