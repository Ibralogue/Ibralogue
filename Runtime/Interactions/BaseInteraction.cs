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

        private bool _eventsAttached;

        public virtual void StartDialogue()
        {
            AttachEvents();
        }

        public DialogueAsset GetDialogueAsset(int index)
        {
            return InteractionDialogues[index];
        }

        private void AttachEvents()
        {
            if (_eventsAttached) return;

            dialogueEngine.OnConversationStart.AddListener(OnConversationStart.Invoke);
            dialogueEngine.OnConversationEnd.AddListener(OnConversationEnd.Invoke);
            _eventsAttached = true;
        }

        private void OnDestroy()
        {
            if (!_eventsAttached || dialogueEngine == null) return;

            dialogueEngine.OnConversationStart.RemoveListener(OnConversationStart.Invoke);
            dialogueEngine.OnConversationEnd.RemoveListener(OnConversationEnd.Invoke);
            _eventsAttached = false;
        }
    }
}