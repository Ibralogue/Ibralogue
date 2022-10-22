using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ibralogue.Parser;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ibralogue
{
    public class DialogueManager : MonoBehaviour
    {

        public static readonly Dictionary<string, string> GlobalVariables = new Dictionary<string, string>();

        public UnityEvent OnConversationStart { get; set; } = new UnityEvent();
        public UnityEvent OnConversationEnd { get; set; } = new UnityEvent();

        public List<Conversation> ParsedConversations { get; private set; }
        private Conversation _currentConversation;
        
        private int _dialogueIndex;
        private bool _linePlaying;

        [Header("Dialogue UI")]
        [SerializeField] private float scrollSpeed = 25f;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI sentenceText;
        [SerializeField] private Image speakerPortrait;
        
        [Header("Choice UI")]
        [SerializeField] private Transform choiceButtonHolder;
        [SerializeField] private GameObject choiceButton;
        private List<GameObject> _choiceButtonInstances;
        
        [Header("Function Invocations")] 
        [SerializeField] private bool searchAllAssemblies;
        [SerializeField] private List<string> includedAssemblies;
        
        /// <summary>
        /// Starts a dialogue by parsing all the text in a file, clearing the dialogue box and starting the <see cref="DisplayDialogue"/> function.
        /// </summary>
        /// <param name="interactionDialogue">The dialogue file that we want to use in the conversation</param>
        /// <param name="startIndex">The index of the conversation you want to start.</param>
        public void StartConversation(TextAsset interactionDialogue, int startIndex = 0)
        {
            ParsedConversations = DialogueParser.ParseDialogue(interactionDialogue);
            _currentConversation = ParsedConversations[startIndex];
            ClearDialogueBox(true);
            OnConversationStart.Invoke();
            StartCoroutine(DisplayDialogue());
        }
        
        /// <summary>
        /// Varies from StartConversation due to not requiring a conversation to start the Line.
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
            nameText.text = _currentConversation.Lines[_dialogueIndex].Speaker;
            _linePlaying = true;
            sentenceText.text = _currentConversation.Lines[_dialogueIndex].LineContents.Text;

            IEnumerable<MethodInfo> allDialogueMethods = GetDialogueMethods();
            Dictionary<int,string> functionInvocations = new Dictionary<int, string>();
            functionInvocations = _currentConversation.Lines[_dialogueIndex].LineContents.Invocations;

            DisplaySpeakerImage();
            int index = 0; 
            while(index < _currentConversation.Lines[_dialogueIndex].LineContents.Text.Length)
            {
                if (functionInvocations != null && functionInvocations
                        .TryGetValue(sentenceText.maxVisibleCharacters, out string functionName))
                {
                    foreach (MethodInfo methodInfo in allDialogueMethods)
                    {
                        if (methodInfo.Name != functionName)
                            continue;

                        if (methodInfo.ReturnType == typeof(string))
                        {
                            string replacedText = (string)methodInfo.Invoke(null, null);
                            string processedSentence = _currentConversation.Lines[_dialogueIndex].LineContents.Text.Insert(index, replacedText);
                            sentenceText.text = processedSentence;
                            index -= processedSentence.Length -
                                     _currentConversation.Lines[_dialogueIndex].LineContents.Text.Length;
                        }
                        else
                        {
                            methodInfo.Invoke(null, null);
                        }
                    }
                }
                index++;
                sentenceText.maxVisibleCharacters++;
                yield return new WaitForSeconds(1.0f / scrollSpeed);
            }

            _linePlaying = false;
            yield return null;
        }
        
        /// <summary>
        /// Clears the dialogue box and displays the next <see cref="Line"/> if no sentences are left in the
        /// current one.
        /// </summary>
        public void DisplayNextLine()
        {
            if (_linePlaying) return;
            ClearDialogueBox();
            if (_dialogueIndex < _currentConversation.Lines.Count - 1)
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

        /// <summary>
        /// Uses the Unity UI system and TextMeshPro to render choice buttons.
        /// </summary>
        protected void DisplayChoices()
        {
            _choiceButtonInstances = new List<GameObject>();
            if (_currentConversation.Choices == null || !_currentConversation.Choices.Any()) return;
            foreach (Choice choice in _currentConversation.Choices.Keys)
            {
                Button choiceButtonInstance =
                    Instantiate(choiceButton,choiceButtonHolder).GetComponent<Button>();
                _choiceButtonInstances.Add(choiceButtonInstance.gameObject);
                int conversationIndex = ParsedConversations.FindIndex(c => c.Name == choice.LeadingConversationName);
                if(conversationIndex == -1)
                    DialogueLogger.LogError(2,  $"No conversation called \"{choice.LeadingConversationName}\" found for choice \"{choice.ChoiceName}\" in \"{_currentConversation.Name}\".", gameObject);
                choiceButtonInstance.GetComponentInChildren<TextMeshProUGUI>().text = choice.ChoiceName;
                choiceButtonInstance.onClick.AddListener(() => StartConversation(ParsedConversations[conversationIndex])); 
            }
        }

        /// <summary>
        /// Gets all methods for the current assembly, other specified assemblies, or all assemblies, and checks them against the
        /// DialogueFunction attribute.
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
                    .Where(m => m.GetCustomAttributes(typeof(DialogueFunctionAttribute), false).Length > 0);
                methods.AddRange(allMethods);
            }
            return methods;
        }    

        /// <summary>
        /// Sets the speaker image and makes the Image transparent if there is no speaker image.
        /// </summary>
        protected void DisplaySpeakerImage()
        {
            speakerPortrait.color = _currentConversation.Lines[_dialogueIndex].SpeakerImage == null ? new Color(0,0,0, 0) : new Color(255,255,255,255);
            speakerPortrait.sprite = _currentConversation.Lines[_dialogueIndex].SpeakerImage;
        }

        /// <summary>
        /// Clears all text and Images in the dialogue box.
        /// </summary>
        private void ClearDialogueBox(bool newConversation = false)
        {
            _linePlaying = false;
            nameText.text = string.Empty;
            sentenceText.text = string.Empty;
            sentenceText.maxVisibleCharacters = 0;
            speakerPortrait.color = new Color(0, 0, 0, 0);
            
            if (!newConversation) 
                return;
            _dialogueIndex = 0;
            if (_choiceButtonInstances == null) 
                return;
            foreach (GameObject buttonInstance in _choiceButtonInstances)
            {
                Destroy(buttonInstance);
            }
        }
    }
}