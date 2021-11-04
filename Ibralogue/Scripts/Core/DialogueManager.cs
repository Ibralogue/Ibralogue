using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Ibralogue
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }
        
        public static readonly Dictionary<string, string> GlobalVariables = new Dictionary<string, string>();
        public static readonly Dictionary<string, MethodInfo> DialogueFunctions = new Dictionary<string, MethodInfo>();

        public static UnityEvent OnConversationStart = new UnityEvent();
        public static UnityEvent OnConversationEnd = new UnityEvent();
        
        private List<Conversation> _parsedConversations;
        private Conversation _currentConversation;
        
        private int _dialogueIndex;
        private bool _linePlaying;

        [Header("Dialogue UI")]
        [SerializeField] private float timeBetweenCharacters = 0.1f;
        [SerializeField] private Transform choiceButtonHolder;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI sentenceText;
        [SerializeField] private Image speakerPortrait;
        
        private List<GameObject> _choiceButtonInstances;
        [Header("Prefabs")]
        [SerializeField] private GameObject choiceButton;
        

        [Header("Function Invocations")] 
        [SerializeField] private bool searchAllAssemblies;
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
            //Getting all methods in every assembly and adding them to our own local list for potential invocation.
            IEnumerable<MethodInfo> allDialogueMethods = GetDialogueMethods();
            foreach (MethodInfo methodInfo in allDialogueMethods)
            {
                DialogueFunctions.Add(methodInfo.Name,methodInfo);
            }
            _choiceButtonInstances = new List<GameObject>();
        }

        /// <summary>
        /// Starts a dialogue by parsing all the text in a file, clearing the dialogue box and starting the <see href="DisplayDialogue"/>DisplayDialogue</see> function.
        /// </summary>
        /// <param name="interactionDialogue">The dialogue file that we want to use in the conversation</param>
        /// <param name="startIndex">The index of the conversation you want to start.</param>
        public void StartConversation(TextAsset interactionDialogue, int startIndex = 0)
        {
            _parsedConversations = DialogueParser.ParseDialogue(interactionDialogue);
            _currentConversation = _parsedConversations[startIndex];
            ClearDialogueBox(true);
            OnConversationStart.Invoke();
            StartCoroutine(DisplayDialogue());
        }
        
        /// <summary>
        /// Varies from StartConversation due to not requiring a conversation to start the Dialogue.
        /// <remarks>
        /// Should only be used inside the DialogueManager, as files should ALWAYS be parsed before any conversations
        /// are started (yse the other overload method for this purpose). This function assumes that you have already parsed the dialogue file, and is to be
        /// used to avoid parsing the whole file again.
        /// </remarks>
        /// </summary>
        /// <param name="conversation"></param>
        private void StartConversation(Conversation conversation)
        {
            _currentConversation = conversation;
            ClearDialogueBox(true);
            OnConversationStart.Invoke();
            StartCoroutine(DisplayDialogue());
        }

        /// <summary>
        /// The DisplayDialogue coroutine displays the dialogue character by character in a scrolling manner and sets all other
        /// relevant values.
        /// </summary>
        private IEnumerator DisplayDialogue()
        {
            nameText.text = _currentConversation.Dialogues[_dialogueIndex].Speaker;
            _linePlaying = true;
            sentenceText.text = _currentConversation.Dialogues[_dialogueIndex].Sentence.Text;
            
            Dictionary<int,string> functionInvocations = _currentConversation.Dialogues[_dialogueIndex].Sentence.Invocations;
            if (functionInvocations != null && functionInvocations
                    .TryGetValue(_dialogueIndex, out string functionName))
            {
                if(DialogueFunctions.TryGetValue(functionName, out MethodInfo methodInfo))
                {
                    methodInfo.Invoke(null, null); //TODO: Function invocation for nonstatic methods.
                }
            }
            DisplaySpeakerImage();
            foreach(char _ in _currentConversation.Dialogues[_dialogueIndex].Sentence.Text)
            {
                sentenceText.maxVisibleCharacters++;
                yield return new WaitForSeconds(timeBetweenCharacters); 
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
            if (_dialogueIndex < _currentConversation.Dialogues.Count - 1)
            {
                _dialogueIndex++;
                StartCoroutine(DisplayDialogue());
                if (_currentConversation.Choices != null && _currentConversation.Choices.Count > 0)
                {
                    if (_dialogueIndex == _currentConversation.Choices
                            .FirstOrDefault(x => x.Value == _dialogueIndex).Value)
                    {
                        DisplayChoices();
                    }
                }
            } 
            else
            {
                OnConversationEnd.Invoke();
            }
        }

        protected void DisplayChoices()
        {
            if (_currentConversation.Choices == null || !_currentConversation.Choices.Any()) return;
            foreach (Choice choice in _currentConversation.Choices.Keys)
            {
                Button choiceButtonInstance =
                    Instantiate(choiceButton,choiceButtonHolder).GetComponent<Button>();
                _choiceButtonInstances.Add(choiceButtonInstance.gameObject);
                int conversationIndex = _parsedConversations.FindIndex(c => c.Name == choice.LeadingConversationName);
                if(conversationIndex == -1)
                    throw new ArgumentException($"No conversation called '{choice.LeadingConversationName}' found for choice '{choice.ChoiceName}' in '{_currentConversation.Name}'.");
                choiceButtonInstance.GetComponentInChildren<TextMeshProUGUI>().text = choice.ChoiceName;
                choiceButtonInstance.onClick.AddListener(() => StartConversation(_parsedConversations[conversationIndex])); 
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
            List<MethodInfo> methods = new List<MethodInfo>();
            foreach (Assembly assembly in assemblies)
            {
                IEnumerable<MethodInfo> allMethods = assembly.GetTypes()
                    .SelectMany(t => t.GetMethods())
                    .Where(m => m.GetCustomAttributes(typeof(DialogueFunction), false).Length > 0);
                methods.AddRange(allMethods);
            }
            return methods;
        }    

        /// <summary>
        /// Sets the speaker image and makes the Image transparent if there is no speaker image.
        /// </summary>
        protected void DisplaySpeakerImage()
        {
            speakerPortrait.color = _currentConversation.Dialogues[_dialogueIndex].SpeakerImage == null ? new Color(0,0,0, 0) : new Color(255,255,255,255);
            speakerPortrait.sprite = _currentConversation.Dialogues[_dialogueIndex].SpeakerImage;
        }

        /// <summary>
        /// Clears all text and Images in the dialogue box.
        /// </summary>
        private void ClearDialogueBox(bool newConversation = false)
        {
            _linePlaying = false;
            nameText.text = string.Empty;
            sentenceText.text = string.Empty;
            speakerPortrait.color = new Color(0, 0, 0, 0);
            sentenceText.maxVisibleCharacters = 0;
            if (!newConversation) return;
            _dialogueIndex = 0;
            foreach (GameObject buttonInstance in _choiceButtonInstances)
            {
                Destroy(buttonInstance);
            }
        }
    }
}