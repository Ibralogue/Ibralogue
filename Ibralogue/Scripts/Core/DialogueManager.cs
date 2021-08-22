using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ibralogue
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }
        
        public static readonly Dictionary<string, string> GlobalVariables = new Dictionary<string, string>();
        public static readonly Dictionary<string, MethodInfo> DialogueFunctions = new Dictionary<string, MethodInfo>();

        public static UnityEvent OnDialogueStart = new UnityEvent();
        public static UnityEvent OnDialogueEnd = new UnityEvent();

        private string[] _currentDialogueLines;
        private List<Dialogue> _parsedDialogues;
        
        private int _currentDialogueIndex;
        private int _currentSentenceIndex;
        private bool _linePlaying;

        [Header("Dialogue UI")]
        [SerializeField] private TextMeshProUGUI sentenceText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image speakerPortrait;

        [Header("Function Invocations")] [SerializeField]
        private bool searchAllAssemblies;
        [SerializeField] private List<string> includedAssemblies;
        

        protected void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
            
            IEnumerable<MethodInfo> allDialogueMethods = GetDialogueMethods();
            foreach (MethodInfo methodInfo in allDialogueMethods)
            {
                DialogueFunctions.Add(methodInfo.Name,methodInfo);
            }
        }

        /// <summary>
        /// Starts a dialogue by clearing the dialogue box and starting the <see href="DisplayDialogue"/>DisplayDialogue</see> function.
        /// </summary>
        /// <param name="interactionDialogue">The initial Dialogue that we want to use in the conversation</param>
        public void StartConversation(TextAsset interactionDialogue)
        {
            _parsedDialogues = DialogueParser.ParseDialogue(interactionDialogue);
            ClearDialogueBox();
            OnDialogueStart.Invoke();
            StartCoroutine(DisplayDialogue());
        }
        
        /// <summary>
        /// The DisplayDialogue coroutine displays the dialogue character by character in a scrolling manner and sets all other
        /// relevant values.
        /// </summary>
        private IEnumerator DisplayDialogue()
        {
            nameText.text = _parsedDialogues[_currentDialogueIndex].Speaker;
            _linePlaying = true;
            sentenceText.text = _parsedDialogues[_currentDialogueIndex].Sentences[_currentSentenceIndex];
            
            Dictionary<int,string> functionInvocations = _parsedDialogues[_currentDialogueIndex].FunctionInvocations;
            if (functionInvocations != null && functionInvocations
                    .TryGetValue(_currentSentenceIndex, out string functionName))
            {
                if(DialogueFunctions.TryGetValue(functionName, out MethodInfo methodInfo))
                {
                    methodInfo.Invoke(null, null); //TODO: Function invocation for nonstatic methods.
                }
            }
            
            DisplaySpeakerImage();
            foreach(char _ in _parsedDialogues[_currentDialogueIndex].Sentences[_currentSentenceIndex])
            {
                sentenceText.maxVisibleCharacters++;
                yield return new WaitForSeconds(0.1f); //TODO: Make scroll speed modifiable
            }
            _linePlaying = false;
            yield return null;
        }
        
        /// <summary>
        /// Clears the dialogue box and displays the next <see cref="Dialogue"/> if no sentences are left in the
        /// current one.
        /// </summary>
        public void DisplayNextLine()
        {
            if (_linePlaying) return;
            ClearDialogueBox();
            if (_currentSentenceIndex < _parsedDialogues[_currentDialogueIndex].Sentences.Count - 1)
            {
                _currentSentenceIndex++;
                StartCoroutine(DisplayDialogue());
                return;
            }
            if (_currentDialogueIndex < _parsedDialogues.Count - 1)
            {
                _currentDialogueIndex++;
                _currentSentenceIndex = 0;
                StartCoroutine(DisplayDialogue());
            }
            else
            {
                OnDialogueEnd.Invoke();
            }
        }

        /// <summary>
        /// Gets ALL methods in ALL classes for the current assembly and checks them against the
        /// DialogueFunction attribute. Use this function with caution.
        /// </summary>
        private IEnumerable<MethodInfo> GetDialogueMethods()
        {
            List<Assembly> assemblies = new List<Assembly>();
            Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            if(searchAllAssemblies) assemblies.AddRange(allAssemblies);
            else
            {
                foreach (Assembly assembly in allAssemblies)
                {
                    string name = assembly.GetName().Name;
                    if (name == "Assembly-CSharp" || includedAssemblies.Contains(name) || assembly == Assembly.GetExecutingAssembly())
                    {
                        assemblies.Add(assembly);
                    }
                }
            }
            IEnumerable<MethodInfo> methods = new List<MethodInfo>();
            foreach (Assembly assembly in assemblies)
            {
                methods = assembly.GetTypes()
                    .SelectMany(t => t.GetMethods())
                    .Where(m => m.GetCustomAttributes(typeof(DialogueFunction), false).Length > 0);
            }
            return methods;
        }    

        /// <summary>
        /// Sets the speaker image and makes the Image transparent if there is no speaker image.
        /// </summary>
        private void DisplaySpeakerImage()
        {
            speakerPortrait.color = _parsedDialogues[_currentDialogueIndex].SpeakerImage == null ? new Color(0,0,0, 0) : new Color(255,255,255,255);
            speakerPortrait.sprite = _parsedDialogues[_currentDialogueIndex].SpeakerImage;
        }

        /// <summary>
        /// Clears all text and Images in the dialogue box.
        /// </summary>
        private void ClearDialogueBox()
        {
            nameText.text = string.Empty;
            sentenceText.text = string.Empty;
            speakerPortrait.color = new Color(0, 0, 0, 0);
            sentenceText.maxVisibleCharacters = 0;
            _linePlaying = false;
        }
    }
}