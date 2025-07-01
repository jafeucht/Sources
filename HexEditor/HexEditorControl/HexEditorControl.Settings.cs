// <copyright file="HexEditorControl.Settings.cs" company="CheckSum, LLC">
// Copyright (c) 2025 CheckSum, LLC. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>6/30/2025</date>
// <summary>Implements the hexadecimal editor control. settings class</summary>

using Avalonia.Media;
using Dataescher.Data;
using System;
using System.ComponentModel;

namespace Dataescher.Controls {
	public partial class HexEditorControl {
		/// <summary>Values that represent locations to position data during ShowAddress routine.</summary>
		public enum ShowAddressSettings {
			/// <summary>Position data at top or bottom depending on current address.</summary>
			Auto,
			/// <summary>Position data at top.</summary>
			ShowTop,
			/// <summary>Position data in middle.</summary>
			ShowMiddle,
			/// <summary>Position data at bottom.</summary>
			ShowBottom
		}

		/// <summary>Values that represent row length settings.</summary>
		public enum RowLengthSettings : Int32 {
			/// <summary>An enum constant representing the row length 8 bytes option.</summary>
			RowLength8Bytes = 2,
			/// <summary>An enum constant representing the row length 16 bytes option.</summary>
			RowLength16Bytes,
			/// <summary>An enum constant representing the row length 32 bytes option.</summary>
			RowLength32Bytes,
			/// <summary>An enum constant representing the row length 64 bytes option.</summary>
			RowLength64Bytes
		}
		/// <summary>Values that represent data size settings.</summary>
		public enum DataSizeSettings : Int32 {
			/// <summary>An enum constant representing the data size byte option.</summary>
			DataSizeByte,
			/// <summary>An enum constant representing the data size half-word option.</summary>
			DataSizeHWord,
			/// <summary>An enum constant representing the data size word option.</summary>
			DataSizeWord,
			/// <summary>An enum constant representing the data size double-word option.</summary>
			DataSizeDWord
		}
		/// <summary>The length of the address field.</summary>
		public enum AddressSizeSettings : Int32 {
			/// <summary>An enum constant representing the address size byte option.</summary>
			AddressSizeByte,
			/// <summary>An enum constant representing the address size half-word option.</summary>
			AddressSizeHWord,
			/// <summary>An enum constant representing the address size word option.</summary>
			AddressSizeWord
		}
		/// <summary>Values that represent alignment settings.</summary>
		public enum DataAlignmentSettings : Int32 {
			/// <summary>An enum constant representing the data alignment byte option.</summary>
			DataAlignmentByte,
			/// <summary>An enum constant representing the data alignment half-word option.</summary>
			DataAlignmentHWord,
			/// <summary>An enum constant representing the data alignment word option.</summary>
			DataAlignmentWord,
			/// <summary>An enum constant representing the data alignment double-word option.</summary>
			DataAlignmentDWord
		}

		/// <summary>Invoked when row length setting is changed.</summary>
		[Browsable(true)]
		[Category("Action")]
		[Description("Invoked when row length setting is changed")]
		public event EventHandler RowLengthSettingChanged;

		/// <summary>Invoked when the data size setting is changed.</summary>
		[Browsable(true)]
		[Category("Action")]
		[Description("Invoked when the data size setting is changed")]
		public event EventHandler DataSizeSettingChanged;

		/// <summary>Invoked when the address size setting is changed.</summary>
		[Browsable(true)]
		[Category("Action")]
		[Description("Invoked when the address size setting is changed")]
		public event EventHandler AddressSizeSettingChanged;

		/// <summary>Invoked when the data alignment setting is changed.</summary>
		[Browsable(true)]
		[Category("Action")]
		[Description("Invoked when the data alignment setting is changed")]
		public event EventHandler DataAlignmentSettingChanged;

		/// <summary>Invoked when selected range is changed.</summary>
		[Browsable(true)]
		[Category("Action")]
		[Description("Invoked when selected range is changed")]
		public event EventHandler SelectionRangeChanged;

		/// <summary>Invoked when the cursor address changes.</summary>
		[Browsable(true)]
		[Category("Action")]
		[Description("Invoked when the cursor address changes")]
		public event EventHandler CursorAddressChanged;

		/// <summary>Invoked when the current address is changed.</summary>
		[Browsable(true)]
		[Category("Action")]
		[Description("Invoked when the current address is changed")]
		public event EventHandler CurrentAddressChanged;

		/// <summary>Invoked when any edit is made to the memory.</summary>
		[Browsable(true)]
		[Category("Action")]
		[Description("Invoked when any edit is made to the memory")]
		public event EventHandler Modified;

		/// <summary>Invoked when the modified status for the document changes.</summary>
		[Browsable(true)]
		[Category("Action")]
		[Description("Invoked when the modified status for the document changes")]
		public event EventHandler ModifiedChanged;

		/// <summary>Invoked when errors are encountered during file open.</summary>
		[Browsable(true)]
		[Category("Action")]
		[Description("Invoked when errors are encountered during file open")]
		public event EventHandler OpenFileErrors;

		/// <summary>Invoked when a file is opened.</summary>
		[Browsable(true)]
		[Category("Action")]
		[Description("Invoked when a file is opened")]
		public event EventHandler OpenedFile;

		/// <summary>Invoked when errors are encountered during file save.</summary>
		[Browsable(true)]
		[Category("Action")]
		[Description("Invoked when errors are encountered during file save")]
		public event EventHandler SaveFileErrors;

		/// <summary>Invoked when a file is saved.</summary>
		[Browsable(true)]
		[Category("Action")]
		[Description("Invoked when a file is saved")]
		public event EventHandler SavedFile;

		/// <summary>Invoked when edit mode is exited.</summary>
		[Browsable(true)]
		[Category("Action")]
		[Description("Invoked when edit mode is exited")]
		public event EventHandler EndEdit;

		/// <summary>Invoked when edit mode is entered.</summary>
		[Browsable(true)]
		[Category("Action")]
		[Description("Invoked when edit mode is entered")]
		public event EventHandler BeginEdit;

		/// <summary>Invoked when the edit address changes.</summary>
		[Browsable(true)]
		[Category("Action")]
		[Description("Invoked when the edit address changes")]
		public event EventHandler EditAddressChanged;

		/// <summary>Gets the maximum address depending on the address size setting.</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public UInt32 MaxAddress => MaxByteAddress / Layout.bytesPerAddress;

		///// <summary>Gets the number of rows.</summary>
		//[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		//public Int32 RowCount { get; private set; }

		/// <summary>Gets the region for the entire file.</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public MemoryRegion FileRegion {
			get {
				UInt32 maxAddress = 0;
				UInt32 minAddress = UInt32.MaxValue;
				MemoryRegionCollection memoryRegions = MemoryRegions;
				foreach (MemoryRegion thisMemoryRegion in memoryRegions) {
					maxAddress = Math.Max(maxAddress, thisMemoryRegion.EndAddress);
					minAddress = Math.Min(minAddress, thisMemoryRegion.StartAddress);
				}
				if (minAddress == UInt32.MaxValue) {
					minAddress = 0;
				}
				return MemoryRegion.FromStartAndEndAddresses(minAddress, maxAddress);
			}
		}

		/// <summary>Gets the number of bytes currently on the display.</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public UInt32 BytesOnDisplay => Layout.rowCount * Layout.bytesPerRow;

		/// <summary>Gets the last address displayed on the screen.</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public UInt32 LastDisplayedByteAddress => Math.Min(MaxByteAddress, CurrentByteAddress + BytesOnDisplay - 1);

		/// <summary>Gets or sets the data size setting.</summary>
		[Bindable(true)]
		[Browsable(true)]
		[Category("Behavior")]
		[Description("The number of bytes in one data column.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		public Boolean UndoHistoryEnabled {
			get => undoHistoryEnabled;
			set {
				undoHistoryEnabled = value;
				if (!undoHistoryEnabled) {
					UndoHistory.Clear();
					GC.Collect();
				}
			}
		}

		/// <summary>Gets the background color.</summary>
		[Bindable(true)]
		[Browsable(true)]
		[Category("Appearance")]
		[Description("The background color of the control.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		public Color BackgroundColor {
			get => backgroundColor;
			set {
				if (backgroundColor != value) {
					backgroundColor = value;
					InvalidateVisual();
				}
			}
		}

		/// <summary>Gets the color of the line.</summary>
		[Bindable(true)]
		[Browsable(true)]
		[Category("Appearance")]
		[Description("The line colors on this control.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		public Color LineColor {
			get => lineColor;
			set {
				if (lineColor != value) {
					lineColor = value;
					InvalidateVisual();
				}
			}
		}

		/// <summary>Gets the color of the normal text.</summary>
		[Bindable(true)]
		[Browsable(true)]
		[Category("Appearance")]
		[Description("The color of normal text on this control.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		public Color NormalTextColor {
			get => normalTextColor;
			set {
				if (normalTextColor != value) {
					normalTextColor = value;
					InvalidateVisual();
				}
			}
		}

		/// <summary>Gets the color of the odd column highlighting.</summary>
		[Bindable(true)]
		[Browsable(true)]
		[Category("Appearance")]
		[Description("The shade color for the odd columns.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		public Color OddColumnColor {
			get => oddColumnColor;
			set {
				if (oddColumnColor != value) {
					oddColumnColor = value;
					InvalidateVisual();
				}
			}
		}

		/// <summary>Gets or sets the cursor byte address.</summary>
		public UInt32 CursorByteAddress {
			get => cursorByteAddress;
			set {
				if (cursorByteAddress != value) {
					cursorByteAddress = value - (UInt32)(value % DataSizeBytes);
					ShowByteAddress(cursorByteAddress);
					CursorAddressChanged?.Invoke(this, new EventArgs());
					InvalidateVisual();
				}
			}
		}

		/// <summary>Gets or sets the cursor address.</summary>
		[Bindable(true)]
		[Browsable(true)]
		[Category("Appearance")]
		[Description("The location of the cursor.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		public UInt32 CursorAddress {
			get => CursorByteAddress / Layout.bytesPerAddress;
			set => CursorByteAddress = value * Layout.bytesPerAddress;
		}

		/// <summary>Gets or sets the current byte address.</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public UInt32 CurrentByteAddress {
			get => currentByteAddress;
			set {
				Int64 maxByteAddress = MaxCurrentByteAddress;
				if (value < 0) {
					value = 0;
				} else if (value > maxByteAddress) {
					value = (UInt32)maxByteAddress;
				}
				if (currentByteAddress != value) {
					currentByteAddress = value;
					CurrentAddressChanged?.Invoke(this, new EventArgs());
					InvalidateVisual();
				}
			}
		}

		/// <summary>Gets or sets the current address.</summary>
		[Bindable(true)]
		[Browsable(true)]
		[Category("Appearance")]
		[Description("The address at the top left corner.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		public UInt32 CurrentAddress {
			get => CurrentByteAddress / Layout.bytesPerAddress;
			set => CurrentByteAddress = value * Layout.bytesPerAddress;
		}

		/// <summary>Gets or sets the grid font.</summary>
		[Bindable(true)]
		[Browsable(true)]
		[Category("Appearance")]
		[Description("The font of the text in the control, preferably a fixed-width font.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		public Typeface GridFont {
			get => font;
			set {
				font = value;
				ComputeLayoutParameters();
			}
		}

		/// <summary>Gets or sets the grid font size.</summary>
		[Bindable(true)]
		[Browsable(true)]
		[Category("Appearance")]
		[Description("The size of the font.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		public Double GridFontSize {
			get => fontSize;
			set {
				fontSize = value;
				ComputeLayoutParameters();
			}
		}

		/// <summary>Gets or sets the row length setting.</summary>
		[Bindable(true)]
		[Browsable(true)]
		[Category("Appearance")]
		[Description("The number of data bytes per row.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		public RowLengthSettings RowLengthSetting {
			get => rowLengthSetting;
			set {
				UInt32 rowLengthSettingVal = (UInt32)value;
				UInt32 dataSizeSettingVal = (UInt32)dataSizeSetting;
				if ((rowLengthSettingVal + 1) < dataSizeSettingVal) {
					rowLengthSettingVal = dataSizeSettingVal - 1;
				}
				RowLengthSettings newRowLengthSetting = (RowLengthSettings)rowLengthSettingVal;
				if (rowLengthSetting != newRowLengthSetting) {
					if (undoHistoryEnabled) {
						UndoHistory.RegisterAction(new PropertyUndoAction(this, rowLengthSetting, newRowLengthSetting));
					}
					rowLengthSetting = newRowLengthSetting;
					ComputeLayoutParameters();
					ShowByteAddress(currentByteAddress);
					RowLengthSettingChanged?.Invoke(this, new EventArgs());
				}
			}
		}

		/// <summary>Gets or sets the data size setting.</summary>
		[Bindable(true)]
		[Browsable(true)]
		[Category("Appearance")]
		[Description("The number of bytes in one data column.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		public DataSizeSettings DataSizeSetting {
			get => dataSizeSetting;
			set {
				if (DataSizeSetting != value) {
					Boolean rowLengthSettingChanged = false;
					Boolean dataAlignmentSettingChanged = false;
					if (undoHistoryEnabled) {
						UndoHistory.RegisterAction(new PropertyUndoAction(this, dataSizeSetting, value));
					}
					dataSizeSetting = value;
					UInt32 rowLengthSettingVal = (UInt32)rowLengthSetting;
					UInt32 dataSizeSettingVal = (UInt32)dataSizeSetting;
					UInt32 dataAlignmentSettingVal = (UInt32)dataAlignmentSetting;
					if ((rowLengthSettingVal + 1) < dataSizeSettingVal) {
						RowLengthSettings newRowLengthSetting = (RowLengthSettings)(dataSizeSettingVal - 1);
						if (undoHistoryEnabled) {
							UndoHistory.RegisterAction(new PropertyUndoAction(this, rowLengthSetting, newRowLengthSetting));
						}
						rowLengthSetting = newRowLengthSetting;
						rowLengthSettingChanged = true;
					}
					if (dataSizeSettingVal < dataAlignmentSettingVal) {
						DataAlignmentSettings newDataAlignmentSetting = (DataAlignmentSettings)dataSizeSettingVal;
						if (undoHistoryEnabled) {
							UndoHistory.RegisterAction(new PropertyUndoAction(this, dataAlignmentSetting, newDataAlignmentSetting));
						}
						dataAlignmentSetting = newDataAlignmentSetting;
						dataAlignmentSettingChanged = true;
					}
					ComputeLayoutParameters();
					DataSizeSettingChanged?.Invoke(this, new EventArgs());
					if (rowLengthSettingChanged) {
						RowLengthSettingChanged?.Invoke(this, new EventArgs());
					}
					if (dataAlignmentSettingChanged) {
						DataAlignmentSettingChanged?.Invoke(this, new EventArgs());
					}
				}
			}
		}

		/// <summary>Gets or sets the address size setting.</summary>
		[Bindable(true)]
		[Browsable(true)]
		[Category("Appearance")]
		[Description("The number of bytes used for the address.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		public AddressSizeSettings AddressSizeSetting {
			get => addressSizeSetting;
			set {
				if (addressSizeSetting != value) {
					// Check if this operation will delete data from the memory map
					UInt32 maxAddress = Memory.EndAddress;
					switch (value) {
						case AddressSizeSettings.AddressSizeByte:
							if (maxAddress >= 0x100) {
								Crop(MemoryRegion.FromStartAndEndAddresses(0x00, 0xFF), false);
								LoadDataBuffer(true);
							}
							break;
						case AddressSizeSettings.AddressSizeHWord:
							if (maxAddress > 0x10000) {
								Crop(MemoryRegion.FromStartAndEndAddresses(0x0000, 0xFFFF), false);
								LoadDataBuffer(true);
							}
							break;
					}
					if (undoHistoryEnabled) {
						UndoHistory.RegisterAction(new PropertyUndoAction(this, addressSizeSetting, value));
					}
					addressSizeSetting = value;
					ComputeLayoutParameters();
					if (currentByteAddress > MaxCurrentByteAddress) {
						CurrentAddress = (UInt32)MaxCurrentByteAddress;
					}
					if (cursorByteAddress > MaxCurrentByteAddress) {
						CursorByteAddress = (UInt32)MaxCurrentByteAddress;
					}
					AddressSizeSettingChanged?.Invoke(this, new EventArgs());
				}
			}
		}

		/// <summary>Gets or sets the data alignment setting.</summary>
		[Bindable(true)]
		[Browsable(true)]
		[Category("Appearance")]
		[Description("The number of data bytes for each address location.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		public DataAlignmentSettings DataAlignmentSetting {
			get => dataAlignmentSetting;
			set {
				if (dataAlignmentSetting != value) {
					Boolean dataSizeSettingChanged = false;
					if (undoHistoryEnabled) {
						UndoHistory.RegisterAction(new PropertyUndoAction(this, dataAlignmentSetting, value));
					}
					dataAlignmentSetting = value;
					if ((Int32)dataSizeSetting < (Int32)dataAlignmentSetting) {
						DataSizeSettings newDataSizeSetting = (DataSizeSettings)(Int32)dataAlignmentSetting;
						if (undoHistoryEnabled) {
							UndoHistory.RegisterAction(new PropertyUndoAction(this, dataSizeSetting, newDataSizeSetting));
						}
						dataSizeSetting = newDataSizeSetting;
						dataSizeSettingChanged = true;
					}
					EditMode = false;
					ComputeLayoutParameters();
					DataAlignmentSettingChanged?.Invoke(this, new EventArgs());
					if (dataSizeSettingChanged) {
						DataSizeSettingChanged?.Invoke(this, new EventArgs());
					}
				}
			}
		}

		/// <summary>Gets or sets a value indicating whether the data is read-only.</summary>
		[Bindable(true)]
		[Browsable(true)]
		[Category("Behavior")]
		[Description("Indicates whether the user can edit bytes on the control.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		public Boolean ReadOnly {
			get => readOnly;
			set {
				readOnly = value;
				if (readOnly) {
					if (EditMode) {
						// Exit edit mode
						EditMode = false;
						InvalidateVisual();
					}
				}
			}
		}

		/// <summary>The default state for unimplemented bytes.</summary>
		[Bindable(true)]
		[Browsable(true)]
		[Category("Appearance")]
		[Description("The default display value for unimplemented bytes.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		public Byte BlankData {
			get => Memory.BlankData;
			set {
				Memory.BlankData = value;
				InvalidateVisual();
			}
		}

		/// <summary>Cursor background color (active).</summary>
		[Bindable(true)]
		[Browsable(true)]
		[Category("Appearance")]
		[Description("Cursor background color (active).")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		public Color CursorColorActive {
			get => cursorColorActive;
			set {
				cursorColorActive = value;
				InvalidateVisual();
			}
		}

		/// <summary>Cursor background color (passive).</summary>
		[Bindable(true)]
		[Browsable(true)]
		[Category("Appearance")]
		[Description("Cursor background color (passive).")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		public Color CursorColorPassive {
			get => cursorColorPassive;
			set {
				cursorColorPassive = value;
				InvalidateVisual();
			}
		}

		/// <summary>Selection background color.</summary>
		[Bindable(true)]
		[Browsable(true)]
		[Category("Appearance")]
		[Description("Selection background color (active).")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		public Color SelectionBackgroundColorActive {
			get => selectionBackgroundColorActive;
			set {
				selectionBackgroundColorActive = value;
				InvalidateVisual();
			}
		}

		/// <summary>Selection background color (passive).</summary>
		[Bindable(true)]
		[Browsable(true)]
		[Category("Appearance")]
		[Description("Selection background color (passive).")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		public Color SelectionBackgroundColorPassive {
			get => selectionBackgroundColorPassive;
			set {
				selectionBackgroundColorPassive = value;
				InvalidateVisual();
			}
		}

		/// <summary>Edit region background color.</summary>
		[Bindable(true)]
		[Browsable(true)]
		[Category("Appearance")]
		[Description("Edit region background color.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		public Color EditRegionColor {
			get => editRegionBackgroundColor;
			set {
				editRegionBackgroundColor = value;
				InvalidateVisual();
			}
		}

		/// <summary>Edit region background color.</summary>
		[Bindable(true)]
		[Browsable(true)]
		[Category("Appearance")]
		[Description("Unimplemented byte text color.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		[EditorBrowsable(EditorBrowsableState.Always)]
		public Color UnimplementedByteTextColor {
			get => unimplementedByteTextColor;
			set {
				unimplementedByteTextColor = value;
				InvalidateVisual();
			}
		}
	}
}