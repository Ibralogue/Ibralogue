using Ibralogue.Parser;
using UnityEngine.Localization.Events;

namespace UnityEngine.Localization.Components
{
    [AddComponentMenu("Localization/Asset/Localize Dialogue Asset Event")]
    public class LocalizeDialogueAssetEvent : LocalizedAssetEvent<DialogueAsset, LocalizedDialogueAsset, UnityEventDialogueAsset>
    {
    }
}
