using System.Collections;
using Ibralogue.Parser;
using Ibralogue.Plugins;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Ibralogue.Views
{
    public class PunchDialogueView : DialogueViewBase
    {
        [Header("Punch Settings")]
        [SerializeField] private float wordDelay = 0.2f;

        private Coroutine _punchCoroutine;
        private bool _skipRequested;

        public UnityEvent OnPunchEffectUpdated = new UnityEvent();

        /// <summary>
        /// Sets the view according to a line in a given Conversation with a word-by-word punch effect.
        /// </summary>
        public override void SetView(Conversation conversation, int lineIndex)
        {
            nameText.text = conversation.Lines[lineIndex].Speaker;

            if (_punchCoroutine != null)
            {
                StopCoroutine(_punchCoroutine);
            }

            _punchCoroutine = StartCoroutine(PunchEffect(conversation.Lines[lineIndex].LineContent.Text));
        }

        /// <summary>
        /// Displays text word by word.
        /// </summary>
        private IEnumerator PunchEffect(string fullText)
        {
            _isStillDisplaying = true;
            _skipRequested = false;

            sentenceText.text = fullText;
            sentenceText.maxVisibleCharacters = 0;
            sentenceText.ForceMeshUpdate();

            TMP_TextInfo textInfo = sentenceText.textInfo;
            int totalChars = textInfo.characterCount;
            int visibleChars = 0;

            yield return null;

            while (visibleChars < totalChars)
            {
                if (_isPaused)
                {
                    yield return new WaitUntil(() => !_isPaused);
                }

                if (_skipRequested)
                {
                    sentenceText.maxVisibleCharacters = totalChars;
                    break;
                }

                int nextWordEndIndex = totalChars;

                for (int i = visibleChars; i < totalChars; i++)
                {
                    if (char.IsWhiteSpace(textInfo.characterInfo[i].character))
                    {
                        nextWordEndIndex = i + 1;
                        break;
                    }
                }

                visibleChars = nextWordEndIndex;
                sentenceText.maxVisibleCharacters = visibleChars;
                OnPunchEffectUpdated.Invoke();

                yield return new WaitForSeconds(wordDelay);
            }

            _isStillDisplaying = false;
            OnLineComplete.Invoke();
            _punchCoroutine = null;
        }

        /// <summary>
        /// Requests to skip the effect and display full text immediately.
        /// </summary>
        public override void SkipViewEffect()
        {
            if (_isStillDisplaying)
            {
                _skipRequested = true;
            }
        }

        /// <summary>
        /// Clears all elements in the view and stops the coroutine.
        /// </summary>
        public override void ClearView(EnginePlugin[] enginePlugins)
        {
            if (_punchCoroutine != null)
            {
                StopCoroutine(_punchCoroutine);
                _punchCoroutine = null;
            }

            _isStillDisplaying = false;
            _skipRequested = false;
            base.ClearView(enginePlugins);
        }

        /// <summary>
        /// Sets the delay between words.
        /// </summary>
        public void SetWordDelay(float delay)
        {
            wordDelay = Mathf.Max(0f, delay);
        }

        /// <summary>
        /// Gets the current word delay.
        /// </summary>
        public float GetWordDelay()
        {
            return wordDelay;
        }
    }
}