using System.Collections.Generic;

namespace Ibralogue.Parser
{
    /// <summary>
    /// The internal contents of a line, including but not limited its text and metadata
    /// </summary>
    public struct LineContent
    {
        public string Text;
        public Dictionary<int, string> Invocations;
        public Dictionary<string, string> Metadata;
    }
}

