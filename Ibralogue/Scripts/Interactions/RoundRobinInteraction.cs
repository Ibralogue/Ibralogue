using UnityEngine;

namespace Ibralogue.Interactions
{
    /// <summary>
    /// Loops through each dialogue one by one and stops at the last one unless "Loop" is checked in which case it starts from the first dialogue.
    /// </summary>
    public class RoundRobinInteraction : DefaultInteraction
    {
        private int _iteration;
        [SerializeField] private bool loop;
        
        public override void StartDialogue()
        {
            DialogueManager.Instance.StartConversation(_interactionDialogues[_iteration]);
            if (_iteration == _interactionDialogues.Length - 1)
            {
                _iteration = loop ? 0 : _iteration;
                return;
            }
            _iteration++;
        }
    }
}
