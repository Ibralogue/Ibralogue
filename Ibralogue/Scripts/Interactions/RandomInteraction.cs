using UnityEngine;

namespace Ibralogue.Interactions
{
    /// <summary>
    /// Plays a random dialogue from within the interaction dialogue array.
    /// </summary>
    public class RandomInteraction : DefaultInteraction
  {
      protected override void StartDialogue()
      {
          DialogueManager.Instance.StartConversation(
              InteractionDialogues[Random.Range(0, InteractionDialogues.Length)]);
      }
  }
}