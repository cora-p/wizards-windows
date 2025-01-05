using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static Utility;

#pragma warning disable IDE0044,IDE0051,IDE0060
public class LevelManager : Node2D, Manager {
    [Export]
    int width, height;

    [Export]
    Vector2 start, mainMenuStart;

    [Export]
    float bgTileSize;
    [Export]
    Vector2 rowShiftExtent, tileShiftExtent;
    [Export]
    float rowCantExtent, tileCantExtent;
    [Export]
    float rowShiftChance, rowCantChance, tileShiftChance, tileCantChance;

    PackedScene bgTileScene, endcapScene;

    Node2D[] rowHolders;

    Node2D[,] bgTiles;
    Node2D[] endcaps;

    [Export]
    List<string> levelNames;
    List<PackedScene> levelScenes;
    int currentLevel = 0;

    public const string MAIN_MENU = "Main Menu";


    CollisionShape2D progressBlocker, progress;

    Sprite backdrop;

    public static LevelManager Instance { get; private set; }

    PackedScene transition;

    string CurrentLevelName { get => levelNames[currentLevel]; }
    public bool IsLastLevel { get => levelNames.Count == currentLevel + 1; }
    public bool IsOnMainMenu { get => currentLevel == 0; }
    public int NextLevelIndex { get => IsLastLevel ? 0 : currentLevel + 1; }



    public override void _Ready() {
        backdrop = GetNode<Sprite>("Backdrop");
        progressBlocker = GetNode<CollisionShape2D>("Progress Blocker/CollisionShape2D");
        progress = GetNode<CollisionShape2D>("Progress/CollisionShape2D");
        progress.GetParent().Connect("body_entered", this, "OnProgress");
        bgTileScene = GD.Load<PackedScene>("res://_scenes/env/other/Wall.tscn");
        endcapScene = GD.Load<PackedScene>("res://_scenes/env/other/Endcap.tscn");
        transition = GD.Load<PackedScene>("res://_scenes/fx/transition.tscn");

        levelScenes = new List<PackedScene>();
        foreach (var lvlName in levelNames) {
            levelScenes.Add(GD.Load<PackedScene>($"res://levels/{lvlName}.tscn"));
        }


        GD.Print("Initialized with level list (index, name, next):");
        for (var i = 0; i < levelNames.Count; i++) {
            currentLevel = i;
            GD.Print($"{currentLevel},{CurrentLevelName},{NextLevelIndex}");
        }
        currentLevel = 0;
        Instance = this;
        progressBlocker.SetDeferred("disabled", true);
        Overseer.Instance.ReportReady(this);
    }

    void GenerateBackground() {
        backdrop.Visible = !IsOnMainMenu;
        var bgScene = GD.Load<PackedScene>("res://_scenes/env/other/background.tscn");
        var bg = bgScene.Instance<Node2D>();
        AddChild(bg);
        bg.RotationDegrees = GetJitter(360);
        var s = IsOnMainMenu ? mainMenuStart : start;
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
        }

        rowHolders = new Node2D[height];
        bgTiles = new Node2D[width, height];
        endcaps = new Node2D[height * 2];

        Vector2 rowShift, tileShift;
        float rowCant, tileCant;

        //^ main tiles
        //* start building from bottom as it looks better with the higher row tiles in front
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
            rowHolder.Position = new Vector2(s.x + width * bgTileSize, s.y + y * height);
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
                var position = s + new Vector2((x * bgTileSize) + tileShift.x,
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
            var endcap = endcapScene.Instance<Sprite>();
            var holder = rowHolders[y];
            holder.AddChild(endcap);
            var t = bgTiles[0, y];
            endcap.Position = t.Position - Vector2.Right * bgTileSize;
            endcap.Rotation = t.Rotation;
            endcap.FlipH = true;

            endcaps[height + y] = endcap;
            endcap = endcapScene.Instance<Sprite>();
            holder.AddChild(endcap);
            t = bgTiles[width - 1, y];
            endcap.Position = t.Position + Vector2.Right * bgTileSize;
            endcap.Rotation = t.Rotation;
        }
    }

    public void OnAllReady() {
        CallDeferred("GenerateBackground");
    }
    bool IsLucky(float chance) => GD.Randf() <= chance;

    public void AllowProgression() {
        if (IsLastLevel) {
            GrimeManager.Instance.ShowWinLabel("THE END...\nFOR NOW!");
        } else {
            GrimeManager.Instance.ShowWinLabel();
        }
        progressBlocker.SetDeferred("disabled", true);
    }
    void OnProgress(Node n) {
        AddChild(transition.Instance());

        ChangeLevel();
    }

    public bool Reset() {
        progressBlocker.SetDeferred("disabled", false);
        return false;
    }

    void ChangeLevel(int specificLevel = -1) {
        var oldLevelIndex = currentLevel;
        if (specificLevel >= 0) currentLevel = specificLevel;
        currentLevel = NextLevelIndex;
        GD.Print($"Progressing from level {oldLevelIndex} to {currentLevel}");
        GetTree().ChangeSceneTo(levelScenes[currentLevel]);
        Overseer.Instance.Reset();
    }
    public PackedScene GetPackedScene() => null;
}
