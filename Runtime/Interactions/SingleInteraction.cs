using UnityEngine;

namespace Ibralogue.Interactions
{
    /// <summary>
    /// Place the first conversation of a Dialogue file.
    /// </summary>
    public class SingleInteraction : BaseInteraction
    {
        /// <summary>
        /// The index of the Dialogue file to use from the array.
        /// </summary>
        [SerializeField] private int index;

        public override void StartDialogue()
        {
            base.StartDialogue();
            dialogueManager.StartConversation(InteractionDialogues[index]);
        }
    }
}