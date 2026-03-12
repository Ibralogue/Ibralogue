## Quickstart

Add an empty GameObject with the `SimpleDialogueEngine` component to your scene. Assign a Dialogue View to its `Dialogue View` field (see [Dialogue Views](dialogue-views.md) for the available options).

Add a button, give it the `SingleInteraction` component, and assign its `Dialogue Engine` reference to your `SimpleDialogueEngine`.

Assign the button component's click callback to `SingleInteraction.StartDialogue`.

For the `Interaction Dialogues` reference, create a new Ibralogue file from the create menu like so:

![Image Showing The Create Menu in Unity](https://i.ibb.co/F6hcNJz/image.png)

...and then assign it to the `Interaction Dialogues` array.

Open the Ibralogue file you have created and it should look something like this:

```text
[NPC]
Hello World!
```

The square brackets define a speaker, and the lines that follow are what they say. Pressing the dialogue button will display "Hello World!" spoken by "NPC".

To add more lines to the conversation, just add another speaker tag:

```text
[NPC]
Hello World!
[NPC]
Welcome to Ibralogue.
```

Each `[Speaker]` block is a separate dialogue line. The player advances through them one at a time.

From here, you can explore the rest of the manual to learn about conversations, choices, variables, and more.
