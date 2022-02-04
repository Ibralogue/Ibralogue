using UnityEngine;

namespace Ibralogue.Parser
{
    /// <summary>
    /// The Dialogue struct contains information about an individual line of dialogue.
    /// </summary>
    /// <remarks>
    /// A list of these Dialogue structs makes up a conversation, and a list of conversations makes up a single dialogue file,
    /// due to being able to have multiple conversations in a single file for dialogue branching.
    /// </remarks>
    public struct Dialogue
    {
        public string Speaker;
        public Sprite SpeakerImage;
        public Sentence Sentence;
    }
}