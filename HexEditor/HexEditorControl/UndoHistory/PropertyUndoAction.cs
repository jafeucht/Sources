// <copyright file="PropertyUndoAction.cs" company="Dataescher">
// 	Copyright (c) 2022 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements an undo action comprising of a modified parameter.</summary>

using System;

namespace Dataescher.Controls {
	public partial class HexEditorControl {
		/// <summary>An undo action comprising of a modified parameter.</summary>
		/// <seealso cref="T:Dataescher.Controls.HexEditorControl.HecUndoAction"/>
		internal class PropertyUndoAction : HecUndoAction {
			/// <summary>(Immutable) The parent HexEditorControl.</summary>
			private readonly HexEditorControl hexEditorControl;
			/// <summary>(Immutable) The old property value.</summary>
			private readonly Object oldProperty;
			/// <summary>(Immutable) The new property value.</summary>
			private readonly Object newProperty;

			/// <summary>Initializes a new instance of the Dataescher.UndoHistory.PropertyUndoAction class.</summary>
			/// <param name="hexEditorControl">The parent HexEditorControl.</param>
			/// <param name="oldProperty">The old property.</param>
			/// <param name="newProperty">The new property.</param>
			public PropertyUndoAction(HexEditorControl hexEditorControl, Object oldProperty, Object newProperty) {
				this.hexEditorControl = hexEditorControl;
				this.oldProperty = oldProperty;
				this.newProperty = newProperty;
			}

			/// <summary>Perform a redo action.</summary>
			public override void Redo() {
				if (newProperty is AddressSizeSettings addressSizeSetting) {
					hexEditorControl.AddressSizeSetting = addressSizeSetting;
				} else if (newProperty is DataSizeSettings dataSizeSetting) {
					hexEditorControl.DataSizeSetting = dataSizeSetting;
				} else if (newProperty is DataAlignmentSettings dataAlignmentSetting) {
					hexEditorControl.DataAlignmentSetting = dataAlignmentSetting;
				} else if (newProperty is RowLengthSettings rowLengthSettings) {
					hexEditorControl.RowLengthSetting = rowLengthSettings;
				}
			}

			/// <summary>Perform an undo action.</summary>
			public override void Undo() {
				if (oldProperty is AddressSizeSettings addressSizeSetting) {
					hexEditorControl.AddressSizeSetting = addressSizeSetting;
				} else if (oldProperty is DataSizeSettings dataSizeSetting) {
					hexEditorControl.DataSizeSetting = dataSizeSetting;
				} else if (oldProperty is DataAlignmentSettings dataAlignmentSetting) {
					hexEditorControl.DataAlignmentSetting = dataAlignmentSetting;
				} else if (oldProperty is RowLengthSettings rowLengthSettings) {
					hexEditorControl.RowLengthSetting = rowLengthSettings;
				}
			}
		}
	}
}