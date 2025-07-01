// <copyright file="MultipleUndoAction.cs" company="Dataescher">
// 	Copyright (c) 2022 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements an undo action comprising of a collection of individual steps.</summary>

using System.Collections.Generic;
using System.Linq;

namespace Dataescher.Controls {
	public partial class HexEditorControl {
		/// <summary>An undo action comprising of a collection of individual steps.</summary>
		/// <seealso cref="T:Dataescher.Controls.HexEditorControl.HecUndoAction"/>
		internal class MultipleUndoAction : HecUndoAction {
			/// <summary>The undo action collection.</summary>
			public List<HecUndoAction> UndoActions;

			/// <summary>Initializes a new instance of the Dataescher.UndoHistory.MultipleUndoAction class.</summary>
			public MultipleUndoAction() {
				UndoActions = new();
			}

			/// <summary>Perform a redo action.</summary>
			public override void Redo() {
				foreach (HecUndoAction action in UndoActions) {
					action.Redo();
				}
			}

			/// <summary>Perform an undo action.</summary>
			public override void Undo() {
				foreach (HecUndoAction action in Enumerable.Reverse(UndoActions)) {
					action.Undo();
				}
			}
		}
	}
}