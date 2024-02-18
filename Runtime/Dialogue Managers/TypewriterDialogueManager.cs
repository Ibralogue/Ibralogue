using Ibralogue;
using Ibralogue.Parser;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class TypewriterDialogueManager : SimpleDialogueManager 
{
    protected override IEnumerator DisplayDialogue()
    {
        _linePlaying = true;

        if (_currentConversation.Choices != null && _currentConversation.Choices.Count > 0)
        {
            KeyValuePair<Choice, int> foundChoice =
                _currentConversation.Choices.FirstOrDefault(x => x.Value == _lineIndex);
            if (foundChoice.Key != null && _lineIndex == foundChoice.Value) DisplayChoices();
        }

        nameText.text = _currentConversation.Lines[_lineIndex].Speaker;
        sentenceText.text = _currentConversation.Lines[_lineIndex].LineContent.Text;
        DisplaySpeakerImage();

        int index = 0;
        float elapsedTime = 0f;

        while (_currentConversation != null &&
               index < _currentConversation.Lines[_lineIndex].LineContent.Text.Length)
        {
            elapsedTime += Time.deltaTime;
            if (elapsedTime >= 1f / scrollSpeed)
            {
                InvokeFunctionsTimed(index, _currentConversation.Lines[_lineIndex].LineContent.Invocations);
                index++;
                sentenceText.maxVisibleCharacters++;
            }
        }

        _linePlaying = false;
        yield return null;
    }

    protected void InvokeFunctionsTimed(int index, Dictionary<int, string> functionInvocations)
    {
        IEnumerable<MethodInfo> dialogueMethods = GetDialogueMethods();

        if (functionInvocations != null && functionInvocations
    .TryGetValue(sentenceText.maxVisibleCharacters, out string functionName))
            foreach (MethodInfo methodInfo in dialogueMethods)
            {
                if (methodInfo.Name != functionName)
                    continue;

                if (methodInfo.ReturnType == typeof(string))
                {
                    string replacedText = methodInfo.GetParameters().Length > 0 ? (string)methodInfo.Invoke(null, new object[] { this }) : (string)methodInfo.Invoke(null, null);
                    string processedSentence = _currentConversation.Lines[_lineIndex].LineContent.Text
                        .Insert(index, replacedText);
                    sentenceText.text = processedSentence;
                    index -= processedSentence.Length -
                             _currentConversation.Lines[_lineIndex].LineContent.Text.Length;
                }
                else
                {
                    if (methodInfo.GetParameters().Length > 0)
                    {
                        methodInfo.Invoke(null, new object[] { this });
                    }
                    else
                    {
                        methodInfo.Invoke(null, null);
                    }
                }
            }
    }

    /// <summary>
    /// Skip the typewriter animation of the sentence.
    /// </summary>
    public void SkipLineAnimation()
    {
        if (!_linePlaying)
            return;
        StopCoroutine(DisplayDialogue());
        _linePlaying = false;
        sentenceText.maxVisibleCharacters = sentenceText.text.Length;
    }

    protected override void ClearDialogueBox()
    {
        base.ClearDialogueBox();
        sentenceText.maxVisibleCharacters = 0;
    }

    protected void TryAdvanceDialogue()
    {
        if (_currentConversation == null)
            return;

        SkipLineAnimation();
        TryDisplayNextLine();
    }
}