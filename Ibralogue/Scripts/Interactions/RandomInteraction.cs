using UnityEngine;

namespace Ibralogue.Interactions
{
  public class RandomInteraction : DefaultInteraction
  {
      protected override void StartDialogue()
      {
          base.StartDialogue();
            DialogueManager.Instance.StartConversation(
              InteractionDialogues[Random.Range(0, InteractionDialogues.Length)]);
      }
  }
}