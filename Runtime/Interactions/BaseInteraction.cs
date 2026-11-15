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

        /// <summary>
        /// Carries on a dialogue of this interaction that <see cref="OnInterrupted"/> kept.
        /// It is the same dialogue to the interaction's listeners, so the start event
        /// does not fire again.
        /// </summary>
        protected void ContinueConversation(DialogueAsset asset, ConversationProgress progress)
        {
            dialogueEngine.ResumeFromProgress(asset, progress, null, HandleEnded);
        }

        /// <summary>
        /// Called when another dialogue starts over this interaction's dialogue.
        /// Return true to keep it for <see cref="ContinueConversation"/>: it has not
        /// ended, so neither <see cref="OnInteractionEnded"/> nor the end event fire.
        /// </summary>
        protected virtual bool OnInterrupted(ConversationProgress progress)
        {
            return false;
        }

        /// <summary>
        /// Called when this interaction's dialogue is over, just before its end event.
        /// </summary>
        protected virtual void OnInteractionEnded()
        {
        }

        private void HandleStarted()
        {
            if (this != null)
                OnConversationStart.Invoke();
        }

        private void HandleEnded()
        {
            if (this == null)
                return;

            ConversationProgress interrupted = dialogueEngine.InterruptedProgress;
            if (interrupted != null && OnInterrupted(interrupted))
                return;

            OnInteractionEnded();
            OnConversationEnd.Invoke();
        }
    }
}
