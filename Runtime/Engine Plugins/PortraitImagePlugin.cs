using Ibralogue.Parser;
using UnityEngine;
using UnityEngine.UI;

namespace Ibralogue.Plugins
{
    [RequireComponent(typeof(SimpleDialogueEngine))]
    public class PortraitImagePlugin : EnginePlugin
    {
        [SerializeField] protected Image speakerPortrait;

        /// <summary>
        /// Sets the speaker image and makes the Image transparent if there is no speaker image.
        /// </summary>
        public override void Display(Conversation currentConversation, int lineIndex)
        {
            speakerPortrait.color = currentConversation.Lines[lineIndex].SpeakerImage == null
                ? new Color(0, 0, 0, 0)
                : new Color(255, 255, 255, 255);
            speakerPortrait.sprite = currentConversation.Lines[lineIndex].SpeakerImage;
        }

        /// <summary>
        /// Sets the speaker image and makes the Image transparent if there is no speaker image.
        /// </summary>
        public override void Clear()
        {
            speakerPortrait.color = new Color(0, 0, 0, 0);
        }
    }
}