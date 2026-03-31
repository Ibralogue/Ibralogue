using Ibralogue.Parser;
using UnityEngine;
using UnityEngine.UI;

namespace Ibralogue.Plugins
{
    [RequireComponent(typeof(DialogueEngineBase))]
    public class PortraitImagePlugin : EnginePlugin
    {
        [SerializeField] protected Image speakerPortrait;

        private Sprite _cachedSprite;
        private string _cachedPath;

        public override void Display(Line line)
        {
        }

        /// <summary>
        /// Changes the displayed portrait sprite. Called by the built-in
        /// {{Image(path)}} function during dialogue display.
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
                speakerPortrait.sprite = sprite;
                speakerPortrait.enabled = true;
            }
            else
            {
                DialogueLogger.LogWarning($"Sprite not found at path: {path}");
                HidePortrait();
            }
        }

        public override void Clear()
        {
            HidePortrait();
        }

        private void HidePortrait()
        {
            speakerPortrait.enabled = false;
            speakerPortrait.sprite = null;
        }
    }
}
