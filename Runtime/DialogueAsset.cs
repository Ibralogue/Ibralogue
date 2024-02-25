using UnityEngine;

namespace Ibralogue.Parser
{
    /// <summary>
    /// Scriptable object that stores the context of the entire dialogue file inside it.
    /// </summary>
    public class DialogueAsset : ScriptableObject
    {
        [field: SerializeField, TextArea(5, 50)] public string Content { get; internal set; }
    }
}