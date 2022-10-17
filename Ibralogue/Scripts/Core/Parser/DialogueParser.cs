using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Ibralogue.Parser;
using UnityEngine;

namespace Ibralogue
{
   public static class DialogueParser
   {
      private const string SpeakerPattern = @"^\[(.+)\]";
      private const string InvokePattern = @"^<<(.+)>>";
      private const string InlineInvokePattern = @"<<(.+)>>";
      private const string ArgumentInvokePattern = @"<<(.+:+\[+.+])>>";
      private const string ChoicePattern = @"^-(.+)->(.+)";
      
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
         ImageInvoke,
         DialogueNameInvoke,
         EndInvoke
      }
      
      /// <returns>
      /// The GetLineToken function checks what character the line it is given starts with and returns a "token" with it
      /// according to that.
      /// </returns>
      /// <param name="line">The line of the line we need the token of.</param>
      private static Tokens GetLineToken(string line)
      {
         if (line.StartsWith("#")) return Tokens.Comment;
         if (Regex.IsMatch(line, SpeakerPattern)) return Tokens.Speaker;
         if (Regex.IsMatch(line, InvokePattern))
         {
            string processedLine = line.Trim().Substring(2);
            string[] arguments = processedLine.Substring(0, processedLine.Length - 2).Split(':');
            switch (arguments[0])
            {
                case "Image":
                    return Tokens.ImageInvoke;
                case "DialogueName":
                    return Tokens.DialogueNameInvoke;
                case "DialogueEnd":
                    return Tokens.EndInvoke;
                default:
                    return Tokens.Sentence;
            }
         }
         if (Regex.IsMatch(line, ChoicePattern)) return Tokens.Choice;
         return Tokens.Sentence;
      }   
      
      /// <summary>
      /// The ParseDialogue function returns an array of conversations and associates information
      /// with each element in the line array. Speaker Name, LineContents, Image etc. as well as additional per-conversation metadata.
      /// </summary>
      public static List<Conversation> ParseDialogue(TextAsset dialogueAsset)
      {
         string dialogueText = dialogueAsset.text;
         string[] textLines = dialogueText.Split('\n');
         
         List<Conversation> conversations = new List<Conversation>();
         List<LineContents> sentences = new List<LineContents>();

         Conversation conversation = new Conversation {Lines = new List<Line>()};
         Line line = new Line
         {
            LineContents = new LineContents
            {
               Invocations = new Dictionary<int, string>()
            }
         };
         
         for (int index = 0; index < textLines.Length; index++)
         {
            string textLine = textLines[index];
            Tokens token = GetLineToken(textLine);
            string processedLine = GetProcessedLine(token, textLine);

            switch (token)
            {
               case Tokens.Comment:
                  break;
               case Tokens.Speaker when line.Speaker == null:
               {
                  processedLine = ReplaceGlobalVariables(processedLine);
                  line.Speaker = processedLine;
                  break;
               }
               case Tokens.Speaker:
               {
                  line.LineContents.Text = string.Join("\n", sentences.Select(sentence => sentence.Text));
                  AddInvocationsToDialogue(sentences, line);
                  
                  conversation.Lines.Add(line);
                  
                  processedLine = ReplaceGlobalVariables(processedLine);
                  line = new Line
                  {
                     Speaker = processedLine,
                     LineContents = new LineContents
                     {
                        Invocations = new Dictionary<int, string>()
                     }
                  };
                  sentences.Clear();
                  break;
               }
               case Tokens.Sentence:
               {
                  processedLine = ReplaceGlobalVariables(processedLine);
                  LineContents lineContents = new LineContents
                  {
                     Text = processedLine, 
                     Invocations = GatherInlineFunctionInvocations(textLine)
                  };
                  sentences.Add(lineContents);
                  break;
               }
               case Tokens.ImageInvoke:
               {
                  if (Resources.Load(processedLine) == null)
                     DialogueLogger.LogError(index+1, $"Invalid image path {processedLine} in {dialogueAsset.name}");
                  
                  line.SpeakerImage = Resources.Load<Sprite>(processedLine);
                  break;
               }
               case Tokens.DialogueNameInvoke:
               {
                  conversation.Name = processedLine;
                  break;
               }
               case Tokens.EndInvoke:
               {
                  line.LineContents.Text = string.Join("\n", sentences.Select(sentence => sentence.Text));
                  AddInvocationsToDialogue(sentences, line);
                  
                  sentences.Clear();

                  conversation.Lines.Add(line);
                  line = new Line
                  {
                     LineContents = new LineContents
                     {
                        Invocations = new Dictionary<int, string>()
                     }
                  };
                  
                  conversations.Add(conversation);
                  conversation = new Conversation {Lines = new List<Line>()};
                  break;
               }
               case Tokens.Choice:
                  if (conversation.Choices == null)
                     conversation.Choices = new Dictionary<Choice, int>();
                  string[] arguments = Regex.Split(processedLine, @"(->)");
                  Choice choice = new Choice() {ChoiceName = arguments[0].Trim(), LeadingConversationName = arguments[2].Trim()};
                  conversation.Choices.Add(choice,conversation.Lines.Count);
                  break;
               default:
                  throw new ArgumentOutOfRangeException();
            }
         }

         if (sentences.Count == 0) 
            return conversations;
         
         line.LineContents.Text = string.Join("\n", sentences.Select(sentence => sentence.Text));
         AddInvocationsToDialogue(sentences, line);

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
            case Tokens.ImageInvoke:
            case Tokens.DialogueNameInvoke:
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
               break;
            case Tokens.Sentence:
               foreach (Match match in Regex.Matches(line, InlineInvokePattern))
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
         foreach (Match match in Regex.Matches(line,InlineInvokePattern))
         {
            string functionName = match.ToString();
            int characterIndex = line.IndexOf(functionName);
            
            functionName = functionName.Trim().Substring(2);
            functionName = functionName.Substring(0, functionName.Length - 2);
            inlineFunctionNames.Add(characterIndex, functionName);
         }
         return inlineFunctionNames;
      }

      /// <summary>
      /// For every invocation all of our local sentences we add them to the dialogues lineContents invocation.
      /// </summary>
      private static void AddInvocationsToDialogue(IEnumerable<LineContents> sentences, Line line)
      {
         foreach (LineContents sentence in sentences.Where(sentence => sentence.Invocations.Count > 0))
         {
            foreach (KeyValuePair<int, string> keyValuePair in sentence.Invocations)
            {
               line.LineContents.Invocations.Add(keyValuePair.Key, keyValuePair.Value);
            }
         }
      }
   }
}