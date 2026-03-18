using Ibralogue.Parser;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Ibralogue.Parser.Expressions;
using ExpressionEvaluator = Ibralogue.Parser.Expressions.ExpressionEvaluator;
using ExpressionParser = Ibralogue.Parser.Expressions.ExpressionParser;

namespace Ibralogue.Editor.Tests
{
	public class EngineOverhaulTests
	{
		private DialogueAsset dialogueAsset;

		[SetUp]
		public void Setup()
		{
			dialogueAsset = ScriptableObject.CreateInstance<DialogueAsset>();
			VariableStore.ClearAll();
			VisitTracker.Clear();
			DialogueParser.ClearCache();
		}

		[TearDown]
		public void Teardown()
		{
			Object.DestroyImmediate(dialogueAsset);
			VariableStore.ClearAll();
			VisitTracker.Clear();
			DialogueParser.ClearCache();
		}

		// --- VariableStore.IsDefined ---

		[Test]
		public void IsDefined_ReturnsTrueForNullValuedGlobal()
		{
			VariableStore.SetGlobal("EMPTY", null);

			Assert.That(VariableStore.IsDefined(null, "EMPTY"), Is.True);
		}

		[Test]
		public void IsDefined_ReturnsTrueForNullValuedLocal()
		{
			VariableStore.SetLocal("asset", "EMPTY", null);

			Assert.That(VariableStore.IsDefined("asset", "EMPTY"), Is.True);
		}

		[Test]
		public void IsDefined_ReturnsFalseForUndefinedVariable()
		{
			Assert.That(VariableStore.IsDefined(null, "NONEXISTENT"), Is.False);
		}

		// --- VariableStore.OnVariableChanged ---

		[Test]
		public void OnVariableChanged_FiresOnSetGlobal()
		{
			string receivedName = null;
			object receivedOld = "SENTINEL";
			object receivedNew = "SENTINEL";

			VariableStore.OnVariableChanged += (name, oldVal, newVal) =>
			{
				receivedName = name;
				receivedOld = oldVal;
				receivedNew = newVal;
			};

			VariableStore.SetGlobal("SCORE", 100.0);

			Assert.That(receivedName, Is.EqualTo("SCORE"));
			Assert.That(receivedOld, Is.Null);
			Assert.That(receivedNew, Is.EqualTo(100.0));

			// Clean up static event
			VariableStore.OnVariableChanged = null;
		}

		// --- VariableStore ExportState / ImportState ---

		[Test]
		public void ExportState_CapturesGlobalsAndLocals()
		{
			VariableStore.SetGlobal("NAME", "Alice");
			VariableStore.SetLocal("asset1", "TEMP", 42.0);

			VariableSnapshot snapshot = VariableStore.ExportState();

			Assert.That(snapshot.Globals, Contains.Key("NAME"));
			Assert.That(snapshot.Globals["NAME"], Is.EqualTo("Alice"));
			Assert.That(snapshot.Locals, Contains.Key("asset1"));
			Assert.That(snapshot.Locals["asset1"]["TEMP"], Is.EqualTo("42"));
		}

		[Test]
		public void ImportState_RestoresVariables()
		{
			VariableStore.SetGlobal("NAME", "Alice");
			VariableSnapshot snapshot = VariableStore.ExportState();

			VariableStore.ClearAll();
			Assert.That(VariableStore.Resolve(null, "NAME"), Is.Null);

			VariableStore.ImportState(snapshot);
			Assert.That(VariableStore.Resolve(null, "NAME"), Is.EqualTo("Alice"));
		}

		// --- DialogueParser caching ---

		[Test]
		public void ParseDialogue_ReturnsCachedResult()
		{
			dialogueAsset.Content = "[NPC]\nHello\n";

			var first = DialogueParser.ParseDialogue(dialogueAsset);
			var second = DialogueParser.ParseDialogue(dialogueAsset);

			Assert.That(second, Is.SameAs(first));
		}

		[Test]
		public void InvalidateCache_ForcesReparse()
		{
			dialogueAsset.Content = "[NPC]\nHello\n";

			var first = DialogueParser.ParseDialogue(dialogueAsset);
			DialogueParser.InvalidateCache(dialogueAsset);
			var second = DialogueParser.ParseDialogue(dialogueAsset);

			Assert.That(second, Is.Not.SameAs(first));
		}

		// --- VisitTracker ---

		[Test]
		public void VisitTracker_Mark_MakesKeyVisited()
		{
			Assert.That(VisitTracker.HasVisited("Tavern"), Is.False);

			VisitTracker.Mark("Tavern");

			Assert.That(VisitTracker.HasVisited("Tavern"), Is.True);
		}

		[Test]
		public void VisitTracker_Clear_RemovesAllRecords()
		{
			VisitTracker.Mark("A");
			VisitTracker.Mark("B");
			VisitTracker.Clear();

			Assert.That(VisitTracker.HasVisited("A"), Is.False);
			Assert.That(VisitTracker.HasVisited("B"), Is.False);
		}

		[Test]
		public void VisitTracker_ExportImport_RoundTrips()
		{
			VisitTracker.Mark("Tavern");
			VisitTracker.Mark("Forest");

			VisitSnapshot snapshot = VisitTracker.ExportState();
			VisitTracker.Clear();

			Assert.That(VisitTracker.HasVisited("Tavern"), Is.False);

			VisitTracker.ImportState(snapshot);

			Assert.That(VisitTracker.HasVisited("Tavern"), Is.True);
			Assert.That(VisitTracker.HasVisited("Forest"), Is.True);
		}

		// --- Expression function calls ---

		[Test]
		public void ExpressionParser_ParsesFunctionCallNoArgs()
		{
			var lexer = new ExpressionLexer("GetValue()");
			var tokens = lexer.Tokenize();
			var parser = new ExpressionParser(tokens);
			var node = parser.Parse();

			Assert.That(node, Is.InstanceOf<FunctionCallNode>());
			var call = (FunctionCallNode)node;
			Assert.That(call.Name, Is.EqualTo("GetValue"));
			Assert.That(call.Arguments, Has.Count.EqualTo(0));
		}

		[Test]
		public void ExpressionParser_ParsesFunctionCallWithStringArg()
		{
			var lexer = new ExpressionLexer("Visited(\"Tavern\")");
			var tokens = lexer.Tokenize();
			var parser = new ExpressionParser(tokens);
			var node = parser.Parse();

			Assert.That(node, Is.InstanceOf<FunctionCallNode>());
			var call = (FunctionCallNode)node;
			Assert.That(call.Name, Is.EqualTo("Visited"));
			Assert.That(call.Arguments, Has.Count.EqualTo(1));
			Assert.That(call.Arguments[0], Is.InstanceOf<LiteralNode>());
			Assert.That(((LiteralNode)call.Arguments[0]).Value, Is.EqualTo("Tavern"));
		}

		[Test]
		public void ExpressionParser_ParsesFunctionCallWithMultipleArgs()
		{
			var lexer = new ExpressionLexer("Clamp($X, 0, 100)");
			var tokens = lexer.Tokenize();
			var parser = new ExpressionParser(tokens);
			var node = parser.Parse();

			Assert.That(node, Is.InstanceOf<FunctionCallNode>());
			var call = (FunctionCallNode)node;
			Assert.That(call.Name, Is.EqualTo("Clamp"));
			Assert.That(call.Arguments, Has.Count.EqualTo(3));
		}

		[Test]
		public void ExpressionParser_FunctionCallInBinaryExpression()
		{
			var lexer = new ExpressionLexer("Visited(\"Tavern\") AND $GOLD > 50");
			var tokens = lexer.Tokenize();
			var parser = new ExpressionParser(tokens);
			var node = parser.Parse();

			Assert.That(node, Is.InstanceOf<BinaryNode>());
			var binary = (BinaryNode)node;
			Assert.That(binary.Left, Is.InstanceOf<FunctionCallNode>());
		}

		[Test]
		public void ExpressionEvaluator_CallsFunctionResolver()
		{
			var lexer = new ExpressionLexer("IsReady(\"test\")");
			var tokens = lexer.Tokenize();
			var parser = new ExpressionParser(tokens);
			var node = parser.Parse();

			var evaluator = new ExpressionEvaluator(
				name => null,
				(name, args) =>
				{
					if (name == "IsReady" && args.Length == 1 && (string)args[0] == "test")
						return true;
					return false;
				}
			);

			Assert.That(evaluator.EvaluateTruthy(node), Is.True);
		}

		[Test]
		public void ExpressionEvaluator_FunctionReturningFalse_IsFalsy()
		{
			var lexer = new ExpressionLexer("AlwaysFalse()");
			var tokens = lexer.Tokenize();
			var parser = new ExpressionParser(tokens);
			var node = parser.Parse();

			var evaluator = new ExpressionEvaluator(
				name => null,
				(name, args) => false
			);

			Assert.That(evaluator.EvaluateTruthy(node), Is.False);
		}

		[Test]
		public void ExpressionEvaluator_VariableWithoutDollarSign_StillResolvesAsVariable()
		{
			// Bare identifiers without parens should still resolve as variables
			// for backward compatibility
			var lexer = new ExpressionLexer("MYVAR");
			var tokens = lexer.Tokenize();
			var parser = new ExpressionParser(tokens);
			var node = parser.Parse();

			Assert.That(node, Is.InstanceOf<VariableNode>());

			var evaluator = new ExpressionEvaluator(
				name => name == "MYVAR" ? (object)true : null
			);

			Assert.That(evaluator.EvaluateTruthy(node), Is.True);
		}

		[Test]
		public void ExpressionEvaluator_NotFunctionCall_Works()
		{
			var lexer = new ExpressionLexer("NOT Visited(\"Tavern\")");
			var tokens = lexer.Tokenize();
			var parser = new ExpressionParser(tokens);
			var node = parser.Parse();

			var evaluator = new ExpressionEvaluator(
				name => null,
				(name, args) => false
			);

			Assert.That(evaluator.EvaluateTruthy(node), Is.True);
		}
	}
}
