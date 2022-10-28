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
      private static readonly Regex MetadataRegex = new Regex(@"##.*");
      private static readonly Regex LineCommentRegex = new Regex(@"^#.*");
      private static readonly Regex ChoiceRegex = new Regex(@"^-(.+)->(.+)");
      private static readonly Regex VariableRegex = new Regex(@"\$[a-zA-Z0-9]*");
      
      private static readonly Regex FunctionRegex = new Regex(@"{{(.+)}}");
      private static readonly Regex ArgumentFunctionRegex = new Regex(@"{{.*(.+\(.*\)).*}}");
      
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
      }
      
      /// <returns>
      /// The GetLineToken function reads the given line and assigns the entire line a token based on that.
      /// </returns>
      /// <param name="line">The line of the dialogue we need the token of.</param>
      private static Token GetLineToken(string line)
      {
         if (LineCommentRegex.IsMatch(line)) 
            return Token.Comment;
         if (SpeakerRegex.IsMatch(line)) 
            return Token.Speaker;
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
      public static List<Conversation> ParseDialogue(DialogueAsset dialogueAsset)
      {
         string[] textLines = dialogueAsset.Content.Split('\n');
         
         List<Conversation> conversations = new List<Conversation>();
         List<LineContent> lineContents = new List<LineContent>();

         Conversation conversation = new Conversation {Lines = new List<Line>()};
         Line line = new Line
         {
            LineContent = new LineContent
            {
               Invocations = new Dictionary<int, string>(),
               Metadata = new Dictionary<string, string>()
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
                  line.LineContent.Text = string.Join("\n", lineContents.Select(sentence => sentence.Text));
                  AddHeadersToLine(lineContents, line);
                  
                  conversation.Lines.Add(line);
                  
                  processedLine = ReplaceGlobalVariables(processedLine);
                  line = new Line
                  {
                     Speaker = processedLine,
                     LineContent = new LineContent
                     {
                        Invocations = new Dictionary<int, string>(),
                        Metadata = new Dictionary<string, string>()
                     }
                  };
                  lineContents.Clear();
                  break;
               }
               case Token.Sentence:
               {
                  if (lineContents.Count == 0 && processedLine == string.Empty) 
                     break;
                  
                  processedLine = ReplaceGlobalVariables(processedLine);
                  LineContent lineContent = new LineContent
                  {
                     Text = processedLine, 
                     Invocations = GatherInlineFunctionInvocations(textLine),
                     Metadata = GatherMetadata(textLine)
                  };
                  lineContents.Add(lineContent);
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
                  if (conversations.Count == 0)
                  {
                     conversation = new Conversation
                     {
                        Lines = new List<Line>(),
                        Name = processedLine
                     };
                     conversations.Add(conversation);
                  }
                  else
                  {
                     line.LineContent.Text = string.Join("\n", lineContents.Select(sentence => sentence.Text));
                     AddHeadersToLine(lineContents, line);
                     conversation.Lines.Add(line);
                     line = new Line
                     {
                        LineContent = new LineContent
                        {
                           Invocations = new Dictionary<int, string>(),
                           Metadata = new Dictionary<string, string>()
                        }
                     };

                     conversations.Add(conversation);
                     conversation = new Conversation
                     {
                        Lines = new List<Line>(),
                        Name = processedLine
                     };
                     lineContents.Clear();
                  }
                  break;
               }
               case Token.Choice:
                  if (conversation.Choices == null)
                     conversation.Choices = new Dictionary<Choice, int>();
                  string[] arguments = Regex.Split(processedLine, @"(->)");
                  Choice choice = new Choice
                  {
                     ChoiceName = arguments[0].Trim(), LeadingConversationName = arguments[2].Trim(),
                     Metadata = GatherMetadata(textLine)
                  };
                  conversation.Choices.Add(choice,conversation.Lines.Count);
                  break;
               default:
                  DialogueLogger.LogError(index+1, "Encountered unexpected token while parsing!");
                  break;
            }
         }
         
         line.LineContent.Text = string.Join("\n", lineContents.Select(sentence => sentence.Text));
         AddHeadersToLine(lineContents, line);

         conversation.Lines.Add(line);
         conversations.Add(conversation);
         conversations.RemoveAt(0);
         
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
                  DialogueLogger.LogError(-1, $"Invocation name too short! Are you sure you used the syntax properly?  At: {token} - {line}");
               }
               break;
            case Token.Sentence:
               foreach (Match match in FunctionRegex.Matches(line))
               {
                  string functionName = match.ToString();
                  line = line.Replace(functionName, string.Empty);
               }
               foreach (Match match in MetadataRegex.Matches(line))
               {
                  string functionName = match.ToString();
                  line = line.Replace(functionName, string.Empty);
               }
               break;
            case Token.Choice:
               line = line.Trim();
               line = line.Substring(1);
               foreach (Match match in MetadataRegex.Matches(line))
               {
                  string functionName = match.ToString();
                  line = line.Replace(functionName, string.Empty);
               }
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
            if (DialogueGlobals.GlobalVariables.TryGetValue(processedVariable, out string keyValue))
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
      /// Gets all metadata for a line.
      /// </summary>
      private static Dictionary<string, string> GatherMetadata(string line)
      {
         Dictionary<string,string> metadata = new Dictionary<string,string>();
         
         foreach (Match match in MetadataRegex.Matches(line))
         {
            string comment = match.ToString().Trim();
            comment = comment.Replace("##", "").Trim();
            
            foreach (string data in comment.Split(' '))
            {
               string[] keyValuePairs = data.Split(':');
               
               if (keyValuePairs[0] == data)
               {
                  metadata.Add(data, data);
               }
               else
               {
                  for (int i = 0; i < keyValuePairs.Length - 1; i+=2)
                  {
                     metadata.Add(keyValuePairs[i], keyValuePairs[i+1]);
                  }
               }
            }
         }
         return metadata;
      }


      /// <summary>
      /// For every invocation all of our local sentences we add them to the dialogues lineContent invocation.
      /// </summary>
      private static void AddHeadersToLine(IEnumerable<LineContent> sentences, Line line)
      {
         foreach (LineContent sentence in sentences.Where(sentence => sentence.Invocations.Count > 0))
         {
            foreach (KeyValuePair<int, string> keyValuePair in sentence.Invocations)
            {
               line.LineContent.Invocations.Add(keyValuePair.Key, keyValuePair.Value);
            }
         }
         foreach (LineContent sentence in sentences.Where(sentence => sentence.Metadata.Count > 0))
         {
            foreach (KeyValuePair<string, string> keyValuePair in sentence.Metadata)
            {
               line.LineContent.Metadata.Add(keyValuePair.Key, keyValuePair.Value);
            }
         }
      }
   }
}