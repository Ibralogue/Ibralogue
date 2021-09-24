using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
         Speaker,
         Sentence,
         Choice,
         Comment,
         Invoke,
         ImageInvoke,
         DialogueNameInvoke,
         EndInvoke
      }
      
      /// <returns>
      /// The GetLineToken function checks what character the line it is given starts with and returns a "token" with it
      /// according to that.
      /// </returns>
      /// <param name="line">The line of the dialogue we need the token of.</param>
      private static Tokens GetLineToken(string line)
      {
         if (line.StartsWith("#")) return Tokens.Comment;
         if (Regex.IsMatch(line, @"^\[(.+)\]")) return Tokens.Speaker;
         if (Regex.IsMatch(line, @"^<<(.+)>>"))
         {
            string processedLine = line.Trim().Substring(2);
            string[] arguments = processedLine.Substring(0, processedLine.Length - 2).Split(',');
            return arguments[0] switch
            {
               "Invoke" => Tokens.Invoke,
               "Image" => Tokens.ImageInvoke,
               "DialogueName" => Tokens.DialogueNameInvoke,
               "end" => Tokens.EndInvoke,
               _ => Tokens.Invoke
            };
         }
         return Tokens.Sentence;
      }   
      
      /// <summary>
      /// The ParseDialogue function returns an array of conversations and associates information
      /// with each element in the dialogue array. Speaker Name, Sentence, Image etc. as well as additional per-conversation metadata.
      /// </summary>
      public static List<Conversation> ParseDialogue(TextAsset dialogueAsset)
      {
         string dialogueText = dialogueAsset.text;
         string[] textLines = dialogueText.Split('\n');
         
         List<string> sentences = new List<string>();
         List<Conversation> conversations = new List<Conversation>();

         Conversation conversation = new Conversation {Dialogues = new List<Dialogue>()};
         Dialogue dialogue = new Dialogue();

         for (int i = 0; i < textLines.Length; i++)
         {
            string line = textLines[i];
            Tokens token = GetLineToken(line);
            string processedLine = GetProcessedLine(token, line);

            switch (token)
            {
               case Tokens.Comment:
                  break;
               case Tokens.Speaker when dialogue.Speaker == null:
               {
                  processedLine = ReplaceGlobalVariables(processedLine);
                  dialogue.Speaker = processedLine;
                  break;
               }
               case Tokens.Speaker:
               {
                  //Handle the previous Dialogue before handling this one.
                  dialogue.Sentence = string.Join("\n", sentences.ToArray());
                  conversation.Dialogues.Add(dialogue);
                  
                  processedLine = ReplaceGlobalVariables(processedLine);
                  dialogue = new Dialogue {Speaker = processedLine};
                  sentences.Clear();
                  break;
               }
               case Tokens.Sentence:
               {
                  processedLine = ReplaceGlobalVariables(processedLine);
                  sentences.Add(processedLine);
                  break;
               }
               case Tokens.ImageInvoke:
               {
                  if (Resources.Load(processedLine) == null)
                     Debug.LogError(
                        $"[Ibralogue] Invalid image path {processedLine} at line {i + 1} in {dialogueAsset.name}.");
                  dialogue.SpeakerImage = Resources.Load<Sprite>(processedLine);
                  break;
               }
               case Tokens.Invoke:
               {
                  if (dialogue.FunctionInvocations == null)
                     dialogue.FunctionInvocations = new Dictionary<int, string>();
                  dialogue.FunctionInvocations.Add(sentences.Count - 1, processedLine);
                  break;
               }
               case Tokens.DialogueNameInvoke:
               {
                  conversation.Name = processedLine;
                  break;
               }
               case Tokens.EndInvoke:
               {
                  dialogue.Sentence = string.Join("\n", sentences.ToArray());
                  sentences.Clear();
                  
                  conversation.Dialogues.Add(dialogue);
                  dialogue = new Dialogue();
                  
                  conversations.Add(conversation);
                  conversation = new Conversation {Dialogues = new List<Dialogue>()};
                  break;
               }
               case Tokens.Choice:
                  Debug.Log(processedLine);
                  // if (conversation.Choices == null)
                  //    conversation.Choices = new List<Choice>();
                  // string[] arguments = Regex.Split(processedLine, @"(->)");
                  //
                  // Choice choice = new Choice() {ChoiceName = arguments[0], LeadingConversationName = arguments[1]};
                  // Debug.Log(choice.ChoiceName);
                  // Debug.Log(choice.LeadingConversationName);
                  // conversation.Choices.Add(choice);
                  break;
               default:
                  throw new ArgumentOutOfRangeException();
            }
         }
         if(sentences.Count != 0) dialogue.Sentence = 
            string.Join("\n", sentences.ToArray());

         foreach (Dialogue dial in conversations[0].Dialogues)
         {
            Debug.Log($"{dial.Speaker}: {dial.Sentence}");
         }
         sentences.Clear();
         return conversations;
      }

      /// <summary>
      /// The GetProcessedLine function takes in a token and a line, and removes/adds anything that requires removal or addition in the
      /// final processed line.
      /// </summary>
      private static string GetProcessedLine(Tokens token, string line)
      {
         switch (token)
         {
            case Tokens.Speaker:
               if (line.Length >= 2) {
                  line = line.Trim().Substring(1);
                  line = line.Substring(0, line.Length - 1);
               } 
               break;
            case Tokens.Invoke:
            case Tokens.ImageInvoke:
            case Tokens.DialogueNameInvoke:
               line = line.Trim().Substring(2);
               //We don't need to pass in the first argument since we already know the type of method being invoked;
               line = line.Substring(0, line.Length - 2).Split(':')[1];
               break;
            case Tokens.Comment:
            case Tokens.Sentence:
               break;
            case Tokens.Choice:
               line = line.Trim();
               line = line.Substring(1);
               break;
            case Tokens.EndInvoke:
               break;
            default:
               Debug.LogError($"Argument Out Of Range: {token}");
               break;
         }
         return line;
      }
      
      /// <summary>
      /// Replaces all Global Variable "Keys" with all of their stored "Values".
      /// </summary>
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