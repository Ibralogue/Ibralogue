### Global Variables

Global variables allow for predefined variables that can be used in dialogues for things like player names and other options that can be chosen or changed at runtime. They would be defined in the code like so:

```cs
private void Awake() =>
    DialogueManager.GlobalVariables.Add("PLAYERNAME","Ibrahim");
```

and would be declared in `.ibra` files like so:  

```text
[NPC]
Hi, $PLAYERNAME%.
[$PLAYERNAME%]
Hi. What's up?
```
