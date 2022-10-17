using UnityEngine;

namespace Ibralogue.Examples
{
    public class DialogueInput : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                DialogueManager.Instance.DisplayNextLine();
            if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Space))
                DialogueManager.Instance.DisplayNextLine();
        }
    }
}
