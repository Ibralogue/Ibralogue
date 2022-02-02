### Invocations

Invocations are a very powerful feature in Ibralogue. They allow you to invoke static functions directly from Ibralogue, and also to define character portraits. they are enclosed like so: `<<Foo>>`. The type of the invocation and name are separated between colons. If the type of an invocation is not supplied, it will automatically be inferred as a function invocation.

#### Function Invocation

Static functions can be invoked directly from Ibralogue. To set up a static function to be recognized by Ibralogue, add the `DialogueFunction` attribute like so:

```cs
[DialogueFunction]
public static void Die() 
{
    Debug.Log("Dead.");
}
```

...and invoking the function is extremely simple! It would look something like this:

```text
[NPC]
Time To Die.
<<Die>>
```

#### Image Invocation

Character portraits are really important in a lot of story games, and Ibralogue cant forget them. Ibralogue makes use of [Resources.Load](https://docs.unity3d.com/ScriptReference/Resources.Load.html) to load images directly from dialogue files.

To use Image invocations, make a Resources folder anywhere in your project and then specify directories relative from that folder in the dialogue file. For example:

- With a file located in `./Assets/Sprites/Resources/CharacterPortraits/AvaSmiling.png`, do:

```text
[Ava]
<<Image: CharacterPortraits/AvaSmiling.png>>
It's a beautiful day outside!
```

#### Choice Invocations

- `<<DialogueName: Foo>>` allows you to specify the name of a given `Conversation`. This is required for branching dialogue so the interpreter knows what conversation to branch to.

- `<<DialogueEnd>>` is a reserved invocation to signify the end of a given `Conversation`. Ibralogue files can have multiple conversations, so as to conveniently enable branching between them, and the `<<DialogueEnd>>` keyword is required after a conversation has ended so the interpreter can tell two conversations apart.

 For example:

```text
<<DialogueName: Initial>>
[NPC]
Time To Die
- No -> Denial
- Sure -> Acceptance
<<DialogueEnd>>


<<DialogueName: Denial>>
[Player]
Did you really think...
I came this far...
[Player]
To give up now? HAHAHAHA
<<DialogueEnd>>


<<DialogueName: Acceptance>>
[Player]
Y'know what?
[Player]
Maybe you're right... I am tired of life.
<<DialogueEnd>>
```
