### Localization

Ibralogue uses a string-table approach to localization. The `.ibra` file is the main reference point for dialogue structure and default-locale text. Translations live in separate CSV files that serve to modify only the displayable text.

#### How It Works

The dialogue file contains all structure (conversations, choices, conditionals, jumps) and the default text:

```text
{{ConversationName(Greeting)}}
[NPC]
Hello, traveller.
What brings you here?
- I'm fine -> Fine
- Not great -> NotGreat
```

A French translation CSV contains only the text, keyed by auto-generated IDs:

```csv
key,text
Greeting.line.0,"Bonjour, voyageur.\nQu'est-ce qui vous amène ici ?"
Greeting.choice.0,"Ça va"
Greeting.choice.1,"Pas super"
speaker.NPC,"PNJ"
```

At display time, the engine checks the active localization provider. If a translation exists for the current line's key, it replaces the default text. Variables (`$NAME`) and invocations (`{{GetDay}}`) in translated text are resolved the same way as in the original.

#### Localization Keys

Every dialogue line, choice, and speaker name is assigned a key automatically:

| Content | Key format | Example |
|---------|------------|---------|
| Dialogue line | `{ConversationName}.line.{index}` | `Greeting.line.0` |
| Choice text | `{ConversationName}.choice.{index}` | `Greeting.choice.0` |
| Speaker name | `speaker.{originalName}` | `speaker.NPC` |

To assign a custom key, use the `loc` metadata tag:

```text
[NPC]
Hello, traveller. ## loc:greeting_hello
```

The custom key `greeting_hello` is used instead of the auto-generated one.

#### Exporting a Template

Right-click any `.ibra` file in the Project window and select **Ibralogue > Export Localization Template**. This generates a CSV file alongside the asset containing all keys and their default text, ready to hand to translators.

#### Setting Up the CSV Provider

1. Place translation CSV files in a `Resources` folder, named `{AssetName}.{locale}`. For example, if your dialogue asset is called `MyDialogue`, the French translation file would be `MyDialogue.fr.csv` (imported as `MyDialogue.fr`).

2. Add a `CsvLocalizationProvider` component to your engine's GameObject.

3. Assign it to the engine's `Localization Provider Component` field.

4. Set the locale from code:

```cs
csvProvider.SetLocale("fr");
csvProvider.LoadTable("MyDialogue");
```

When no translation file exists for a locale, or when a key has no entry, the original text from the `.ibra` file is used as a fallback.

#### Locale Switching at Runtime

Changing the locale takes effect on the next line displayed. The current line finishes in the old locale, and the next line the player advances to uses the new one. No conversation restart is needed.

```cs
csvProvider.SetLocale("ja");
csvProvider.LoadTable("MyDialogue");
// Next line displayed will use Japanese translations
```

#### Custom Providers

To integrate with a different localization backend (Unity Localization, custom databases, etc.), implement the `ILocalizationProvider` interface:

```cs
using Ibralogue.Localization;

public class MyProvider : MonoBehaviour, ILocalizationProvider
{
    public string Resolve(string key)
    {
        // Look up the key in your localization system
        // Return null to fall back to the original text
        return MyLocalizationSystem.GetString(key);
    }
}
```

Assign the component to the engine's `Localization Provider Component` field, or set it from code:

```cs
engine.LocalizationProvider = myProvider;
```

#### Translated Text with Variables and Invocations

Translated strings can contain variable references and invocations. They are parsed and resolved at display time just like the original text:

```csv
key,text
Greeting.line.0,"Bonjour, $PLAYERNAME. Aujourd'hui c'est {{GetDay}}."
```
