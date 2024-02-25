using System.Collections.Generic;

namespace Ibralogue.Parser
{
	/// <summary>
	/// A Conversation is a block of lines with a name associated with it that can lead to other Conversation's.
	/// </summary>
	public class Conversation
	{
		public string Name;
		public List<Line> Lines;
		public Dictionary<Choice, int> Choices;
	}
}