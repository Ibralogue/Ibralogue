## Quickstart

Add an empty GameObject with the component `DialogueManager` to your scene and assign its required references. 

Add a button, give it the `SingleInteraction` component, and assign its reference to the `DialogueManager`. 

Assign the button component's callback to `DialogueInteraction.StartDialogue`.

For the `Interaction Dialogue` reference, create a new Ibralogue file from the create menu like so:

![Image Showing The Create Menu in Unity](https://i.ibb.co/F6hcNJz/image.png)

...and then assign it to the `Interaction Dialogue` reference.

Open the Ibralogue file you have created and it should look something like this:

```text
{{DialogueName(Init)}}
[NPC]
Hello World!
{{DialogueEnd}}
```

*For some more information on the syntax of the dialogue files, take a look at the [Syntax Guide](syntax-guide.md).*
