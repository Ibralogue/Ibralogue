using Ibralogue;
using UnityEngine;

public class RandomInteraction : DialogueInteraction
{
  public override void StartDialogue()
  {
    DialogueManager.Instance.StartConversation(_interactionDialogues[Random.Range(0, _interactionDialogues.Length)]);
  }
}