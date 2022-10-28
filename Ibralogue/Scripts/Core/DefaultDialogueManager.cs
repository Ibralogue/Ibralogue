using Ibralogue.Parser;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine.UI;

namespace Ibralogue
{

    public class DefaultDialogueManager : DialogueManagerBase<Button>
    {
        protected override void PrepareChoiceButton(ChoiceButtonHandle handle, Choice choice)
        {
            handle.ChoiceButton.GetComponentInChildren<TextMeshProUGUI>().text = choice.ChoiceName;
            handle.ClickEvent = handle.ChoiceButton.onClick;
        }
    }
}