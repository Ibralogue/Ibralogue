using System;
using System.Collections.Generic;
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
         SPEAKER,
         SENTENCE,
         IMAGE,
         COMMENT,
         INVOKE,
      }


      /// <returns>
      /// The GetLineToken function checks what character the line it is given starts with and returns a "token" with it
      /// according to that.
      /// </returns>
      /// <param name="line">The line of the dialogue we need the token of.</param>
      private static Tokens GetLineToken(string line)
      {
         if (line.StartsWith("#")) return Tokens.COMMENT;
         if (Regex.IsMatch(line, @"^\[(.+?)\]")) return Tokens.SPEAKER;
         if (Regex.IsMatch(line, @"^<<(.+?)>>"))
         {
            string[] arguments = line.Trim().Substring(2, line.Length-5).Split(',');
            return arguments[0] switch
            {
               "Invoke" => Tokens.INVOKE,
               "Image" => Tokens.IMAGE,
               _ => Tokens.INVOKE
            };
         }
         return Tokens.SENTENCE;
      }   
      
      /// <summary>
      /// The ParseDialogue function returns an array of dialogues and associates information
      /// with each element in the array. Speaker Name, Sentence, Image etc.
      /// </summary>
      public static List<Dialogue> ParseDialogue(TextAsset dialogueAsset)
      {
         string dialogueText = dialogueAsset.text;
         string[] fLines = dialogueText.Split('\n');
         List<string> sentences = new List<string>();

         List<Dialogue> dialogues = new List<Dialogue>();
         Dialogue dialogue = new Dialogue();

         for (int i = 0; i < fLines.Length; i++)
         {
            string line = fLines[i];
            
            Tokens token = GetLineToken(line);
            string processedLine = GetProcessedLine(token, line);

            switch (token)
            {
               case Tokens.SPEAKER when dialogue.Speaker == null:
               {
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
                           $"[Ibralogue] Variable declaration detected, ({match}) but no entry found in dictionary!");
                     }
                  }

                  dialogue.Speaker = processedLine;
                  break;
               }
               case Tokens.SPEAKER:
                  processedLine = ReplaceGlobalVariables(processedLine);
                  dialogue.Sentence = string.Join("\n", sentences.ToArray());
                  dialogues.Add(dialogue);
                  sentences.Clear();
                  dialogue = new Dialogue {Speaker = processedLine};
                  break;
               case Tokens.SENTENCE:
               {
                  processedLine = ReplaceGlobalVariables(processedLine);
                  sentences.Add(processedLine);
                  break;
               }
               case Tokens.IMAGE:
               {
                  if (Resources.Load(processedLine) == null)
                     Debug.LogError(
                        $"[Ibralogue] Invalid image path {processedLine} at line {i + 1} in {dialogueAsset.name}.");
                  dialogue.SpeakerImage = Resources.Load<Sprite>(processedLine);
                  break;
               }
               case Tokens.INVOKE:
               {
                  if (dialogue.FunctionInvocations == null)
                     dialogue.FunctionInvocations = new Dictionary<int, string>();
                  dialogue.FunctionInvocations.Add(sentences.Count - 1, processedLine);
                  break;
               }
            }
         }
         dialogue.Sentence = string.Join("\n", sentences.ToArray());
         dialogues.Add(dialogue); 
         sentences.Clear();
         return dialogues;
      }

      private static string GetProcessedLine(Tokens token, string line)
      {
         switch (token)
         {
            case Tokens.SPEAKER:
               if (line.Length > 2) {
                  line = line.Trim().Substring(1, line.Length-3);
               } 
               break;
            case Tokens.INVOKE:
            case Tokens.IMAGE:
               line = Regex.Replace(line.Trim().Substring(2, line.Length-5), @"^(.*?),", string.Empty).Trim();
               break;
            case Tokens.COMMENT:
            case Tokens.SENTENCE:
               break;
            default:
               throw new ArgumentOutOfRangeException(nameof(token), token, null);
         }
         return line;
      }

      private static string ReplaceGlobalVariables(string line)
      {
         foreach (Match match in Regex.Matches(line, @"(%\w+%)"))
         {
            string processedVariable = match.ToString().Replace("%", string.Empty);
            if (DialogueManager.GlobalVariables.TryGetValue(processedVariable, out string keyValue))
            {
               line = line.Replace(match.ToString(), keyValue);
            }
            else
            {
               Debug.LogWarning(
                  $"[Ibralogue] Variable declaration detected, ({match}) but no entry found in dictionary!");
            }
         }
         return line;
      }
   }
}