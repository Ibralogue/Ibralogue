using System;
using System.Collections.Generic;
using Ibralogue.Parser;
using TMPro;
using UnityEngine;

namespace Ibralogue.Views
{
	/// <summary>
	/// Default choice presenter that instantiates button prefabs into a container.
	/// This is the same behavior previously built into <see cref="DialogueViewBase"/>,
	/// extracted as a standalone component for independent use.
	/// </summary>
	public class ButtonChoicePresenter : MonoBehaviour, IChoicePresenter
	{
		[SerializeField] private Transform choiceButtonHolder;
		[SerializeField] private GameObject choiceButtonPrefab;

		private readonly List<ChoiceButton> _instances = new List<ChoiceButton>();

		public void DisplayChoices(List<Choice> choices, Action<Choice> onChoiceSelected)
		{
			ClearChoices();
			if (choices == null || choices.Count == 0) return;

			foreach (Choice choice in choices)
			{
				ChoiceButton instance = Instantiate(choiceButtonPrefab, choiceButtonHolder)
					.GetComponent<ChoiceButton>();

				if (instance == null)
				{
					DialogueLogger.LogError(
						"ChoiceButton component missing on choice button prefab.");
					continue;
				}

				Choice captured = choice;
				instance.OnChoiceClick.AddListener(() => onChoiceSelected(captured));
				instance.GetComponentInChildren<TextMeshProUGUI>().text = choice.ChoiceName;

				_instances.Add(instance);
			}
		}

		public void ClearChoices()
		{
			foreach (ChoiceButton instance in _instances)
			{
				instance.OnChoiceClick.RemoveAllListeners();
				Destroy(instance.gameObject);
			}

			_instances.Clear();
		}
	}
}
