using UnityEngine;

namespace Ibralogue.Examples
{
    public class ExampleDialogueInput : MonoBehaviour
    {
        [SerializeField] private SimpleDialogueManager dialogueManager;
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                dialogueManager.TryDisplayNextLine();
            if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Space))
                dialogueManager.TryDisplayNextLine();
        }
    }
}
