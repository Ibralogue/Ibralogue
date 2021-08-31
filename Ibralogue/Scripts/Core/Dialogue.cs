using System.Collections.Generic;
using UnityEngine;

namespace Ibralogue
{
    public struct Dialogue
    {
        public string Speaker;
        public Sprite SpeakerImage;
        public string Sentence;
        public Dictionary<int,string> FunctionInvocations;
    }
}