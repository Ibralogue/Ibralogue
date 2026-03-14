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
    public abstract class DialogueEngineBase : MonoBehaviour
    {
        protected EnginePlugin[] enginePlugins;

        public UnityEvent PersistentOnConversationStart = new UnityEvent();
        public UnityEvent PersistentOnConversationEnd = new UnityEvent();

        [HideInInspector] public UnityEvent OnConversationStart = new UnityEvent();
        [HideInInspector] public UnityEvent OnConversationEnd = new UnityEvent();

        public List<Conversation> ParsedConversations { get; protected set; }

        protected Conversation _currentConversation;
        protected bool _linePlaying;
        protected bool _isPaused = false;

        private Coroutine _displayCoroutine;
        private string _currentAssetName;
        private ContentCursor _cursor;
        private RuntimeLine _currentRuntimeLine;
        private bool _choicesActive;

        public UnityEvent OnConversationPaused = new UnityEvent();
        public UnityEvent OnConversationResumed = new UnityEvent();

        [Header("Dialogue Views")]
        [SerializeField] protected DialogueViewBase dialogueView;

        [Header("Function Invocations")]
        [SerializeField]
        private bool searchAllAssemblies;

        [SerializeField] private List<string> includedAssemblies = new List<string>();

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

            OnConversationStart.AddListener(PersistentOnConversationStart.Invoke);
            OnConversationEnd.AddListener(PersistentOnConversationEnd.Invoke);

            OnConversationStart.Invoke();
            AdvanceAndDisplay();
        }

        /// <summary>
        /// Stops the currently playing conversation and clears the dialogue box.
        /// </summary>
        public void StopConversation()
        {
            if (_displayCoroutine != null)
            {
                StopCoroutine(_displayCoroutine);
                _displayCoroutine = null;
            }

            dialogueView.ClearView(enginePlugins);

            _linePlaying = false;
            _currentConversation = null;
            _currentRuntimeLine = null;
            _cursor = null;
            _choicesActive = false;
            _isPaused = false;

            OnConversationEnd.Invoke();

            OnConversationStart.RemoveAllListeners();
            OnConversationEnd.RemoveAllListeners();
        }

        public void PauseConversation()
        {
            if (_currentConversation == null || _isPaused) return;
            _isPaused = true;
            dialogueView.Pause();
            OnConversationPaused.Invoke();
        }

        public void ResumeConversation()
        {
            if (_currentConversation == null || !_isPaused) return;
            _isPaused = false;
            dialogueView.Resume();
            OnConversationResumed.Invoke();
        }

        public bool IsConversationPaused()
        {
            return _isPaused;
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
        /// Attempts to display the next line. If the current line is still playing,
        /// this does nothing. Handles jump targets and conversation completion.
        /// </summary>
        public void TryDisplayNextLine()
        {
            if (_linePlaying) return;
            if (_currentConversation == null) return;
            if (_choicesActive) return;

            if (_currentRuntimeLine != null)
            {
                string jumpTarget = _currentRuntimeLine.Line.JumpTarget;
                if (!string.IsNullOrEmpty(jumpTarget))
                {
                    JumpTo(jumpTarget);
                    return;
                }
            }

            dialogueView.ClearView(enginePlugins);
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
            RuntimeContentNode displayable = AdvanceToNextDisplayable();

            if (displayable is RuntimeLine line)
            {
                _currentRuntimeLine = line;
                ResolveLineText(line);

                RuntimeContentNode peek = PeekNextDisplayable();
                if (peek is RuntimeChoicePoint choicePoint)
                {
                    _displayCoroutine = StartCoroutine(DisplayDialogue(line.Line, choicePoint));
                }
                else
                {
                    _displayCoroutine = StartCoroutine(DisplayDialogue(line.Line, null));
                }
            }
            else if (displayable is RuntimeChoicePoint standAloneChoices)
            {
                _choicesActive = true;
                List<Choice> resolved = ResolveChoices(standAloneChoices);
                dialogueView.DisplayChoices(resolved, OnChoiceSelected);
            }
            else
            {
                StopConversation();
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
                dialogueView.DisplayChoices(resolved, OnChoiceSelected);
                AdvanceToNextDisplayable();
            }

            yield return StartCoroutine(OnDisplayLine(line));

            _linePlaying = false;
            yield return null;
        }

        /// <summary>
        /// Called when a dialogue line is ready to be displayed. Override this to
        /// customize how lines are presented, add custom effects, or inject
        /// additional logic between lines.
        /// </summary>
        protected virtual IEnumerator OnDisplayLine(Line line)
        {
            dialogueView.SetView(line);

            foreach (EnginePlugin plugin in enginePlugins)
            {
                plugin.Display(line);
            }

            InvokeFunctions(line.LineContent.Invocations, line);

            yield return new WaitUntil(() => !dialogueView.IsStillDisplaying() || _isPaused);

            if (_isPaused)
            {
                yield return new WaitUntil(() => !_isPaused);
            }
        }

        private void OnChoiceSelected(Choice choice)
        {
            _choicesActive = false;

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

        private void ResolveLineText(RuntimeLine runtimeLine)
        {
            LineResolver.Resolve(runtimeLine, _currentAssetName);
        }

        private List<Choice> ResolveChoices(RuntimeChoicePoint choicePoint)
        {
            return LineResolver.ResolveChoices(choicePoint, _currentAssetName);
        }

        private Parser.Expressions.ExpressionEvaluator CreateEvaluator()
        {
            string assetName = _currentAssetName;
            return new Parser.Expressions.ExpressionEvaluator(name => VariableStore.Resolve(assetName, name));
        }

        /// <summary>
        /// Invokes [DialogueFunction] methods found in the current line's invocations.
        /// </summary>
        protected virtual void InvokeFunctions(List<FunctionInvocation> functionInvocations, Line line)
        {
            if (functionInvocations == null || functionInvocations.Count == 0)
                return;

            IEnumerable<MethodInfo> dialogueMethods = GetDialogueMethods();

            foreach (FunctionInvocation function in functionInvocations)
            {
                MethodInfo method = ResolveDialogueFunction(dialogueMethods, function);
                if (method == null)
                    continue;

                object[] args = BuildInvocationArguments(method, function);
                if (args == null)
                    continue;

                object result = method.Invoke(null, args);

                if (method.ReturnType != typeof(void))
                {
                    string insertText = Convert.ToString(result, CultureInfo.InvariantCulture) ?? "";
                    line.LineContent.Text =
                        line.LineContent.Text.Insert(function.CharacterIndex, insertText);
                    dialogueView.SetView(line);
                }
            }
        }

        private MethodInfo ResolveDialogueFunction(IEnumerable<MethodInfo> methods, FunctionInvocation function)
        {
            bool nameFound = false;
            int argCount = function.Arguments != null ? function.Arguments.Count : 0;

            foreach (MethodInfo method in methods)
            {
                if (method.Name != function.Name)
                    continue;

                nameFound = true;
                ParameterInfo[] parameters = method.GetParameters();
                int expectedArgs = parameters.Length;

                if (expectedArgs > 0 && typeof(DialogueEngineBase).IsAssignableFrom(parameters[0].ParameterType))
                    expectedArgs--;

                if (expectedArgs == argCount)
                    return method;
            }

            if (nameFound)
            {
                Debug.LogWarning($"[Ibralogue] [line {function.Line}:{function.Column}] " +
                    $"[DialogueFunction] '{function.Name}' exists but no overload accepts {argCount} argument(s)");
            }
            else
            {
                Debug.LogWarning($"[Ibralogue] [line {function.Line}:{function.Column}] " +
                    $"No [DialogueFunction] method found for invocation '{function.Name}'");
            }

            return null;
        }

        private object[] BuildInvocationArguments(MethodInfo method, FunctionInvocation function)
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
                    Debug.LogWarning($"[Ibralogue] [line {function.Line}:{function.Column}] " +
                        $"Failed to convert argument {argIndex} ('{rawValue}') to {paramType.Name} " +
                        $"for function '{function.Name}': {ex.Message}");
                    return null;
                }

                argIndex++;
            }

            return args;
        }

        protected IEnumerable<MethodInfo> GetDialogueMethods()
        {
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

            List<MethodInfo> methods = new List<MethodInfo>();
            foreach (Assembly assembly in assemblies)
            {
                IEnumerable<MethodInfo> allMethods = assembly.GetTypes()
                    .SelectMany(t => t.GetMethods())
                    .Where(m => m.GetCustomAttributes(typeof(DialogueFunctionAttribute), false).Length > 0);
                methods.AddRange(allMethods);
            }

            return methods;
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
