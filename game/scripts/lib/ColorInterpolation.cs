using System;
using Godot;

// Started from some snippets I found https://www.alanzucconi.com/2016/01/06/colour-interpolation/
public static class ColorInterpolation {
    public static Color LerpHSV(Color a, Color b, float t) {
        // Hue interpolation
        float h = 0f;
        float d = b.h - a.h;
        if (a.h > b.h) {
            var h3 = b.h;
            b.h = a.h;
            a.h = h3;

            d = -d;
            t = 1 - t;
        }

        if (d > 0.5f) {
            a.h = a.h + 1;
            h = (a.h + t * (b.h - a.h)) % 1;
        }
        if (d <= 0.5) {
            h = a.h + t * d;
        }

        // Interpolates the rest
        return Color.FromHsv(
            h,
            a.s + t * (b.s - a.s),
            a.v + t * (b.v - a.v),
            1);
    }
}