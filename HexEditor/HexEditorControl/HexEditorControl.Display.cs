// <copyright file="HexEditorControl.Display.cs" company="CheckSum, LLC">
// Copyright (c) 2025 CheckSum, LLC. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>6/30/2025</date>
// <summary>Implements the hexadecimal editor control. display class</summary>

using Avalonia;
using System;

namespace Dataescher.Controls {
	public partial class HexEditorControl {
		/// <summary>Values that represent various fields on the component.</summary>
		private enum HexEditorControlFields {
			/// <summary>No field selected.</summary>
			None,
			/// <summary>Cursor address field selected.</summary>
			CursorAddressField,
			/// <summary>Address header field selected.</summary>
			AddressHeaderField,
			/// <summary>Data index header field selected.</summary>
			DataIndexHeaderField,
			/// <summary>Data field selected.</summary>
			DataField,
			/// <summary>Data character field selected.</summary>
			DataCharacterField,
			/// <summary>Unused field selected.</summary>
			UnusedField
		};

		/// <summary>Check a point to see if it is in the cursor address field.</summary>
		/// <param name="pt">The point.</param>
		/// <returns>True if the test passes, false otherwise.</returns>
		private Boolean PointInCursorAddressField(Point pt) {
			return (pt.X < Layout.lDataDivX) && (pt.Y < Layout.tDataDivY);
		}

		/// <summary>Check a point to see if it is in the address header field.</summary>
		/// <param name="pt">The point.</param>
		/// <returns>True if the test passes, false otherwise.</returns>
		private Boolean PointInAddressHeaderField(Point pt) {
			return (pt.X < Layout.lDataDivX) && (pt.Y > Layout.tDataDivY);
		}

		/// <summary>Check a point to see if it is in the data index header field.</summary>
		/// <param name="pt">The point.</param>
		/// <returns>True if the test passes, false otherwise.</returns>
		private Boolean PointInDataIndexHeaderField(Point pt) {
			return (pt.X > Layout.lDataDivX) && (pt.X < Layout.rDataDivX) && (pt.Y < Layout.tDataDivY);
		}

		/// <summary>Check a point to see if it is in the data field.</summary>
		/// <param name="pt">The point.</param>
		/// <returns>True if the test passes, false otherwise.</returns>
		private Boolean PointInDataField(Point pt) {
			return (pt.X > Layout.lDataDivX) && (pt.X < Layout.rDataDivX) && (pt.Y > Layout.tDataDivY);
		}

		/// <summary>Check a point to see if it is in the data character field.</summary>
		/// <param name="pt">The point.</param>
		/// <returns>True if the test passes, false otherwise.</returns>
		private Boolean PointInDataCharacterField(Point pt) {
			return Layout.showDataChars && (pt.X > Layout.lDataDivX) && (pt.X > Layout.rDataDivX) && (pt.Y > Layout.tDataDivY);
		}

		/// <summary>Check a point to see if it is in the unused field in the upper right corner.</summary>
		/// <param name="pt">The point.</param>
		/// <returns>True if the test passes, false otherwise.</returns>
		private Boolean PointInUnusedField(Point pt) {
			return (pt.X > Layout.lDataDivX) && (pt.X > Layout.rDataDivX) && (pt.Y < Layout.tDataDivY);
		}
		/// <summary>Get an enumeration indicating the region of a point on the control.</summary>
		/// <param name="pt">The point.</param>
		/// <returns>The HexEditorControlFields.</returns>
		private HexEditorControlFields PointToField(Point pt) {
			HexEditorControlFields retval;
			// Figure out what was clicked on
			if (PointInDataField(pt)) {
				// Selected the data field.
				retval = HexEditorControlFields.DataField;
			} else if (PointInDataCharacterField(pt)) {
				// Selected the data character field.
				retval = HexEditorControlFields.DataCharacterField;
				editPosition = 0;
			} else if (PointInCursorAddressField(pt)) {
				// Selected the cursor address field. Figure what address was selected.
				retval = HexEditorControlFields.CursorAddressField;
			} else if (PointInAddressHeaderField(pt)) {
				// Selected the address header field.
				retval = HexEditorControlFields.AddressHeaderField;
			} else if (PointInDataIndexHeaderField(pt)) {
				// Selected the data index header field.
				retval = HexEditorControlFields.DataIndexHeaderField;
			} else if (PointInUnusedField(pt)) {
				retval = HexEditorControlFields.UnusedField;
			} else {
				// Some other field or directly on a line
				retval = HexEditorControlFields.None;
			}
			return retval;
		}

		/// <summary>The field in which the last mouse down event occurred.</summary>
		private HexEditorControlFields mouseDownField;

		/// <summary>The data position of the last mouse down event.</summary>
		private Point mouseDownDataPosition;

		/// <summary>The data position of the last mouse move event.</summary>
		private Point mouseMoveDataPosition;

		/// <summary>The address from the last mouse move event.</summary>
		private UInt32 mouseDownByteAddress;

		/// <summary>True if we're in selection mode.</summary>
		private Boolean mouseIsDown;

		/// <summary>The mouse down position.</summary>
		private Point mouseDownPosition;

		/// <summary>True if the user is currently dragging on the control with mouse down.</summary>
		private Boolean mouseDragging;

		/// <summary>Adjust the current address so that a position is on the screen.</summary>
		/// <param name="byteAddress">Zero-based index of the data byte.</param>
		/// <param name="showAddressSettings">(Optional) The show address settings.</param>
		internal void ScrollToByteAddressVertical(UInt32 byteAddress, ShowAddressSettings showAddressSettings = ShowAddressSettings.Auto) {
			if (ByteAddressIsShown(byteAddress)) {
				return;
			}
			if (showAddressSettings == ShowAddressSettings.Auto) {
				showAddressSettings = byteAddress < currentByteAddress ? ShowAddressSettings.ShowTop : ShowAddressSettings.ShowBottom;
			}
			Int64 address = byteAddress - (byteAddress % Layout.bytesPerRow);
			switch (showAddressSettings) {
				case ShowAddressSettings.ShowTop:
					break;
				case ShowAddressSettings.ShowMiddle:
					address -= (Int64)(Layout.bytesPerRow * (UInt64)Layout.rowCount / 2);
					break;
				case ShowAddressSettings.ShowBottom:
					address -= (Int64)(Layout.bytesPerRow * ((UInt64)Layout.rowCount - 1));
					break;
			}

			CurrentByteAddress = (UInt32)Math.Max(0, address);
		}

		/// <summary>Scroll to data address (vertically, based on mouse down field).</summary>
		/// <param name="address">The address which to show.</param>
		private void ScrollToByteAddressHorizontal(UInt32 address) {
			UInt32 addrOffset = address - currentByteAddress;
			UInt32 addrOffsetRow = addrOffset / Layout.bytesPerRow;
			UInt32 addrOffsetCol = addrOffset % Layout.bytesPerRow;
			if (mouseDownField == HexEditorControlFields.DataCharacterField) {
				Rect leftCol = Layout.dataCharCursorRects[Math.Max(0, (Int32)addrOffsetCol - 1), addrOffsetRow];
				Rect rightCol = Layout.dataCharCursorRects[Math.Min(Layout.bytesPerRow - 1, addrOffsetCol + 1), addrOffsetRow];
				if (leftCol.Left < 0) {
					Layout._hOffset += leftCol.Left;
					ComputeLayoutParameters();
				} else if (rightCol.Right > Width) {
					Layout._hOffset += rightCol.Right - Width;
					ComputeLayoutParameters();
				}
			} else {
				Rect leftCol = Layout.dataCursorRects[Math.Max(0, (Int32)addrOffsetCol - 1), addrOffsetRow];
				Rect rightCol = Layout.dataCursorRects[Math.Min(Layout.bytesPerRow - 1, addrOffsetCol + 1), addrOffsetRow];
				if (leftCol.Left < 0) {
					Layout._hOffset += leftCol.Left;
					ComputeLayoutParameters();
				} else if (rightCol.Right > Width) {
					Layout._hOffset += rightCol.Right - Width;
					ComputeLayoutParameters();
				}
			}
		}

		/// <summary>Scrolls the control so a specified byte address location is shown on the control.</summary>
		/// <param name="address">The address which to show.</param>
		/// <param name="showAddressSettings">(Optional) The show address settings.</param>
		internal void ShowByteAddress(UInt32 address, ShowAddressSettings showAddressSettings = ShowAddressSettings.Auto) {
			ScrollToByteAddressVertical(address, showAddressSettings);
			ScrollToByteAddressHorizontal(address);
		}

		/// <summary>Scrolls the control so a specified address location is shown on the control.</summary>
		/// <param name="address">The address which to show.</param>
		/// <param name="showAddressSettings">(Optional) The show address settings.</param>
		public void ShowAddress(UInt32 address, ShowAddressSettings showAddressSettings = ShowAddressSettings.Auto) {
			ShowByteAddress(address / Layout.bytesPerAddress, showAddressSettings);
		}
	}
}
