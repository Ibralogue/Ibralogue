using System.Collections.Generic;
using System.Linq;
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
        /// Sets the according to a line in a given Conversation.
        /// </summary>
        public virtual void SetView(Conversation conversation, int lineIndex)
        {
            nameText.text = conversation.Lines[lineIndex].Speaker;
            sentenceText.text = conversation.Lines[lineIndex].LineContent.Text;
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
        /// Uses the Unity UI system and TextMeshPro to render choice buttons.
        /// </summary>
        public virtual void DisplayChoices(DialogueEngineBase engine, Conversation conversation, List<Conversation> parsedConversations)
        {
            _choiceButtonInstances.Clear();
            if (conversation.Choices == null || !conversation.Choices.Any()) return;
            foreach (Choice choice in conversation.Choices.Keys)
            {
                ChoiceButton choiceButtonInstance = Instantiate(choiceButton, choiceButtonHolder).GetComponent<ChoiceButton>();
                if (choiceButtonInstance == null)
                {
                    DialogueLogger.LogError(2, "ChoiceButton is null. Make sure you have the ChoiceButton component added to your Button object!");
                }

                UnityAction onClickAction = null;
                int conversationIndex = -1;

                switch (choice.LeadingConversationName)
                {
                    case ">>":
                        DialogueLogger.LogError(2,
                            "The embedded choice is not yet implemented, '>>' keyword is reserved for future use");
                        break;
                    default:
                        conversationIndex =
                            parsedConversations.FindIndex(c => c.Name == choice.LeadingConversationName);
                        if (conversationIndex == -1)
                            DialogueLogger.LogError(2,
                                $"No conversation called \"{choice.LeadingConversationName}\" found for choice \"{choice.ChoiceName}\" in \"{conversation.Name}\".",
                                this);
                        onClickAction = () => engine.SwitchConversation(parsedConversations[conversationIndex]);
                        break;
                }

                choiceButtonInstance.GetComponentInChildren<TextMeshProUGUI>().text = choice.ChoiceName;
                choiceButtonInstance.OnChoiceClick.AddListener(onClickAction);

                _choiceButtonInstances.Add(choiceButtonInstance);
            }
        }
    }
}