using Ibralogue.Parser;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ibralogue.Tests
{
    public class TestDialogueManager : DialogueManagerBase<TestDialogueChoice>
    {
        public float ScrollSpeed {  get => scrollSpeed; set => scrollSpeed = value; }
        public TextMeshProUGUI NameText { get => nameText; set => nameText = value; }
        public TextMeshProUGUI SentenceText { get => sentenceText; set => sentenceText = value; }
        public Image SpeakerPortrait { get => speakerPortrait; set => speakerPortrait = value; }
        public Transform ChoiceButtonHolder { get => choiceButtonHolder; set => choiceButtonHolder = value; }
        public TestDialogueChoice ChoiceButton { get => choiceButton; set => choiceButton = value; }


        protected override void PrepareChoiceButton(ChoiceButtonHandle handle, Choice choice)
        {
            handle.ChoiceButton.Name = choice.ChoiceName;
            handle.ChoiceButton.LeadingConversation = choice.LeadingConversationName;
            handle.ClickEvent = handle.ChoiceButton.Event;
        }
    }
}

