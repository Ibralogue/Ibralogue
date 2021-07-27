using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Ibralogue
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        private string[] _currentDialogueLines;
        
        private List<Dialog> parsedDialogue;
        private int currentIndex;

        private TextMeshProUGUI _sentenceText, _nameText;
        private Sprite _speakerName;

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
            foreach (Dialog dialog in parsedDialogue)
            {
                Debug.Log($"{dialog.speaker} | {dialog.sentences[0]}");
            }
        }

        private IEnumerator DisplayDialogue()
        {
            yield return null;
        } 

        public void DisplayNextLine()
        {
            if (currentIndex < parsedDialogue.Count)
            {
                
            }
        }
    }

}