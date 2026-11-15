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
        [SerializeField] protected DialogueEngineBase dialogueEngine;
        [SerializeField] protected DialogueAsset[] InteractionDialogues;

        [SerializeField] private UnityEvent OnConversationStart = new UnityEvent();
        [SerializeField] private UnityEvent OnConversationEnd = new UnityEvent();

        /// <summary>
        /// Picks one of the interaction's dialogues and starts it with <see cref="StartConversation"/>.
        /// </summary>
        public abstract void StartDialogue();

        public DialogueAsset GetDialogueAsset(int index)
        {
            return InteractionDialogues[index];
        }

        /// <summary>
        /// Starts a dialogue that belongs to this interaction: the interaction's
        /// start and end events fire for it and for no other.
        /// </summary>
        protected void StartConversation(DialogueAsset asset, int startIndex = 0)
        {
            dialogueEngine.StartConversation(asset, startIndex, HandleStarted, HandleEnded);
        }

        private void HandleStarted()
        {
            if (this != null)
                OnConversationStart.Invoke();
        }

        private void HandleEnded()
        {
            if (this != null)
                OnConversationEnd.Invoke();
        }
    }
}
