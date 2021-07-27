using System;
using TMPro;
using UnityEngine;

namespace Ibralogue
{
    public class DialogueInteraction : MonoBehaviour
    {
        [SerializeField] private TextAsset _interactionDialogue;

        public void StartDialogue() =>
            DialogueManager.Instance.StartConversation(_interactionDialogue);
    }
}
