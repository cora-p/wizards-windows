using System;
using Godot;

public interface Manager {
	/// <summary>
	/// Called by ManagerManager when all Managers report ready.
	/// </summary>
	void OnAllReady();
}