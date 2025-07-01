// <copyright file="HecUndoAction.cs" company="Dataescher">
// 	Copyright (c) 2022 Dataescher. All rights reserved.
// </copyright>
// <summary>An abstract base class for a HexEditor control undo history item.</summary>

namespace Dataescher.Controls {
	public partial class HexEditorControl {
		/// <summary>A HexEditor control undo history item.</summary>
		internal abstract class HecUndoAction {
			/// <summary>Perform an undo action.</summary>
			public abstract void Undo();
			/// <summary>Perform a redo action.</summary>
			public abstract void Redo();
		}
	}
}