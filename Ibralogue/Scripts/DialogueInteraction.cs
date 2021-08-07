using UnityEngine;

namespace Ibralogue
{
    public class DialogueInteraction : MonoBehaviour
    {
        [SerializeField] private TextAsset _interactionDialogue;
        [SerializeField] private string playerName = "Joe";

        private void Awake() =>
            DialogueManager.GlobalVariables.Add("PLAYERNAME", playerName);


        public void StartDialogue() =>
            DialogueManager.Instance.StartConversation(_interactionDialogue);
    }
}