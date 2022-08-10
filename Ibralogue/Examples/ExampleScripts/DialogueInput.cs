using UnityEngine;

namespace Ibralogue.Examples
{
    public class DialogueInput : MonoBehaviour
    {
        [SerializeField] private DialogueManager dialogueManager;
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                dialogueManager.DisplayNextLine();
            if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Space))
                dialogueManager.DisplayNextLine();
        }
    }
}
