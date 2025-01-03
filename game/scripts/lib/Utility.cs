using System;
using Godot;


public static class Utility {
    public static float GetDistance(Vector2 to, Rect2 boundingBox) {
        var x0 = boundingBox.Position.x - boundingBox.Size.x / 2;
        var x1 = boundingBox.Position.x + boundingBox.Size.x / 2;
        var y0 = boundingBox.Position.y - boundingBox.Size.y / 2;
        var y1 = boundingBox.Position.y + boundingBox.Size.y / 2;
        var bbPoints = new Vector2[] {
            new Vector2(x0,y0),
            new Vector2(x1,y0),
            new Vector2(x1,y1),
            new Vector2(x0,y1),
        };
        float minDist = float.MaxValue;
        for (var i = 0; i < bbPoints.Length; i++) {
            var p1 = bbPoints[i];
            var p2 = i == (bbPoints.Length - 1) ? bbPoints[0] : bbPoints[i + 1];
            minDist = Mathf.Min(minDist, Geometry.GetClosestPointToSegment2d(to, p1, p2).DistanceTo(to));
        }
        return minDist;
    }
}