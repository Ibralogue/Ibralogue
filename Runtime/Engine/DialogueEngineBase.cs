using Ibralogue.Localization;
using Ibralogue.Parser;
using Ibralogue.Plugins;
using Ibralogue.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace Ibralogue
{
    /// <summary>
    /// Serializable UnityEvent that passes a <see cref="Line"/> to listeners.
    /// </summary>
    [Serializable] public class LineEvent : UnityEvent<Line> { }

    /// <summary>
    /// Serializable UnityEvent that passes a list of <see cref="Choice"/> to listeners.
    /// </summary>
    [Serializable] public class ChoiceListEvent : UnityEvent<List<Choice>> { }

    /// <summary>
    /// Serializable UnityEvent that passes a single <see cref="Choice"/> to listeners.
    /// </summary>
    [Serializable] public class ChoiceEvent : UnityEvent<Choice> { }

    public abstract class DialogueEngineBase : MonoBehaviour
    {
        protected EnginePlugin[] enginePlugins;

        public UnityEvent PersistentOnConversationStart = new UnityEvent();
        public UnityEvent PersistentOnConversationEnd = new UnityEvent();

        [HideInInspector] public UnityEvent OnConversationStart = new UnityEvent();
        [HideInInspector] public UnityEvent OnConversationEnd = new UnityEvent();

        /// <summary>
        /// Fired after a dialogue line has been resolved and is about to be displayed.
        /// Passes the fully resolved <see cref="Line"/> to listeners.
        /// </summary>
        public LineEvent OnLineDisplayed = new LineEvent();

        /// <summary>
        /// Fired when choices are presented to the player. Passes the full list
        /// of resolved <see cref="Choice"/> objects.
        /// </summary>
        public ChoiceListEvent OnChoicesPresented = new ChoiceListEvent();

        /// <summary>
        /// Fired when the player selects a choice. Passes the selected
        /// <see cref="Choice"/> to listeners.
        /// </summary>
        public ChoiceEvent OnChoiceSelected = new ChoiceEvent();

        /// <summary>
        /// Optional filter applied to choices before they are presented.
        /// Receives the full resolved list and returns the filtered list.
        /// Use this to hide, reorder, or modify choices based on game state.
        /// </summary>
        public Func<List<Choice>, List<Choice>> ChoiceFilter { get; set; }

        public List<Conversation> ParsedConversations { get; protected set; }

        private readonly List<Line> _history = new List<Line>();

        /// <summary>
        /// A read-only log of every line displayed by this engine, in order.
        /// Cleared when a conversation stops unless <see cref="PersistHistory"/>
        /// is enabled.
        /// </summary>
        public IReadOnlyList<Line> History => _history;

        /// <summary>
        /// When true, the history log is preserved across conversations.
        /// When false (default), it is cleared each time a conversation stops.
        /// </summary>
        public bool PersistHistory { get; set; }

        /// <summary>
        /// Clears the history log.
        /// </summary>
        public void ClearHistory()
        {
            _history.Clear();
        }

        /// <summary>
        /// True when a conversation is currently running. False after
        /// <see cref="StopConversation"/> or before any conversation starts.
        /// </summary>
        public bool IsConversationActive => _currentConversation != null;

        /// <summary>
        /// True while a dialogue line is being displayed (typewriter running,
        /// waiting for player input, etc.). False between lines and when no
        /// conversation is active.
        /// </summary>
        public bool IsLineActive => _linePlaying;

        /// <summary>
        /// The line currently being displayed, or null if no line is active.
        /// </summary>
        public Line CurrentLine { get; private set; }

        /// <summary>
        /// The speaker of the currently displayed line, or null if no line
        /// is active.
        /// </summary>
        public string CurrentSpeaker => CurrentLine?.Speaker;

        protected Conversation _currentConversation;
        protected bool _linePlaying;
        protected bool _isPaused = false;

        private Coroutine _displayCoroutine;
        private Coroutine _asyncInvocationCoroutine;
        private string _currentAssetName;
        private ContentCursor _cursor;
        private RuntimeLine _currentRuntimeLine;
        private bool _choicesActive;
        private float _pendingWaitSeconds;
        private int _displayedNodeCount;

        private List<CachedInvocation> _cachedInvocationMethods;
        private bool _invocationCacheDirty = true;

        public UnityEvent OnConversationPaused = new UnityEvent();
        public UnityEvent OnConversationResumed = new UnityEvent();

        [Header("Dialogue Views")]
        [SerializeField] protected DialogueViewBase dialogueView;

        [Tooltip("Optional separate component for choice display. When set, choices " +
                 "are handled by this presenter instead of the dialogue view. Assign any " +
                 "MonoBehaviour implementing IChoicePresenter.")]
        [SerializeField] private MonoBehaviour choicePresenterComponent;

        /// <summary>
        /// True when a dialogue view is assigned. When false, the engine runs
        /// in headless mode: events still fire but no UI is driven.
        /// </summary>
        protected bool HasView => dialogueView != null;

        private IChoicePresenter _choicePresenter;

        private IChoicePresenter ChoicePresenter
        {
            get
            {
                if (_choicePresenter != null)
                    return _choicePresenter;
                if (choicePresenterComponent is IChoicePresenter presenter)
                {
                    _choicePresenter = presenter;
                    return _choicePresenter;
                }
                return null;
            }
        }

        [Header("Auto-Advance")]
        [Tooltip("When greater than zero, lines advance automatically after the display " +
                 "effect finishes plus this delay in seconds. Set to zero to disable. " +
                 "Does not apply when choices are active.")]
        [SerializeField] private float autoAdvanceDelay;

        /// <summary>
        /// When greater than zero, lines advance automatically after the display
        /// effect finishes plus this delay in seconds. Choices still require
        /// player input. Set to zero to require manual advancement.
        /// </summary>
        public float AutoAdvanceDelay
        {
            get => autoAdvanceDelay;
            set => autoAdvanceDelay = Mathf.Max(0f, value);
        }

        [Header("Localization")]
        [SerializeField] private MonoBehaviour localizationProviderComponent;

        [Header("Audio")]
        [SerializeField] private MonoBehaviour audioProviderComponent;

        [Header("Function Invocations")]
        [SerializeField]
        private bool searchAllAssemblies;

        [SerializeField] private List<string> includedAssemblies = new List<string>();

        [Tooltip("Additional MonoBehaviours to scan for [DialogueInvocation] instance methods. " +
                 "Components on the engine's own GameObject are always scanned automatically.")]
        [SerializeField] private MonoBehaviour[] invocationProviders = new MonoBehaviour[0];

        /// <summary>
        /// The active localization provider. When set, translated text is used
        /// in place of the original dialogue text. Assign a MonoBehaviour
        /// implementing <see cref="ILocalizationProvider"/> in the Inspector,
        /// or set this property from code.
        /// </summary>
        public ILocalizationProvider LocalizationProvider
        {
            get
            {
                if (_localizationProvider != null)
                    return _localizationProvider;
                if (localizationProviderComponent is ILocalizationProvider provider)
                    return provider;
                return null;
            }
            set { _localizationProvider = value; }
        }
        private ILocalizationProvider _localizationProvider;

        /// <summary>
        /// The active audio provider. When set and a dialogue line has an "audio"
        /// metadata key, the provider plays the corresponding clip.
        /// </summary>
        public IAudioProvider AudioProvider
        {
            get
            {
                if (_audioProvider != null)
                    return _audioProvider;
                if (audioProviderComponent is IAudioProvider provider)
                    return provider;
                return null;
            }
            set { _audioProvider = value; }
        }
        private IAudioProvider _audioProvider;

        /// <summary>
        /// Starts a dialogue by parsing the asset and beginning the first (or specified) conversation.
        /// </summary>
        public void StartConversation(DialogueAsset interactionDialogue, int startIndex = 0)
        {
            if (interactionDialogue == null)
                throw new ArgumentNullException(nameof(interactionDialogue));

            _currentAssetName = interactionDialogue.name ?? "unknown";
            ParsedConversations = DialogueParser.ParseDialogue(interactionDialogue);

            if (startIndex < 0 || startIndex >= ParsedConversations.Count)
                throw new ArgumentOutOfRangeException(nameof(startIndex),
                    "Expected value is between 0 and conversations count (exclusive)");

            enginePlugins = GetComponents<EnginePlugin>();
            SwitchConversation(ParsedConversations[startIndex]);
        }

        /// <summary>
        /// Switches to a different conversation. Assumes the dialogue file has been parsed.
        /// </summary>
        public void SwitchConversation(Conversation conversation)
        {
            StopConversation();
            _currentConversation = conversation;
            _cursor = new ContentCursor(conversation.Content);
            _choicesActive = false;

            if (enginePlugins != null)
                foreach (EnginePlugin plugin in enginePlugins)
                    plugin.OnConversationStart(conversation);

            PersistentOnConversationStart.Invoke();
            OnConversationStart.Invoke();
            AdvanceAndDisplay();
        }

        /// <summary>
        /// Exports a snapshot of the engine's current position within a conversation.
        /// Returns null if no conversation is active. Use alongside
        /// <see cref="VariableStore.ExportState"/> and <see cref="VisitTracker.ExportState"/>
        /// for complete save/load support.
        /// </summary>
        public ConversationProgress ExportProgress()
        {
            if (_currentConversation == null) return null;

            int count = _displayedNodeCount;

            // If mid-display or mid-choice, back up by one so the current
            // node re-displays on restore instead of being skipped.
            if (_linePlaying || _choicesActive)
                count = Mathf.Max(0, count - 1);

            return new ConversationProgress
            {
                AssetName = _currentAssetName,
                ConversationName = _currentConversation.Name,
                DisplayedNodeCount = count
            };
        }

        /// <summary>
        /// Resumes a conversation from a previously exported progress snapshot.
        /// Variable and visit state should be restored via
        /// <see cref="VariableStore.ImportState"/> and <see cref="VisitTracker.ImportState"/>
        /// before calling this method.
        /// </summary>
        public void ResumeFromProgress(DialogueAsset asset, ConversationProgress progress)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            if (progress == null)
                throw new ArgumentNullException(nameof(progress));

            StopConversation();

            _currentAssetName = asset.name ?? "unknown";
            ParsedConversations = DialogueParser.ParseDialogue(asset);
            enginePlugins = GetComponents<EnginePlugin>();

            Conversation conversation = ParsedConversations.Find(
                c => c.Name == progress.ConversationName);

            if (conversation == null)
            {
                DialogueLogger.LogWarning(
                    $"Conversation '{progress.ConversationName}' not found in " +
                    $"'{progress.AssetName}'. Starting from the beginning.");
                StartConversation(asset);
                return;
            }

            _currentConversation = conversation;
            _cursor = new ContentCursor(conversation.Content);
            _choicesActive = false;

            SkipDisplayableNodes(progress.DisplayedNodeCount);

            if (enginePlugins != null)
                foreach (EnginePlugin plugin in enginePlugins)
                    plugin.OnConversationStart(conversation);

            PersistentOnConversationStart.Invoke();
            OnConversationStart.Invoke();
            AdvanceAndDisplay();
        }

        private void SkipDisplayableNodes(int count)
        {
            Parser.Expressions.ExpressionEvaluator evaluator = CreateEvaluator();
            int skipped = 0;

            while (skipped < count)
            {
                RuntimeContentNode node = _cursor.Current;
                if (node == null) break;

                if (node is RuntimeLine || node is RuntimeChoicePoint)
                {
                    skipped++;
                    _displayedNodeCount++;
                    _cursor.Advance();
                    continue;
                }

                if (node is RuntimeSetCommand set)
                {
                    object value = evaluator.Evaluate(set.Value);
                    VariableStore.Set(_currentAssetName, set.VariableName, value);
                    _cursor.Advance();
                    continue;
                }

                if (node is RuntimeGlobalDecl global)
                {
                    if (global.DefaultValue != null)
                    {
                        object value = evaluator.Evaluate(global.DefaultValue);
                        VariableStore.SetGlobal(global.VariableName, value);
                    }
                    else if (!VariableStore.IsDefined(_currentAssetName, global.VariableName))
                    {
                        VariableStore.SetGlobal(global.VariableName, null);
                    }
                    _cursor.Advance();
                    continue;
                }

                if (node is RuntimeConditionalBlock conditional)
                {
                    _cursor.Advance();
                    foreach (RuntimeBranch branch in conditional.Branches)
                    {
                        if (branch.Condition == null ||
                            evaluator.EvaluateTruthy(branch.Condition))
                        {
                            _cursor.PushScope(branch.Body);
                            break;
                        }
                    }
                    continue;
                }

                _cursor.Advance();
            }
        }

        /// <summary>
        /// Stops the currently playing conversation and clears the dialogue box.
        /// </summary>
        public void StopConversation()
        {
            bool wasActive = _currentConversation != null;

            StopTrackedCoroutines();

            ClearDisplay();
            ClearPlugins();

            IAudioProvider audio = AudioProvider;
            if (audio != null)
                audio.Stop();

            // Only fire end events when a conversation was actually running.
            // This prevents spurious events during the first StartConversation
            // call or when StopConversation is called while already stopped.
            if (wasActive)
            {
                if (enginePlugins != null)
                    foreach (EnginePlugin plugin in enginePlugins)
                        plugin.OnConversationEnd();

                PersistentOnConversationEnd.Invoke();
                OnConversationEnd.Invoke();
            }

            _linePlaying = false;
            CurrentLine = null;
            _currentConversation = null;
            _currentRuntimeLine = null;
            _cursor = null;
            _choicesActive = false;
            _displayedNodeCount = 0;
            _isPaused = false;

            if (!PersistHistory)
                _history.Clear();
        }

        private void StopTrackedCoroutines()
        {
            if (_displayCoroutine != null)
            {
                StopCoroutine(_displayCoroutine);
                _displayCoroutine = null;
            }
            if (_asyncInvocationCoroutine != null)
            {
                StopCoroutine(_asyncInvocationCoroutine);
                _asyncInvocationCoroutine = null;
            }
        }

        public void PauseConversation()
        {
            if (_currentConversation == null || _isPaused) return;
            _isPaused = true;
            if (HasView) dialogueView.Pause();
            OnConversationPaused.Invoke();
        }

        public void ResumeConversation()
        {
            if (_currentConversation == null || !_isPaused) return;
            _isPaused = false;
            if (HasView) dialogueView.Resume();
            OnConversationResumed.Invoke();
        }

        public bool IsConversationPaused => _isPaused;

        /// <summary>
        /// Requests a pause in the display animation for the given duration.
        /// Called by the built-in {{Wait(seconds)}} function.
        /// </summary>
        public void RequestWait(float seconds)
        {
            _pendingWaitSeconds = seconds;
        }

        /// <summary>
        /// Jumps to a conversation by name.
        /// </summary>
        public void JumpTo(string conversationName)
        {
            if (ParsedConversations == null || ParsedConversations.Count == 0)
                throw new InvalidOperationException(
                    "There is no ongoing conversation, therefore the jump cannot be executed");

            Conversation conversation = ParsedConversations.Find(c => c.Name == conversationName);

            if (conversation == null || conversation.Name == null)
                throw new ArgumentException($"No conversation matching '{conversationName}' found",
                    nameof(conversationName));

            SwitchConversation(conversation);
        }

        /// <summary>
        /// Convenience method that skips the current display effect if a line is
        /// still playing, or advances to the next line if idle. This is the
        /// recommended single-call handler for player input (click, key press, tap).
        /// </summary>
        public void Advance()
        {
            if (_linePlaying)
            {
                if (HasView) dialogueView.SkipViewEffect();
                return;
            }

            Next();
        }

        /// <summary>
        /// Advances to the next line if the current line is finished and no
        /// choices are active. Does nothing if a line is still playing.
        /// Use <see cref="Advance"/> for a single-call handler that also skips.
        /// </summary>
        public void Next()
        {
            if (_linePlaying) return;
            if (_currentConversation == null) return;
            if (_choicesActive) return;

            if (CurrentLine != null)
            {
                string jumpTarget = CurrentLine.JumpTarget;
                if (!string.IsNullOrEmpty(jumpTarget))
                {
                    JumpTo(jumpTarget);
                    return;
                }
            }

            ClearDisplay();
            ClearPlugins();
            AdvanceAndDisplay();
        }

        /// <summary>
        /// Walks the cursor forward past all non-displayable nodes (Set, Global, conditionals)
        /// until it finds a RuntimeLine or RuntimeChoicePoint, or reaches the end.
        /// </summary>
        private RuntimeContentNode AdvanceToNextDisplayable()
        {
            Parser.Expressions.ExpressionEvaluator evaluator = CreateEvaluator();

            while (true)
            {
                RuntimeContentNode node = _cursor.Current;
                if (node == null)
                    return null;

                if (node is RuntimeLine line)
                {
                    _cursor.Advance();
                    return line;
                }

                if (node is RuntimeChoicePoint choices)
                {
                    _cursor.Advance();
                    return choices;
                }

                if (node is RuntimeSetCommand set)
                {
                    object value = evaluator.Evaluate(set.Value);
                    VariableStore.Set(_currentAssetName, set.VariableName, value);
                    _cursor.Advance();
                    continue;
                }

                if (node is RuntimeGlobalDecl global)
                {
                    if (global.DefaultValue != null)
                    {
                        object value = evaluator.Evaluate(global.DefaultValue);
                        VariableStore.SetGlobal(global.VariableName, value);
                    }
                    else if (!VariableStore.IsDefined(_currentAssetName, global.VariableName))
                    {
                        VariableStore.SetGlobal(global.VariableName, null);
                    }
                    _cursor.Advance();
                    continue;
                }

                if (node is RuntimeConditionalBlock conditional)
                {
                    _cursor.Advance();
                    bool matched = false;

                    foreach (RuntimeBranch branch in conditional.Branches)
                    {
                        if (branch.Condition == null || evaluator.EvaluateTruthy(branch.Condition))
                        {
                            _cursor.PushScope(branch.Body);
                            matched = true;
                            break;
                        }
                    }

                    if (!matched)
                        continue;

                    continue;
                }

                _cursor.Advance();
            }
        }

        /// <summary>
        /// Advances the cursor and starts displaying whatever comes next.
        /// </summary>
        private void AdvanceAndDisplay()
        {
            while (true)
            {
                RuntimeContentNode displayable = AdvanceToNextDisplayable();

                if (displayable is RuntimeLine runtimeLine)
                {
                    _displayedNodeCount++;
                    _currentRuntimeLine = runtimeLine;
                    Line resolved = ResolveLineText(runtimeLine);

                    if (resolved.Silent)
                    {
                        if (HasAsyncInvocations(resolved.LineContent.Invocations))
                        {
                            _asyncInvocationCoroutine = StartCoroutine(
                                InvokeFunctionsAsync(resolved.LineContent.Invocations, resolved));
                            return;
                        }
                        InvokeFunctions(resolved.LineContent.Invocations, resolved);
                        continue;
                    }

                    RuntimeContentNode peek = PeekNextDisplayable();
                    if (peek is RuntimeChoicePoint choicePoint)
                    {
                        _displayCoroutine = StartCoroutine(DisplayDialogue(resolved, choicePoint));
                    }
                    else
                    {
                        _displayCoroutine = StartCoroutine(DisplayDialogue(resolved, null));
                    }
                    return;
                }

                if (displayable is RuntimeChoicePoint standAloneChoices)
                {
                    _displayedNodeCount++;
                    _choicesActive = true;
                    List<Choice> resolved = ResolveChoices(standAloneChoices);
                    PresentChoices(resolved);
                    return;
                }

                StopConversation();
                return;
            }
        }

        /// <summary>
        /// Peeks ahead in the cursor to check if choices follow the current line,
        /// without consuming any nodes.
        /// </summary>
        private RuntimeContentNode PeekNextDisplayable()
        {
            ContentCursor peekCursor = _cursor.Clone();
            Parser.Expressions.ExpressionEvaluator evaluator = CreateEvaluator();

            while (true)
            {
                RuntimeContentNode node = peekCursor.Current;
                if (node == null)
                    return null;

                if (node is RuntimeLine || node is RuntimeChoicePoint)
                    return node;

                if (node is RuntimeSetCommand || node is RuntimeGlobalDecl)
                {
                    peekCursor.Advance();
                    continue;
                }

                if (node is RuntimeConditionalBlock conditional)
                {
                    peekCursor.Advance();
                    foreach (RuntimeBranch branch in conditional.Branches)
                    {
                        if (branch.Condition == null || evaluator.EvaluateTruthy(branch.Condition))
                        {
                            peekCursor.PushScope(branch.Body);
                            break;
                        }
                    }
                    continue;
                }

                peekCursor.Advance();
            }
        }

        private IEnumerator DisplayDialogue(Line line, RuntimeChoicePoint choices)
        {
            _linePlaying = true;

            if (choices != null)
            {
                _choicesActive = true;
                List<Choice> resolved = ResolveChoices(choices);
                PresentChoices(resolved);
                AdvanceToNextDisplayable();
            }

            yield return StartCoroutine(OnDisplayLine(line));

            _linePlaying = false;

            if (autoAdvanceDelay > 0f && !_choicesActive)
            {
                yield return new WaitForSeconds(autoAdvanceDelay);
                if (_currentConversation != null && !_isPaused && !_choicesActive)
                    AdvanceAndDisplay();
            }

            yield return null;
        }

        /// <summary>
        /// Called when a dialogue line is ready to be displayed. Override this to
        /// customize how lines are presented, add custom effects, or inject
        /// additional logic between lines.
        ///
        /// Text-producing functions (non-void return) fire immediately before
        /// the animation starts. Void functions fire at their character position
        /// during the animated reveal.
        /// </summary>
        protected virtual IEnumerator OnDisplayLine(Line line)
        {
            IEnumerable<CachedInvocation> dialogueMethods = GetInvocationMethods();
            List<ResolvedInvocation> resolved = ResolveAllInvocations(line, dialogueMethods);

            InvokeTextProducingFunctions(resolved, line);

            CurrentLine = line;
            if (HasView) dialogueView.SetView(line);
            _history.Add(line);
            OnLineDisplayed.Invoke(line);

            foreach (EnginePlugin plugin in enginePlugins)
            {
                plugin.Display(line);
            }

            List<ResolvedInvocation> pending = CollectPendingVoidInvocations(resolved);
            int nextPending = 0;
            _pendingWaitSeconds = 0f;

            while (HasView && dialogueView.IsStillDisplaying)
            {
                if (_isPaused)
                    yield return new WaitUntil(() => !_isPaused);

                int visibleChars = dialogueView.VisibleCharacterCount;
                while (nextPending < pending.Count &&
                       pending[nextPending].Invocation.CharacterIndex <= visibleChars)
                {
                    object result = InvokeSingle(pending[nextPending], line);
                    nextPending++;

                    if (result is IEnumerator coroutine)
                    {
                        dialogueView.Pause();
                        yield return coroutine;
                        dialogueView.Resume();
                    }
                    else if (_pendingWaitSeconds > 0f)
                    {
                        dialogueView.Pause();
                        yield return new WaitForSeconds(_pendingWaitSeconds);
                        _pendingWaitSeconds = 0f;
                        dialogueView.Resume();
                    }
                }

                yield return null;
            }

            while (nextPending < pending.Count)
            {
                object result = InvokeSingle(pending[nextPending], line);
                nextPending++;

                if (result is IEnumerator remainingCoroutine)
                {
                    yield return remainingCoroutine;
                }
                else if (_pendingWaitSeconds > 0f)
                {
                    yield return new WaitForSeconds(_pendingWaitSeconds);
                    _pendingWaitSeconds = 0f;
                }
            }

            if (_isPaused)
                yield return new WaitUntil(() => !_isPaused);
        }

        private void PresentChoices(List<Choice> resolved)
        {
            if (ChoiceFilter != null)
                resolved = ChoiceFilter(resolved);

            if (resolved == null || resolved.Count == 0)
            {
                DialogueLogger.LogWarning("All choices were filtered out. Stopping conversation.");
                _choicesActive = false;
                StopConversation();
                return;
            }

            OnChoicesPresented.Invoke(resolved);

            if (enginePlugins != null)
                foreach (EnginePlugin plugin in enginePlugins)
                    plugin.OnChoicesPresented(resolved);

            IChoicePresenter presenter = ChoicePresenter;
            if (presenter != null)
                presenter.DisplayChoices(resolved, HandleChoiceSelected);
            else if (HasView)
                dialogueView.DisplayChoices(resolved, HandleChoiceSelected);
        }

        /// <summary>
        /// Submits a choice selection programmatically. Use this in headless/event-driven
        /// setups where there is no view handling choice buttons, or when driving
        /// choices from custom UI.
        /// </summary>
        public void SelectChoice(Choice choice)
        {
            if (!_choicesActive) return;
            HandleChoiceSelected(choice);
        }

        private void HandleChoiceSelected(Choice choice)
        {
            _choicesActive = false;
            OnChoiceSelected.Invoke(choice);

            if (enginePlugins != null)
                foreach (EnginePlugin plugin in enginePlugins)
                    plugin.OnChoiceSelected(choice);

            if (choice.LeadingConversationName == ">>")
            {
                StopTrackedCoroutines();
                _linePlaying = false;
                ClearDisplay();
                ClearPlugins();
                AdvanceAndDisplay();
                return;
            }

            if (ParsedConversations == null) return;

            int conversationIndex = ParsedConversations.FindIndex(c => c.Name == choice.LeadingConversationName);
            if (conversationIndex == -1)
            {
                DialogueLogger.LogError(0,
                    $"No conversation called \"{choice.LeadingConversationName}\" found for choice \"{choice.ChoiceName}\"");
                return;
            }

            SwitchConversation(ParsedConversations[conversationIndex]);
        }

        private Line ResolveLineText(RuntimeLine runtimeLine)
        {
            return LineResolver.Resolve(runtimeLine, _currentAssetName, LocalizationProvider);
        }

        private List<Choice> ResolveChoices(RuntimeChoicePoint choicePoint)
        {
            return LineResolver.ResolveChoices(choicePoint, _currentAssetName, LocalizationProvider);
        }

        private Parser.Expressions.ExpressionEvaluator CreateEvaluator()
        {
            string assetName = _currentAssetName;
            return new Parser.Expressions.ExpressionEvaluator(
                name => VariableStore.Resolve(assetName, name),
                ResolveExpressionFunction
            );
        }

        private object ResolveExpressionFunction(string name, object[] arguments)
        {
            IEnumerable<CachedInvocation> methods = GetInvocationMethods();
            int argCount = arguments != null ? arguments.Length : 0;

            foreach (CachedInvocation cached in methods)
            {
                if (cached.Method.Name != name)
                    continue;

                ParameterInfo[] parameters = cached.Method.GetParameters();
                int expectedArgs = parameters.Length;

                if (expectedArgs > 0 && typeof(DialogueEngineBase).IsAssignableFrom(parameters[0].ParameterType))
                    expectedArgs--;

                if (expectedArgs != argCount)
                    continue;

                object[] callArgs = new object[parameters.Length];
                int argIndex = 0;

                for (int i = 0; i < parameters.Length; i++)
                {
                    Type paramType = parameters[i].ParameterType;

                    if (i == 0 && typeof(DialogueEngineBase).IsAssignableFrom(paramType))
                    {
                        callArgs[i] = this;
                        continue;
                    }

                    object rawValue = arguments[argIndex];

                    try
                    {
                        if (rawValue != null && paramType.IsAssignableFrom(rawValue.GetType()))
                            callArgs[i] = rawValue;
                        else
                            callArgs[i] = Convert.ChangeType(
                                rawValue, paramType, CultureInfo.InvariantCulture);
                    }
                    catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException)
                    {
                        DialogueLogger.LogWarning(
                            $"Failed to convert argument {argIndex} to {paramType.Name} " +
                            $"for expression function '{name}': {ex.Message}");
                        return null;
                    }

                    argIndex++;
                }

                return cached.Method.Invoke(cached.Target, callArgs);
            }

            DialogueLogger.LogWarning(
                $"No [DialogueInvocation] method found for expression function '{name}' " +
                $"accepting {argCount} argument(s)");
            return null;
        }

        /// <summary>
        /// Pairs a method with its invocation target. Target is null for static methods.
        /// </summary>
        protected struct CachedInvocation
        {
            public MethodInfo Method;
            public object Target;
        }

        private struct ResolvedInvocation
        {
            public Invocation Invocation;
            public MethodInfo Method;
            public object Target;
            public object[] Arguments;
        }

        private List<ResolvedInvocation> ResolveAllInvocations(Line line,
            IEnumerable<CachedInvocation> dialogueMethods)
        {
            List<ResolvedInvocation> result = new List<ResolvedInvocation>();
            if (line.LineContent.Invocations == null)
                return result;

            foreach (Invocation function in line.LineContent.Invocations)
            {
                CachedInvocation? cached = ResolveInvocation(dialogueMethods, function);
                if (cached == null) continue;

                object[] args = BuildInvocationArguments(cached.Value.Method, function);
                if (args == null) continue;

                result.Add(new ResolvedInvocation
                {
                    Invocation = function,
                    Method = cached.Value.Method,
                    Target = cached.Value.Target,
                    Arguments = args
                });
            }

            return result;
        }

        private void InvokeTextProducingFunctions(List<ResolvedInvocation> resolved, Line line)
        {
            foreach (ResolvedInvocation r in resolved)
            {
                if (r.Method.ReturnType == typeof(void)
                    || typeof(IEnumerator).IsAssignableFrom(r.Method.ReturnType))
                    continue;

                object result = r.Method.Invoke(r.Target, r.Arguments);
                string insertText = Convert.ToString(result, CultureInfo.InvariantCulture) ?? "";
                line.LineContent.Text =
                    line.LineContent.Text.Insert(r.Invocation.CharacterIndex, insertText);
            }
        }

        private List<ResolvedInvocation> CollectPendingVoidInvocations(List<ResolvedInvocation> resolved)
        {
            List<ResolvedInvocation> pending = new List<ResolvedInvocation>();
            foreach (ResolvedInvocation r in resolved)
            {
                if (r.Method.ReturnType == typeof(void)
                    || typeof(IEnumerator).IsAssignableFrom(r.Method.ReturnType))
                    pending.Add(r);
            }
            pending.Sort((a, b) => a.Invocation.CharacterIndex.CompareTo(b.Invocation.CharacterIndex));
            return pending;
        }

        private object InvokeSingle(ResolvedInvocation r, Line line)
        {
            return r.Method.Invoke(r.Target, r.Arguments);
        }

        private bool HasAsyncInvocations(List<Invocation> invocations)
        {
            if (invocations == null || invocations.Count == 0) return false;

            IEnumerable<CachedInvocation> methods = GetInvocationMethods();
            foreach (Invocation function in invocations)
            {
                CachedInvocation? cached = ResolveInvocation(methods, function, true);
                if (cached != null && typeof(IEnumerator).IsAssignableFrom(cached.Value.Method.ReturnType))
                    return true;
            }
            return false;
        }

        private IEnumerator InvokeFunctionsAsync(List<Invocation> functionInvocations, Line line)
        {
            if (functionInvocations == null || functionInvocations.Count == 0) yield break;

            IEnumerable<CachedInvocation> dialogueMethods = GetInvocationMethods();

            foreach (Invocation function in functionInvocations)
            {
                CachedInvocation? cached = ResolveInvocation(dialogueMethods, function);
                if (cached == null) continue;

                object[] args = BuildInvocationArguments(cached.Value.Method, function);
                if (args == null) continue;

                object result = cached.Value.Method.Invoke(cached.Value.Target, args);

                if (result is IEnumerator coroutine)
                {
                    yield return coroutine;
                }
                else if (cached.Value.Method.ReturnType != typeof(void))
                {
                    string insertText = Convert.ToString(result, CultureInfo.InvariantCulture) ?? "";
                    line.LineContent.Text =
                        line.LineContent.Text.Insert(function.CharacterIndex, insertText);
                }
            }

            AdvanceAndDisplay();
        }

        /// <summary>
        /// Invokes all functions immediately. Used for silent lines where
        /// there is no animated display.
        /// </summary>
        protected void InvokeFunctions(List<Invocation> functionInvocations, Line line)
        {
            if (functionInvocations == null || functionInvocations.Count == 0)
                return;

            IEnumerable<CachedInvocation> dialogueMethods = GetInvocationMethods();

            foreach (Invocation function in functionInvocations)
            {
                CachedInvocation? cached = ResolveInvocation(dialogueMethods, function);
                if (cached == null) continue;

                object[] args = BuildInvocationArguments(cached.Value.Method, function);
                if (args == null) continue;

                object result = cached.Value.Method.Invoke(cached.Value.Target, args);

                if (cached.Value.Method.ReturnType != typeof(void))
                {
                    string insertText = Convert.ToString(result, CultureInfo.InvariantCulture) ?? "";
                    line.LineContent.Text =
                        line.LineContent.Text.Insert(function.CharacterIndex, insertText);
                }
            }
        }

        private CachedInvocation? ResolveInvocation(IEnumerable<CachedInvocation> methods,
            Invocation function, bool silent = false)
        {
            bool nameFound = false;
            int argCount = function.Arguments != null ? function.Arguments.Count : 0;

            foreach (CachedInvocation cached in methods)
            {
                if (cached.Method.Name != function.Name)
                    continue;

                nameFound = true;
                ParameterInfo[] parameters = cached.Method.GetParameters();
                int expectedArgs = parameters.Length;

                if (expectedArgs > 0 && typeof(DialogueEngineBase).IsAssignableFrom(parameters[0].ParameterType))
                    expectedArgs--;

                if (expectedArgs == argCount)
                    return cached;
            }

            if (!silent)
            {
                if (nameFound)
                {
                    DialogueLogger.LogWarning(function.Line, function.Column,
                        $"[DialogueInvocation] '{function.Name}' exists but no overload accepts {argCount} argument(s)");
                }
                else
                {
                    DialogueLogger.LogWarning(function.Line, function.Column,
                        $"No [DialogueInvocation] method found for invocation '{function.Name}'");
                }
            }

            return null;
        }

        private object[] BuildInvocationArguments(MethodInfo method, Invocation function)
        {
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 0)
                return Array.Empty<object>();

            object[] args = new object[parameters.Length];
            int argIndex = 0;

            for (int i = 0; i < parameters.Length; i++)
            {
                Type paramType = parameters[i].ParameterType;

                if (i == 0 && typeof(DialogueEngineBase).IsAssignableFrom(paramType))
                {
                    args[i] = this;
                    continue;
                }

                string rawValue = function.Arguments[argIndex];

                try
                {
                    args[i] = paramType == typeof(string)
                        ? rawValue
                        : Convert.ChangeType(rawValue, paramType, CultureInfo.InvariantCulture);
                }
                catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException)
                {
                    DialogueLogger.LogWarning(function.Line, function.Column,
                        $"Failed to convert argument {argIndex} ('{rawValue}') to {paramType.Name} " +
                        $"for invocation '{function.Name}': {ex.Message}");
                    return null;
                }

                argIndex++;
            }

            return args;
        }

        private void ClearDisplay()
        {
            if (HasView) dialogueView.ClearView();
            ChoicePresenter?.ClearChoices();
        }

        private void ClearPlugins()
        {
            if (enginePlugins == null) return;
            foreach (EnginePlugin plugin in enginePlugins)
                plugin.Clear();
        }

        /// <summary>
        /// Marks the invocation method cache as dirty, forcing a re-scan on the
        /// next line display. Call this if you add or change assemblies at runtime.
        /// </summary>
        public void InvalidateInvocationCache()
        {
            _invocationCacheDirty = true;
        }

        protected IEnumerable<CachedInvocation> GetInvocationMethods()
        {
            if (!_invocationCacheDirty && _cachedInvocationMethods != null)
                return _cachedInvocationMethods;

            _cachedInvocationMethods = new List<CachedInvocation>();

            // Static methods from assemblies
            List<Assembly> assemblies = new List<Assembly>();
            Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            if (searchAllAssemblies) assemblies.AddRange(allAssemblies);
            else
                foreach (Assembly assembly in allAssemblies)
                {
                    string name = assembly.GetName().Name;
                    if (name == "Assembly-CSharp" || includedAssemblies.Contains(name) ||
                        assembly == Assembly.GetExecutingAssembly()) assemblies.Add(assembly);
                }

            foreach (Assembly assembly in assemblies)
            {
                IEnumerable<MethodInfo> staticMethods = assembly.GetTypes()
                    .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                    .Where(m => m.GetCustomAttributes(typeof(DialogueInvocationAttribute), true).Length > 0);

                foreach (MethodInfo method in staticMethods)
                    _cachedInvocationMethods.Add(new CachedInvocation { Method = method, Target = null });
            }

            // Instance methods on MonoBehaviours: same GameObject + explicit providers
            HashSet<MonoBehaviour> scanned = new HashSet<MonoBehaviour>();

            foreach (MonoBehaviour component in GetComponents<MonoBehaviour>())
                scanned.Add(component);

            if (invocationProviders != null)
                foreach (MonoBehaviour provider in invocationProviders)
                    if (provider != null)
                        scanned.Add(provider);

            foreach (MonoBehaviour component in scanned)
            {
                MethodInfo[] methods = component.GetType()
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance);

                foreach (MethodInfo method in methods)
                {
                    if (method.GetCustomAttributes(typeof(DialogueInvocationAttribute), true).Length > 0)
                        _cachedInvocationMethods.Add(new CachedInvocation { Method = method, Target = component });
                }
            }

            _invocationCacheDirty = false;
            return _cachedInvocationMethods;
        }

    }

    /// <summary>
    /// Stack-based cursor for walking a tree of runtime content nodes.
    /// Each frame tracks a position within a list of nodes. When a frame
    /// is exhausted, the cursor pops back to the parent frame.
    /// </summary>
    internal class ContentCursor
    {
        private class Frame
        {
            public readonly List<RuntimeContentNode> Nodes;
            public int Index;

            public Frame(List<RuntimeContentNode> nodes, int index)
            {
                Nodes = nodes;
                Index = index;
            }
        }

        private readonly List<Frame> _stack = new List<Frame>();

        public ContentCursor(List<RuntimeContentNode> content)
        {
            _stack.Add(new Frame(content, 0));
        }

        private ContentCursor(List<Frame> frames)
        {
            foreach (Frame f in frames)
                _stack.Add(new Frame(f.Nodes, f.Index));
        }

        /// <summary>
        /// Returns the current node, or null if all frames are exhausted.
        /// Automatically pops completed frames.
        /// </summary>
        public RuntimeContentNode Current
        {
            get
            {
                while (_stack.Count > 0)
                {
                    Frame top = _stack[_stack.Count - 1];
                    if (top.Index < top.Nodes.Count)
                        return top.Nodes[top.Index];
                    _stack.RemoveAt(_stack.Count - 1);
                }
                return null;
            }
        }

        /// <summary>
        /// Moves past the current node in the topmost frame.
        /// </summary>
        public void Advance()
        {
            if (_stack.Count > 0)
            {
                _stack[_stack.Count - 1].Index++;
            }
        }

        /// <summary>
        /// Pushes a new scope (e.g. a conditional branch body) onto the stack.
        /// </summary>
        public void PushScope(List<RuntimeContentNode> nodes)
        {
            if (nodes != null && nodes.Count > 0)
                _stack.Add(new Frame(nodes, 0));
        }

        public ContentCursor Clone()
        {
            return new ContentCursor(_stack);
        }
    }
}
