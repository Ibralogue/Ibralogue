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
      private static readonly Regex SpeakerRegex = new Regex(@"^\[(.+)\]");
      private static readonly Regex CommentRegex = new Regex(@"#.*");
      private static readonly Regex ChoiceRegex = new Regex(@"^-(.+)->(.+)");
      private static readonly Regex VariableRegex = new Regex(@"\$[a-zA-Z]*");
      
      private static readonly Regex FunctionRegex = new Regex(@"{{(.+)}}");
      private static readonly Regex SingleFunctionRegex = new Regex(@"^{{(.+)}}");
      private static readonly Regex ArgumentFunctionRegex = new Regex(@"{{.*(.+\(.*\)).*}}"); //TODO: the syntax is {{ Foo(args) }}
      
      /// <summary>
      /// Tokens are a representation of the attribute of the current line we are parsing
      /// which provides additional information about the lexeme that the token represents. 
      /// </summary>
      private enum Token
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
      /// The GetLineToken function reads the given line and assigns the entire line a token based on that.
      /// </returns>
      /// <param name="line">The line of the dialogue we need the token of.</param>
      private static Token GetLineToken(string line)
      {
         if (CommentRegex.IsMatch(line)) 
            return Token.Comment;
         if (SpeakerRegex.IsMatch(line)) 
            return Token.Speaker;
         if (SingleFunctionRegex.IsMatch(line))
         {
            string functionName = line.Trim();
            functionName = line.Substring(2);
            functionName = functionName.Remove(functionName.Length - 2);
            
            switch (functionName)
            {
               case "DialogueEnd":
                  return Token.EndInvoke;
               default:
                  return Token.Sentence;
            }
         }
         if (ArgumentFunctionRegex.IsMatch(line))
         {
            string functionName = Regex.Match(line.Substring(2), @"^[^\(]+").Value;
            switch (functionName)
            {
               case "Image":
                  return Token.ImageInvoke;
               case "DialogueName":
                  return Token.DialogueNameInvoke;
               default:
                  return Token.Sentence;
            }
         }
         if (ChoiceRegex.IsMatch(line)) 
            return Token.Choice;
         
         return Token.Sentence;
      }   
      
      /// <summary>
      /// The ParseDialogue function returns an array of conversations and associates information
      /// with each element in the dialogue array (Speaker Name, Sentence, Image etc.) as well as additional metadata.
      /// </summary>
      public static List<Conversation> ParseDialogue(TextAsset dialogueAsset)
      {
         string[] textLines = dialogueAsset.text.Split('\n');
         
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
            Token token = GetLineToken(textLine);
            string processedLine = GetProcessedLine(token, textLine);

            switch (token)
            {
               case Token.Comment:
                  break;
               case Token.Speaker when line.Speaker == null:
               {
                  processedLine = ReplaceGlobalVariables(processedLine);
                  line.Speaker = processedLine;
                  break;
               }
               case Token.Speaker:
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
               case Token.Sentence:
               {
                  if (sentences.Count == 0 && processedLine == string.Empty) 
                     break;
                  
                  processedLine = ReplaceGlobalVariables(processedLine);
                  LineContents lineContents = new LineContents
                  {
                     Text = processedLine, 
                     Invocations = GatherInlineFunctionInvocations(textLine)
                  };
                  sentences.Add(lineContents);
                  break;
               }
               case Token.ImageInvoke:
               {
                  if (Resources.Load(processedLine) == null)
                     DialogueLogger.LogError(index+1, $"Invalid image path {processedLine} in {dialogueAsset.name}");
                  
                  line.SpeakerImage = Resources.Load<Sprite>(processedLine);
                  break;
               }
               case Token.DialogueNameInvoke:
               {
                  conversation.Name = processedLine;
                  break;
               }
               case Token.EndInvoke:
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
               case Token.Choice:
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
      /// The GetProcessedLine function takes in a token and a line, and removes or adds anything that requires it in the
      /// final processed line. It does not handle the logic, but merely just how the final string is represented.
      /// </summary>
      private static string GetProcessedLine(Token token, string line)
      {
         switch (token)
         {
            case Token.Comment:
               break;
            case Token.Speaker:
               if (line.Length >= 2) {
                  line = line.Trim().Substring(1);
                  line = line.Substring(0, line.Length - 1);
               } 
               break;
            case Token.ImageInvoke:
            case Token.DialogueNameInvoke:
               if (line.Length > 4)
               {
                  line = line.Trim();
                  line = Regex.Match(line, @"\(([^\)]+)\)").Value;
                  line = line.Replace("(", string.Empty).Replace(")", string.Empty);
               }
               else
               {
                  //TODO: implement proper error handling
                  throw new ArgumentOutOfRangeException($"[Ibralogue] Invocation name too short! Are you sure you used the syntax properly? At: {token} - {line}");
               }
               break;
            case Token.Sentence:
               foreach (Match match in FunctionRegex.Matches(line))
               {
                  string functionName = match.ToString();
                  line = line.Replace(functionName, string.Empty);
               }
               break;
            case Token.Choice:
               line = line.Trim();
               line = line.Substring(1);
               break;
            case Token.EndInvoke:
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
         foreach (Match match in VariableRegex.Matches(line))
         {
            string processedVariable = match.ToString().Trim().Replace("$", string.Empty);
            if (DialogueManager.GlobalVariables.TryGetValue(processedVariable, out string keyValue))
            {
               line = line.Replace(match.ToString(), keyValue);
            }
            else
            {
               Debug.LogWarning(
                  $"[Ibralogue] Variable declaration detected, ({processedVariable}) but no entry found in dictionary!");
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
         foreach (Match match in FunctionRegex.Matches(line))
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