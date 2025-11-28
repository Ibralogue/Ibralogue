using System.Collections;
using Ibralogue.Parser;
using Ibralogue.Plugins;
using UnityEngine;
using UnityEngine.Events;

namespace Ibralogue.Views
{
    public class TypewriterDialogueView : DialogueViewBase
    {
        [Header("Typewriter Settings")]
        [SerializeField] private float characterDelay = 0.03f;
        [SerializeField] private int characterWindow = 1;

        private Coroutine _typewriterCoroutine;
        private bool _skipRequested;

        public UnityEvent OnTypewriterEffectUpdated = new UnityEvent();

        /// <summary>
        /// Sets the view according to a line in a given Conversation with typewriter effect.
        /// </summary>
        public override void SetView(Conversation conversation, int lineIndex)
        {
            nameText.text = conversation.Lines[lineIndex].Speaker;

            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
            }

            _typewriterCoroutine = StartCoroutine(TypewriterEffect(conversation.Lines[lineIndex].LineContent.Text));
        }

        /// <summary>
        /// Displays text character by character with typewriter effect.
        /// </summary>
        private IEnumerator TypewriterEffect(string fullText)
        {
            _isStillDisplaying = true;
            _skipRequested = false;

            sentenceText.text = fullText;
            sentenceText.maxVisibleCharacters = 0;
            sentenceText.ForceMeshUpdate();

            int totalChars = sentenceText.textInfo.characterCount;
            int visibleChars = 0;
            float elapsed = 0f;

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

                elapsed += Time.deltaTime;
                int charactersToShow = Mathf.FloorToInt(elapsed / characterDelay) * characterWindow;

                if (charactersToShow > visibleChars)
                {
                    visibleChars = Mathf.Min(charactersToShow, totalChars);
                    sentenceText.maxVisibleCharacters = visibleChars;
                    OnTypewriterEffectUpdated.Invoke();
                }

                yield return null;
            }

            _isStillDisplaying = false;
            OnLineComplete.Invoke();
            _typewriterCoroutine = null;
        }

        /// <summary>
        /// Requests to skip the typewriter effect and display full text immediately.
        /// </summary>
        public void SkipTypewriter()
        {
            if (_isStillDisplaying)
            {
                _skipRequested = true;
            }
        }

        /// <summary>
        /// Clears all elements in the view and stops the typewriter effect.
        /// </summary>
        public override void ClearView(EnginePlugin[] enginePlugins)
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            _isStillDisplaying = false;
            _skipRequested = false;

            base.ClearView(enginePlugins);
        }

        /// <summary>
        /// Sets the character delay for the typewriter effect.
        /// </summary>
        public void SetCharacterDelay(float delay)
        {
            characterDelay = Mathf.Max(0f, delay);
        }

        /// <summary>
        /// Gets the current character delay.
        /// </summary>
        public float GetCharacterDelay()
        {
            return characterDelay;
        }
    }
}