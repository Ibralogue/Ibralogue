using UnityEngine;

namespace Ibralogue.Interactions
{
    /// <summary>
    /// Adds a global variable key-value pair and plays the very first conversation in the interaction dialogue array.
    /// </summary>
    public class GlobalVariableInteraction : DefaultInteraction
    {
        [SerializeField] private string _variableKey = "PLAYERNAME";
        [SerializeField] private string _variableValue = "Ibrahim";
    
        private void Awake() =>
            DialogueManager.GlobalVariables.Add(_variableKey, _variableValue);

        protected override void StartDialogue()
        {
            base.StartDialogue();
            DialogueManager.Instance.StartConversation(InteractionDialogues[0]);
        }
    }
}
