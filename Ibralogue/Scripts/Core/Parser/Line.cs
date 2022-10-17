using UnityEngine;

namespace Ibralogue.Parser
{
    /// <summary>
    /// The Line struct contains information about an individual line of dialogue.
    /// </summary>
    /// <remarks>
    /// A list of these Line structs makes up a conversation, and a list of conversations makes up a single dialogue file,
    /// due to being able to have multiple conversations in a single file for dialogue branching.
    /// </remarks>
    public struct Line
    {
        public string Speaker;
        public LineContents LineContents;
        public Sprite SpeakerImage;
    }
}