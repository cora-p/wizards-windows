using System;
using Godot;

public interface Manager {
    /// <summary>
    /// Called by ManagerManager when all Managers report ready.
    /// </summary>
    void OnAllReady();

    /// <summary>
    /// Called by Overseer on Reset (for new level).
    /// </summary>
    /// <returns>true if the Manager should consider this manager dead</returns>
    bool Reset();

    /// <summary>
    /// Returns either null or a PackedScene, the later in cases where Overseer should instantiate it on reset.
    /// </summary>
    /// <returns>The PackedScene Overseer should instantiate.</returns>
    PackedScene GetPackedScene();
}