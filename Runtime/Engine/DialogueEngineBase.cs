using Ibralogue.Parser;
using Ibralogue.Plugins;
using Ibralogue.Views;
using System;
using System.Collections;
using System.Collections.Generic;
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
            StartCoroutine(DisplayDialogue());
        }

        /// <summary>
        /// Stops the currently playing conversation and clears the dialogue box.
        /// </summary>
        public void StopConversation()
        {
            StopCoroutine(DisplayDialogue());
            dialogueView.ClearView(enginePlugins);

            _linePlaying = false;
            _lineIndex = 0;
            _currentConversation = null;

            OnConversationEnd.Invoke();

            OnConversationStart.RemoveAllListeners();
            OnConversationEnd.RemoveAllListeners();
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
            yield return new WaitUntil(() => !dialogueView.IsStillDisplaying());

            _linePlaying = false;
            yield return null;
        }

        /// <summary>
        /// Looks for functions and invokes them in a given line. The function also handles multiple return types and the parameters passed in.
        /// </summary>
        /// <param name="functionInvocations">The invocations inside the current line being displayed.</param>
        protected virtual void InvokeFunctions(Dictionary<int, string> functionInvocations)
        {
            IEnumerable<MethodInfo> dialogueMethods = GetDialogueMethods();

            if (functionInvocations != null)
            {
                foreach (KeyValuePair<int, string> function in functionInvocations)
                {
                    foreach (MethodInfo methodInfo in dialogueMethods)
                    {
                        if (methodInfo.Name != function.Value)
                            continue;

                        if (methodInfo.ReturnType == typeof(string))
                        {

                            string replacedText = methodInfo.GetParameters().Length > 0 ? (string)methodInfo.Invoke(null, new object[] { this }) : (string)methodInfo.Invoke(null, null);
                            _currentConversation.Lines[_lineIndex].LineContent.Text = _currentConversation.Lines[_lineIndex].LineContent.Text.Insert(function.Key, replacedText);

                            dialogueView.SetView(_currentConversation, _lineIndex);
                        }
                        else
                        {
                            if (methodInfo.GetParameters().Length > 0)
                            {
                                methodInfo.Invoke(null, new object[] { this });
                            }
                            else
                            {
                                methodInfo.Invoke(null, null);
                            }
                        }
                    }
                }

            }
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