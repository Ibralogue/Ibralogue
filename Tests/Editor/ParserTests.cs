using Ibralogue.Parser;
using NUnit.Framework;
using UnityEngine;

namespace Ibralogue.Editor.Tests
{
    public class ParserTests
    {
        private DialogueAsset dialogueAsset;

        [SetUp]
        public void Setup()
        {
            dialogueAsset = ScriptableObject.CreateInstance<DialogueAsset>();
        }

        [TearDown]
        public void Teardown()
        {
            UnityEngine.Object.DestroyImmediate(dialogueAsset);
        }

        [Test]
        public void EmptyDialogue_Throws_Exception()
        {
            dialogueAsset.Content = "";

            TestDelegate action = () => DialogueParser.ParseDialogue(dialogueAsset);

            Assert.That(action, Throws.ArgumentException);
        }

        [Test]
        public void OneConversation_ParsesCorrectly()
        {
            dialogueAsset.Content =
                "{{DialogueName(Init)}}" +
                "\n[Foo]\nMessage\n" +
                "\n[Bar]\nOther message\n";

            var result = DialogueParser.ParseDialogue(dialogueAsset);

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Lines, Is.Not.Null);
            Assert.That(result[0].Lines[0].Speaker, Is.EqualTo("Foo"));
            Assert.That(result[0].Lines[1].Speaker, Is.EqualTo("Bar"));
            Assert.That(result[0].Lines[0].LineContent.Text, Is.EqualTo("Message\n"));
            Assert.That(result[0].Lines[1].LineContent.Text, Is.EqualTo("Other message\n"));
        }

        [Test]
        public void MultipleConversations_ParseCorrectly()
        {
            dialogueAsset.Content =
                "{{DialogueName(A)}}\n[Foo]\nMessage\n" +
                "{{DialogueName(B)}}\n[Bar]\nMessage\n";

            var result = DialogueParser.ParseDialogue(dialogueAsset);

            Assert.That(result, Has.Count.EqualTo(2));

            Assert.That(result[0].Lines, Has.Count.EqualTo(1));
            Assert.That(result[0].Lines[0].Speaker, Is.EqualTo("Foo"));
            Assert.That(result[0].Lines[0].LineContent.Text, Is.EqualTo("Message"));

            Assert.That(result[1].Lines, Has.Count.EqualTo(1));
            Assert.That(result[1].Lines[0].Speaker, Is.EqualTo("Bar"));
            Assert.That(result[1].Lines[0].LineContent.Text, Is.EqualTo("Message\n"));
        }

        [Test]
        public void Choices_ParseCorrectly()
        {
            dialogueAsset.Content =
                "{{DialogueName(A)}}\n[Foo]\nMessage\n" +
                "- Choice 1 -> Foo\n" +
                "- Choice 2 -> Bar\n";

            var result = DialogueParser.ParseDialogue(dialogueAsset);

            Assert.That(result, Has.Count.EqualTo(1));

            Assert.That(result[0].Choices, Has.Count.EqualTo(2));
        }
    }
}
