using UnityEngine;

namespace Ibralogue
{
    public class DialogueAsset : ScriptableObject
    {
        [field:SerializeField] public string Content { get; internal set; }
    }
}