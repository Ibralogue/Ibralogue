using System;
using System.Collections.Generic;
using Ibralogue.Parser;
using Ibralogue.Plugins;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Ibralogue.Views
{
    public class DialogueViewBase : MonoBehaviour
    {
        [SerializeField] protected TextMeshProUGUI nameText;
        [SerializeField] protected TextMeshProUGUI sentenceText;

        protected bool _isStillDisplaying = false;
        protected bool _isPaused = false;

        [Header("Choice UI")][SerializeField] protected Transform choiceButtonHolder;
        [SerializeField] protected GameObject choiceButton;
        protected List<ChoiceButton> _choiceButtonInstances = new List<ChoiceButton>();

        public UnityEvent OnSetView = new UnityEvent();
        public UnityEvent OnLineComplete = new UnityEvent();

        /// <summary>
        /// Checks if the view is currently displaying content (e.g., effect in progress).
        /// </summary>
        public virtual bool IsStillDisplaying()
        {
            return _isStillDisplaying;
        }

        /// <summary>
        /// Returns the number of characters currently visible in the dialogue text.
        /// Used by the engine to fire inline function invocations at the correct
        /// character position during animated display.
        /// </summary>
        public virtual int VisibleCharacterCount
        {
            get { return sentenceText != null ? sentenceText.text.Length : 0; }
        }

        /// <summary>
        /// Displays a single dialogue line.
        /// </summary>
        public virtual void SetView(Line line)
        {
            nameText.text = line.Speaker;
            sentenceText.text = line.LineContent.Text;
            OnSetView.Invoke();
        }

        /// <summary>
        /// Clears all elements in the view.
        /// </summary>
        public virtual void ClearView(EnginePlugin[] enginePlugins)
        {
            nameText.text = string.Empty;
            sentenceText.text = string.Empty;

            foreach (EnginePlugin plugin in enginePlugins)
            {
                plugin.Clear();
            }

            if (_choiceButtonInstances == null)
                return;

            foreach (ChoiceButton choiceButton in _choiceButtonInstances)
            {
                choiceButton.OnChoiceClick.RemoveAllListeners();
                Destroy(choiceButton.gameObject);
            }

            _choiceButtonInstances.Clear();
        }

        /// <summary>
        /// Skips whatever display effect the dialogue view is playing.
        /// </summary>
        public virtual void SkipViewEffect()
        {
        }

        /// <summary>
        /// Pauses the currently playing effect.
        /// </summary>
        public virtual void Pause()
        {
            _isPaused = true;
        }

        /// <summary>
        /// Resumed the currently playing effect.
        /// </summary>
        public virtual void Resume()
        {
            _isPaused = false;
        }

        /// <summary>
        /// Returns whether the current effect is playing.
        /// </summary>
        public virtual bool IsPaused()
        {
            return _isPaused;
        }

        /// <summary>
        /// Presents choice buttons to the player. When a choice is selected, the
        /// <paramref name="onChoiceSelected"/> callback is invoked with the chosen option.
        /// </summary>
        public virtual void DisplayChoices(List<Choice> choices, Action<Choice> onChoiceSelected)
        {
            _choiceButtonInstances.Clear();
            if (choices == null || choices.Count == 0) return;

            foreach (Choice choice in choices)
            {
                ChoiceButton choiceButtonInstance = Instantiate(choiceButton, choiceButtonHolder).GetComponent<ChoiceButton>();
                if (choiceButtonInstance == null)
                {
                    DialogueLogger.LogError(2, "ChoiceButton is null. Make sure you have the ChoiceButton component added to your Button object!");
                    continue;
                }

                Choice captured = choice;
                choiceButtonInstance.OnChoiceClick.AddListener(() => onChoiceSelected(captured));
                choiceButtonInstance.GetComponentInChildren<TextMeshProUGUI>().text = choice.ChoiceName;

                _choiceButtonInstances.Add(choiceButtonInstance);
            }
        }
    }
}
