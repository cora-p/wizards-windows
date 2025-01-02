using System;
using Godot;
#pragma warning disable CS0649, IDE0044,IDE0017
public class LevelManager : Node2D, Manager {
    [Export]
    int width, height;

    [Export]
    Vector2 start;

    [Export]
    float bgTileSize;
    [Export]
    Vector2 rowShiftExtent, tileShiftExtent;
    [Export]
    float rowCantExtent, tileCantExtent;
    [Export]
    float rowShiftChance, rowCantChance, tileShiftChance, tileCantChance;

    PackedScene bgTileScene, bgEdgeScene;

    Node2D[] rowHolders;

    Node2D[,] bgTiles;
    Node2D[] endcaps;



    public override void _Ready() {
        bgTileScene = GD.Load<PackedScene>("res://_scenes/env/other/StoneWall.tscn");
        bgEdgeScene = GD.Load<PackedScene>("res://_scenes/env/other/StoneWallEdge.tscn");
        GenerateBackground();
    }

    void GenerateBackground() {
        if (bgTiles != null) {
            // clean up existing bgTiles, if present
            for (var x = 0; x < width; x++) {
                for (var y = 0; y < height; y++) {
                    bgTiles[x, y].QueueFree();
                }
            }
            for (var i = 0; i < rowHolders.Length; i++) {
                rowHolders[i].QueueFree();
            }
            for (var i = 0; i < endcaps.Length; i++) {
                endcaps[i].QueueFree();
            }
        }

        rowHolders = new Node2D[height];
        bgTiles = new Node2D[width, height];
        endcaps = new Node2D[height * 2];
        //* start building from bottom as it looks better with the higher row tiles in front
        Vector2 rowShift, tileShift;
        float rowCant, tileCant;

        //^ main tiles
        for (var y = height - 1; y >= 0; y--) {
            if (IsLucky(rowShiftChance)) {
                rowShift = new Vector2(GetJitter(rowShiftExtent.x), 0);
            } else {
                rowShift = Vector2.Zero;
            }
            //* shift horizontally and vertically separately, otherwise too noticeable
            if (IsLucky(rowShiftChance)) {
                rowShift += new Vector2(0, GetJitter(rowShiftExtent.y));
            }
            if (IsLucky(rowCantChance)) {
                rowCant = GetJitter(rowCantExtent);
            } else {
                rowCant = 0;
            }
            var rowHolder = new Node2D();
            rowHolders[y] = rowHolder;
            AddChild(rowHolder);
            rowHolder.Position = new Vector2(start.x + width * bgTileSize, start.y + y * height);
            for (var x = 0; x < width; x++) {
                if (IsLucky(tileShiftChance)) {
                    tileShift = new Vector2(GetJitter(tileShiftExtent.x), 0);
                } else {
                    tileShift = Vector2.Zero;
                }
                if (IsLucky(tileShiftChance)) {
                    tileShift += new Vector2(0, GetJitter(tileShiftExtent.y));
                }

                if (IsLucky(tileCantChance)) {
                    tileCant = GetJitter(tileCantExtent);
                } else {
                    tileCant = 0;
                }
                var tile = bgTileScene.Instance<Node2D>();
                var position = start + new Vector2((x * bgTileSize) + tileShift.x,
                                                    (y * bgTileSize) + tileShift.y);
                tile.Position = rowHolder.ToLocal(position);
                tile.RotationDegrees = tileCant;
                rowHolder.AddChild(tile);
                bgTiles[x, y] = tile;
            }
            rowHolder.RotationDegrees = rowCant;
            rowHolder.Position += rowShift;
        }

        //^ endcaps
        for (var y = 0; y < height; y++) {
            var endcap = bgEdgeScene.Instance<Sprite>();
            var holder = rowHolders[y];
            holder.AddChild(endcap);
            var t = bgTiles[0, y];
            endcap.Position = t.Position - Vector2.Right * bgTileSize;
            endcap.Rotation = t.Rotation;
            endcap.FlipH = true;

            endcaps[height + y] = endcap;
            endcap = bgEdgeScene.Instance<Sprite>();
            holder.AddChild(endcap);
            t = bgTiles[width - 1, y];
            endcap.Position = t.Position + Vector2.Right * bgTileSize;
            endcap.Rotation = t.Rotation;
        }
    }

    public void OnAllReady() {
        throw new NotImplementedException();
    }

    float GetJitter(float extent) => (float)GD.RandRange(-extent, extent);
    bool IsLucky(float chance) => GD.Randf() <= chance;
}
