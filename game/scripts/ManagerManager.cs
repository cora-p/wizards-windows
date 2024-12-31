using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class ManagerManager : Node {

    [Export]
    /// <summary>
    /// A list of names of Managers that aren't ready yet. Values are removed as they report ready. 
    /// </summary>
    List<String> unreadyManagerNames;
    List<Manager> readyManagers = new List<Manager>();

    public bool HasCalledOnAllReady {
        get; private set;
    }

    static ManagerManager _Instance;
    public static ManagerManager Instance {
        get {
            return _Instance;
        }
        private set {
            _Instance = value;
        }
    }

    public override void _EnterTree() {
        GD.Randomize();
        Instance = this;
    }
    public override void _Process(float delta) {
        if (HasCalledOnAllReady) return;

        if (unreadyManagerNames.Count == 0) {
            HasCalledOnAllReady = true;
            GD.Print("All Managers reporting ready, invoking their OnAllReady methods.");
            foreach (var m in readyManagers) {
                m.OnAllReady();
            }
        }
    }

    /// <summary>
    /// Called from a Manager when it is ready.
    /// </summary>
    /// <param name="m">The implementation of Manager reporting ready.</param>
    /// <param name="managerName">The name of the Manager to be removed from unreadyManagerNames</param>
    public void ReportReady(Manager m) {
        var managerName = m.GetType().Name;
        if (unreadyManagerNames.Contains(managerName)) {
            unreadyManagerNames.Remove(managerName);
            readyManagers.Add(m);
        } else {
            throw new ArgumentException($"{managerName} isn't present in unready manager names: [{String.Join(",", unreadyManagerNames.ToArray())}]");
        }
    }
}
