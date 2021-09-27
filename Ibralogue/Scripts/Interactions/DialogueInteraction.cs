using System;
using UnityEngine;

namespace Ibralogue
{
    public class DialogueInteraction : MonoBehaviour
    {
        [SerializeField] protected TextAsset[] _interactionDialogues;
        [SerializeField] private string playerName = "Ibrahim";

        private void Awake() =>
            DialogueManager.GlobalVariables.Add("PLAYERNAME", playerName);

        public virtual void StartDialogue() =>
            DialogueManager.Instance.StartConversation(_interactionDialogues[0]);

        [DialogueFunction]
        public static void How()
        {
            Debug.Log("Function Trigger");
        }
    }
}