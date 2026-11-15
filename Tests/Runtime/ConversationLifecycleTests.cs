using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Ibralogue.Interactions;
using Ibralogue.Parser;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace Ibralogue.Tests
{
    /// <summary>
    /// A dialogue starts once and ends once, whatever jumps and choices happen
    /// inside it, and the engine is idle by the time anyone hears that it ended.
    /// </summary>
    public class ConversationLifecycleTests
    {
        private class TestInteraction : SingleInteraction
        {
            public int Started;
            public int Ended;

            public void Setup(DialogueEngineBase engine, DialogueAsset asset)
            {
                dialogueEngine = engine;
                InteractionDialogues = new[] { asset };
                Event("OnConversationStart").AddListener(() => Started++);
                Event("OnConversationEnd").AddListener(() => Ended++);
            }

            public UnityEvent Event(string field)
            {
                return (UnityEvent)typeof(BaseInteraction)
                    .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(this);
            }
        }

        private class ResumingInteraction : TestInteraction
        {
            public ConversationProgress Held;

            protected override bool OnInterrupted(ConversationProgress progress)
            {
                Held = progress;
                return true;
            }

            public void Continue() => ContinueConversation(InteractionDialogues[0], Held);
        }

        private class Marker : MonoBehaviour
        {
            public int Count;

            [DialogueInvocation]
            public void Mark() => Count++;
        }

        private GameObject _go;
        private SimpleDialogueEngine _engine;
        private DialogueAsset _a;
        private DialogueAsset _b;
        private readonly List<DialogueAsset> _assets = new List<DialogueAsset>();
        private readonly List<string> _shown = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestEngine");
            _engine = _go.AddComponent<SimpleDialogueEngine>();
            _engine.OnLineDisplayed.AddListener(line => _shown.Add(line.LineContent.Text));
            _a = Asset("[NPC]\nFrom A\n");
            _b = Asset("[NPC]\nFrom B\n");
            VariableStore.ClearAll();
            VisitTracker.Clear();
            DialogueParser.ClearCache();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.Destroy(_go);
            foreach (DialogueAsset asset in _assets)
                Object.DestroyImmediate(asset);
            _assets.Clear();
            _shown.Clear();
            VariableStore.ClearAll();
            VisitTracker.Clear();
            DialogueParser.ClearCache();
        }

        private DialogueAsset Asset(string content)
        {
            DialogueAsset asset = ScriptableObject.CreateInstance<DialogueAsset>();
            asset.name = "asset_" + _assets.Count;
            asset.Content = content;
            _assets.Add(asset);
            return asset;
        }

        private T Interaction<T>(DialogueAsset asset) where T : TestInteraction
        {
            T interaction = _go.AddComponent<T>();
            interaction.Setup(_engine, asset);
            return interaction;
        }

        private IEnumerator Finish()
        {
            for (int i = 0; i < 16 && _engine.IsConversationActive; i++)
            {
                yield return null;
                _engine.Next();
            }

            yield return null;
        }

        // --- The engine is idle before end listeners run ---

        [UnityTest]
        public IEnumerator EndListeners_SeeAnIdleEngine()
        {
            bool active = true;
            Line line = new Line();

            _engine.OnConversationEnd.AddListener(() =>
            {
                active = _engine.IsConversationActive;
                line = _engine.CurrentLine;
            });

            _engine.StartConversation(_a);
            yield return Finish();

            Assert.That(active, Is.False);
            Assert.That(line, Is.Null);
        }

        [UnityTest]
        public IEnumerator StartingFromAnEndListener_PlaysTheNextConversation()
        {
            UnityAction chain = null;
            chain = () =>
            {
                _engine.OnConversationEnd.RemoveListener(chain);
                _engine.StartConversation(_b);
            };
            _engine.OnConversationEnd.AddListener(chain);

            _engine.StartConversation(_a);
            yield return null;
            _engine.Next();
            yield return null;

            Assert.That(_engine.IsConversationActive, Is.True, "the chained conversation was cleared");
            Assert.That(_shown, Is.EqualTo(new[] { "From A", "From B" }));

            yield return Finish();
            Assert.That(_engine.IsConversationActive, Is.False);
        }

        [UnityTest]
        public IEnumerator StopConversation_WhenIdle_FiresNothing()
        {
            int ended = 0;
            _engine.OnConversationEnd.AddListener(() => ended++);

            _engine.StopConversation();
            yield return null;

            Assert.That(ended, Is.EqualTo(0));
        }

        // --- Jumps and choices are part of the dialogue ---

        [UnityTest]
        public IEnumerator Jump_FiresNoStartOrEndEvents()
        {
            DialogueAsset jumps = Asset(
                "{{ConversationName(One)}}\n[NPC]\n{{Jump(Two)}}\nFirst\n\n" +
                "{{ConversationName(Two)}}\n[NPC]\nSecond\n");
            int started = 0;
            int ended = 0;
            _engine.OnConversationStart.AddListener(() => started++);
            _engine.OnConversationEnd.AddListener(() => ended++);

            _engine.StartConversation(jumps);
            yield return null;
            _engine.Next();
            yield return null;

            Assert.That(_shown, Is.EqualTo(new[] { "First", "Second" }));
            Assert.That(started, Is.EqualTo(1), "a jump is not a new start");
            Assert.That(ended, Is.EqualTo(0), "a jump is not an end");
            Assert.That(_engine.History, Has.Count.EqualTo(2), "history should survive the jump");

            yield return Finish();

            Assert.That(started, Is.EqualTo(1));
            Assert.That(ended, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Choice_FiresNoStartOrEndEvents()
        {
            DialogueAsset branches = Asset(
                "[NPC]\nPick one\n- Option A -> BranchA\n\n" +
                "{{ConversationName(BranchA)}}\n[NPC]\nYou picked A\n");
            List<Choice> presented = null;
            int started = 0;
            int ended = 0;
            _engine.OnChoicesPresented.AddListener(choices => presented = choices);
            _engine.OnConversationStart.AddListener(() => started++);
            _engine.OnConversationEnd.AddListener(() => ended++);

            _engine.StartConversation(branches);
            yield return null;
            _engine.SelectChoice(presented[0]);
            yield return null;

            Assert.That(_shown, Is.EqualTo(new[] { "Pick one", "You picked A" }));
            Assert.That(started, Is.EqualTo(1));
            Assert.That(ended, Is.EqualTo(0));

            yield return Finish();
            Assert.That(ended, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator JumpTo_WhenIdle_Throws()
        {
            _engine.StartConversation(_a);
            yield return Finish();

            Assert.Throws<System.InvalidOperationException>(() => _engine.JumpTo("Default"));
        }

        // --- Callbacks belong to the dialogue they were passed with ---

        [UnityTest]
        public IEnumerator Callbacks_FireOnceForTheirOwnDialogue()
        {
            int started = 0;
            int ended = 0;

            _engine.StartConversation(_a, 0, () => started++, () => ended++);
            Assert.That(started, Is.EqualTo(1));
            Assert.That(ended, Is.EqualTo(0));

            yield return Finish();
            Assert.That(ended, Is.EqualTo(1));

            _engine.StartConversation(_b);
            yield return Finish();

            Assert.That(started, Is.EqualTo(1), "start fired for another dialogue");
            Assert.That(ended, Is.EqualTo(1), "end fired for another dialogue");
        }

        [UnityTest]
        public IEnumerator EndCallback_FiresWhenAnotherDialogueReplacesIt()
        {
            int ended = 0;

            _engine.StartConversation(_a, onEnd: () => ended++);
            yield return null;
            _engine.StartConversation(_b);

            Assert.That(ended, Is.EqualTo(1));
            yield return Finish();
            Assert.That(ended, Is.EqualTo(1));
        }

        // --- A start that cannot go ahead changes nothing ---

        [UnityTest]
        public IEnumerator InvalidStartIndex_LeavesTheRunningConversationAlone()
        {
            DialogueAsset branches = Asset(
                "[NPC]\nPick one\n- Option A -> BranchA\n\n" +
                "{{ConversationName(BranchA)}}\n[NPC]\nYou picked A\n");
            List<Choice> presented = null;
            int ended = 0;
            _engine.OnChoicesPresented.AddListener(choices => presented = choices);
            _engine.OnConversationEnd.AddListener(() => ended++);

            _engine.StartConversation(branches);
            yield return null;

            Assert.Throws<System.ArgumentOutOfRangeException>(() => _engine.StartConversation(_b, 99));

            Assert.That(_engine.IsConversationActive, Is.True);
            Assert.That(ended, Is.EqualTo(0));

            _engine.SelectChoice(presented[0]);
            yield return null;

            Assert.That(_shown, Is.EqualTo(new[] { "Pick one", "You picked A" }),
                "the choice was resolved against the rejected asset");
        }

        [UnityTest]
        public IEnumerator ResumeFromProgress_WithUnknownConversation_StartsFromTheBeginning()
        {
            ConversationProgress progress = new ConversationProgress
            {
                AssetName = _a.name,
                ConversationName = "Missing",
                DisplayedNodeCount = 3
            };

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Missing"));
            _engine.ResumeFromProgress(_a, progress);
            yield return null;

            Assert.That(_shown, Is.EqualTo(new[] { "From A" }));
        }

        [UnityTest]
        public IEnumerator EmptyDialogue_StartsAndEndsWithoutShowingAnything()
        {
            DialogueAsset empty = Asset("# nothing written yet\n");
            int started = 0;
            int ended = 0;

            _engine.StartConversation(empty, 0, () => started++, () => ended++);
            yield return null;

            Assert.That(started, Is.EqualTo(1));
            Assert.That(ended, Is.EqualTo(1));
            Assert.That(_shown, Is.Empty);
            Assert.That(_engine.IsConversationActive, Is.False);
        }

        // --- Stopping a conversation stops everything it started ---

        // Stopping a line has to stop the routine revealing it. Left running, it carries
        // on over the next conversation and fires what was left of the old line.
        [UnityTest]
        public IEnumerator StoppedLine_LeavesNothingRunning()
        {
            Marker marker = _go.AddComponent<Marker>();
            _engine.InvalidateInvocationCache();
            DialogueAsset waits = Asset("[NPC]\nHello{{Wait(0.05)}} there{{Mark}}\n");

            _engine.StartConversation(waits);
            yield return null;

            _engine.StartConversation(_b);
            yield return new WaitForSeconds(0.2f);

            Assert.That(marker.Count, Is.EqualTo(0), "the old line kept going after it was stopped");
        }

        // --- Being interrupted ---

        [UnityTest]
        public IEnumerator InterruptedProgress_IsOnlySetWhileBeingInterrupted()
        {
            ConversationProgress seenOnStop = new ConversationProgress();
            ConversationProgress seenOnReplace = null;

            _engine.StartConversation(_a, onEnd: () => seenOnStop = _engine.InterruptedProgress);
            yield return null;
            _engine.StopConversation();
            Assert.That(seenOnStop, Is.Null, "a plain stop is not an interruption");

            _engine.StartConversation(_a, onEnd: () => seenOnReplace = _engine.InterruptedProgress);
            yield return null;
            _engine.StartConversation(_b);

            Assert.That(seenOnReplace, Is.Not.Null);
            Assert.That(seenOnReplace.ConversationName, Is.EqualTo("Default"));
            Assert.That(_engine.InterruptedProgress, Is.Null, "it should not outlive the interruption");
            yield return Finish();
        }

        [UnityTest]
        public IEnumerator StartFromAnEndListener_IsIgnoredWhileAnotherDialogueTakesOver()
        {
            DialogueAsset chained = Asset("[NPC]\nFrom the chain\n");
            int chainedStarted = 0;
            _engine.OnConversationEnd.AddListener(
                () => _engine.StartConversation(chained, onStart: () => chainedStarted++));

            _engine.StartConversation(_a);
            yield return null;
            _engine.StartConversation(_b);
            yield return null;

            Assert.That(chainedStarted, Is.EqualTo(0), "the chained dialogue started under the one taking over");
            Assert.That(_shown, Is.EqualTo(new[] { "From A", "From B" }));

            _engine.OnConversationEnd.RemoveAllListeners();
            yield return Finish();
        }

        // --- An interaction's events are for its own dialogue ---

        [UnityTest]
        public IEnumerator InteractionEvents_DoNotFireForLaterConversations()
        {
            TestInteraction first = Interaction<TestInteraction>(_a);

            first.StartDialogue();
            yield return Finish();
            Assert.That(first.Started, Is.EqualTo(1));
            Assert.That(first.Ended, Is.EqualTo(1));

            _engine.StartConversation(_b);
            yield return Finish();

            Assert.That(first.Started, Is.EqualTo(1), "start fired for someone else's conversation");
            Assert.That(first.Ended, Is.EqualTo(1), "end fired for someone else's conversation");
        }

        [UnityTest]
        public IEnumerator InteractionEvents_FireAgainWhenItsOwnDialogueRepeats()
        {
            TestInteraction first = Interaction<TestInteraction>(_a);

            first.StartDialogue();
            yield return Finish();
            first.StartDialogue();
            yield return Finish();

            Assert.That(first.Started, Is.EqualTo(2));
            Assert.That(first.Ended, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator StartingOverAnotherConversation_DoesNotEndEarly()
        {
            TestInteraction talk = Interaction<TestInteraction>(_b);

            _engine.StartConversation(_a);
            yield return null;

            talk.StartDialogue();

            Assert.That(_engine.IsConversationActive, Is.True);
            Assert.That(talk.Ended, Is.EqualTo(0), "the conversation it replaced ending was taken for its own");

            yield return Finish();
            Assert.That(talk.Ended, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ReplacedInteraction_HearsItsOwnEndOnce()
        {
            TestInteraction first = Interaction<TestInteraction>(_a);
            TestInteraction second = Interaction<TestInteraction>(_b);

            first.StartDialogue();
            yield return null;
            second.StartDialogue();

            Assert.That(first.Ended, Is.EqualTo(1));
            Assert.That(second.Ended, Is.EqualTo(0));

            yield return Finish();

            Assert.That(first.Ended, Is.EqualTo(1));
            Assert.That(second.Ended, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator InteractionEvents_TreatAJumpAsTheSameDialogue()
        {
            DialogueAsset jumps = Asset(
                "{{ConversationName(One)}}\n[NPC]\n{{Jump(Two)}}\nFirst\n\n" +
                "{{ConversationName(Two)}}\n[NPC]\nSecond\n");
            TestInteraction talk = Interaction<TestInteraction>(jumps);

            talk.StartDialogue();
            yield return null;
            _engine.Next();
            yield return null;

            Assert.That(_engine.IsConversationActive, Is.True, "the jump should have landed");
            Assert.That(talk.Started, Is.EqualTo(1), "a jump is not a new start");
            Assert.That(talk.Ended, Is.EqualTo(0), "a jump is not an end");

            yield return Finish();

            Assert.That(talk.Started, Is.EqualTo(1));
            Assert.That(talk.Ended, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator InteractionChain_RunsEachOnce()
        {
            TestInteraction first = Interaction<TestInteraction>(_a);
            TestInteraction second = Interaction<TestInteraction>(_b);
            first.Event("OnConversationEnd").AddListener(second.StartDialogue);

            first.StartDialogue();
            yield return null;
            _engine.Next();
            yield return null;

            Assert.That(_engine.IsConversationActive, Is.True, "the chained conversation was cleared");
            Assert.That(second.Started, Is.EqualTo(1));

            yield return Finish();

            Assert.That(first.Started, Is.EqualTo(1));
            Assert.That(first.Ended, Is.EqualTo(1));
            Assert.That(second.Ended, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DestroyedInteraction_IsNotCalled()
        {
            GameObject host = new GameObject("host");
            TestInteraction talk = host.AddComponent<TestInteraction>();
            talk.Setup(_engine, _a);

            talk.StartDialogue();
            yield return null;
            Object.Destroy(host);
            yield return null;

            Assert.That(talk == null);
            yield return Finish();

            Assert.That(_engine.IsConversationActive, Is.False);
            Assert.That(talk.Ended, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator HeldInteraction_CarriesOnFromTheLineItWasOn()
        {
            DialogueAsset three = Asset(
                "{{ConversationName(Beat)}}\n[NPC]\nOne\n[NPC]\nTwo\n[NPC]\nThree\n");

            ResumingInteraction beat = Interaction<ResumingInteraction>(three);
            beat.StartDialogue();
            yield return null;
            _engine.Next();
            yield return null;
            Assert.That(_shown, Is.EqualTo(new[] { "One", "Two" }));

            _engine.StartConversation(_b);
            Assert.That(beat.Ended, Is.EqualTo(0), "being interrupted is not the end");
            Assert.That(beat.Held, Is.Not.Null);
            yield return Finish();

            beat.Continue();
            yield return null;
            Assert.That(_shown[_shown.Count - 1], Is.EqualTo("Two"), "the covered line should show again");

            yield return Finish();
            Assert.That(_shown[_shown.Count - 1], Is.EqualTo("Three"));
            Assert.That(beat.Started, Is.EqualTo(1), "carrying on is not a new start");
            Assert.That(beat.Ended, Is.EqualTo(1));
        }

        // --- HasMoreToShow ---

        [UnityTest]
        public IEnumerator HasMoreToShow_IsFalseOnTheLastLine()
        {
            DialogueAsset two = Asset("[NPC]\nOne\n[NPC]\nTwo\n");
            Assert.That(_engine.HasMoreToShow, Is.False);

            _engine.StartConversation(two);
            yield return null;
            Assert.That(_engine.HasMoreToShow, Is.True);

            _engine.Next();
            yield return null;
            Assert.That(_engine.HasMoreToShow, Is.False);
        }

        [UnityTest]
        public IEnumerator HasMoreToShow_FollowsAJump()
        {
            DialogueAsset jumps = Asset(
                "{{ConversationName(One)}}\n[NPC]\n{{Jump(Two)}}\nFirst\n\n" +
                "{{ConversationName(Two)}}\n[NPC]\nSecond\n");

            _engine.StartConversation(jumps);
            yield return null;

            Assert.That(_engine.HasMoreToShow, Is.True);
        }
    }
}
