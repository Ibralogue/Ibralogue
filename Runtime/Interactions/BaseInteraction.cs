using UnityEngine;
using UnityEngine.Events;

namespace Ibralogue.Interactions
{
    /// <summary>
    /// The base class that is inherited over by other interactions. This class is not meant to directly be added to a GameObject.
    /// </summary>
    public abstract class BaseInteraction : MonoBehaviour
    {
        [SerializeField] protected SimpleDialogueManager dialogueManager;
        [SerializeField] protected DialogueAsset[] InteractionDialogues;

        [SerializeField] private UnityEvent OnConversationStart;
        [SerializeField] private UnityEvent OnConversationEnd;

        public virtual void StartDialogue()
        {
            AttachEvents();
        }

        private void AttachEvents()
        {
            dialogueManager.OnConversationStart = OnConversationStart;
            dialogueManager.OnConversationEnd = OnConversationEnd;
        }
    }
}