using UnityEngine;

namespace Ibralogue
{
    public class DialogueInteraction : MonoBehaviour
    {
        [SerializeField] private TextAsset[] _interactionDialogues;

        public virtual void StartDialogue() =>
            DialogueManager.Instance.StartConversation(_interactionDialogues[0]);
    }
}