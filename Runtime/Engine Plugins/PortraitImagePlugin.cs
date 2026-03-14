using Ibralogue.Parser;
using UnityEngine;
using UnityEngine.UI;

namespace Ibralogue.Plugins
{
    [RequireComponent(typeof(SimpleDialogueEngine))]
    public class PortraitImagePlugin : EnginePlugin
    {
        [SerializeField] protected Image speakerPortrait;

        private Sprite _cachedSprite;
        private string _cachedPath;

        public override void Display(Line line)
        {
            string imagePath;
            if (!line.LineContent.Metadata.TryGetValue("image", out imagePath)
                || string.IsNullOrEmpty(imagePath))
            {
                speakerPortrait.color = new Color(0, 0, 0, 0);
                speakerPortrait.sprite = null;
                return;
            }

            Sprite sprite;
            if (imagePath == _cachedPath && _cachedSprite != null)
            {
                sprite = _cachedSprite;
            }
            else
            {
                sprite = Resources.Load<Sprite>(imagePath);
                _cachedPath = imagePath;
                _cachedSprite = sprite;
            }

            if (sprite != null)
            {
                speakerPortrait.color = new Color(255, 255, 255, 255);
                speakerPortrait.sprite = sprite;
            }
            else
            {
                Debug.LogWarning($"[Ibralogue] Sprite not found at path: {imagePath}");
                speakerPortrait.color = new Color(0, 0, 0, 0);
            }
        }

        public override void Clear()
        {
            speakerPortrait.color = new Color(0, 0, 0, 0);
        }
    }
}
