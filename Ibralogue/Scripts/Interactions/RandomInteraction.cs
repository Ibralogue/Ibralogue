using UnityEngine;

namespace Ibralogue.Interactions
{
  public class RandomInteraction : DefaultInteraction
  {
    public override void StartDialogue() => 
      DialogueManager.Instance.StartConversation(_interactionDialogues[Random.Range(0, _interactionDialogues.Length)]);
  }
}