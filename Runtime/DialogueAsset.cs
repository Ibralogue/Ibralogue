using UnityEngine;

namespace Ibralogue
{
    public class DialogueAsset : ScriptableObject
    {
        [field: SerializeField, TextArea(5, 50)] public string Content { get; internal set; }
    }
}