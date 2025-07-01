using Ibralogue.Parser;
using UnityEngine;
using UnityEngine.Events;

namespace Ibralogue.Interactions
{
    /// <summary>
    /// The base class that is inherited over by other interactions. This class is not meant to directly be added to a GameObject.
    /// </summary>
    public abstract class BaseInteraction : MonoBehaviour
    {
        [SerializeField] protected SimpleDialogueEngine dialogueManager;
        [SerializeField] protected DialogueAsset[] InteractionDialogues;

        [SerializeField] private UnityEvent OnConversationStart = new UnityEvent();
        [SerializeField] private UnityEvent OnConversationEnd = new UnityEvent();

        public virtual void StartDialogue()
        {
            AttachEvents();
        }

        private void AttachEvents()
        {
            dialogueManager.OnConversationStart.AddListener(OnConversationStart.Invoke);
            dialogueManager.OnConversationEnd.AddListener(OnConversationEnd.Invoke);
        }
    }
}