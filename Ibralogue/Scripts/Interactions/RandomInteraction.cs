using UnityEngine;

namespace Ibralogue.Interactions
{
    /// <summary>
    /// Plays a random dialogue from within the interaction dialogue array.
    /// </summary>
    public class RandomInteraction : BaseInteraction
  {
      public override void StartDialogue()
      {
          base.StartDialogue();
          dialogueManager.StartConversation(
              InteractionDialogues[Random.Range(0, InteractionDialogues.Length)]);
      }
  }
}