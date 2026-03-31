using Ibralogue.Parser;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Ibralogue.Editor.Tests
{
	public class VariableStoreTests
	{
		[SetUp]
		public void Setup()
		{
			VariableStore.ClearAll();
			VisitTracker.Clear();
		}

		[TearDown]
		public void Teardown()
		{
			VariableStore.ClearAll();
			VisitTracker.Clear();
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

			System.Action<string, object, object> handler = (name, oldVal, newVal) =>
			{
				receivedName = name;
				receivedOld = oldVal;
				receivedNew = newVal;
			};

			VariableStore.OnVariableChanged += handler;
			VariableStore.SetGlobal("SCORE", 100.0);
			VariableStore.OnVariableChanged -= handler;

			Assert.That(receivedName, Is.EqualTo("SCORE"));
			Assert.That(receivedOld, Is.Null);
			Assert.That(receivedNew, Is.EqualTo(100.0));
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

		[Test]
		public void ImportState_ClearsExistingBeforeRestore()
		{
			VariableStore.SetGlobal("OLD", "value");

			VariableSnapshot snapshot = new VariableSnapshot();
			snapshot.Globals["NEW"] = "fresh";

			VariableStore.ImportState(snapshot);

			Assert.That(VariableStore.Resolve(null, "OLD"), Is.Null);
			Assert.That(VariableStore.Resolve(null, "NEW"), Is.EqualTo("fresh"));
		}

		// --- VariableStore read-only views ---

		[Test]
		public void Globals_ExposesReadOnlyView()
		{
			VariableStore.SetGlobal("SCORE", 42.0);

			Assert.That(VariableStore.Globals, Contains.Key("SCORE"));
			Assert.That(VariableStore.Globals["SCORE"], Is.EqualTo(42.0));
		}

		[Test]
		public void Locals_ExposesReadOnlyView()
		{
			VariableStore.SetLocal("testAsset", "TEMP", "hello");

			Assert.That(VariableStore.Locals, Contains.Key("testAsset"));
			Assert.That(VariableStore.Locals["testAsset"]["TEMP"], Is.EqualTo("hello"));
		}

		// --- Undefined variable resolution ---

		[Test]
		public void UndefinedVariable_ResolvesToEmptyInLineText()
		{
			var asset = ScriptableObject.CreateInstance<DialogueAsset>();
			asset.Content = "[NPC]\nHello $UNDEFINED!\n";

			var result = DialogueParser.ParseDialogue(asset);
			var lines = LineResolver.CollectLines(result[0].Content);
			Line resolved = LineResolver.Resolve(lines[0], null);

			Assert.That(resolved.LineContent.Text, Is.EqualTo("Hello !"));

			Object.DestroyImmediate(asset);
			DialogueParser.ClearCache();
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

		[Test]
		public void VisitTracker_VisitedKeys_ExposesReadOnlyView()
		{
			VisitTracker.Mark("Forest");
			VisitTracker.Mark("Tavern");

			Assert.That(VisitTracker.VisitedKeys, Has.Count.EqualTo(2));
			Assert.That(VisitTracker.VisitedKeys, Contains.Item("Forest"));
			Assert.That(VisitTracker.VisitedKeys, Contains.Item("Tavern"));
		}
	}
}
