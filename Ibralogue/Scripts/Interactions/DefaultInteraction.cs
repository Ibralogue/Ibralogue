using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

namespace Ibralogue.Interactions
{
    /// <summary>
    /// The base class that is inherited over by other interactions. This class is not meant to directly be added to a GameObject.
    /// </summary>
    public abstract class DefaultInteraction : MonoBehaviour
    {
        [SerializeField] protected TextAsset[] InteractionDialogues;

        [SerializeField] private UnityEvent _onDialogueStart;
        [SerializeField] private UnityEvent _onDialogueEnd;

        protected abstract void StartDialogue();
        
        private void AttachEvents()
        {
            DialogueManager.Instance.OnConversationStart = _onDialogueStart;
            DialogueManager.Instance.OnConversationEnd = _onDialogueEnd;
        }
    }
}