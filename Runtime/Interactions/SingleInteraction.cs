using UnityEngine;

namespace Ibralogue.Interactions
{
    public class SingleInteraction : BaseInteraction
    {
        [SerializeField] private int index;

        public override void StartDialogue()
        {
            base.StartDialogue();
            dialogueManager.StartConversation(InteractionDialogues[index]);
        }
    }
}