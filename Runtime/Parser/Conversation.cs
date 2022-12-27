using System.Collections.Generic;

namespace Ibralogue.Parser
{
	public class Conversation
	{
		public string Name;
		public List<Line> Lines;
		public Dictionary<Choice, int> Choices;
	}
}