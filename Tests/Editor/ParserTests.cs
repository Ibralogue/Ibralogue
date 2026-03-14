using Ibralogue.Parser;
using System.Collections.Generic;
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
			VariableStore.ClearAll();
			DialogueGlobals.GlobalVariables.Clear();
		}

		[TearDown]
		public void Teardown()
		{
			Object.DestroyImmediate(dialogueAsset);
			VariableStore.ClearAll();
			DialogueGlobals.GlobalVariables.Clear();
		}

		private Line GetLine(Conversation conversation, int index)
		{
			List<RuntimeLine> lines = LineResolver.CollectLines(conversation.Content);
			LineResolver.Resolve(lines[index], null);
			return lines[index].Line;
		}

		private List<Choice> GetChoices(Conversation conversation)
		{
			RuntimeChoicePoint cp = LineResolver.FindChoicePoint(conversation.Content);
			if (cp == null) return new List<Choice>();
			return LineResolver.ResolveChoices(cp, null);
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
			Assert.That(GetLine(result[0], 0).Speaker, Is.EqualTo("Foo"));
			Assert.That(GetLine(result[0], 1).Speaker, Is.EqualTo("Bar"));
			Assert.That(GetLine(result[0], 0).LineContent.Text, Is.EqualTo("Message"));
			Assert.That(GetLine(result[0], 1).LineContent.Text, Is.EqualTo("Other message"));
		}

		[Test]
		public void MultipleConversations_ParseCorrectly()
		{
			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[Foo]\nMessage\n" +
				"{{ConversationName(B)}}\n[Bar]\nMessage\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Assert.That(result, Has.Count.EqualTo(2));

			Assert.That(LineResolver.CollectLines(result[0].Content), Has.Count.EqualTo(1));
			Assert.That(GetLine(result[0], 0).Speaker, Is.EqualTo("Foo"));
			Assert.That(GetLine(result[0], 0).LineContent.Text, Is.EqualTo("Message"));

			Assert.That(LineResolver.CollectLines(result[1].Content), Has.Count.EqualTo(1));
			Assert.That(GetLine(result[1], 0).Speaker, Is.EqualTo("Bar"));
			Assert.That(GetLine(result[1], 0).LineContent.Text, Is.EqualTo("Message"));
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
			Assert.That(GetChoices(result[0]), Has.Count.EqualTo(2));
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

			Assert.That(GetLine(result[0], 0).LineContent.Text, Is.EqualTo("First line.\nSecond line."));
		}

		[Test]
		public void Metadata_ParsesCorrectly()
		{
			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[Foo]\nHello ## mood:happy\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Assert.That(GetLine(result[0], 0).LineContent.Metadata, Contains.Key("mood"));
			Assert.That(GetLine(result[0], 0).LineContent.Metadata["mood"], Is.EqualTo("happy"));
		}

		[Test]
		public void InlineFunction_ParsesCorrectly()
		{
			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[NPC]\nToday is {{GetDay}}.\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Line line = GetLine(result[0], 0);
			Assert.That(line.LineContent.Invocations, Has.Count.EqualTo(1));
			Assert.That(line.LineContent.Invocations.Any(i => i.Name == "GetDay"));
			Assert.That(line.LineContent.Invocations[0].Arguments, Has.Count.EqualTo(0));
			Assert.That(line.LineContent.Text, Is.EqualTo("Today is ."));
		}

		[Test]
		public void InlineFunction_WithSingleArgument_ParsesCorrectly()
		{
			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[NPC]\nYou owe {{FormatGold(500)}} coins.\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Line line = GetLine(result[0], 0);
			var invocation = line.LineContent.Invocations[0];
			Assert.That(invocation.Name, Is.EqualTo("FormatGold"));
			Assert.That(invocation.Arguments, Has.Count.EqualTo(1));
			Assert.That(invocation.Arguments[0], Is.EqualTo("500"));
			Assert.That(line.LineContent.Text, Is.EqualTo("You owe  coins."));
		}

		[Test]
		public void InlineFunction_WithMultipleArguments_ParsesCorrectly()
		{
			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[NPC]\nThe answer is {{Add(3, 4)}}.\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Line line = GetLine(result[0], 0);
			var invocation = line.LineContent.Invocations[0];
			Assert.That(invocation.Name, Is.EqualTo("Add"));
			Assert.That(invocation.Arguments, Has.Count.EqualTo(2));
			Assert.That(invocation.Arguments[0], Is.EqualTo("3"));
			Assert.That(invocation.Arguments[1], Is.EqualTo("4"));
			Assert.That(line.LineContent.Text, Is.EqualTo("The answer is ."));
		}

		[Test]
		public void GlobalVariable_ReplacesCorrectly()
		{
			DialogueGlobals.GlobalVariables["PLAYERNAME"] = "TestPlayer";

			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[$PLAYERNAME]\nHello $PLAYERNAME.\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Line line = GetLine(result[0], 0);
			Assert.That(line.Speaker, Is.EqualTo("TestPlayer"));
			Assert.That(line.LineContent.Text, Is.EqualTo("Hello TestPlayer."));
		}

		[Test]
		public void Comment_IsIgnored()
		{
			dialogueAsset.Content =
				"{{ConversationName(A)}}\n# This is a comment\n[Foo]\nHello\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Assert.That(GetLine(result[0], 0).Speaker, Is.EqualTo("Foo"));
			Assert.That(GetLine(result[0], 0).LineContent.Text, Is.EqualTo("Hello"));
		}

		[Test]
		public void Jump_ParsesCorrectly()
		{
			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[Foo]\n{{Jump(B)}}\nHello\n" +
				"{{ConversationName(B)}}\n[Bar]\nWorld\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Assert.That(result, Has.Count.EqualTo(2));
			Assert.That(GetLine(result[0], 0).JumpTarget, Is.EqualTo("B"));
			Assert.That(GetLine(result[0], 0).LineContent.Text, Is.EqualTo("Hello"));
			Assert.That(GetLine(result[1], 0).JumpTarget, Is.Null);
		}

		[Test]
		public void Jump_AfterSentences_ParsesCorrectly()
		{
			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[Foo]\nHello\n{{Jump(B)}}\n" +
				"{{ConversationName(B)}}\n[Bar]\nWorld\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Assert.That(result, Has.Count.EqualTo(2));
			Assert.That(GetLine(result[0], 0).JumpTarget, Is.EqualTo("B"));
			Assert.That(GetLine(result[0], 0).LineContent.Text, Is.EqualTo("Hello"));
		}

		[Test]
		public void Choice_Metadata_ParsesCorrectly()
		{
			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[Foo]\nMessage\n" +
				"- Choice 1 -> Target ## tag:important\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			List<Choice> choices = GetChoices(result[0]);
			Assert.That(choices, Has.Count.EqualTo(1));
			Assert.That(choices[0].ChoiceName, Is.EqualTo("Choice 1"));
			Assert.That(choices[0].LeadingConversationName, Is.EqualTo("Target"));
			Assert.That(choices[0].Metadata, Contains.Key("tag"));
			Assert.That(choices[0].Metadata["tag"], Is.EqualTo("important"));
		}

		[Test]
		public void GlobalVariable_InFunctionArgument_ResolvesCorrectly()
		{
			DialogueGlobals.GlobalVariables["ITEM"] = "Sword";

			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[NPC]\nYou received {{GiveItem($ITEM)}}.\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Line line = GetLine(result[0], 0);
			var invocation = line.LineContent.Invocations[0];
			Assert.That(invocation.Name, Is.EqualTo("GiveItem"));
			Assert.That(invocation.Arguments, Has.Count.EqualTo(1));
			Assert.That(invocation.Arguments[0], Is.EqualTo("Sword"));
		}

		[Test]
		public void GlobalVariable_InFunctionArgument_WithMultipleArgs_ResolvesCorrectly()
		{
			DialogueGlobals.GlobalVariables["TARGET"] = "Dragon";
			DialogueGlobals.GlobalVariables["DMG"] = "50";

			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[NPC]\nYou hit {{DealDamage($TARGET, $DMG)}}!\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Line line = GetLine(result[0], 0);
			var invocation = line.LineContent.Invocations[0];
			Assert.That(invocation.Arguments, Has.Count.EqualTo(2));
			Assert.That(invocation.Arguments[0], Is.EqualTo("Dragon"));
			Assert.That(invocation.Arguments[1], Is.EqualTo("50"));
		}

		[Test]
		public void GlobalVariable_InMetadata_ResolvesCorrectly()
		{
			DialogueGlobals.GlobalVariables["MOOD"] = "angry";

			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[NPC]\nGet out of here! ## emotion:$MOOD\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Line line = GetLine(result[0], 0);
			Assert.That(line.LineContent.Metadata, Contains.Key("emotion"));
			Assert.That(line.LineContent.Metadata["emotion"], Is.EqualTo("angry"));
		}

		[Test]
		public void GlobalVariable_InChoiceMetadata_ResolvesCorrectly()
		{
			DialogueGlobals.GlobalVariables["QUESTID"] = "main01";

			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[NPC]\nWill you help?\n" +
				"- Sure -> Accept ## quest:$QUESTID\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			List<Choice> choices = GetChoices(result[0]);
			Assert.That(choices[0].Metadata, Contains.Key("quest"));
			Assert.That(choices[0].Metadata["quest"], Is.EqualTo("main01"));
		}

		[Test]
		public void EscapedDoubleBrace_InlineIsLiteralText()
		{
			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[Foo]\nThis is \\{{not a function}}.\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Line line = GetLine(result[0], 0);
			Assert.That(line.LineContent.Text, Is.EqualTo("This is {{not a function}}."));
			Assert.That(line.LineContent.Invocations, Has.Count.EqualTo(0));
		}

		[Test]
		public void EscapedDollar_InlineIsLiteralText()
		{
			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[Foo]\nPrice is \\$100.\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Assert.That(GetLine(result[0], 0).LineContent.Text, Is.EqualTo("Price is $100."));
		}

		[Test]
		public void EscapedHash_AtLineStartIsLiteralText()
		{
			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[Foo]\n\\# This is not a comment.\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Assert.That(GetLine(result[0], 0).LineContent.Text, Is.EqualTo("# This is not a comment."));
		}

		[Test]
		public void EscapedDoubleHash_InlineIsLiteralText()
		{
			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[Foo]\nSee section \\## for details.\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Line line = GetLine(result[0], 0);
			Assert.That(line.LineContent.Text, Is.EqualTo("See section ## for details."));
			Assert.That(line.LineContent.Metadata, Has.Count.EqualTo(0));
		}

		[Test]
		public void EscapedBracket_AtLineStartIsLiteralText()
		{
			dialogueAsset.Content =
				"{{ConversationName(A)}}\n[Foo]\n\\[Not a speaker]\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Assert.That(GetLine(result[0], 0).LineContent.Text, Is.EqualTo("[Not a speaker]"));
		}

		[Test]
		public void If_TrueBranch_ParsesCorrectly()
		{
			VariableStore.SetGlobal("HEALTH", 100.0);

			dialogueAsset.Content =
				"[Doctor]\n" +
				"{{If($HEALTH > 50)}}\n" +
				"[Doctor]\nYou look healthy!\n" +
				"{{Else}}\n" +
				"[Doctor]\nYou need rest.\n" +
				"{{EndIf}}\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Assert.That(result, Has.Count.EqualTo(1));
			Assert.That(result[0].Content, Has.Count.EqualTo(2));
			Assert.That(result[0].Content[1], Is.InstanceOf<RuntimeConditionalBlock>());

			var cond = (RuntimeConditionalBlock)result[0].Content[1];
			Assert.That(cond.Branches, Has.Count.EqualTo(2));
		}

		[Test]
		public void If_ElseIf_Else_ParsesCorrectly()
		{
			dialogueAsset.Content =
				"[NPC]\nLet me check...\n" +
				"{{If($RANK == \"gold\")}}\n" +
				"[NPC]\nWelcome, gold member!\n" +
				"{{ElseIf($RANK == \"silver\")}}\n" +
				"[NPC]\nWelcome, silver member!\n" +
				"{{Else}}\n" +
				"[NPC]\nWelcome, visitor.\n" +
				"{{EndIf}}\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			var cond = (RuntimeConditionalBlock)result[0].Content[1];
			Assert.That(cond.Branches, Has.Count.EqualTo(3));
			Assert.That(cond.Branches[2].Condition, Is.Null);
		}

		[Test]
		public void If_Nested_ParsesCorrectly()
		{
			VariableStore.SetGlobal("A", true);
			VariableStore.SetGlobal("B", true);

			dialogueAsset.Content =
				"[NPC]\nOuter\n" +
				"{{If($A)}}\n" +
				"[NPC]\nA is true\n" +
				"{{If($B)}}\n" +
				"[NPC]\nB is also true\n" +
				"{{EndIf}}\n" +
				"{{EndIf}}\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			var outerCond = (RuntimeConditionalBlock)result[0].Content[1];
			Assert.That(outerCond.Branches, Has.Count.EqualTo(1));

			var innerContent = outerCond.Branches[0].Body;
			Assert.That(innerContent[1], Is.InstanceOf<RuntimeConditionalBlock>());
		}

		[Test]
		public void Set_ParsesCorrectly()
		{
			dialogueAsset.Content =
				"{{Set($GOLD, 100)}}\n" +
				"[NPC]\nHello\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Assert.That(result[0].Content[0], Is.InstanceOf<RuntimeSetCommand>());
			var set = (RuntimeSetCommand)result[0].Content[0];
			Assert.That(set.VariableName, Is.EqualTo("GOLD"));
		}

		[Test]
		public void Global_ParsesCorrectly()
		{
			dialogueAsset.Content =
				"{{Global($SCORE, 0)}}\n" +
				"[NPC]\nHello\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Assert.That(result[0].Content[0], Is.InstanceOf<RuntimeGlobalDecl>());
			var global = (RuntimeGlobalDecl)result[0].Content[0];
			Assert.That(global.VariableName, Is.EqualTo("SCORE"));
		}

		[Test]
		public void Set_WithExpression_ParsesCorrectly()
		{
			dialogueAsset.Content =
				"{{Set($HEALTH, $HEALTH - 10)}}\n" +
				"[NPC]\nOuch!\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);

			Assert.That(result[0].Content[0], Is.InstanceOf<RuntimeSetCommand>());
		}

		[Test]
		public void VariableStore_GlobalScope_Works()
		{
			VariableStore.SetGlobal("NAME", "Alice");

			object value = VariableStore.Resolve(null, "NAME");
			Assert.That(value, Is.EqualTo("Alice"));
		}

		[Test]
		public void VariableStore_LocalScope_Works()
		{
			VariableStore.SetLocal("testAsset", "temp", 42.0);

			object value = VariableStore.Resolve("testAsset", "temp");
			Assert.That(value, Is.EqualTo(42.0));

			Assert.That(VariableStore.Resolve("otherAsset", "temp"), Is.Null);
		}

		[Test]
		public void VariableStore_LocalShadowsGlobal()
		{
			VariableStore.SetGlobal("X", "global");
			VariableStore.SetLocal("testAsset", "X", "local");

			Assert.That(VariableStore.Resolve("testAsset", "X"), Is.EqualTo("local"));
			Assert.That(VariableStore.Resolve(null, "X"), Is.EqualTo("global"));
		}

		[Test]
		public void VariableStore_LegacyDialogueGlobals_FallsThrough()
		{
			DialogueGlobals.GlobalVariables["LEGACY"] = "old_value";

			object value = VariableStore.Resolve(null, "LEGACY");
			Assert.That(value, Is.EqualTo("old_value"));
		}

		[Test]
		public void RuntimeVariableResolution_ReflectsCurrentState()
		{
			dialogueAsset.Content =
				"[NPC]\nHello $NAME!\n";

			var result = DialogueParser.ParseDialogue(dialogueAsset);
			var lines = LineResolver.CollectLines(result[0].Content);

			VariableStore.SetGlobal("NAME", "Alice");
			LineResolver.Resolve(lines[0], null);
			Assert.That(lines[0].Line.LineContent.Text, Is.EqualTo("Hello Alice!"));

			VariableStore.SetGlobal("NAME", "Bob");
			LineResolver.Resolve(lines[0], null);
			Assert.That(lines[0].Line.LineContent.Text, Is.EqualTo("Hello Bob!"));
		}
	}
}
