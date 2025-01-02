public static class Layer {
    public const uint None = 0U;
    public const uint Shallow = 1U;
    public const uint Deep = 2U;
    public const uint Player = 3U;
    public const uint Rope = 4U;
    public const uint Brush = 5U;
    public const uint Grime = 6U;
    public const uint Mobs = 7U;
    public const uint VFX = 8U;
}

public static class ZIndices {
    public const int Background = -1000;
    public const int TowerBackdrop = -500;
    public const int Tower = -100;
    public const int Windows = -10;
    public const int Grime = -5;
    public const int ShallowShadow = -1;
    public const int Shallow = 0;
    public const int Brush = 5;
    public const int Cleaner = 10;
    public const int Platform = 20;
    public const int Rope = 100;
    public const int DeepShadow = 199;
    public const int Deep = 200;
    public const int HudBackground = 300;
    public const int HudMiddleground = 305;
    public const int HudForeground = 310;
    public const int HudFrame = 311;
    public const int WinCondition = 350;
    public const int MenuBackground = 400;
    public const int MenuForeground = 401;
}