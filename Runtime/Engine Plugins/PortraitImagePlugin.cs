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
            if (line.LineContent.Metadata.TryGetValue("image", out imagePath)
                && !string.IsNullOrEmpty(imagePath))
                SetImage(imagePath);
            else
                HidePortrait();
        }

        /// <summary>
        /// Changes the displayed portrait sprite. Can be called from a
        /// [DialogueFunction] to update the portrait mid-line during
        /// animated display.
        /// </summary>
        public void SetImage(string path)
        {
            Sprite sprite;
            if (path == _cachedPath && _cachedSprite != null)
            {
                sprite = _cachedSprite;
            }
            else
            {
                sprite = Resources.Load<Sprite>(path);
                _cachedPath = path;
                _cachedSprite = sprite;
            }

            if (sprite != null)
            {
                speakerPortrait.color = new Color(255, 255, 255, 255);
                speakerPortrait.sprite = sprite;
            }
            else
            {
                Debug.LogWarning($"[Ibralogue] Sprite not found at path: {path}");
                HidePortrait();
            }
        }

        public override void Clear()
        {
            HidePortrait();
        }

        private void HidePortrait()
        {
            speakerPortrait.color = new Color(0, 0, 0, 0);
            speakerPortrait.sprite = null;
        }
    }
}
