# Guidelines Phase 1

Enthaltene Änderungen:

- Neue Library `Mockup.Guidelines` / Projekt `VIA.Mockup.Guidelines`
- Neue reine Berechnungslogik `AlignmentGuidelineManager`
- Keine WPF-Abhängigkeit
- Keine Designer-/ViewModel-Abhängigkeit
- Keine Collection-Mutation
- `Mockup/VIA.Mockup.csproj` um ProjectReference erweitert
- `VIA.UserFlow.sln` um neues Projekt erweitert
- Alte `MagnetAnchor.cs` und `MagnetLine.cs` müssen gelöscht werden

Noch nicht enthalten:

- Keine MouseHandler-Anbindung
- Kein Rendering blauer Hilfslinien
- Kein Snap auf MouseUp

Das folgt in Phase 2 nach erfolgreichem Build dieser Phase.
