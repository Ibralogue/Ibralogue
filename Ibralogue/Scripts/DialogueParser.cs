using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Ibralogue
{
   public static class DialogueParser
   {
      /// <summary>
      /// Tokens are a representation of the attribute of the current line we are parsing
      /// which provides additional information about the lexeme that the token represents. 
      /// </summary>
      private enum Tokens
      {
         ILLEGAL,
         SPEAKER,
         SENTENCE,
         IMAGE,
         COMMENT,
         FUNCTION,
      }
      
      
      /// <returns>
      /// The GetLineToken function checks what character the line it is given starts with and returns a "token" with it
      /// according to that.
      /// </returns>
      /// <param name="line">The line in the dialogue we need the token of.</param>
      private static Tokens GetLineToken(string line)
      {
         if (line.StartsWith("~")) return Tokens.SPEAKER;
         if (line.StartsWith("-")) return Tokens.SENTENCE;
         if (line.StartsWith("!")) return Tokens.IMAGE;
         if (line.StartsWith("!")) return Tokens.IMAGE;
         if (line.StartsWith("#")) return Tokens.COMMENT;
         if (Regex.IsMatch(line, @"[a-zA-Z]+\(+\)")) return Tokens.FUNCTION;
         return Tokens.ILLEGAL;
      }   
      
      /// <summary>
      /// The ParseDialogue function returns an array of dialogues and associates information
      /// with each element in the array. Speaker Name, Sentence, Image etc.
      /// </summary>
      public static List<Dialogue> ParseDialogue(TextAsset dialogueAsset)
      {
         string dialogueText = dialogueAsset.text;
         string[] fLines = dialogueText.Split('\n');
         
         List<Dialogue> dialogues = new List<Dialogue>();
         Dialogue dialogue = new Dialogue{sentences = new List<string>()};

         for (int index = 0; index < fLines.Length; index++)
         {
            string line = fLines[index];
            Tokens token = GetLineToken(line);
            
            string processedLine = Regex.Replace(line, "^(~|-|!|#)", string.Empty);
            
            switch (token)
            {
               case Tokens.SPEAKER when dialogue.speaker == null:
                  dialogue.speaker = processedLine;
                  break;
               case Tokens.SPEAKER:
                  dialogues.Add(dialogue);
                  dialogue = new Dialogue{sentences = new List<string>()};
                  foreach (Match match in Regex.Matches(processedLine, @"(%\w+%)"))
                  {
                     string processedVariable = match.ToString().Replace("%", string.Empty);
                     if (DialogueManager.GlobalVariables.TryGetValue(processedVariable, out string keyValue))
                     {
                        processedLine = processedLine.Replace(match.ToString(), keyValue);
                     }
                     else
                     {
                        Debug.LogWarning(
                           $"[Ibralogue] Variable declaration detected ({match}), but no entry found in dictionary!");
                     }
                  }
                  dialogue.speaker = processedLine;
                  break;
               case Tokens.SENTENCE:
                  foreach (Match match in Regex.Matches(processedLine, @"(%\w+%)"))
                  {
                     string processedVariable = match.ToString().Replace("%", string.Empty);
                     if (DialogueManager.GlobalVariables.TryGetValue(processedVariable, out string keyValue))
                     {
                        processedLine = processedLine.Replace(match.ToString(), keyValue);
                     }
                     else
                     {
                        Debug.LogWarning($"[Ibralogue] Variable declaration detected ({match}), but no entry found in dictionary!");
                     }
                  }
                  dialogue.sentences.Add(processedLine);
                  break;
               case Tokens.IMAGE:
                  string imagePath = Regex.Replace(processedLine.Replace("\"", ""), @"\s+", "");
                  if(Resources.Load(imagePath) == null) throw new Exception($"[Ibralogue] Invalid image path {processedLine} at {index+1}");
                  dialogue.speakerImage = Resources.Load<Sprite>(imagePath);
                  break;
               case Tokens.COMMENT:
                  break;
               case Tokens.FUNCTION:
                  Debug.Log("This token type has not been implemented yet!");
                  break;
               case Tokens.ILLEGAL:
                  throw new Exception($"[Ibralogue] Illegal Starter Token at Line {index+1} in {dialogueAsset.name}");
               default:
                  throw new ArgumentOutOfRangeException($"[Ibralogue] Unexpected Argument Received at {line+1}");
            }
            if (index == fLines.Length - 1) dialogues.Add(dialogue); 
         }
         return dialogues;
      }
   }
}