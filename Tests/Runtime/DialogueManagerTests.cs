using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Ibralogue.Tests
{
    public abstract class DialogueManagerTestsBase
    {
        protected TestDialogueManager manager;
        protected TextMeshProUGUI nameText;
        protected TextMeshProUGUI sentenceText;
        protected Image speakerPortrait;
        protected Transform choiceButtonHolder;
        protected TestDialogueChoice choiceButton;

        [SetUp]
        public void BaseSetup()
        {
            var managerGO = new GameObject("Manager");
            managerGO.SetActive(false);

            manager = managerGO.AddComponent<TestDialogueManager>();
            nameText = new GameObject("Name Text").AddComponent<TextMeshProUGUI>();
            sentenceText = new GameObject("Sentence Text").AddComponent<TextMeshProUGUI>();
            speakerPortrait = new GameObject("Speaker Portrait").AddComponent<Image>();
            choiceButtonHolder = new GameObject("Choice Holder").transform;
            choiceButton = new GameObject("Choice Button").AddComponent<TestDialogueChoice>();

            manager.ScrollSpeed = 60;
            manager.NameText = nameText;
            manager.SentenceText = sentenceText;
            manager.SpeakerPortrait = speakerPortrait;
            manager.ChoiceButtonHolder = choiceButtonHolder;
            manager.ChoiceButton = choiceButton;

            managerGO.SetActive(true);
        }

        [TearDown]
        public void BaseTeardown()
        {
            Object.DestroyImmediate(manager.gameObject);
            Object.DestroyImmediate(nameText.gameObject);
            Object.DestroyImmediate(sentenceText.gameObject);
            Object.DestroyImmediate(speakerPortrait.gameObject);
            Object.DestroyImmediate(choiceButtonHolder.gameObject);
            Object.DestroyImmediate(choiceButton.gameObject);
        }
    }

    [TestFixture]
    public class SimpleDialogueTests : DialogueManagerTestsBase
    {
        private DialogueAsset dialogueAsset;

        [SetUp]
        public void Setup()
        {
            dialogueAsset = ScriptableObject.CreateInstance<DialogueAsset>();
            dialogueAsset.Content = 
                "{{DialogueName(Init)}}\n" +
                "[NPC]\n" +
                "Hello adventurer!\n" +
                "[Player]\n" +
                "How are you?\n";
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(dialogueAsset);
        }

        [UnityTest]
        public IEnumerator SimpleDialogue_Is_Shown()
        {
            manager.StartConversation(dialogueAsset);

            yield return null;

            Assert.That(nameText.text, Is.EqualTo("NPC"));
            Assert.That(sentenceText.text, Is.EqualTo("Hello adventurer!"));
        }
    }
}
