### Project Settings

Open **Edit > Project Settings > Ibralogue** to configure project-wide behavior. The settings asset is created automatically on first visit.

These settings apply across the entire project, regardless of how many dialogue engines exist in your scenes. If no settings asset exists, Ibralogue uses sensible defaults.

#### Import

| Setting | Default | Description |
|---------|---------|-------------|
| `Validate On Import` | On | Runs the parser on `.ibra` files at import time. Syntax errors appear immediately in the console. |
| `Diagnostic Strictness` | Normal | `Normal` logs warnings. `Strict` promotes warnings to errors during import. |

#### Diagnostics

| Setting | Default | Description |
|---------|---------|-------------|
| `Log Level` | Warnings And Errors | `Errors Only` suppresses warnings. `Verbose` includes debug info. |

#### Localization

| Setting | Default | Description |
|---------|---------|-------------|
| `Base Locale` | `en` | BCP 47 language tag for your source `.ibra` files. Used as fallback when no localization provider is active. |

#### State Management

By default, `VariableStore`, `VisitTracker`, and the parser cache persist across scene loads. These settings opt into automatic cleanup.

| Setting | Default | Description |
|---------|---------|-------------|
| `Clear Variables On Scene Load` | Off | Clears all `VariableStore` variables when a new scene loads. |
| `Clear Visits On Scene Load` | Off | Clears all `VisitTracker` records when a new scene loads. |
| `Clear Parse Cache On Scene Load` | Off | Invalidates the parser cache on scene load. Only needed if you modify dialogue assets at runtime. |

#### Accessing from Code

```cs
// Static accessors with safe defaults (never throw, even without a settings asset)
bool validate = IbralogueSettings.ShouldValidateOnImport;
bool strict = IbralogueSettings.IsStrictMode;
LogLevel level = IbralogueSettings.ActiveLogLevel;
string locale = IbralogueSettings.ActiveBaseLocale;
```
