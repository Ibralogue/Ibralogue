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

        [SerializeField] private UnityEvent OnDialogueStart;
        [SerializeField] private UnityEvent OnDialogueEnd;

        protected virtual void StartDialogue()
        {
            AttachEvents();
        }

        protected void AttachEvents()
        {
            DialogueManager.Instance.OnConversationStart = OnDialogueStart;
            DialogueManager.Instance.OnConversationEnd = OnDialogueEnd;
        }
    }
}