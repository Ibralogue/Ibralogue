using Ibralogue.Parser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ibralogue
{
	public static class DialogueGlobals
	{
		public static readonly Dictionary<string, string> GlobalVariables = new Dictionary<string, string>();
	}

	public abstract class DialogueManagerBase<ChoiceButtonT> : MonoBehaviour where ChoiceButtonT : Component
	{
		public UnityEvent OnConversationStart { get; set; } = new UnityEvent();
		public UnityEvent OnConversationEnd { get; set; } = new UnityEvent();

		public List<Conversation> ParsedConversations { get; protected set; }
		protected Conversation _currentConversation;

		protected int _lineIndex;
		protected bool _linePlaying;

		[Header("Dialogue UI")] [SerializeField]
		protected float scrollSpeed = 25f;

		[SerializeField] protected TextMeshProUGUI nameText;
		[SerializeField] protected TextMeshProUGUI sentenceText;
		[SerializeField] protected Image speakerPortrait;

		[Header("Choice UI")] [SerializeField] protected Transform choiceButtonHolder;
		[SerializeField] protected ChoiceButtonT choiceButton;
		protected List<ChoiceButtonHandle> _choiceButtonInstances = new List<ChoiceButtonHandle>();

		[Header("Function Invocations")] [SerializeField]
		private bool searchAllAssemblies;

		[SerializeField] private List<string> includedAssemblies = new List<string>();


		/// <summary>
		/// Starts a dialogue by parsing all the text in a file, clearing the dialogue box and starting the <see cref="DisplayDialogue"/> function.
		/// </summary>
		/// <param name="interactionDialogue">The dialogue file that we want to use in the conversation</param>
		/// <param name="startIndex">The index of the conversation you want to start.</param>
		public void StartConversation(DialogueAsset interactionDialogue, int startIndex = 0)
		{
			if (interactionDialogue == null)
				throw new ArgumentNullException(nameof(interactionDialogue));

			ParsedConversations = DialogueParser.ParseDialogue(interactionDialogue);

			if (startIndex < 0 || startIndex > ParsedConversations.Count)
				throw new ArgumentOutOfRangeException(nameof(startIndex),
					"Expected value is between 0 and conversations count (exclusive)");

			StartConversation(ParsedConversations[startIndex]);
		}

		/// <summary>
		/// <remarks>
		/// Should only be used inside the DialogueManager, as files should ALWAYS be parsed before any conversations
		/// are started (use the other overload method for this purpose). This function assumes that you have already parsed the dialogue file, and is to be
		/// used to avoid parsing the whole file again.
		/// </remarks>
		/// </summary>
		/// <param name="conversation"></param>
		private void StartConversation(Conversation conversation)
		{
			StopConversation();
			_currentConversation = conversation;
			OnConversationStart.Invoke();
			StartCoroutine(DisplayDialogue());
		}

		/// <summary>
		/// Stops the currently playing conversation and clears the dialogue box.
		/// </summary>
		public void StopConversation()
		{
			StopCoroutine(DisplayDialogue());
			ClearDialogueBox();
			_lineIndex = 0;
			_currentConversation = null;
			OnConversationEnd.Invoke();
		}

		/// <summary>
		/// Jumps to a  given conversation in the dialogue by using its name.
		/// </summary>
		/// <param name="conversationName">Name as seen in the DialogueAsset</param>
		public void JumpTo(string conversationName)
		{
			if (ParsedConversations == null || ParsedConversations.Count == 0)
				throw new InvalidOperationException(
					"There is no ongoing conversation, therefore the jump cannot be executed");

			Conversation conversation = ParsedConversations.Find(c => c.Name == conversationName);

			if (conversation.Name == null)
				throw new ArgumentException($"There is no {nameof(conversation)} matching the given argument",
					nameof(conversationName));

			StartConversation(conversation);
		}

		/// <summary>
		/// The DisplayDialogue coroutine displays the dialogue character by character in a scrolling manner and sets all other
		/// relevant values.
		/// </summary>
		private IEnumerator DisplayDialogue()
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
			IEnumerable<MethodInfo> dialogueMethods = GetDialogueMethods();

            int index = 0;
            while (_currentConversation != null &&
			       index < _currentConversation.Lines[_lineIndex].LineContent.Text.Length)
			{
				InvokeFunctions(index, dialogueMethods, _currentConversation.Lines[_lineIndex].LineContent.Invocations);
				index++;
				sentenceText.maxVisibleCharacters++;
				yield return new WaitForSeconds(1f / scrollSpeed);
			}

			_linePlaying = false;
			yield return null;
		}

		/// <summary>
		/// Looks for functions and invokes them in a given line. The function also handles multiple return types and the parameters passed in.
		/// </summary>
		/// <param name="index">The index of the current visible character.</param>
		/// <param name="dialogueMethods">All of the dialogue methods to be searched through.</param>
		/// <param name="functionInvocations">The invocations inside the current line being displayed.</param>
		private void InvokeFunctions(int index, IEnumerable<MethodInfo> dialogueMethods, Dictionary<int, string> functionInvocations)
		{
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
		/// Clears the dialogue box and displays the next <see cref="Line"/> if no sentences are left in the
		/// current one.
		/// </summary>
		public void TryDisplayNextLine()
		{
			if (_linePlaying) return;
			if (_currentConversation == null) return;
			if (_choiceButtonInstances.Count > 0) return;

			ClearDialogueBox();

			if (_lineIndex < _currentConversation.Lines.Count - 1)
			{
				_lineIndex++;
				StartCoroutine(DisplayDialogue());
			}
			else
			{
				StopConversation();
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


		public void TryAdvanceDialogue()
		{
			if (_currentConversation == null)
				return;
			if (_linePlaying)
			{
				SkipLineAnimation();
				return;
			}

			TryDisplayNextLine();
		}

		/// <summary>
		/// Uses the Unity UI system and TextMeshPro to render choice buttons.
		/// </summary>
		protected void DisplayChoices()
		{
			_choiceButtonInstances.Clear();
			if (_currentConversation.Choices == null || !_currentConversation.Choices.Any()) return;
			foreach (Choice choice in _currentConversation.Choices.Keys)
			{
				ChoiceButtonT choiceButtonInstance = CreateChoiceButton();

				UnityAction onClickAction = null;
				int conversationIndex = -1;

				switch (choice.LeadingConversationName)
				{
					case ">>":
						DialogueLogger.LogError(2,
							"The embedded choice is not yet implemented, '>>' keyword is reserved for future use");
						break;
					default:
						conversationIndex =
							ParsedConversations.FindIndex(c => c.Name == choice.LeadingConversationName);
						if (conversationIndex == -1)
							DialogueLogger.LogError(2,
								$"No conversation called \"{choice.LeadingConversationName}\" found for choice \"{choice.ChoiceName}\" in \"{_currentConversation.Name}\".",
								this);
						onClickAction = () => StartConversation(ParsedConversations[conversationIndex]);
						break;
				}


				ChoiceButtonHandle handle = new ChoiceButtonHandle(
					choiceButtonInstance,
					onClickAction
				);

				_choiceButtonInstances.Add(handle);
				PrepareChoiceButton(handle, choice);

				choiceButtonInstance.GetComponentInChildren<TextMeshProUGUI>().text = choice.ChoiceName;
				handle.ClickEvent.AddListener(handle.ClickCallback);
			}
		}

		protected abstract void PrepareChoiceButton(ChoiceButtonHandle handle, Choice choice);

		/// <summary>
		/// Gets all methods for the current assembly, other specified assemblies, or all assemblies, and checks them against the
		/// DialogueFunction attribute.
		/// </summary>
		private IEnumerable<MethodInfo> GetDialogueMethods()
		{
			List<Assembly> assemblies = new List<Assembly>();
			Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
			if (searchAllAssemblies) assemblies.AddRange(allAssemblies);
			else
				foreach (Assembly assembly in allAssemblies)
				{
					string name = assembly.GetName().Name;
					if (name == "Assembly-CSharp" || includedAssemblies.Contains(name) ||
					    assembly == Assembly.GetExecutingAssembly()) assemblies.Add(assembly);
				}

			List<MethodInfo> methods = new List<MethodInfo>();
			foreach (Assembly assembly in assemblies)
			{
				IEnumerable<MethodInfo> allMethods = assembly.GetTypes()
					.SelectMany(t => t.GetMethods())
					.Where(m => m.GetCustomAttributes(typeof(DialogueFunctionAttribute), false).Length > 0);
				methods.AddRange(allMethods);
			}

			return methods;
		}

		/// <summary>
		/// Sets the speaker image and makes the Image transparent if there is no speaker image.
		/// </summary>
		protected void DisplaySpeakerImage()
		{
			speakerPortrait.color = _currentConversation.Lines[_lineIndex].SpeakerImage == null
				? new Color(0, 0, 0, 0)
				: new Color(255, 255, 255, 255);
			speakerPortrait.sprite = _currentConversation.Lines[_lineIndex].SpeakerImage;
		}

		/// <summary>
		/// Clears all text and Images in the dialogue box.
		/// </summary>
		private void ClearDialogueBox()
		{
			_linePlaying = false;
			nameText.text = string.Empty;
			sentenceText.text = string.Empty;
			sentenceText.maxVisibleCharacters = 0;
			speakerPortrait.color = new Color(0, 0, 0, 0);

			if (_choiceButtonInstances == null)
				return;

			foreach (ChoiceButtonHandle buttonHandle in _choiceButtonInstances)
			{
				ClearChoiceButton(buttonHandle);
				RemoveChoiceButton(buttonHandle);
			}

			_choiceButtonInstances.Clear();
		}

		protected virtual ChoiceButtonT CreateChoiceButton()
		{
			return Instantiate(choiceButton, choiceButtonHolder);
		}

		protected virtual void ClearChoiceButton(ChoiceButtonHandle buttonHandle)
		{
			buttonHandle.ClickEvent.RemoveListener(buttonHandle.ClickCallback);
		}

		protected virtual void RemoveChoiceButton(ChoiceButtonHandle buttonHandle)
		{
			Destroy(buttonHandle.ChoiceButton.gameObject);
		}

		/// <summary>
		/// Represent a single spawned choice button, contains general information about said button
		/// </summary>
		protected class ChoiceButtonHandle
		{
			public ChoiceButtonHandle(ChoiceButtonT choiceButton, UnityAction clickCallback)
			{
				ChoiceButton = choiceButton;
				ClickCallback = clickCallback;
			}

			public UnityEvent ClickEvent { get; set; }

			public ChoiceButtonT ChoiceButton { get; private set; }
			public UnityAction ClickCallback { get; private set; }
		}
	}
}