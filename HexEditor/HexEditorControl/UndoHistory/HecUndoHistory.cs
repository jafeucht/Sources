// <copyright file="HecUndoHistory.cs" company="Dataescher">
// 	Copyright (c) 2022 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements a HexEditor control undo history buffer.</summary>

using System;
using System.Collections.Generic;

namespace Dataescher.Controls {
	public partial class HexEditorControl {
		/// <summary>A HexEditor control undo history buffer.</summary>
		public class HecUndoHistory {
			/// <summary>(Immutable) The undo action list.</summary>
			private readonly List<HecUndoAction> UndoActions;

			/// <summary>The current undo level.</summary>
			private Int32 undoLevel;

			/// <summary>
			///     The point in the history in which the file has been saved, at which level to consider there to be no
			///     modifications to the open file.
			/// </summary>
			private Int32 saveLevel;

			/// <summary>
			///     True if performing an undo action, indicating that no modifications to the undo history should take place.
			/// </summary>
			private Boolean performingAction;

			/// <summary>(Immutable) The hex editor control.</summary>
			private readonly HexEditorControl hexEditorControl;

			/// <summary>Initializes a new instance of the Dataescher.UndoHistory.HecUndoHistory class.</summary>
			/// <param name="hexEditorControl">The hex editor control.</param>
			public HecUndoHistory(HexEditorControl hexEditorControl) {
				UndoActions = new();
				performingAction = false;
				this.hexEditorControl = hexEditorControl;
			}

			/// <summary>Registers a new undo history item.</summary>
			/// <param name="newAction">The new action.</param>
			internal void RegisterAction(HecUndoAction newAction) {
				if (!performingAction) {
					if (CanRedo) {
						// Delete all redo history items from the list
						UndoActions.RemoveRange(undoLevel, UndoActions.Count - undoLevel);
					}
					UndoActions.Add(newAction);
					undoLevel++;
				}
			}
			/// <summary>Perform an undo action.</summary>
			public void Undo() {
				if (CanUndo) {
					hexEditorControl.EditMode = false;
					performingAction = true;
					undoLevel--;
					UndoActions[undoLevel].Undo();
					performingAction = false;
					hexEditorControl.IsModified = undoLevel != saveLevel;
				}
			}
			/// <summary>Perform a redo action.</summary>
			public void Redo() {
				if (CanRedo) {
					hexEditorControl.EditMode = false;
					performingAction = true;
					UndoActions[undoLevel].Redo();
					undoLevel++;
					performingAction = false;
					hexEditorControl.IsModified = undoLevel != saveLevel;
				}
			}

			/// <summary>Gets a boolean value indicating whether we can undo an action.</summary>
			public Boolean CanUndo => undoLevel > 0;

			/// <summary>Gets a boolean value indicating whether we can redo an action.</summary>
			public Boolean CanRedo => undoLevel < UndoActions.Count;

			/// <summary>Sets the save point.</summary>
			public void SetSavePoint() {
				saveLevel = undoLevel;
				hexEditorControl.IsModified = false;
			}

			/// <summary>Clear all history items.</summary>
			public void Clear() {
				UndoActions.Clear();
				undoLevel = 0;
				hexEditorControl.IsModified = false;
			}
		}
	}
}