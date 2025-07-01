// <copyright file="MemoryChangedUndoAction.cs" company="Dataescher">
// 	Copyright (c) 2022 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements a memory change undo action.</summary>

using Dataescher.Data;
using System;

namespace Dataescher.Controls {
	public partial class HexEditorControl {
		/// <summary>A memory change undo action.</summary>
		/// <seealso cref="T:Dataescher.Controls.HexEditorControl.HecUndoAction"/>
		internal class MemoryChangedUndoAction : HecUndoAction {
			/// <summary>(Immutable) The parent HexEditorControl.</summary>
			private readonly HexEditorControl hexEditorControl;
			/// <summary>(Immutable) The starting address of the memory change.</summary>
			private readonly UInt32 address;
			/// <summary>(Immutable) The old data.</summary>
			private readonly DataBuffer oldData;
			/// <summary>(Immutable) The new data.</summary>
			private readonly DataBuffer newData;

			/// <summary>Initializes a new instance of the Dataescher.UndoHistory.MemoryChangedUndoAction class.</summary>
			/// <param name="hexEditorControl">The parent HexEditorControl.</param>
			/// <param name="address">The address.</param>
			/// <param name="oldData">The old data.</param>
			/// <param name="newData">The new data.</param>
			public MemoryChangedUndoAction(HexEditorControl hexEditorControl, UInt32 address, DataBuffer oldData, DataBuffer newData) {
				this.hexEditorControl = hexEditorControl;
				this.address = address;
				this.oldData = oldData;
				this.newData = newData;
			}

			/// <summary>Perform a redo action.</summary>
			public override void Redo() {
				hexEditorControl.ShowByteAddress(address, ShowAddressSettings.ShowMiddle);
				hexEditorControl.SetData(address, newData, true);
				hexEditorControl.SelectionRange = MemoryRegion.FromStartAddressAndSize(address, oldData.Data.Length);
			}

			/// <summary>Perform an undo action.</summary>
			public override void Undo() {
				hexEditorControl.ShowByteAddress(address, ShowAddressSettings.ShowMiddle);
				hexEditorControl.SetData(address, oldData, true);
				hexEditorControl.SelectionRange = MemoryRegion.FromStartAddressAndSize(address, oldData.Data.Length);
			}
		}
	}
}