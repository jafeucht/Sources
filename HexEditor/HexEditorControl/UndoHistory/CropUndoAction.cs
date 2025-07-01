// <copyright file="CropUndoAction.cs" company="Dataescher">
// 	Copyright (c) 2022 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements a delete undo action.</summary>

using Dataescher.Data;

using System.Collections.Generic;

namespace Dataescher.Controls {
	public partial class HexEditorControl {
		/// <summary>A delete undo action.</summary>
		/// <seealso cref="T:Dataescher.Controls.HexEditorControl.HecUndoAction"/>
		internal class CropUndoAction : HecUndoAction {
			/// <summary>(Immutable) The parent HexEditorControl.</summary>
			private readonly HexEditorControl hexEditorControl;
			/// <summary>(Immutable) The crop region.</summary>
			private readonly MemoryRegion cropRegion;
			/// <summary>The associated delete actions.</summary>
			public List<DeleteUndoAction> DeleteActions;

			/// <summary>Adds a delete action.</summary>
			/// <param name="deleteAction">The delete action.</param>
			public void AddDeleteAction(DeleteUndoAction deleteAction) {
				DeleteActions.Add(deleteAction);
			}

			/// <summary>Initializes a new instance of the Dataescher.UndoHistory.CropUndoAction class.</summary>
			/// <param name="hexEditorControl">The parent HexEditorControl.</param>
			/// <param name="cropRegion">The crop region.</param>
			public CropUndoAction(HexEditorControl hexEditorControl, MemoryRegion cropRegion) {
				this.hexEditorControl = hexEditorControl;
				this.cropRegion = cropRegion;
				DeleteActions = new();
			}

			/// <summary>Perform a redo action.</summary>
			public override void Redo() {
				foreach (DeleteUndoAction deleteUndoAction in DeleteActions) {
					deleteUndoAction.Redo();
				}
				hexEditorControl.SelectionByteRegion = cropRegion;
			}

			/// <summary>Perform an undo action.</summary>
			public override void Undo() {
				foreach (DeleteUndoAction deleteUndoAction in DeleteActions) {
					deleteUndoAction.Undo();
				}
				hexEditorControl.SelectionByteRegion = cropRegion;
			}
		}
	}
}