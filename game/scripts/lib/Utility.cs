using System;
using Godot;


public static class Utility {
    public static float GetDistanceToRect(Vector2 to, Rect2 rect) {
        var x0 = rect.Position.x - rect.Size.x / 2;
        var x1 = rect.Position.x + rect.Size.x / 2;
        var y0 = rect.Position.y - rect.Size.y / 2;
        var y1 = rect.Position.y + rect.Size.y / 2;
        var points = new Vector2[] {
            new Vector2(x0,y0),
            new Vector2(x1,y0),
            new Vector2(x1,y1),
            new Vector2(x0,y1),
        };
        float minDist = float.MaxValue;
        for (var i = 0; i < points.Length; i++) {
            var p1 = points[i];
            var p2 = i == (points.Length - 1) ? points[0] : points[i + 1];
            minDist = Mathf.Min(minDist, Geometry.GetClosestPointToSegment2d(to, p1, p2).DistanceTo(to));
        }
        return minDist;
    }
    public static float GetJitter(float extent) => (float)GD.RandRange(-extent, extent);

    public static void DrawDashedRay(Node2D caller, Vector2 from, Vector2 dir, float length, Color color, float dashLength, float gapLength, float width = 1) {
        var hasReachedTo = false;
        var p = from;
        var isSolid = true;
        var actualTo = p + dir * length;
        while (!hasReachedTo) {
            if (isSolid) {
                // dash
                caller.DrawLine(p, p + (dir * dashLength), color, width);
                p += dir * Mathf.Min(dashLength, p.DistanceTo(actualTo));
                length -= dashLength;
            } else {
                // gap
                p += dir * Mathf.Min(gapLength, p.DistanceTo(actualTo));
                length -= gapLength;
            }
            isSolid = !isSolid;
            hasReachedTo = length < Mathf.Max(gapLength, dashLength);
        }
    }
}