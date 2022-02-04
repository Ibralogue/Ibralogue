using System.Collections.Generic;

namespace Ibralogue.Parser
{
    public struct Conversation
    {
        public string Name;
        public List<Dialogue> Dialogues;
        public Dictionary<Choice, int> Choices;
    }
}