using System;

namespace Ibralogue
{
	/// <summary>
	/// Marks a static method as a custom invocation that can be called from dialogue files.
	/// </summary>
	public class DialogueInvocationAttribute : Attribute {}

	/// <summary>
	/// Legacy alias for <see cref="DialogueInvocationAttribute"/>.
	/// </summary>
	[Obsolete("Use [DialogueInvocation] instead.")]
	public class DialogueFunctionAttribute : DialogueInvocationAttribute {}
}
