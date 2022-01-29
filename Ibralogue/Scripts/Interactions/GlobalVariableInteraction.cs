using UnityEngine;

namespace Ibralogue.Interactions
{
    /// <summary>
    /// Adds a global variable key-value pair and plays the very first conversation in the interaction dialogue array.
    /// </summary>
    public class GlobalVariableInteraction : DefaultInteraction
    {
        [SerializeField] private string variableKey = "PLAYERNAME";
        [SerializeField] private string variableValue = "Ibrahim";
    
        private void Awake() =>
            DialogueManager.GlobalVariables.Add(variableKey, variableValue);

        protected override void StartDialogue()
        {
            DialogueManager.Instance.StartConversation(_interactionDialogues[0]);
        }
    }
}
