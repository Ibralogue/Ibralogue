using Ibralogue.Interactions;
using UnityEngine;

public class SingleInteraction : BaseInteraction
{
    [SerializeField] private int index;
    /// <summary>
    /// The index of the conversation you want to start.
    /// <remarks>One dialogue file can contain multiple conversations.</remarks>
    /// </summary>
    [SerializeField] private int conversationIndex;
    
    public override void StartDialogue()
    {
        base.StartDialogue();
        dialogueManager.StartConversation(InteractionDialogues[index], conversationIndex);
    }
}