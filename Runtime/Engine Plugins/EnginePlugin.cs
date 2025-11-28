using Ibralogue.Parser;
using UnityEngine;

namespace Ibralogue.Plugins
{ 
    public abstract class EnginePlugin : MonoBehaviour
    {
        public abstract void Display(Conversation currentConversation, int lineIndex);
        public abstract void Clear();
    }
}
