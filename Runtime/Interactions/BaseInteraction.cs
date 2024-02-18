using UnityEngine;
using UnityEngine.Events;

namespace Ibralogue.Interactions
{
    /// <summary>
    /// The base class that is inherited over by other interactions. This class is not meant to directly be added to a GameObject.
    /// </summary>
    public abstract class BaseInteraction : MonoBehaviour
    {
        [SerializeField] protected DefaultDialogueManager dialogueManager;
        [SerializeField] protected DialogueAsset[] InteractionDialogues;

        [SerializeField] private UnityEvent OnDialogueStart;
        [SerializeField] private UnityEvent OnDialogueEnd;

        public virtual void StartDialogue()
        {
            AttachEvents();
        }

        private void AttachEvents()
        {
            dialogueManager.OnConversationStart = OnDialogueStart;
            dialogueManager.OnConversationEnd = OnDialogueEnd;
        }
    }
}