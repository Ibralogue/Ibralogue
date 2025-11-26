using Ibralogue.Localization;
using Ibralogue.Parser;
using UnityEngine;
using UnityEngine.Events;

namespace Ibralogue.Interactions
{
    /// <summary>
    /// The base class that is inherited over by other interactions. This class is not meant to directly be added to a GameObject.
    /// </summary>
    public abstract class LocalizedBaseInteraction : MonoBehaviour
    {
        [SerializeField] protected SimpleDialogueEngine dialogueEngine;
        [SerializeField] protected LocalizedDialogueAsset[] InteractionDialogues;

        [SerializeField] private UnityEvent OnConversationStart = new UnityEvent();
        [SerializeField] private UnityEvent OnConversationEnd = new UnityEvent();

        public virtual void StartDialogue()
        {
            AttachEvents();
        }

        public DialogueAsset GetDialogueAsset(int index)
        {
            return InteractionDialogues[index].LoadAsset();
        }

        private void AttachEvents()
        {
            dialogueEngine.OnConversationStart.AddListener(OnConversationStart.Invoke);
            dialogueEngine.OnConversationEnd.AddListener(OnConversationEnd.Invoke);
        }

        void OnEnable()
        {
            foreach (LocalizedDialogueAsset dialogueAsset in InteractionDialogues)
            {
                dialogueAsset.AssetChanged += OnAssetChanged;
            }
        }

        void OnDisable()
        {
            foreach (LocalizedDialogueAsset dialogueAsset in InteractionDialogues)
            {
                dialogueAsset.AssetChanged -= OnAssetChanged;
            }
        }

        void OnAssetChanged(DialogueAsset asset)
        {
            dialogueEngine.StopConversation();
        }
    }
}