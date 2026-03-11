using Ibralogue.Parser;
using System.Linq;
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
			Object.DestroyImmediate(dialogueAsset);
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
				"{{ConversationName(Init)}}" +
				"\n[Foo]\nMessage\n" +
				"\n[Bar]\nOther message\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Assert.That(result, Has.Count.EqualTo(1));
			Assert.That(result[0].Lines, Is.Not.Null);
			Assert.That(result[0].Lines[0].Speaker, Is.EqualTo("Foo"));
			Assert.That(result[0].Lines[1].Speaker, Is.EqualTo("Bar"));
			Assert.That(result[0].Lines[0].LineContent.Text, Is.EqualTo("Message"));
			Assert.That(result[0].Lines[1].LineContent.Text, Is.EqualTo("Other message"));
		}

		[Test]
		public void MultipleConversations_ParseCorrectly()
		{
			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[Foo]\nMessage\n" +
				"{{ConversationName(B)}}\n[Bar]\nMessage\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Assert.That(result, Has.Count.EqualTo(2));

			Assert.That(result[0].Lines, Has.Count.EqualTo(1));
			Assert.That(result[0].Lines[0].Speaker, Is.EqualTo("Foo"));
			Assert.That(result[0].Lines[0].LineContent.Text, Is.EqualTo("Message"));

			Assert.That(result[1].Lines, Has.Count.EqualTo(1));
			Assert.That(result[1].Lines[0].Speaker, Is.EqualTo("Bar"));
			Assert.That(result[1].Lines[0].LineContent.Text, Is.EqualTo("Message"));
		}

		[Test]
		public void Choices_ParseCorrectly()
		{
			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[Foo]\nMessage\n" +
				"- Choice 1 -> Foo\n" +
				"- Choice 2 -> Bar\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Assert.That(result, Has.Count.EqualTo(1));
			Assert.That(result[0].Choices, Has.Count.EqualTo(2));
		}

		[Test]
		public void ConversationName_SetsCorrectly()
		{
			dialogueAsset.Content =
				"{{ConversationName(MyConversation)}}\n[NPC]\nHello traveller.\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Assert.That(result, Has.Count.EqualTo(1));
			Assert.That(result[0].Name, Is.EqualTo("MyConversation"));
		}

		[Test]
		public void MultipleSentences_JoinWithNewlines()
		{
			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[Foo]\nFirst line.\nSecond line.\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Assert.That(result[0].Lines[0].LineContent.Text, Is.EqualTo("First line.\nSecond line."));
		}

		[Test]
		public void Metadata_ParsesCorrectly()
		{
			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[Foo]\nHello ## mood:happy\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Assert.That(result[0].Lines[0].LineContent.Metadata, Contains.Key("mood"));
			Assert.That(result[0].Lines[0].LineContent.Metadata["mood"], Is.EqualTo("happy"));
		}

		[Test]
		public void InlineFunction_ParsesCorrectly()
		{
			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[NPC]\nToday is {{GetDay}}.\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Assert.That(result[0].Lines[0].LineContent.Invocations, Has.Count.EqualTo(1));
			Assert.That(result[0].Lines[0].LineContent.Invocations.Any(i => i.Name == "GetDay"));
			Assert.That(result[0].Lines[0].LineContent.Text, Is.EqualTo("Today is ."));
		}

		[Test]
		public void GlobalVariable_ReplacesCorrectly()
		{
			DialogueGlobals.GlobalVariables["PLAYERNAME"] = "TestPlayer";

			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[$PLAYERNAME]\nHello $PLAYERNAME.\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Assert.That(result[0].Lines[0].Speaker, Is.EqualTo("TestPlayer"));
			Assert.That(result[0].Lines[0].LineContent.Text, Is.EqualTo("Hello TestPlayer."));

			DialogueGlobals.GlobalVariables.Remove("PLAYERNAME");
		}

		[Test]
		public void Comment_IsIgnored()
		{
			dialogueAsset.Content =
				"{{ConversationName(A)}}\n# This is a comment\n[Foo]\nHello\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Assert.That(result[0].Lines[0].Speaker, Is.EqualTo("Foo"));
			Assert.That(result[0].Lines[0].LineContent.Text, Is.EqualTo("Hello"));
		}

		[Test]
		public void Jump_ParsesCorrectly()
		{
			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[Foo]\n{{Jump(B)}}\nHello\n" +
				"{{ConversationName(B)}}\n[Bar]\nWorld\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Assert.That(result, Has.Count.EqualTo(2));
			Assert.That(result[0].Lines[0].JumpTarget, Is.EqualTo("B"));
			Assert.That(result[0].Lines[0].LineContent.Text, Is.EqualTo("Hello"));
			Assert.That(result[1].Lines[0].JumpTarget, Is.Null);
		}

		[Test]
		public void Jump_AfterSentences_ParsesCorrectly()
		{
			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[Foo]\nHello\n{{Jump(B)}}\n" +
				"{{ConversationName(B)}}\n[Bar]\nWorld\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Assert.That(result, Has.Count.EqualTo(2));
			Assert.That(result[0].Lines[0].JumpTarget, Is.EqualTo("B"));
			Assert.That(result[0].Lines[0].LineContent.Text, Is.EqualTo("Hello"));
		}

		[Test]
		public void Choice_Metadata_ParsesCorrectly()
		{
			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[Foo]\nMessage\n" +
				"- Choice 1 -> Target ## tag:important\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Assert.That(result[0].Choices, Has.Count.EqualTo(1));

			foreach (var kv in result[0].Choices)
			{
				Assert.That(kv.Key.ChoiceName, Is.EqualTo("Choice 1"));
				Assert.That(kv.Key.LeadingConversationName, Is.EqualTo("Target"));
				Assert.That(kv.Key.Metadata, Contains.Key("tag"));
				Assert.That(kv.Key.Metadata["tag"], Is.EqualTo("important"));
			}
		}
	}
}
