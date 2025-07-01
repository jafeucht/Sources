// <copyright file="DeleteUndoAction.cs" company="Dataescher">
// 	Copyright (c) 2022 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements a delete undo action.</summary>

using System;

namespace Dataescher.Controls {
	public partial class HexEditorControl {
		/// <summary>A delete undo action.</summary>
		/// <seealso cref="T:Dataescher.Controls.HexEditorControl.HecUndoAction"/>
		internal class OffsetAllDataUndoAction : HecUndoAction {
			/// <summary>(Immutable) The parent HexEditorControl.</summary>
			private readonly HexEditorControl hexEditorControl;
			/// <summary>(Immutable) The starting address.</summary>
			private readonly UInt32 offset;
			/// <summary>(Immutable) The starting address.</summary>
			private readonly Boolean moveUp;

			/// <summary>Initializes a new instance of the Dataescher.UndoHistory.DeleteUndoAction class.</summary>
			/// <param name="hexEditorControl">The parent HexEditorControl.</param>
			/// <param name="offset">The offset in bytes.</param>
			/// <param name="moveUp">If true, move up; otherwise, move down.</param>
			public OffsetAllDataUndoAction(HexEditorControl hexEditorControl, UInt32 offset, Boolean moveUp) {
				this.hexEditorControl = hexEditorControl;
				this.offset = offset;
				this.moveUp = moveUp;
			}

			/// <summary>Perform a redo action.</summary>
			public override void Redo() {
				hexEditorControl.OffsetAllData(offset, moveUp, true);
			}

			/// <summary>Perform an undo action.</summary>
			public override void Undo() {
				hexEditorControl.OffsetAllData(offset, !moveUp, true);
			}
		}
	}
}