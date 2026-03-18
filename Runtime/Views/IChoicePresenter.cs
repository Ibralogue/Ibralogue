using System;
using System.Collections.Generic;
using Ibralogue.Parser;

namespace Ibralogue.Views
{
	/// <summary>
	/// Provides choice display and cleanup. Implement this interface on any
	/// MonoBehaviour to handle choice presentation independently from the
	/// dialogue view. Assign it to the engine's Choice Presenter field.
	/// </summary>
	public interface IChoicePresenter
	{
		/// <summary>
		/// Presents choices to the player. When a choice is selected,
		/// invoke <paramref name="onChoiceSelected"/> with the chosen option.
		/// </summary>
		void DisplayChoices(List<Choice> choices, Action<Choice> onChoiceSelected);

		/// <summary>
		/// Clears all choice UI elements.
		/// </summary>
		void ClearChoices();
	}
}
