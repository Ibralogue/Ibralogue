using System.Collections.Generic;

namespace Ibralogue.Parser
{
	/// <summary>
	/// A named block of dialogue content that the engine walks at runtime.
	/// </summary>
	public class Conversation
	{
		public string Name;
		internal List<RuntimeContentNode> Content;
	}
}
