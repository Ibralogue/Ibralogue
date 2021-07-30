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
        
        private List<Dialogue> parsedDialogue;
        private int currentDialogueIndex;
        private int currentSentenceIndex;

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
            parsedDialogue = DialogueParser.ParseDialogue(interactionDialogue);
            ClearDialogueBox();
            StartCoroutine(DisplayDialogue());
        }

        private IEnumerator DisplayDialogue()
        { 
            
            nameText.text = parsedDialogue[currentDialogueIndex].speaker;
            sentenceText.text = parsedDialogue[currentDialogueIndex].sentences[currentSentenceIndex];
            DisplaySpeakerImage();

            foreach(char unused in parsedDialogue[currentDialogueIndex].sentences[currentSentenceIndex])
            {
                sentenceText.maxVisibleCharacters++;
                yield return new WaitForSeconds(0.1f); 
            }
            yield return null;
        }

        public void DisplayNextLine()
        {
            ClearDialogueBox();
            if (currentSentenceIndex < parsedDialogue[currentDialogueIndex].sentences.Count - 1)
            {
                currentSentenceIndex++;
                StartCoroutine(DisplayDialogue());
                return;
            }
            if (currentDialogueIndex < parsedDialogue.Count - 1)
            {
                currentDialogueIndex++;
                currentSentenceIndex = 0;
                StartCoroutine(DisplayDialogue());
            }
        }
        
        private void DisplaySpeakerImage()
        {
            speakerPortrait.color = parsedDialogue[currentDialogueIndex].speakerImage == null ? new Color(0,0,0, 0) : new Color(255,255,255,255);
            speakerPortrait.sprite = parsedDialogue[currentDialogueIndex].speakerImage;
        }

        private void ClearDialogueBox()
        {
            nameText.text = string.Empty;
            sentenceText.text = string.Empty;
            sentenceText.maxVisibleCharacters = 0;
        }
    }
}