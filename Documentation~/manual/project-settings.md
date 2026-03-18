### Project Settings

Ibralogue has project-wide settings accessible from **Edit > Project Settings > Ibralogue**. Opening this panel for the first time creates an `IbralogueSettings` asset in your `Resources` folder automatically.

These settings apply across the entire project, regardless of how many dialogue engines exist in your scenes.

#### Import

| Setting | Default | Description |
|---------|---------|-------------|
| `Validate On Import` | On | Runs the parser on every `.ibra` file when it is saved or imported. Syntax errors and warnings appear immediately in the Unity console. Disable this if validation causes noticeable import delays on very large projects. |

#### Logging

| Setting | Default | Description |
|---------|---------|-------------|
| `Log Level` | Warnings And Errors | Controls which messages Ibralogue writes to the console at runtime. `Errors Only` suppresses warnings. `Verbose` includes additional debug information. |

#### Runtime

| Setting | Default | Description |
|---------|---------|-------------|
| `Clear Variables On Scene Load` | Off | When enabled, all `VariableStore` variables are cleared automatically when a new scene loads. Useful for projects where dialogue state should not persist across scenes. |
| `Clear Visits On Scene Load` | Off | When enabled, all `VisitTracker` records are cleared automatically when a new scene loads. |
