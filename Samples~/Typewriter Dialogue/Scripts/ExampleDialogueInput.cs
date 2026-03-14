using UnityEngine;

namespace Ibralogue.Examples
{
    public class ExampleDialogueInput : MonoBehaviour
    {
        [SerializeField] private SimpleDialogueEngine dialogueEngine;
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                dialogueEngine.TryDisplayNextLine();
            if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Space))
                dialogueEngine.TryDisplayNextLine();
        }
    }
}
