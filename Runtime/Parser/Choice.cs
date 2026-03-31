using System.Collections.Generic;

namespace Ibralogue.Parser
{
    /// <summary>
    /// Defines a player-facing choice option that can branch to a conversation.
    /// </summary>
    public class Choice: IMetadata
    {
        public string ChoiceName { get; internal set; }
        public string TargetConversation { get; internal set; }
        public Dictionary<string, string> Metadata { get; internal set; }
        
        public bool HasMetadata(string key) => 
            Metadata.ContainsKey(key);

        public bool TryGetMetadataValue(string key, out string value)
        {
            if (Metadata.ContainsKey(key) && Metadata[key] != null)
            {
                value = Metadata[key];
                return true;
            }
            value = null;
            return false;
        }
    }
}
