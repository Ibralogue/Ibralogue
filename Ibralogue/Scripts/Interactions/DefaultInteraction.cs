using System;
using Ibralogue;
using UnityEngine;

namespace Ibralogue.Interactions
{
    /// <summary>
    /// Plays the very first conversation in the interaction dialogue array.
    /// </summary>
    public class DefaultInteraction : MonoBehaviour
    {
        [SerializeField] protected TextAsset[] _interactionDialogues;

        public virtual void StartDialogue() =>
            DialogueManager.Instance.StartConversation(_interactionDialogues[0]);


    }
}