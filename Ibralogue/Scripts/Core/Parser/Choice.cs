using System.Collections.Generic;

namespace Ibralogue.Parser
{
    public class Choice: IMetadata
    {
        public string ChoiceName;
        public string LeadingConversationName;
        private Dictionary<string, string> metadata;
        
        public bool HasTag(string key) => 
            metadata.ContainsKey(key);

        public bool TryGetTagValue(string key, out string value)
        {
            if (metadata.ContainsKey(key) && metadata[key] != null)
            {
                value = metadata[key];
                return true;
            }
            value = null;
            return false;
        }
    }
}