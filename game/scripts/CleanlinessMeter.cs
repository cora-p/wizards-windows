using System;
using Godot;
#pragma warning disable CS0649, IDE0044

public class CleanlinessMeter : Node2D, Manager {

    FastNoiseLite noise;
    [Export]
    float frequency;

    Sprite background;
    Sprite foreground;

    Color grimeColor1, grimeColor2;

    Color[,] grimeNoiseValues;

    float lastPercentGrimy;

    int Width { get => grimeNoiseValues.GetLength(0); }
    int Height { get => grimeNoiseValues.GetLength(1); }

    public static CleanlinessMeter Instance {
        get; private set;
    }
    public override void _Ready() {
        lastPercentGrimy = 0f;
        Instance = this;
        background = GetNode<Sprite>("background");
        foreground = GetNode<Sprite>("foreground");
        noise = new FastNoiseLite();
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetFrequency(frequency);
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);

        grimeNoiseValues = new Color[(int)background.Scale.x, (int)background.Scale.y];

        Overseer.Instance.ReportReady(this);
    }

    public override void _Process(float delta) {
        if (GrimeManager.Instance.PercentGrimy != lastPercentGrimy) {
            lastPercentGrimy = GrimeManager.Instance.PercentGrimy;
            Update();
        }
    }

    public override void _Draw() {
        if (Overseer.Instance.HasCalledOnAllReady) {
            var grimeImage = new Image();
            grimeImage.Create(Width, Height, false, Image.Format.Rgba8);
            grimeImage.Fill(Colors.Transparent);
            grimeImage.Lock();
            var minXToDraw = (int)((1f - lastPercentGrimy) * Width);
            for (var x = minXToDraw; x < Width; x++) {
                for (var y = 0; y < Height; y++) {
                    grimeImage.SetPixel(x, y, grimeNoiseValues[x, y]);
                }
            }
            grimeImage.Unlock();
            var it = new ImageTexture();
            it.CreateFromImage(grimeImage);
            foreground.Texture = it;
        }
    }

    public void OnAllReady() {
        grimeColor1 = GrimeManager.Instance.GetRandomGrimeColor(Colors.Transparent);
        grimeColor2 = GrimeManager.Instance.GetRandomGrimeColor(grimeColor1);

        for (var x = 0; x < Width; x++) {
            for (var y = 0; y < Height; y++) {
                var t = (1 + noise.GetNoise(x, y)) / 2f;
                grimeNoiseValues[x, y] = ColorInterpolation.LerpHSV(grimeColor1, grimeColor2, t);
            }
        }
    }

    public bool Reset() => false;
    public PackedScene GetPackedScene() => null;
}
