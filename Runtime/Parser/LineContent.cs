using System.Collections.Generic;

namespace Ibralogue.Parser
{
    /// <summary>
    /// An invocation embedded in a dialogue line, retaining source location for diagnostics.
    /// </summary>
    public readonly struct Invocation
    {
        /// <summary>The name of the invocation to call.</summary>
        public readonly string Name;

        /// <summary>The arguments passed to the invocation, as raw strings from the source.</summary>
        public readonly List<string> Arguments;

        /// <summary>The character position in the rendered text where this invocation occurs.</summary>
        public readonly int CharacterIndex;

        /// <summary>The source line number for diagnostic reporting.</summary>
        public readonly int Line;

        /// <summary>The source column number for diagnostic reporting.</summary>
        public readonly int Column;

        public Invocation(string name, List<string> arguments, int characterIndex, int line, int column)
        {
            Name = name;
            Arguments = arguments;
            CharacterIndex = characterIndex;
            Line = line;
            Column = column;
        }
    }

    /// <summary>
    /// The internal contents of a line, including but not limited its text and metadata.
    /// </summary>
    public struct LineContent
    {
        public string Text;
        public List<Invocation> Invocations;
        public Dictionary<string, string> Metadata;
    }
}
