using System.Collections.Generic;
using Ibralogue.Parser;
using UnityEngine;

namespace Ibralogue.Plugins
{ 
    public abstract class EnginePlugin : MonoBehaviour
    {
        public abstract void Display(Line line);
        public abstract void Clear();

        /// <summary>
        /// Called when a conversation starts. Override to initialize plugin state.
        /// </summary>
        public virtual void OnConversationStart(Conversation conversation) { }

        /// <summary>
        /// Called when a conversation ends. Override to finalize plugin state.
        /// </summary>
        public virtual void OnConversationEnd() { }

        /// <summary>
        /// Called when choices are presented to the player.
        /// </summary>
        public virtual void OnChoicesPresented(List<Choice> choices) { }

        /// <summary>
        /// Called when the player selects a choice.
        /// </summary>
        public virtual void OnChoiceSelected(Choice choice) { }
    }
}
