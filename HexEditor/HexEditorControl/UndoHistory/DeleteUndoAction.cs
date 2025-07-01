// <copyright file="DeleteUndoAction.cs" company="Dataescher">
// 	Copyright (c) 2022 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements a delete undo action.</summary>

using Dataescher.Data;
using System;

namespace Dataescher.Controls {
	public partial class HexEditorControl {
		/// <summary>A delete undo action.</summary>
		/// <seealso cref="T:Dataescher.Controls.HexEditorControl.HecUndoAction"/>
		internal class DeleteUndoAction : HecUndoAction {
			/// <summary>(Immutable) The parent HexEditorControl.</summary>
			private readonly HexEditorControl hexEditorControl;
			/// <summary>(Immutable) The starting address.</summary>
			private readonly UInt32 address;
			/// <summary>(Immutable) The data before the delete.</summary>
			private readonly DataBuffer oldData;

			/// <summary>Initializes a new instance of the Dataescher.UndoHistory.DeleteUndoAction class.</summary>
			/// <param name="hexEditorControl">The parent HexEditorControl.</param>
			/// <param name="address">The starting address.</param>
			/// <param name="oldData">The data before the delete.</param>
			public DeleteUndoAction(HexEditorControl hexEditorControl, UInt32 address, DataBuffer oldData) {
				this.hexEditorControl = hexEditorControl;
				this.address = address;
				this.oldData = oldData;
			}

			/// <summary>Perform a redo action.</summary>
			public override void Redo() {
				hexEditorControl.SelectionByteRegion = MemoryRegion.FromStartAddressAndSize(address, oldData.Data.Length);
				hexEditorControl.ShowByteAddress(address, ShowAddressSettings.ShowMiddle);
				hexEditorControl.Delete(MemoryRegion.FromStartAddressAndSize(address, oldData.Data.Length), true);
			}

			/// <summary>Perform an undo action.</summary>
			public override void Undo() {
				hexEditorControl.SelectionByteRegion = MemoryRegion.FromStartAddressAndSize(address, oldData.Data.Length);
				hexEditorControl.ShowByteAddress(address, ShowAddressSettings.ShowMiddle);
				hexEditorControl.SetData(address, oldData, true);
			}
		}
	}
}