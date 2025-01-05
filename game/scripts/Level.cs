using System;
using Godot;

public class Level : Resource {
    [Export]
    public string spell;
    [Export]
    public PackedScene scene;
}
