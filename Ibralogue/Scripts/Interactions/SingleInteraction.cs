using Ibralogue.Interactions;
using UnityEngine;

public class SingleInteraction : BaseInteraction
{
    [SerializeField] private int index;
    
    public override void StartDialogue()
    {
        base.StartDialogue();
        dialogueManager.StartConversation(InteractionDialogues[index]);
    }
}