using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Ibralogue
{
   public static class DialogueParser
   {
      private const string speakerPattern = @"^\[(.+)\]";
      private const string invokePattern = @"^<<(.+)>>";
      private const string inlineInvokePattern = @"<<(.+)>>";
      private const string choicePattern = @"^-(.+)->(.+)";
      
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
         ExplicitInvoke,
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
         if (Regex.IsMatch(line, speakerPattern)) return Tokens.Speaker;
         if (Regex.IsMatch(line, invokePattern))
         {
            string processedLine = line.Trim().Substring(2);
            string[] arguments = processedLine.Substring(0, processedLine.Length - 2).Split(':');
            return arguments[0] switch
            {
               "Invoke" => Tokens.ExplicitInvoke,
               "Image" => Tokens.ImageInvoke,
               "DialogueName" => Tokens.DialogueNameInvoke,
               "DialogueEnd" => Tokens.EndInvoke,
               _ => Tokens.Invoke
            };
         }
         if (Regex.IsMatch(line, choicePattern)) return Tokens.Choice;
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
         
         List<Conversation> conversations = new List<Conversation>();
         List<Sentence> sentences = new List<Sentence>();

         Conversation conversation = new Conversation {Dialogues = new List<Dialogue>()};
         Dialogue dialogue = new Dialogue();
         
         for (int index = 0; index < textLines.Length; index++)
         {
            string line = textLines[index];
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
                  dialogue.Sentence.Text = string.Join("\n", sentences.Select(sentence => sentence.Text));
                  
                  dialogue.Sentence.Invocations = new Dictionary<int, string>();
                  foreach (Sentence sentence in sentences.Where(sentence => sentence.Invocations.Count > 0))
                  {
                     foreach (KeyValuePair<int, string> keyValuePair in sentence.Invocations)
                     {
                        dialogue.Sentence.Invocations.Add(keyValuePair.Key, keyValuePair.Value);
                     }
                  }
                  conversation.Dialogues.Add(dialogue);
                  
                  processedLine = ReplaceGlobalVariables(processedLine);
                  dialogue = new Dialogue {Speaker = processedLine};
                  sentences.Clear();
                  break;
               }
               case Tokens.Sentence:
               {
                  processedLine = ReplaceGlobalVariables(processedLine);
                  Sentence sentence = new Sentence
                  {
                     Text = processedLine, 
                     Invocations = GatherInlineFunctionInvocations(line)
                  };
                  sentences.Add(sentence);
                  break;
               }
               case Tokens.ImageInvoke:
               {
                  if (Resources.Load(processedLine) == null)
                     Debug.LogError(
                        $"[Ibralogue] Invalid image path {processedLine} at line {index + 1} in {dialogueAsset.name}.");
                  dialogue.SpeakerImage = Resources.Load<Sprite>(processedLine);
                  break;
               }
               case Tokens.Invoke:
               case Tokens.ExplicitInvoke:
               {
                  if (dialogue.Sentence.Invocations == null)
                     dialogue.Sentence.Invocations = new Dictionary<int, string>();
                  dialogue.Sentence.Invocations.Add(conversation.Dialogues.Count, processedLine);
                  break;
               }
               case Tokens.DialogueNameInvoke:
               {
                  conversation.Name = processedLine;
                  break;
               }
               case Tokens.EndInvoke:
               {
                  dialogue.Sentence.Text = string.Join("\n", sentences.Select(sentence => sentence.Text));
                  dialogue.Sentence.Invocations = new Dictionary<int, string>();
                  
                  foreach (Sentence sentence in sentences.Where(sentence => sentence.Invocations.Count > 0))
                  {
                     foreach (KeyValuePair<int, string> keyValuePair in sentence.Invocations)
                     {
                        dialogue.Sentence.Invocations.Add(keyValuePair.Key, keyValuePair.Value);
                     }
                  }
                  
                  sentences.Clear();

                  conversation.Dialogues.Add(dialogue);
                  dialogue = new Dialogue();
                  
                  conversations.Add(conversation);
                  conversation = new Conversation {Dialogues = new List<Dialogue>()};
                  break;
               }
               case Tokens.Choice:
                  if (conversation.Choices == null)
                     conversation.Choices = new Dictionary<Choice, int>();
                  string[] arguments = Regex.Split(processedLine, @"(->)");
                  Choice choice = new Choice() {ChoiceName = arguments[0].Trim(), LeadingConversationName = arguments[2].Trim()};
                  conversation.Choices.Add(choice,conversation.Dialogues.Count);
                  break;
               default:
                  throw new ArgumentOutOfRangeException();
            }
         }

         if (sentences.Count == 0) 
            return conversations;
         
         dialogue.Sentence.Text = string.Join("\n", sentences.Select(sentence => sentence.Text));
         
         dialogue.Sentence.Invocations = new Dictionary<int, string>();
         foreach (Sentence sentence in sentences.Where(sentence => sentence.Invocations.Count > 0))
         {
            foreach (KeyValuePair<int, string> keyValuePair in sentence.Invocations)
            {
               dialogue.Sentence.Invocations.Add(keyValuePair.Key, keyValuePair.Value);
            }
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
               line = line.Trim().Substring(2);
               line = line.Substring(0, line.Length - 2);
               break;
            case Tokens.ImageInvoke:
            case Tokens.DialogueNameInvoke:
            case Tokens.ExplicitInvoke:
               if (line.Length > 4)
               {
                  line = line.Trim().Substring(2);
                  //We don't need to pass in the first argument since we already know the type of method being invoked;
                  line = line.Substring(0, line.Length - 2).Split(':')[1].Trim();
               }
               else
               {
                  throw new ArgumentOutOfRangeException($"[Ibralogue] Invocation name too short! Are you sure you used the syntax properly? At: {token} - {line}");
               }
               break;
            case Tokens.Comment:
            case Tokens.Sentence:
               foreach (Match match in Regex.Matches(line, inlineInvokePattern))
               {
                  string functionName = match.ToString();
                  line = line.Replace(functionName, "");
               }
               break;
            case Tokens.Choice:
               line = line.Trim();
               line = line.Substring(1);
               break;
            case Tokens.EndInvoke:
               break;
            default:
               Debug.LogError($"[Ibralogue] Argument Out Of Range: {token}");
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
      
      
      /// <summary>
      /// Adds all function invocations in a given line.
      /// <param name="replaceFunction">Whether to replace the function or not</param>
      /// </summary>
      private static Dictionary<int,string> GatherInlineFunctionInvocations(string line)
      {
         Dictionary<int,string> inlineFunctionNames = new Dictionary<int,string>();
         foreach (Match match in Regex.Matches(line,inlineInvokePattern))
         {
            string functionName = match.ToString();
            int characterIndex = line.IndexOf(functionName);
            
            functionName = functionName.Trim().Substring(2);
            functionName = functionName.Substring(0, functionName.Length - 2);
            inlineFunctionNames.Add(characterIndex, functionName);
         }
         return inlineFunctionNames;
      }
   }
}