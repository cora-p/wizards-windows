using System;
using System.Linq;
using Godot;
#pragma warning disable IDE0044,IDE0051,IDE0060
public class SpellListener : Node {
    string spell;
    [Export]
    float falloffTime;
    float timeTilNextFalloff;

    [Export]
    int maxSpellLength;

    string[] knownSpells;

    public override void _Ready() {
        spell = string.Empty;
        timeTilNextFalloff = falloffTime;
    }

    public override void _Process(float delta) {
        if (knownSpells == null) {
            knownSpells = LevelManager.Instance?.levels.Select((l) => l.spell.ToUpper()).ToArray();
        }
        if (spell.Length > 0) {
            timeTilNextFalloff -= delta;
            if (timeTilNextFalloff <= 0) {
                spell = spell.Substring(1);
                timeTilNextFalloff = falloffTime;
            }
        }
    }
    public override void _Input(InputEvent @event) {
        if (@event.IsPressed()) {
            var asText = @event.AsText();
            if (asText.Length == 1) {
                // ignore non-text keys, such as the arrow keys or PgDn.
                spell += asText.ToUpper();
                if (spell.Length > maxSpellLength) spell = spell.Substring(spell.Length - maxSpellLength);
                CheckIfValid();
            }
        }

    }

    void CheckIfValid() {
        for (var i = 0; i < knownSpells.Length; i++) {
            var s = knownSpells[i];
            if (spell.IndexOf(s) > -1) {
                spell = string.Empty;
                LevelManager.Instance.ChangeLevel(s);
            }
        }
    }
}
