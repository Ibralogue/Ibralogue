using UnityEngine;

namespace Ibralogue.Examples
{
    public class DialogueInput : MonoBehaviour
    {
        [SerializeField] private DialogueManager dialogueManager;
        private void Update()
        {
            //Preferably, I would have used the new Input System for this, but I didn't want to clutter the examples with
            //dependencies any more than they needed to be.
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                dialogueManager.DisplayNextLine();
            if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Space))
                dialogueManager.DisplayNextLine();
        }
    }
}
