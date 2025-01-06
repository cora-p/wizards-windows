public static class Layer {
    public const uint None = 0U;
    public const uint Shallow = 1U;
    public const uint Deep = 2U;
    public const uint Player = 4U;
    public const uint Rope = 8U;
    public const uint Brush = 16U;
    public const uint Grime = 32U;
    public const uint Mobs = 64U;
    public const uint VFX = 128U;
    public const uint PROGRESS_BLOCKER = 256U;
}

public static class ZIndices {
    public const int Background = -1000;
    public const int TowerBackdrop = -500;
    public const int Tower = -100;
    public const int SetDressing_back = -50;
    public const int SetDressing_mid = -45;
    public const int SetDressing_front = -40;
    public const int Windows = -10;
    public const int Grime = -5;
    public const int ShallowShadow = -1;
    public const int Shallow = 0;
    public const int Brush = 5;
    public const int Cleaner = 10;
    public const int Platform = 20;
    public const int Rope = 100;
    public const int Battlements = 150;
    public const int DeepShadow = 199;
    public const int Deep = 200;

    public const int Lights = 210;
    public const int Birds = 250;
    public const int HudBackground = 300;
    public const int HudMiddleground = 305;
    public const int HudForeground = 310;
    public const int HudFrame = 311;
    public const int WinCondition = 350;
    public const int MenuBackground = 400;
    public const int MenuForeground = 401;

    public const int Transition = 500;
}