using Ibralogue.Parser;
using UnityEngine;
using UnityEngine.UI;

namespace Ibralogue.Plugins
{
    [RequireComponent(typeof(SimpleDialogueEngine))]
    public class PortraitImagePlugin : EnginePlugin
    {
        [SerializeField] protected Image speakerPortrait;

        public override void Display(Line line)
        {
            speakerPortrait.color = line.SpeakerImage == null
                ? new Color(0, 0, 0, 0)
                : new Color(255, 255, 255, 255);
            speakerPortrait.sprite = line.SpeakerImage;
        }

        public override void Clear()
        {
            speakerPortrait.color = new Color(0, 0, 0, 0);
        }
    }
}
