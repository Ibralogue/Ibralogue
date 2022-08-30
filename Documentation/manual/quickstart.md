## Quickstart

* Add an empty GameObject with the component `DialogueManager` to your scene and assign its required references.
* Add a button and give it the `SingleInteraction` component and assign the button's callback to `DialogueInteraction.StartDialogue`

For the `Interaction Dialogue` reference, create a new Ibralogue file from the create menu like so:
![Image Showing The Create Menu in Unity](https://i.ibb.co/F6hcNJz/image.png)

and then assign it to the `Interaction Dialogue` reference.

Open the Ibralogue file you have created and it should look something like this:

```text
[NPC]
Hello World!
```

This is essentially the `Hello World!` equivalent of Ibralogue. For more information on the syntax of how the dialogue files work,take a look at the [Syntax Guide](syntax-guide.md).
