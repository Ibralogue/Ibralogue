using UnityEngine;

namespace Ibralogue.Examples
{
    public class DialogueInput : MonoBehaviour
    {
        [SerializeField] private DefaultDialogueManager dialogueManager;
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                dialogueManager.TryDisplayNextLine();
            if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Space))
                dialogueManager.SkipLine();
        }
    }
}
