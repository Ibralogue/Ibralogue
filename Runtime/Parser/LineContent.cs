using System.Collections.Generic;

namespace Ibralogue.Parser
{
    /// <summary>
    /// A function invocation embedded in a dialogue line, retaining source location for diagnostics.
    /// </summary>
    public readonly struct FunctionInvocation
    {
        /// <summary>The name of the function to invoke.</summary>
        public readonly string Name;

        /// <summary>The character position in the rendered text where this invocation occurs.</summary>
        public readonly int CharacterIndex;

        /// <summary>The source line number for diagnostic reporting.</summary>
        public readonly int Line;

        /// <summary>The source column number for diagnostic reporting.</summary>
        public readonly int Column;

        public FunctionInvocation(string name, int characterIndex, int line, int column)
        {
            Name = name;
            CharacterIndex = characterIndex;
            Line = line;
            Column = column;
        }
    }

    /// <summary>
    /// The internal contents of a line, including but not limited its text and metadata
    /// </summary>
    public struct LineContent
    {
        public string Text;
        public List<FunctionInvocation> Invocations;
        public Dictionary<string, string> Metadata;
    }
}

