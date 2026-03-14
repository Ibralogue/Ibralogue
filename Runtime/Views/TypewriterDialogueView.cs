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

        public override int VisibleCharacterCount
        {
            get { return sentenceText != null ? sentenceText.maxVisibleCharacters : 0; }
        }

        /// <summary>
        /// Displays a dialogue line with a character-by-character typewriter effect.
        /// </summary>
        public override void SetView(Line line)
        {
            nameText.text = line.Speaker;

            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
            }

            _typewriterCoroutine = StartCoroutine(TypewriterEffect(line.LineContent.Text));
        }

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

        public override void SkipViewEffect()
        {
            base.SkipViewEffect();
            if (_isStillDisplaying)
            {
                _skipRequested = true;
            }
        }

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

        public void SetCharacterDelay(float delay)
        {
            characterDelay = Mathf.Max(0f, delay);
        }

        public float GetCharacterDelay()
        {
            return characterDelay;
        }
    }
}
