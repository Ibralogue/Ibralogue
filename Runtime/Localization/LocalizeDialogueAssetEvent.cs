using Ibralogue.Parser;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace Ibralogue.Localization
{
    [AddComponentMenu("Localization/Asset/Localize Dialogue Asset Event")]
    public class LocalizeDialogueAssetEvent : LocalizedAssetEvent<DialogueAsset, LocalizedDialogueAsset, UnityEventDialogueAsset>
    {
    }
}
