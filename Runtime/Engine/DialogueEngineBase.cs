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
    public static class DialogueGlobals
    {
        public static readonly Dictionary<string, string> GlobalVariables = new Dictionary<string, string>();
    }

    public abstract class DialogueEngineBase : MonoBehaviour
    {
        protected EnginePlugin[] enginePlugins;

        public UnityEvent PersistentOnConversationStart = new UnityEvent();
        public UnityEvent PersistentOnConversationEnd = new UnityEvent();

        [HideInInspector] public UnityEvent OnConversationStart = new UnityEvent();
        [HideInInspector] public UnityEvent OnConversationEnd = new UnityEvent();

        public List<Conversation> ParsedConversations { get; protected set; }
        protected Conversation _currentConversation;

        protected int _lineIndex;
        protected bool _linePlaying;

        protected bool _isPaused = false;
        private Coroutine _displayCoroutine;

        public UnityEvent OnConversationPaused = new UnityEvent();
        public UnityEvent OnConversationResumed = new UnityEvent();

        [Header("Dialogue Views")]
        [SerializeField] protected DialogueViewBase dialogueView;

        [Header("Function Invocations")]
        [SerializeField]
        private bool searchAllAssemblies;

        [SerializeField] private List<string> includedAssemblies = new List<string>();

        /// <summary>
        /// Starts a dialogue by parsing all the text in a file, clearing the dialogue box and starting the <see cref="DisplayDialogue"/> function.
        /// </summary>
        /// <param name="interactionDialogue">The dialogue file that we want to use in the conversation</param>
        /// <param name="startIndex">The index of the conversation you want to start.</param>
        public void StartConversation(DialogueAsset interactionDialogue, int startIndex = 0)
        {
            if (interactionDialogue == null)
                throw new ArgumentNullException(nameof(interactionDialogue));

            ParsedConversations = DialogueParser.ParseDialogue(interactionDialogue);

            if (startIndex < 0 || startIndex > ParsedConversations.Count)
                throw new ArgumentOutOfRangeException(nameof(startIndex),
                    "Expected value is between 0 and conversations count (exclusive)");

            enginePlugins = GetComponents<EnginePlugin>();
            SwitchConversation(ParsedConversations[startIndex]);
        }

        /// <summary>
        /// <remarks>
        /// Switches to a different conversation. This functoin assumes the dialogue file is parsed first.
        /// </remarks>
        /// </summary>
        /// <param name="conversation"></param>
        public void SwitchConversation(Conversation conversation)
        {
            StopConversation();
            _currentConversation = conversation;

            OnConversationStart.AddListener(PersistentOnConversationStart.Invoke);
            OnConversationEnd.AddListener(PersistentOnConversationEnd.Invoke);

            OnConversationStart.Invoke();
            _displayCoroutine = StartCoroutine(DisplayDialogue());
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
            _lineIndex = 0;
            _currentConversation = null;
            _isPaused = false;

            OnConversationEnd.Invoke();

            OnConversationStart.RemoveAllListeners();
            OnConversationEnd.RemoveAllListeners();
        }

        /// <summary>
        /// Pauses the current conversation.
        /// </summary>
        public void PauseConversation()
        {
            if (_currentConversation == null || _isPaused) return;

            _isPaused = true;
            dialogueView.Pause();
            OnConversationPaused.Invoke();
        }

        /// <summary>
        /// Resumes the paused conversation.
        /// </summary>
        public void ResumeConversation()
        {
            if (_currentConversation == null || !_isPaused) return;

            _isPaused = false;
            dialogueView.Resume();
            OnConversationResumed.Invoke();
        }

        /// <summary>
        /// Checks if conversation is currently paused.
        /// </summary>
        public bool IsConversationPaused()
        {
            return _isPaused;
        }

        /// <summary>
        /// Jumps to a  given conversation in the dialogue by using its name.
        /// </summary>
        /// <param name="conversationName">Name as seen in the DialogueAsset</param>
        public void JumpTo(string conversationName)
        {
            if (ParsedConversations == null || ParsedConversations.Count == 0)
                throw new InvalidOperationException(
                    "There is no ongoing conversation, therefore the jump cannot be executed");

            Conversation conversation = ParsedConversations.Find(c => c.Name == conversationName);

            if (conversation.Name == null)
                throw new ArgumentException($"There is no {nameof(conversation)} matching the given argument",
                    nameof(conversationName));

            SwitchConversation(conversation);
        }

        /// <summary>
        // Displays the entire dialogue and displays choices if present.
        /// </summary>
        protected virtual IEnumerator DisplayDialogue()
        {
            _linePlaying = true;

            if (_currentConversation.Choices != null && _currentConversation.Choices.Count > 0)
            {
                KeyValuePair<Choice, int> foundChoice =
                    _currentConversation.Choices.FirstOrDefault(x => x.Value == _lineIndex);
                if (foundChoice.Key != null && _lineIndex == foundChoice.Value) dialogueView.DisplayChoices(this, _currentConversation, ParsedConversations);
            }

            dialogueView.SetView(_currentConversation, _lineIndex);

            foreach(EnginePlugin plugin in enginePlugins)
            {
                plugin.Display(_currentConversation,_lineIndex);
            }

            InvokeFunctions(_currentConversation.Lines[_lineIndex].LineContent.Invocations);

            yield return new WaitUntil(() => !dialogueView.IsStillDisplaying() || _isPaused);

            if (_isPaused)
            {
                yield return new WaitUntil(() => !_isPaused);
            }

            _linePlaying = false;
            yield return null;
        }

        /// <summary>
        /// Looks for functions and invokes them in a given line. Supports multiple arguments
        /// and any return type that can be converted to a string for text insertion.
        /// </summary>
        /// <param name="functionInvocations">The invocations inside the current line being displayed.</param>
        protected virtual void InvokeFunctions(List<FunctionInvocation> functionInvocations)
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
                    _currentConversation.Lines[_lineIndex].LineContent.Text =
                        _currentConversation.Lines[_lineIndex].LineContent.Text.Insert(function.CharacterIndex, insertText);
                    dialogueView.SetView(_currentConversation, _lineIndex);
                }
            }
        }

        /// <summary>
        /// Finds the first <see cref="DialogueFunctionAttribute"/> method whose name matches the
        /// invocation and whose parameter count is compatible with the supplied arguments.
        /// The first parameter may optionally accept a <see cref="DialogueEngineBase"/> instance.
        /// </summary>
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

        /// <summary>
        /// Converts the string arguments from a <see cref="FunctionInvocation"/> into a typed
        /// <c>object[]</c> matching the target method's parameter signature. Returns null if
        /// any conversion fails.
        /// </summary>
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

        /// <summary>
        /// Clears the dialogue box and displays the next line if no sentences are left in the
        /// current one.
        /// </summary>
        public void TryDisplayNextLine()
        {
            if (_linePlaying) return;
            if (_currentConversation == null) return;
            //if (_choiceButtonInstances.Count > 0) return;

            // Check if the current line has a jump target
            string jumpTarget = _currentConversation.Lines[_lineIndex].JumpTarget;
            if (!string.IsNullOrEmpty(jumpTarget))
            {
                JumpTo(jumpTarget);
                return;
            }

            _linePlaying = false;
            dialogueView.ClearView(enginePlugins);

            if (_lineIndex < _currentConversation.Lines.Count - 1)
            {
                _lineIndex++;
                StartCoroutine(DisplayDialogue());
            }
            else
            {
                StopConversation();
            }
        }

        /// <summary>
        /// Gets all methods for the current assembly, other specified assemblies, or all assemblies, and checks them against the
        /// DialogueFunction attribute.
        /// </summary>
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
}