using System;
using Godot;

// Started from some snippets I found https://www.alanzucconi.com/2016/01/06/colour-interpolation/
public static class ColorInterpolation {
    public static Color LerpHSV(Color a, Color b, float t) {
        // Hue interpolation
        float h = 0f;
        // storing hues as each call to Color.h is expensive
        float aHue = a.h;
        float bHue = b.h;
        float delta = bHue - aHue;
        if (aHue > bHue) {
            (aHue, bHue) = (bHue, aHue);
            delta = -delta;
            t = 1 - t;
        }

        if (delta > 0.5f) {
            aHue++;
            h = (aHue + t * bHue - aHue) % 1;
        } else {
            h = aHue + t * delta;
        }

        // Interpolates the rest
        return Color.FromHsv(
            h,
            a.s + t * (b.s - a.s),
            a.v + t * (b.v - a.v),
            1);
    }
}