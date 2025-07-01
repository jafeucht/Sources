// <copyright file="HexEditorControl.Globals.cs" company="CheckSum, LLC">
// Copyright (c) 2025 CheckSum, LLC. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>6/30/2025</date>
// <summary>Implements the hexadecimal editor control. globals class</summary>

using Avalonia.Media;
using Dataescher.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Dataescher.Controls {
	public partial class HexEditorControl {
		/// <summary>(Immutable) True if the control is in design mode, false otherwise.</summary>
		private readonly Boolean inDesignMode;

		/// <summary>The memory.</summary>
		public MemoryMap Memory { get; private set; }

		/// <summary>Get a memory map of implemented memory regions.</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public MemoryRegionCollection MemoryRegions {
			get {
				MemoryRegionCollection retval = new();
				if (Memory is not null) {
					retval = Memory.Regions;
				}
				return retval;
			}
		}

		/// <summary>Get the memory size.</summary>
		public Int64 MemorySize => Memory.Size;

		/// <summary>Gets the error messages.</summary>
		public List<String> Errors { get; private set; }

		/// <summary>Gets the warnings.</summary>
		public List<String> Warnings { get; private set; }

		/// <summary>The data buffer.</summary>
		private DataBuffer buffer;

		/// <summary>(Immutable) The undo history.</summary>
		public readonly HecUndoHistory UndoHistory;

		/// <summary>The address from which data has been loaded.</summary>
		private UInt32 dataAddress;

		/// <summary>True to enable, false to disable the undo history.</summary>
		private Boolean undoHistoryEnabled = true;

		/// <summary>Data currently displayed on the screen.</summary>
		private DataBuffer screenData;

		/// <summary>The screen memory region.</summary>
		private MemoryRegion screenRegion;

		/// <summary>The scroll timer.</summary>
		private Timer TmScrollTimer;

		/// <summary>The grid font.</summary>
		private Typeface font = new(new FontFamily("Consolas"), FontStyle.Normal, FontWeight.Normal);

		/// <summary>The grid font size.</summary>
		private Double fontSize = 13;

		/// <summary>The row length setting.</summary>
		private RowLengthSettings rowLengthSetting = RowLengthSettings.RowLength16Bytes;

		/// <summary>The data size setting.</summary>
		private DataSizeSettings dataSizeSetting = DataSizeSettings.DataSizeByte;

		/// <summary>Gets the data size in bytes.</summary>
		private Int32 DataSizeBytes => DataSizeSetting switch {
			DataSizeSettings.DataSizeByte => 1,
			DataSizeSettings.DataSizeHWord => 2,
			DataSizeSettings.DataSizeWord => 4,
			DataSizeSettings.DataSizeDWord => 8,
			_ => 0,
		};

		/// <summary>The address size setting.</summary>
		private AddressSizeSettings addressSizeSetting = AddressSizeSettings.AddressSizeWord;

		/// <summary>The data alignment.</summary>
		private DataAlignmentSettings dataAlignmentSetting = DataAlignmentSettings.DataAlignmentByte;

		/// <summary>The cursor address.</summary>
		private UInt32 cursorByteAddress = 0;

		/// <summary>The byte address of the first row (disregarding data alignment).</summary>
		private UInt32 currentByteAddress = 0;

		/// <summary>If true, data is read only.</summary>
		private Boolean readOnly = false;

		/// <summary>The background color.</summary>
		private Color backgroundColor = Colors.White;

		/// <summary>The background color.</summary>
		private Color oddColumnColor = Colors.LightCyan;

		/// <summary>Gets the color of the line.</summary>
		private Color lineColor = Colors.Green;

		/// <summary>The color of the normal text.</summary>
		private Color normalTextColor = Colors.DarkSlateGray;

		/// <summary>Cursor background color (active).</summary>
		private Color cursorColorActive = Colors.Tomato;

		/// <summary>Cursor background color (passive).</summary>
		private Color cursorColorPassive = Colors.DarkGray;

		/// <summary>Selection background color (active).</summary>
		private Color selectionBackgroundColorActive = Colors.LightBlue;

		/// <summary>Selection background color (passive).</summary>
		private Color selectionBackgroundColorPassive = Colors.LightCyan;

		/// <summary>Edit region background color.</summary>
		private Color editRegionBackgroundColor = Colors.LightGreen;

		/// <summary>Unimplemented byte text color.</summary>
		private Color unimplementedByteTextColor = Colors.LightGray;

		/// <summary>Indicates whether the scroll timer moves up or down.</summary>
		private Boolean moveUp;

		/// <summary>True to enable edit mode, false to disable it.</summary>
		private Boolean editMode;

		/// <summary>Gets a value indicating whether the control is in edit mode.</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Boolean EditMode {
			get => editMode;
			internal set {
				if (editMode != value) {
					editMode = value;
					if (!editMode) {
						if (editPosition > 0) {
							CursorByteAddress = (UInt32)Math.Min(MaxAddress, (Int64)editByteAddress + Layout.bytesPerCell);
						} else {
							cursorByteAddress = editByteAddress;
						}
						EndEdit?.Invoke(this, new EventArgs());
					} else {
						BeginEdit?.Invoke(this, new EventArgs());
					}
					InvalidateVisual();
				}
			}
		}

		/// <summary>A value indicating whether the control is in edit mode.</summary>
		private Boolean isModified;

		/// <summary>Gets or sets a value indicating whether the control is in edit mode.</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Boolean IsModified {
			get => isModified;
			set {
				// Edited event
				Boolean modifiedChanged = value != isModified;
				isModified = value;
				if (isModified) {
					Modified?.Invoke(this, new EventArgs());
				}
				if (modifiedChanged) {
					ModifiedChanged?.Invoke(this, new EventArgs());
				}
			}
		}

		/// <summary>The edit position, which points to a particular nibble at the edit address.</summary>
		private Int32 editPosition;

		/// <summary>The edit position, which points to a particular nibble at the edit address.</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Int32 EditPosition {
			get => editPosition;
			private set {
				if (value >= (Layout.bytesPerCell * 2)) {
					if (EditByteAddress == MaxByteAddress) {
						value = ((Int32)Layout.bytesPerCell * 2) - 1;
					} else if (EditByteAddress < MaxByteAddress) {
						EditByteAddress += Math.Min(MaxByteAddress - EditByteAddress, Layout.bytesPerCell);
						value = 0;
					}
				} else if (value < 0) {
					if (EditByteAddress > 0) {
						value = 0;
					} else if (EditByteAddress > 0) {
						value = 0;
						EditByteAddress -= Math.Min(EditByteAddress, Layout.bytesPerCell);
					}
				}
				if (editPosition != value) {
					editPosition = value;
					InvalidateVisual();
				}
			}
		}

		/// <summary>The edit address.</summary>
		private UInt32 editByteAddress = 0;

		/// <summary>Gets or sets the edit byte address.</summary>
		private UInt32 EditByteAddress {
			get => editByteAddress;
			set {
				if (value != editByteAddress) {
					editByteAddress = value;
					ShowByteAddress(editByteAddress);
					EditAddressChanged?.Invoke(this, new EventArgs());
				}
			}
		}

		/// <summary>The edit address.</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public UInt32 EditAddress {
			get => editByteAddress / Layout.bytesPerAddress;
			set => EditByteAddress = value * Layout.bytesPerAddress;
		}

		/// <summary>Gets the maximum byte address.</summary>
		private UInt32 MaxByteAddress => addressSizeSetting switch {
			AddressSizeSettings.AddressSizeByte => 0xFF,
			AddressSizeSettings.AddressSizeHWord => 0xFFFF,
			AddressSizeSettings.AddressSizeWord => 0xFFFFFFFF,
			_ => 0,
		};

		/// <summary>Gets the maximum value for the current address.</summary>
		private Int64 MaxCurrentByteAddress => Math.Max(0, MaxByteAddress - (Layout.bytesPerRow * Layout.rowCount) + 1);

		/// <summary>The selected data range.</summary>
		private MemoryRegion selectionByteRegion;

		/// <summary>Gets or sets a value indicating whether the selection is locked.</summary>
		[Browsable(false)]
		public Boolean LockSelection { get; set; }

		/// <summary>Get or set the selected data range.</summary>
		internal MemoryRegion SelectionByteRegion {
			get => selectionByteRegion;
			set {
				if (LockSelection) {
					return;
				}
				// Adjust selection range for bytes per cell.
				if (value is not null) {
					value = MemoryRegion.FromStartAndEndAddresses(value.StartAddress - (value.StartAddress % Layout.bytesPerCell), value.EndAddress + Layout.bytesPerCell - 1 - (value.EndAddress % Layout.bytesPerCell));
				}
				Boolean modified = false;
				if ((value is null) && (selectionByteRegion is not null)) {
					modified = true;
				} else if ((value is not null) && (selectionByteRegion is null)) {
					modified = true;
				} else if ((value is not null) && (selectionByteRegion is not null)) {
					if (selectionByteRegion.StartAddress != value.StartAddress) {
						modified = true;
					} else if (selectionByteRegion.EndAddress != value.EndAddress) {
						modified = true;
					}
				}
				if (modified) {
					selectionByteRegion = value;
					SelectionRangeChanged?.Invoke(this, new EventArgs());
					InvalidateVisual();
				}
			}
		}
		/// <summary>The selected data range.</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public MemoryRegion SelectionRange {
			get => selectionByteRegion is null
					? null
					: MemoryRegion.FromStartAndEndAddresses(
						selectionByteRegion.StartAddress / Layout.bytesPerAddress,
						selectionByteRegion.EndAddress / Layout.bytesPerAddress
					);
			set {
				if (LockSelection) {
					return;
				}
				SelectionByteRegion = value is null
					? null
					: MemoryRegion.FromStartAndEndAddresses(
						value.StartAddress * Layout.bytesPerAddress,
						(value.EndAddress * Layout.bytesPerAddress) + (Layout.bytesPerCell - 1)
					);
				;
			}
		}
	}
}
