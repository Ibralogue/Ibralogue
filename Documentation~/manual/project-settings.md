### Project Settings

An `IbralogueSettings` asset controls project-wide behavior for import validation, diagnostics, localization, and state management. These settings apply across the entire project, regardless of how many dialogue engines exist in your scenes.

#### Creating the Settings Asset

Open **Edit > Project Settings > Ibralogue**. The settings panel creates an `IbralogueSettings` asset in your `Resources` folder automatically on first visit.

You can also create one manually: **Assets > Create > ScriptableObject > IbralogueSettings**, then place it at `Assets/Resources/IbralogueSettings.asset`.

If no settings asset exists, Ibralogue uses sensible defaults for all values.

---

#### Import

Settings that control how `.ibra` files are processed when saved or imported into the project.

##### Validate On Import

**Type:** `bool`
**Default:** `true`

Runs the full parser pipeline (preprocessor, lexer, parser, analyzer) on every `.ibra` file at import time. Syntax errors and warnings appear immediately in the Unity console and are attached to the asset in the Inspector.

Disable this if validation causes noticeable import delays on very large projects with hundreds of dialogue files.

##### Diagnostic Strictness

**Type:** `DiagnosticStrictness`
**Default:** `Normal`

| Value | Behavior |
|-------|----------|
| `Normal` | Warnings are logged but do not block playback. |
| `Strict` | Warnings are promoted to errors during import validation. Use this for teams that want zero-tolerance for potential issues. |

---

#### Diagnostics

Settings that control Ibralogue's runtime console output.

##### Log Level

**Type:** `LogLevel`
**Default:** `WarningsAndErrors`

| Value | What is logged |
|-------|----------------|
| `ErrorsOnly` | Only errors. Warnings are suppressed. |
| `WarningsAndErrors` | Both warnings and errors. |
| `Verbose` | All messages including debug information. Useful during development. |

This affects all runtime logging from `DialogueLogger`. Import-time diagnostics are always shown regardless of this setting.

---

#### Localization

Settings related to language and localization.

##### Base Locale

**Type:** `string`
**Default:** `"en"`

An IETF BCP 47 language tag indicating the language your `.ibra` source files are written in. Used as the fallback when no localization provider is active.

Common values:

| Tag | Language |
|-----|----------|
| `en` | English |
| `en-US` | English (United States) |
| `de` | German |
| `fr` | French |
| `ja` | Japanese |
| `zh-Hans` | Chinese (Simplified) |
| `pt-BR` | Portuguese (Brazil) |

See the [Localization](localization.md) page for details on setting up translated dialogue.

---

#### State Management

Settings that control how Ibralogue's static state (variables, visit records, parse cache) behaves across scene transitions.

By default, all state persists across scene loads because `VariableStore`, `VisitTracker`, and the parser cache are static. These settings let you opt into automatic cleanup.

##### Clear Variables On Scene Load

**Type:** `bool`
**Default:** `false`

When enabled, all `VariableStore` variables (both global and local) are cleared automatically when a new scene loads. Enable this for projects where dialogue state should not carry over between scenes.

If you need finer control, call `VariableStore.ClearAll()` or `VariableStore.ClearLocals(assetName)` manually at the appropriate time instead.

##### Clear Visits On Scene Load

**Type:** `bool`
**Default:** `false`

When enabled, all `VisitTracker` records are cleared automatically when a new scene loads. This means `Visited("key")` will return `false` for all keys after a scene transition.

If you are using visits to track long-term progression (e.g., which NPCs the player has spoken to across the game), leave this disabled and manage visit state through `VisitTracker.ExportState()` / `ImportState()`.

##### Clear Parse Cache On Scene Load

**Type:** `bool`
**Default:** `false`

When enabled, the dialogue parser cache is invalidated on scene load, forcing `.ibra` files to be re-parsed the next time they are used.

This is only necessary if you modify `DialogueAsset.Content` at runtime and need each scene to start with a fresh parse. For most projects, leave this disabled.

---

#### Accessing Settings from Code

All settings are available through `IbralogueSettings.Instance`:

```cs
IbralogueSettings settings = IbralogueSettings.Instance;
if (settings != null)
{
    Debug.Log(settings.BaseLocale);
    Debug.Log(settings.LogLevel);
}
```

Static convenience accessors return safe defaults when no settings asset exists:

```cs
// These never throw, even without a settings asset
bool validate = IbralogueSettings.ShouldValidateOnImport;  // default: true
bool strict = IbralogueSettings.IsStrictMode;               // default: false
LogLevel level = IbralogueSettings.ActiveLogLevel;           // default: WarningsAndErrors
string locale = IbralogueSettings.ActiveBaseLocale;          // default: "en"
```
