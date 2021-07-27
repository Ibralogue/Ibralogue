using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Ibralogue
{
   public static class DialogueParser
   {
      /// <remarks>
      /// The Tokens are a representation of the attribute of the current line we are parsing
      /// which provides additional information about the lexeme that the token represents. 
      /// </remarks>
      public enum Tokens
      {
         ILLEGAL,
         SPEAKER,
         SENTENCE,
         IMAGE,
      }

      private static Tokens GetLineToken(string line)
      {
         if (line.StartsWith("~")) return Tokens.SPEAKER;
         if (line.StartsWith("-")) return Tokens.SENTENCE;
         if (line.StartsWith("!")) return Tokens.IMAGE;
         return Tokens.ILLEGAL;
      }   
      
      /// <summary>
      /// The ParseDialogue function returns an array of dialogues and associates information
      /// with each element in the array. Speaker Name, Sentence, Image etc.
      /// </summary>
      public static List<Dialog> ParseDialogue(TextAsset dialogueAsset)
      {
         string dialogueText = dialogueAsset.text;
         string[] fLines = dialogueText.Split('\n');
         
         List<Dialog> dialogue = new List<Dialog>();
         Dialog dialog = new Dialog {sentences = new List<string>()};

         for (int index = 0; index < fLines.Length; index++)
         {
            string line = fLines[index];
            Tokens token = GetLineToken(line);
            string processedLine = Regex.Replace(line, "^~|-|!$", string.Empty);
            
            switch (token)
            {
               case Tokens.SPEAKER when dialog.speaker == null:
                  dialog.speaker = processedLine;
                  break;
               case Tokens.SPEAKER:
                  dialogue.Add(dialog);
                  dialog = new Dialog {sentences = new List<string>()};
                  dialog.speaker = processedLine;
                  break;
               case Tokens.SENTENCE:
                  dialog.sentences.Add(processedLine);
                  break;
               case Tokens.IMAGE:
                  dialog.speakerImage = Resources.Load("BlaBla/Epic") as Sprite;
                  break;
               case Tokens.ILLEGAL:
                  throw new Exception($"[Ibralogue] Illegal Starter Token at Line {index+1} in {dialogueAsset.name}");
               default:
                  throw new ArgumentOutOfRangeException($"[Ibralogue] Unexpected Argument Receibed at {line}");
            }
            if (index == fLines.Length - 1) dialogue.Add(dialog); 
         }
         return dialogue;
      }
   }
}