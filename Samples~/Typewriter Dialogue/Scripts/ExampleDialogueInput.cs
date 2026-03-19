using UnityEngine;

namespace Ibralogue.Examples
{
    public class ExampleDialogueInput : MonoBehaviour
    {
        [SerializeField] private DialogueEngineBase dialogueEngine;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)
                || Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Space))
                dialogueEngine.Advance();
        }
    }
}
