using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Ibralogue
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        private string[] _currentDialogueLines;
        
        private List<Dialogue> _parsedDialogues;

        public static Dictionary<string, string> GlobalVariables = new Dictionary<string, string>();

        private int _currentDialogueIndex;
        private int _currentSentenceIndex;

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

        public void StartConversation(TextAsset interactionDialogue)
        {
            _parsedDialogues = DialogueParser.ParseDialogue(interactionDialogue);
            ClearDialogueBox();
            StartCoroutine(DisplayDialogue());
        }

        private IEnumerator DisplayDialogue()
        {
            nameText.text = _parsedDialogues[_currentDialogueIndex].speaker;
            sentenceText.text = _parsedDialogues[_currentDialogueIndex].sentences[_currentSentenceIndex];
            DisplaySpeakerImage();

            foreach(char unused in _parsedDialogues[_currentDialogueIndex].sentences[_currentSentenceIndex])
            {
                sentenceText.maxVisibleCharacters++;
                yield return new WaitForSeconds(0.1f); 
            }
            yield return null;
        }

        public void DisplayNextLine()
        {
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
        }
        
        private void DisplaySpeakerImage()
        {
            speakerPortrait.color = _parsedDialogues[_currentDialogueIndex].speakerImage == null ? new Color(0,0,0, 0) : new Color(255,255,255,255);
            speakerPortrait.sprite = _parsedDialogues[_currentDialogueIndex].speakerImage;
        }

        private void ClearDialogueBox()
        {
            nameText.text = string.Empty;
            sentenceText.text = string.Empty;
            sentenceText.maxVisibleCharacters = 0;
        }
    }
}