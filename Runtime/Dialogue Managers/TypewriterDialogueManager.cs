using Ibralogue.Parser;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Ibralogue
{
    public class TypewriterDialogueManager : SimpleDialogueManager
    {
        /// <summary>
        /// The DisplayDialogue coroutine displays the dialogue character by character in a scrolling manner and sets all other
        /// relevant values.
        /// </summary>
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

            while (_currentConversation != null &&
                   index < _currentConversation.Lines[_lineIndex].LineContent.Text.Length)
            {
                InvokeFunctionsTimed(index, _currentConversation.Lines[_lineIndex].LineContent.Invocations);
                sentenceText.maxVisibleCharacters++;
                index++;
                yield return new WaitForSecondsRealtime(1f / scrollSpeed);
            }

            _linePlaying = false;
            yield return null;
        }

        /// <summary>
        /// Invokes all the functions in a line timed according to the current visible character.
        /// </summary>
        /// <param name="index">The index of the current visible character.</param>
        /// <param name="functionInvocations">The list of invocations in the Dialogue to be invoked.</param>
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

        /// <remarks>
        /// This override additionally sets maxVisibleCharacters to zero.>
        /// </remarks>
        protected override void ClearDialogueBox()
        {
            base.ClearDialogueBox();
            sentenceText.maxVisibleCharacters = 0;
        }


        /// <summary>
        /// Skips the line animation if it is still playing; otherwise, attempts to display the next line.
        /// </summary>
        protected void TryAdvanceDialogue()
        {
            if (_currentConversation == null)
                return;

            SkipLineAnimation();
            TryDisplayNextLine();
        }
    }
}