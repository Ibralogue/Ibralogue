using System;
using Ibralogue.Parser;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

[Serializable]
public class LocalizedDialogueAsset : LocalizedAsset<DialogueAsset> { }

[AddComponentMenu("Localization/Localized Ibralogue Component")]
public class LocalizedIbralogueComponent : LocalizedAssetBehaviour<DialogueAsset, LocalizedDialogueAsset>
{
    public DialogueAsset dialogueAsset;

    protected override void UpdateAsset(DialogueAsset localizedAsset)
    {
        dialogueAsset.Content = localizedAsset.Content;
    }
}