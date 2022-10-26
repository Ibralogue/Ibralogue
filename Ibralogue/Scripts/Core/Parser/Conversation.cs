using System.Collections.Generic;

namespace Ibralogue.Parser
{
    public struct Conversation
    {
        public string Name;
        public List<Line> Lines;
        public Dictionary<Choice, int> Choices;
    }
}