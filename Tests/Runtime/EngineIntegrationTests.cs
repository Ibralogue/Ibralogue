using System.Collections;
using System.Collections.Generic;
using Ibralogue.Parser;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ibralogue.Tests
{
    public class EngineIntegrationTests
    {
        private GameObject _go;
        private SimpleDialogueEngine _engine;
        private DialogueAsset _asset;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestEngine");
            _engine = _go.AddComponent<SimpleDialogueEngine>();
            _asset = ScriptableObject.CreateInstance<DialogueAsset>();
            VariableStore.ClearAll();
            VisitTracker.Clear();
            DialogueParser.ClearCache();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.Destroy(_go);
            if (_asset != null) Object.DestroyImmediate(_asset);
            VariableStore.ClearAll();
            VisitTracker.Clear();
            DialogueParser.ClearCache();
        }

        // --- Lifecycle ---

        [UnityTest]
        public IEnumerator StartConversation_FiresStartEvent()
        {
            _asset.Content = "[NPC]\nHello\n";
            bool started = false;
            _engine.OnConversationStart.AddListener(() => started = true);

            _engine.StartConversation(_asset);
            yield return null;

            Assert.That(started, Is.True);
        }

        [UnityTest]
        public IEnumerator Conversation_LinesFireInOrder_ThenEnds()
        {
            _asset.Content = "[NPC]\nFirst line\n[NPC]\nSecond line\n";
            List<string> lines = new List<string>();
            bool ended = false;

            _engine.OnLineDisplayed.AddListener(line => lines.Add(line.LineContent.Text));
            _engine.OnConversationEnd.AddListener(() => ended = true);

            _engine.StartConversation(_asset);
            yield return null;

            Assert.That(lines, Has.Count.EqualTo(1));
            Assert.That(lines[0], Is.EqualTo("First line"));

            _engine.Next();
            yield return null;

            Assert.That(lines, Has.Count.EqualTo(2));
            Assert.That(lines[1], Is.EqualTo("Second line"));

            _engine.Next();
            yield return null;

            Assert.That(ended, Is.True);
            Assert.That(_engine.IsConversationActive, Is.False);
        }

        [UnityTest]
        public IEnumerator StartConversation_WithNullAsset_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                _engine.StartConversation(null));
            yield return null;
        }

        [UnityTest]
        public IEnumerator StartConversation_WithStartIndex_SelectsConversation()
        {
            _asset.Content =
                "{{ConversationName(First)}}\n[NPC]\nFirst conversation\n\n" +
                "{{ConversationName(Second)}}\n[NPC]\nSecond conversation\n";

            string text = null;
            _engine.OnLineDisplayed.AddListener(line => text = line.LineContent.Text);

            _engine.StartConversation(_asset, 1);
            yield return null;

            Assert.That(text, Is.EqualTo("Second conversation"));
        }

        // --- History ---

        [UnityTest]
        public IEnumerator History_TracksDisplayedLines()
        {
            _asset.Content = "[NPC]\nLine A\n[NPC]\nLine B\n";

            _engine.StartConversation(_asset);
            yield return null;

            Assert.That(_engine.History, Has.Count.EqualTo(1));

            _engine.Next();
            yield return null;

            Assert.That(_engine.History, Has.Count.EqualTo(2));
            Assert.That(_engine.History[0].LineContent.Text, Is.EqualTo("Line A"));
            Assert.That(_engine.History[1].LineContent.Text, Is.EqualTo("Line B"));
        }

        [UnityTest]
        public IEnumerator History_ClearedOnStop_UnlessPersisted()
        {
            _asset.Content = "[NPC]\nHello\n";

            _engine.StartConversation(_asset);
            yield return null;

            Assert.That(_engine.History, Has.Count.EqualTo(1));

            _engine.Next();
            yield return null;

            Assert.That(_engine.History, Has.Count.EqualTo(0));

            // With PersistHistory
            _engine.PersistHistory = true;
            _engine.StartConversation(_asset);
            yield return null;

            _engine.Next();
            yield return null;

            Assert.That(_engine.History, Has.Count.EqualTo(1));
            _engine.PersistHistory = false;
        }

        // --- Choices ---

        [UnityTest]
        public IEnumerator Choices_Presented_SelectChoice_BranchesCorrectly()
        {
            _asset.Content =
                "[NPC]\nPick one\n- Option A -> BranchA\n- Option B -> BranchB\n\n" +
                "{{ConversationName(BranchA)}}\n[NPC]\nYou picked A\n\n" +
                "{{ConversationName(BranchB)}}\n[NPC]\nYou picked B\n";

            List<Choice> presentedChoices = null;
            Choice selectedChoice = null;
            List<string> lines = new List<string>();

            _engine.OnChoicesPresented.AddListener(choices => presentedChoices = choices);
            _engine.OnChoiceSelected.AddListener(c => selectedChoice = c);
            _engine.OnLineDisplayed.AddListener(line => lines.Add(line.LineContent.Text));

            _engine.StartConversation(_asset);
            yield return null;

            Assert.That(lines, Has.Count.EqualTo(1));
            Assert.That(lines[0], Is.EqualTo("Pick one"));
            Assert.That(presentedChoices, Is.Not.Null);
            Assert.That(presentedChoices, Has.Count.EqualTo(2));

            _engine.SelectChoice(presentedChoices[1]);
            yield return null;

            Assert.That(selectedChoice, Is.Not.Null);
            Assert.That(selectedChoice.ChoiceName, Is.EqualTo("Option B"));
            Assert.That(lines, Has.Count.EqualTo(2));
            Assert.That(lines[1], Is.EqualTo("You picked B"));
        }

        [UnityTest]
        public IEnumerator ContinueChoice_ContinuesSameConversation()
        {
            _asset.Content =
                "[NPC]\nFirst question\n" +
                "- Continue -> >>\n" +
                "[NPC]\nAfter continue\n";

            List<Choice> presentedChoices = null;
            List<string> lines = new List<string>();

            _engine.OnChoicesPresented.AddListener(c => presentedChoices = c);
            _engine.OnLineDisplayed.AddListener(line => lines.Add(line.LineContent.Text));

            _engine.StartConversation(_asset);
            yield return null;

            Assert.That(lines[0], Is.EqualTo("First question"));
            Assert.That(presentedChoices, Has.Count.EqualTo(1));
            Assert.That(presentedChoices[0].LeadingConversationName, Is.EqualTo(">>"));

            _engine.SelectChoice(presentedChoices[0]);
            yield return null;

            Assert.That(lines, Has.Count.EqualTo(2));
            Assert.That(lines[1], Is.EqualTo("After continue"));
        }

        // --- ChoiceFilter ---

        [UnityTest]
        public IEnumerator ChoiceFilter_RemovesChoices()
        {
            _asset.Content =
                "[NPC]\nChoose\n- Keep -> A\n- Remove -> B\n\n" +
                "{{ConversationName(A)}}\n[NPC]\nKept\n\n" +
                "{{ConversationName(B)}}\n[NPC]\nRemoved\n";

            List<Choice> presented = null;
            _engine.OnChoicesPresented.AddListener(c => presented = c);

            _engine.ChoiceFilter = choices =>
                choices.FindAll(c => c.ChoiceName != "Remove");

            _engine.StartConversation(_asset);
            yield return null;

            Assert.That(presented, Is.Not.Null);
            Assert.That(presented, Has.Count.EqualTo(1));
            Assert.That(presented[0].ChoiceName, Is.EqualTo("Keep"));
        }

        // --- Jump targets ---

        [UnityTest]
        public IEnumerator JumpTarget_ResolvesCorrectly()
        {
            _asset.Content =
                "[NPC]\n{{Jump(Target)}}\nBefore jump\n\n" +
                "{{ConversationName(Target)}}\n[NPC]\nAfter jump\n";

            List<string> lines = new List<string>();
            _engine.OnLineDisplayed.AddListener(line => lines.Add(line.LineContent.Text));

            _engine.StartConversation(_asset);
            yield return null;

            Assert.That(lines[0], Is.EqualTo("Before jump"));

            _engine.Next();
            yield return null;

            Assert.That(lines, Has.Count.EqualTo(2));
            Assert.That(lines[1], Is.EqualTo("After jump"));
        }

        // --- Conditionals ---

        [UnityTest]
        public IEnumerator Conditional_EvaluatesAndBranches()
        {
            _asset.Content =
                "{{Global($FLAG, true)}}\n" +
                "{{If($FLAG)}}\n[NPC]\nFlag is set\n{{Else}}\n[NPC]\nFlag is not set\n{{EndIf}}\n";

            List<string> lines = new List<string>();
            _engine.OnLineDisplayed.AddListener(line => lines.Add(line.LineContent.Text));

            _engine.StartConversation(_asset);
            yield return null;

            Assert.That(lines, Has.Count.EqualTo(1));
            Assert.That(lines[0], Is.EqualTo("Flag is set"));
        }

        [UnityTest]
        public IEnumerator Conditional_ElseBranch_WhenFalse()
        {
            _asset.Content =
                "{{Global($FLAG, false)}}\n" +
                "{{If($FLAG)}}\n[NPC]\nFlag is set\n{{Else}}\n[NPC]\nFlag is not set\n{{EndIf}}\n";

            List<string> lines = new List<string>();
            _engine.OnLineDisplayed.AddListener(line => lines.Add(line.LineContent.Text));

            _engine.StartConversation(_asset);
            yield return null;

            Assert.That(lines, Has.Count.EqualTo(1));
            Assert.That(lines[0], Is.EqualTo("Flag is not set"));
        }

        // --- Variables ---

        [UnityTest]
        public IEnumerator SetCommand_UpdatesVariableStore()
        {
            _asset.Content =
                "{{Set($SCORE, 42)}}\n" +
                "[NPC]\nDone\n";

            _engine.StartConversation(_asset);
            yield return null;

            object value = VariableStore.Resolve(_asset.name, "SCORE");
            Assert.That(value, Is.EqualTo(42.0));
        }

        [UnityTest]
        public IEnumerator SetCommand_EvaluatesExpression()
        {
            _asset.Content =
                "{{Set($X, 10)}}\n" +
                "{{Set($X, $X + 5)}}\n" +
                "[NPC]\nDone\n";

            _engine.StartConversation(_asset);
            yield return null;

            object value = VariableStore.Resolve(_asset.name, "X");
            Assert.That(value, Is.EqualTo(15.0));
        }

        [UnityTest]
        public IEnumerator GlobalDecl_CreatesGlobalVariable()
        {
            _asset.Content =
                "{{Global($PLAYER_SCORE, 0)}}\n" +
                "[NPC]\nDone\n";

            _engine.StartConversation(_asset);
            yield return null;

            // Global should be accessible without specifying an asset name
            object value = VariableStore.Resolve(null, "PLAYER_SCORE");
            Assert.That(value, Is.EqualTo(0.0));
        }

        [UnityTest]
        public IEnumerator VariableSubstitution_InDisplayedText()
        {
            VariableStore.SetGlobal("HERO", "Alice");

            _asset.Content = "[NPC]\nWelcome, $HERO!\n";

            string displayedText = null;
            _engine.OnLineDisplayed.AddListener(line => displayedText = line.LineContent.Text);

            _engine.StartConversation(_asset);
            yield return null;

            Assert.That(displayedText, Is.EqualTo("Welcome, Alice!"));
        }

        // --- Save / Load ---

        [UnityTest]
        public IEnumerator ExportProgress_ReturnsSnapshotMidConversation()
        {
            _asset.Content = "[NPC]\nLine 1\n[NPC]\nLine 2\n[NPC]\nLine 3\n";

            _engine.StartConversation(_asset);
            yield return null;

            ConversationProgress progress = _engine.ExportProgress();
            Assert.That(progress, Is.Not.Null);
            Assert.That(progress.ConversationName, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator ResumeFromProgress_ContinuesFromCorrectPoint()
        {
            _asset.Content = "[NPC]\nLine 1\n[NPC]\nLine 2\n[NPC]\nLine 3\n";

            _engine.StartConversation(_asset);
            yield return null;

            // Advance past line 1
            _engine.Next();
            yield return null;

            ConversationProgress progress = _engine.ExportProgress();
            VariableSnapshot varState = VariableStore.ExportState();

            // Stop and restart from saved point
            _engine.StopConversation();
            VariableStore.ImportState(varState);

            List<string> lines = new List<string>();
            _engine.OnLineDisplayed.AddListener(line => lines.Add(line.LineContent.Text));

            _engine.ResumeFromProgress(_asset, progress);
            yield return null;

            Assert.That(lines, Has.Count.GreaterThanOrEqualTo(1));
            Assert.That(lines[0], Is.EqualTo("Line 3"));
        }

        // --- Pause / Resume ---

        [UnityTest]
        public IEnumerator PauseAndResume_FiresEvents()
        {
            _asset.Content = "[NPC]\nHello\n";
            bool paused = false;
            bool resumed = false;

            _engine.OnConversationPaused.AddListener(() => paused = true);
            _engine.OnConversationResumed.AddListener(() => resumed = true);

            _engine.StartConversation(_asset);
            yield return null;

            _engine.PauseConversation();
            Assert.That(paused, Is.True);
            Assert.That(_engine.IsConversationPaused, Is.True);

            _engine.ResumeConversation();
            Assert.That(resumed, Is.True);
            Assert.That(_engine.IsConversationPaused, Is.False);
        }

        // --- Headless mode ---

        [UnityTest]
        public IEnumerator HeadlessMode_EventsStillFire()
        {
            _asset.Content = "[NPC]\nHeadless line\n";

            bool lineDisplayed = false;
            _engine.OnLineDisplayed.AddListener(_ => lineDisplayed = true);

            _engine.StartConversation(_asset);
            yield return null;

            Assert.That(lineDisplayed, Is.True);
            Assert.That(_engine.CurrentLine, Is.Not.Null);
            Assert.That(_engine.CurrentSpeaker, Is.EqualTo("NPC"));
        }

        // --- CurrentLine / CurrentSpeaker ---

        [UnityTest]
        public IEnumerator CurrentLine_SetDuringDisplay_ClearedAfterStop()
        {
            _asset.Content = "[Speaker]\nTest line\n";

            _engine.StartConversation(_asset);
            yield return null;

            Assert.That(_engine.CurrentLine, Is.Not.Null);
            Assert.That(_engine.CurrentLine.LineContent.Text, Is.EqualTo("Test line"));
            Assert.That(_engine.CurrentSpeaker, Is.EqualTo("Speaker"));

            _engine.Next();
            yield return null;

            Assert.That(_engine.CurrentLine, Is.Null);
            Assert.That(_engine.CurrentSpeaker, Is.Null);
        }

        // --- Multi-line speaker blocks ---

        [UnityTest]
        public IEnumerator MultiLine_SameSpeaker_DisplaysAsNewlines()
        {
            _asset.Content = "[NPC]\nFirst sentence\nSecond sentence\n";

            string text = null;
            _engine.OnLineDisplayed.AddListener(line => text = line.LineContent.Text);

            _engine.StartConversation(_asset);
            yield return null;

            Assert.That(text, Does.Contain("First sentence"));
            Assert.That(text, Does.Contain("Second sentence"));
        }
    }
}
