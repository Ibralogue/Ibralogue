using System.Collections;
using System.Collections.Generic;
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

        public static UnityEvent OnDialogueStart = new UnityEvent();
        public static UnityEvent OnDialogueEnd = new UnityEvent();
        
        public static UnityEvent<int> OnDialogueChange;

        private string[] _currentDialogueLines;
        private List<Dialogue> _parsedDialogues;
        
        private int _currentDialogueIndex;
        private int _currentSentenceIndex;
        private bool _linePlaying;

        [SerializeField] private TextMeshProUGUI sentenceText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image speakerPortrait;

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
            nameText.text = _parsedDialogues[_currentDialogueIndex].speaker;
            _linePlaying = true;
            sentenceText.text = _parsedDialogues[_currentDialogueIndex].sentences[_currentSentenceIndex];
            DisplaySpeakerImage();

            foreach(char unused in _parsedDialogues[_currentDialogueIndex].sentences[_currentSentenceIndex])
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
            if (_currentSentenceIndex < _parsedDialogues[_currentDialogueIndex].sentences.Count - 1)
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
        /// Sets the speaker image and makes the Image transparent if there is no speaker image.
        /// </summary>
        private void DisplaySpeakerImage()
        {
            speakerPortrait.color = _parsedDialogues[_currentDialogueIndex].speakerImage == null ? new Color(0,0,0, 0) : new Color(255,255,255,255);
            speakerPortrait.sprite = _parsedDialogues[_currentDialogueIndex].speakerImage;
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