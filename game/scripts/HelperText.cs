using System;
using Godot;
#pragma warning disable CS0649, IDE0044

public class HelperText : Node2D {
    [Export]
    Curve visibilityCurve;

    [Export]
    float maxAlphaChangeRate, fadeIn, fadeOut;

    Label label;


    public override void _EnterTree() {
        Modulate = new Color(Modulate.r, Modulate.g, Modulate.b, 0);
    }
    public override void _Ready() {
        label = GetNode<Label>("Label");
    }

    public override void _Process(float delta) {
        var targetAlpha = 0f;
        if (BrushController.Instance != null) {
            var d = Utility.GetDistance(BrushController.Instance.GlobalPosition, label.GetGlobalRect());
            targetAlpha = GetAlpha(d);
            if (targetAlpha == Modulate.a) return;
        }
        var aDelta = targetAlpha - Modulate.a;

        if (aDelta != 0) {
            var fadeDir = aDelta > 0 ? 1 : -1;
            var absDelta = Mathf.Abs(aDelta);
            if (absDelta < maxAlphaChangeRate) {
                Modulate = new Color(Modulate.r, Modulate.g, Modulate.b, targetAlpha);
            } else {
                Modulate = new Color(Modulate.r, Modulate.g, Modulate.b, Modulate.a + Mathf.Min(absDelta, maxAlphaChangeRate) * fadeDir * delta);
            }
        }
    }

    float GetAlpha(float d) {
        if (d < fadeIn) return 1;
        if (d > fadeOut) return 0;
        var dn = (d - fadeIn) / fadeOut;
        return visibilityCurve.InterpolateBaked(dn);
    }
}
