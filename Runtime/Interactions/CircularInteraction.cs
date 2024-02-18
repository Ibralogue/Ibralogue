using UnityEngine;

namespace Ibralogue.Interactions
{
    /// <summary>
    /// Loops through each dialogue one by one.
    /// </summary>
    public class CircularInteraction : BaseInteraction
    {
        private int _iteration;

        /// <summary>
        /// If this is checked, the first conversation will start playing again after playing the last conversation when calling <see cref="StartDialogue"/>.
        /// </summary>
        [SerializeField] private bool _loop = true;

        public override void StartDialogue()
        {
            base.StartDialogue();
            dialogueManager.StartConversation(InteractionDialogues[_iteration]);
            if (_iteration == InteractionDialogues.Length - 1)
            {
                _iteration = _loop ? 0 : _iteration;
                return;
            }
            _iteration++;
        }
    }
}
