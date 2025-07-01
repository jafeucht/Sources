// <copyright file="HexEditorControl.Memory.cs" company="CheckSum, LLC">
// Copyright (c) 2025 CheckSum, LLC. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>6/30/2025</date>
// <summary>Implements the hexadecimal editor control. memory class</summary>

using Avalonia;
using Avalonia.Media;
using Dataescher.Data;
using System;
using System.Globalization;
using System.IO;
using Encoding = System.Text.Encoding;

namespace Dataescher.Controls {
	public partial class HexEditorControl {

		/// <summary>Gets a region of data.</summary>
		/// <param name="region">The region to read.</param>
		/// <param name="buffer">[in,out] The data buffer.</param>
		public void GetData(MemoryRegion region, out DataBuffer buffer) {
			buffer = new DataBuffer(region, Memory);
		}

		/// <summary>Gets a region of data comprised of one byte.</summary>
		/// <param name="address">The address at which to copy the data.</param>
		/// <param name="buffer">[in,out] The data buffer.</param>
		public void GetData(UInt32 address, out DataBuffer buffer) {
			buffer = new DataBuffer(MemoryRegion.FromStartAddressAndSize(address, 1), Memory);
		}

		/// <summary>Sets a region of data.</summary>
		/// <param name="address">The address at which to copy the data.</param>
		/// <param name="newData">The data buffer.</param>
		/// <param name="suppressUndo">True to suppress creating an undo history entry.</param>
		internal void SetData(UInt32 address, DataBuffer newData, Boolean suppressUndo) {
			if (undoHistoryEnabled) {
				GetData(MemoryRegion.FromStartAddressAndSize(address, newData.Data.Length), out DataBuffer oldData);
				if (!suppressUndo && undoHistoryEnabled) {
					UndoHistory.RegisterAction(new MemoryChangedUndoAction(this, address, oldData, newData));
				}
			}
			Memory.Insert(address, newData.Data, newData.Implemented);
			// Force reload of the buffer
			buffer = null;
			InvalidateVisual();
			Modified?.Invoke(this, new EventArgs());
		}

		/// <summary>Data byte at.</summary>
		/// <param name="address">The address.</param>
		/// <param name="isImplemented">[out] True if is implemented, false if not.</param>
		/// <returns>A Byte.</returns>
		private Byte GetScreenDataAt(UInt32 address, ref Boolean isImplemented) {
			if (inDesignMode) {
				if (buffer is null) {
					GenerateDesignModeData();
				}
				if (address >= buffer.Data.Length) {
					return BlankData;
				} else {
					Boolean byteIsImplemented = buffer.Implemented[address];
					if (byteIsImplemented) {
						isImplemented = true;
						return buffer.Data[address];
					} else {
						return BlankData;
					}
				}
			} else {
				UInt32 lookupIndex = address - screenRegion.StartAddress;
				if (lookupIndex < screenRegion.Size) {
					isImplemented = screenData.IsImplemented(lookupIndex);
					return screenData.Data[lookupIndex];
				} else {
					isImplemented = false;
					return 0xFF;
				}
			}
		}

		/// <summary>Data byte at.</summary>
		/// <param name="address">The address.</param>
		/// <param name="isImplemented">[out] True if is implemented, false if not.</param>
		/// <returns>A Byte.</returns>
		private Byte GetDataAt(UInt32 address, ref Boolean isImplemented) {
			if (inDesignMode) {
				if (buffer is null) {
					GenerateDesignModeData();
				}
				if (address >= buffer.Data.Length) {
					return BlankData;
				} else {
					Boolean byteIsImplemented = buffer.Implemented[address];
					if (byteIsImplemented) {
						isImplemented = true;
						return buffer.Data[address];
					} else {
						return BlankData;
					}
				}
			} else {
				GetData(address, out DataBuffer dataBuffer);
				Byte data;
				if (dataBuffer.Data.Length > 0) {
					data = dataBuffer.Data[0];
					if (dataBuffer.Implemented[0]) {
						isImplemented = true;
					} else {
						data = BlankData;
					}
				} else {
					data = BlankData;
				}
				return data;
			}
		}

		/// <summary>Sets one byte of data at the selected address.</summary>
		/// <param name="address">The address at which to set one byte.</param>
		/// <param name="newValue">The new value.</param>
		/// <param name="suppressUndo">True to suppress creating an undo history entry.</param>
		private void SetData(UInt32 address, Byte newValue, Boolean suppressUndo) {
			DataBuffer newData = new(1);
			newData.Data[0] = newValue;
			newData.Implemented[0] = true;
			SetData(address, newData, suppressUndo);
		}

		/// <summary>Offset all data in the file.</summary>
		/// <param name="offset">The offset.</param>
		/// <param name="moveUp">True to move up.</param>
		/// <param name="suppressUndo">True to suppress creating an undo history entry.</param>
		internal void OffsetAllData(UInt32 offset, Boolean moveUp, Boolean suppressUndo) {
			MemoryRegionCollection intersectRanges;
			Int64 newCursorAddress = cursorByteAddress;
			Int64 newSelectionStartAddress = 0;
			Int64 newSelectionEndAddress = 0;
			if (selectionByteRegion is not null) {
				newSelectionStartAddress = selectionByteRegion.StartAddress;
				newSelectionEndAddress = selectionByteRegion.EndAddress;
			}
			if (moveUp) {
				newCursorAddress -= offset;
				newSelectionStartAddress -= offset;
				newSelectionEndAddress -= offset;
				intersectRanges = Memory.IntersectRegions(MemoryRegion.FromStartAndEndAddresses(0, offset - 1));
			} else {
				newCursorAddress += offset;
				newSelectionStartAddress += offset;
				newSelectionEndAddress += offset;
				intersectRanges = Memory.IntersectRegions(MemoryRegion.FromStartAndEndAddresses(0xFFFFFFFF - offset, 0xFFFFFFFF));
			}
			MultipleUndoAction mud = new();
			// Check if any data will be moved past the end of the available address space. We want to be able
			// to restore data which was deleted by falling off the edge of the address space.
			foreach (MemoryRegion thisMemoryRegion in intersectRanges) {
				GetData(thisMemoryRegion, out DataBuffer oldDataBuffer);
				if (!suppressUndo && undoHistoryEnabled) {
					mud.UndoActions.Add(new DeleteUndoAction(this, thisMemoryRegion.StartAddress, oldDataBuffer));
				}
				Memory.Delete(thisMemoryRegion);
			}
			// Now move all memory regions
			Memory.OffsetAllData(offset, moveUp);
			CursorByteAddress = (UInt32)Math.Max(Math.Min(newCursorAddress, 0xFFFFFFFF), 0);
			if (selectionByteRegion is not null) {
				if ((newSelectionStartAddress < 0) && (newSelectionEndAddress < 0)) {
					SelectionByteRegion = null;
				} else if ((newSelectionStartAddress > 0xFFFFFFFF) && (newSelectionEndAddress > 0xFFFFFFFF)) {
					SelectionByteRegion = null;
				} else {
					newSelectionStartAddress = (UInt32)Math.Max(Math.Min(newSelectionStartAddress, 0xFFFFFFFF), 0);
					newSelectionEndAddress = (UInt32)Math.Max(Math.Min(newSelectionEndAddress, 0xFFFFFFFF), 0);
					SelectionByteRegion = MemoryRegion.FromStartAndEndAddresses((UInt32)newSelectionStartAddress, (UInt32)newSelectionEndAddress);
				}
			}
			if (!suppressUndo && undoHistoryEnabled) {
				UndoHistory.RegisterAction(mud);
			}
			InvalidateVisual();
			Modified?.Invoke(this, new EventArgs());
		}

		/// <summary>Move the selected data up or down with offset.</summary>
		/// <param name="offsetRegion">The region to offset.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="moveUp">True to move up.</param>
		internal void OffsetSelectedData(MemoryRegion offsetRegion, UInt32 offset, Boolean moveUp) {
			Int64 moveToAddress = offsetRegion.StartAddress;
			Int64 newCursorAddress = cursorByteAddress;
			MemoryRegion selectionRange = offsetRegion;
			MemoryRegion copyRange = offsetRegion;
			Int64 newSelectionStartAddress = selectionRange.StartAddress;
			Int64 newSelectionEndAddress = selectionRange.EndAddress;
			if (moveUp) {
				moveToAddress -= offset;
				newCursorAddress -= offset;
				newSelectionStartAddress -= offset;
				newSelectionEndAddress -= offset;
			} else {
				moveToAddress += offset;
				newCursorAddress += offset;
				newSelectionEndAddress += offset;
				newSelectionStartAddress += offset;
			}
			// Detect if we're going to run into a problem with crossing the top or bottom of
			// the memory address space
			if (newSelectionStartAddress < 0) {
				if (newSelectionEndAddress < 0) {
					// Just delete the data
					Delete();
					CursorAddress = 0;
					SelectionRange = null;
					return;
				}
				// Part of the selection is cropped
				copyRange = MemoryRegion.FromStartAndEndAddresses((UInt32)(selectionRange.StartAddress - newSelectionStartAddress), copyRange.EndAddress);
				newSelectionStartAddress = 0;
				moveToAddress = 0;
				newCursorAddress = Math.Max(0, newCursorAddress);
			} else if (newSelectionEndAddress >= 0x100000000) {
				if (newSelectionStartAddress >= 0x100000000) {
					// Just delete the data
					Delete();
					CursorAddress = 0xFFFFFFFF;
					SelectionRange = null;
					return;
				}
				// Part of the selection is cropped
				copyRange = MemoryRegion.FromStartAndEndAddresses(copyRange.StartAddress, copyRange.EndAddress - (UInt32)(newSelectionEndAddress - 0xFFFFFFFF));
				newSelectionEndAddress = 0xFFFFFFFF;
				newCursorAddress = Math.Min(0xFFFFFFFF, newCursorAddress);
			}
			// Copy the data from the selected region
			selectionByteRegion = copyRange;
			CopyRegion copyRegion = new(this);
			// Delete the data from the previous location
			MultipleUndoAction mud = new();
			if (undoHistoryEnabled) {
				GetData(selectionRange, out DataBuffer dataBeforeDelete);
				mud.UndoActions.Add(new DeleteUndoAction(this, selectionRange.StartAddress, dataBeforeDelete));
			}
			Memory.Delete(MemoryRegion.FromStartAddressAndSize(selectionRange.StartAddress, selectionRange.Size));
			// Save the data at the final location
			SelectionByteRegion = MemoryRegion.FromStartAndEndAddresses((UInt32)newSelectionStartAddress, (UInt32)newSelectionEndAddress);
			CursorByteAddress = (UInt32)newCursorAddress;
			if (undoHistoryEnabled) {
				// Back up the data first
				GetData(SelectionByteRegion, out DataBuffer dataBeforePaste);
				mud.UndoActions.Add(new MemoryChangedUndoAction(this, (UInt32)moveToAddress, dataBeforePaste, copyRegion.Buffer));
				UndoHistory.RegisterAction(mud);
			}
			SetData((UInt32)moveToAddress, copyRegion.Buffer, true);
			InvalidateVisual();
			Modified?.Invoke(this, new EventArgs());
		}

		/// <summary>Deletes a specified memory region.</summary>
		/// <param name="deleteRegion">The memory region to delete.</param>
		/// <param name="suppressUndo">True to suppress creating an undo history entry.</param>
		internal void Delete(MemoryRegion deleteRegion, Boolean suppressUndo) {
			if (!suppressUndo && undoHistoryEnabled) {
				// Slow method: Save deleted data
				MemoryRegion byteMemoryRegion = MemoryRegion.FromStartAndEndAddresses(deleteRegion.StartAddress * Layout.bytesPerAddress, (deleteRegion.EndAddress * Layout.bytesPerAddress) + (Layout.bytesPerCell - 1));
				MemoryRegionCollection intersectRegions = Memory.IntersectRegions(byteMemoryRegion);
				if (intersectRegions.Count == 0) {
					return;
				}
				MultipleUndoAction mud = new();
				foreach (MemoryRegion thisMemoryRegion in intersectRegions) {
					GetData(thisMemoryRegion, out DataBuffer oldDataBuffer);
					if (!suppressUndo && undoHistoryEnabled) {
						mud.UndoActions.Add(new DeleteUndoAction(this, thisMemoryRegion.StartAddress, oldDataBuffer));
					}
					Memory.Delete(MemoryRegion.FromStartAddressAndSize(thisMemoryRegion.StartAddress, thisMemoryRegion.Size));
				}
				UndoHistory.RegisterAction(mud);
			} else {
				// Fast method: Just delete data, no need to save data
				Memory.Delete(deleteRegion);
			}
			buffer = null;
			InvalidateVisual();
			Modified?.Invoke(this, new EventArgs());
		}

		/// <summary>Crops all data to the selected memory region.</summary>
		/// <param name="cropRegion">The crop region.</param>
		/// <param name="suppressUndo">True to suppress creating an undo history entry.</param>
		internal void Crop(MemoryRegion cropRegion, Boolean suppressUndo) {
			if (cropRegion is not null) {
				if (!suppressUndo && undoHistoryEnabled) {
					// Use the slow method
					MemoryRegionCollection nonIntersectRegions = Memory.NonIntersectRegions(cropRegion);
					if (nonIntersectRegions.Count == 0) {
						return;
					}
					CropUndoAction cud = new(this, cropRegion);
					foreach (MemoryRegion thisMemoryRegion in nonIntersectRegions) {
						GetData(thisMemoryRegion, out DataBuffer oldDataBuffer);
						cud.DeleteActions.Add(new DeleteUndoAction(this, thisMemoryRegion.StartAddress, oldDataBuffer));
					}
					UndoHistory.RegisterAction(cud);
					Memory.Delete(nonIntersectRegions);
				} else {
					// Use the fast method
					Memory.Crop(cropRegion);
				}
				buffer = null;
				InvalidateVisual();
				Modified?.Invoke(this, new EventArgs());
			}
		}

		/// <summary>Fills the selected memory region with a fill value.</summary>
		/// <param name="fillRegion">The fill region.</param>
		/// <param name="fillValue">The fill value.</param>
		/// <param name="suppressUndo">True to suppress creating an undo history entry.</param>
		private void Fill(MemoryRegion fillRegion, UInt64 fillValue, Boolean suppressUndo) {
			if (fillRegion is not null) {
				DataBuffer newDataBuffer = new(fillRegion.Size);
				UInt32 fillIdx = 0;
				while (fillIdx < fillRegion.Size) {
					switch (dataSizeSetting) {
						case DataSizeSettings.DataSizeByte:
							newDataBuffer.Data[fillIdx] = (Byte)fillValue;
							break;
						case DataSizeSettings.DataSizeHWord:
							newDataBuffer.Data[fillIdx] = (Byte)(fillValue >> (Int32)(8 * (fillIdx % 2)));
							break;
						case DataSizeSettings.DataSizeWord:
							newDataBuffer.Data[fillIdx] = (Byte)(fillValue >> (Int32)(8 * (fillIdx % 4)));
							break;
						case DataSizeSettings.DataSizeDWord:
							newDataBuffer.Data[fillIdx] = (Byte)(fillValue >> (Int32)(8 * (fillIdx % 16)));
							break;
					}
					fillIdx++;
				}
				SetData(fillRegion.StartAddress, newDataBuffer, suppressUndo);
				Modified?.Invoke(this, new EventArgs());
			}
		}

		/// <summary>Generate random data for the selected memory region.</summary>
		/// <param name="randomizeRegion">The randomize region.</param>
		private void Randomize(MemoryRegion randomizeRegion) {
			if (randomizeRegion is not null) {
				SetData(randomizeRegion.StartAddress, DataBuffer.RandomDataBuffer(randomizeRegion.Size), false);
				Modified?.Invoke(this, new EventArgs());
			}
		}

		/// <summary>Invert all data in the selected memory region.</summary>
		/// <param name="invertRegion">The invert region.</param>
		private void Invert(MemoryRegion invertRegion) {
			if (invertRegion is not null) {
				GetData(invertRegion, out DataBuffer buffer);
				for (UInt32 byteIdx = 0; byteIdx < buffer.Data.Length; byteIdx++) {
					buffer.Data[byteIdx] = (Byte)~buffer.Data[byteIdx];
				}
				buffer.ImplementAll();
				SetData(invertRegion.StartAddress, buffer, false);
				Modified?.Invoke(this, new EventArgs());
			}
		}

		/// <summary>Sets a region of data.</summary>
		/// <param name="address">The address at which to copy the data.</param>
		/// <param name="newData">The data buffer.</param>
		public void SetData(UInt32 address, DataBuffer newData) {
			SetData(address, newData, false);
		}

		/// <summary>Offset all data in the file.</summary>
		/// <param name="offset">The offset.</param>
		/// <param name="moveUp">True to move up.</param>
		public void OffsetAllData(UInt32 offset, Boolean moveUp) {
			OffsetAllData(offset, moveUp, false);
		}

		/// <summary>Move the selected data up or down with offset.</summary>
		/// <param name="offset">The offset.</param>
		/// <param name="moveUp">True to move up.</param>
		public void OffsetSelectedData(UInt32 offset, Boolean moveUp) {
			OffsetSelectedData(selectionByteRegion, offset, moveUp);
		}

		/// <summary>Deletes a specified memory region.</summary>
		public void Delete() {
			if (SelectionByteRegion is not null) {
				Delete(SelectionByteRegion, false);
			} else {
				Delete(MemoryRegion.FromStartAddressAndSize(CursorByteAddress, 1), false);
			}
		}

		/// <summary>Crops all data to the selected memory region.</summary>
		public void Crop() {
			if (SelectionRange is not null) {
				Crop(SelectionByteRegion, false);
			}
		}

		/// <summary>Fills the selected memory region with a fill value.</summary>
		/// <param name="fillValue">The fill value.</param>
		public void Fill(UInt64 fillValue) {
			Fill(SelectionByteRegion, fillValue, false);
		}

		/// <summary>Generate random data for the selected memory region.</summary>
		public void Randomize() {
			Randomize(SelectionByteRegion);
		}

		/// <summary>Invert all data in the selected memory region.</summary>
		public void Invert() {
			Invert(SelectionByteRegion);
		}

		/// <summary>Reverses the selected data.</summary>
		/// <param name="reverseBits">True to reverse bits as well as bytes.</param>
		public void Reverse(Boolean reverseBits) {
			GetData(selectionByteRegion, out DataBuffer buffer);
			buffer.ReverseBytes();
			if (reverseBits) {
				buffer.ReverseBits();
			}
			SetData(selectionByteRegion.StartAddress, buffer, false);
		}

		/// <summary>Generates design mode data.</summary>
		private void GenerateDesignModeData() {
			// Load some dummy data for design mode
			Random rnd = new();
			buffer = new DataBuffer((Int32)(Layout.bytesPerRow * Layout.rowCount));
			for (UInt32 byteIdx = 0; byteIdx < buffer.Data.Length; byteIdx++) {
				buffer.Data[byteIdx] = (Byte)rnd.Next(0, 255);
				buffer.Implemented[byteIdx] = rnd.Next(0, 1) != 0;
			}
		}

		/// <summary>Loads data buffer.</summary>
		/// <param name="forceLoadData">(Optional) True to force load data.</param>
		private void LoadDataBuffer(Boolean forceLoadData = false) {
			if (inDesignMode) {
				// Load some dummy data for design mode
				GenerateDesignModeData();
				return;
			}
			Boolean needToLoadData = false;
			buffer = new DataBuffer((Int32)(Layout.bytesPerRow * Layout.rowCount));
			UInt32 firstDispAddress = CurrentByteAddress;
			UInt32 lastDispAddress = LastDisplayedByteAddress;
			UInt32 firstDataAddress = dataAddress;
			if (!forceLoadData) {
				UInt32 lastDataAddress = (UInt32)(dataAddress + buffer.Data.Length - 1);
				if ((firstDispAddress < firstDataAddress) || (lastDispAddress > lastDataAddress)) {
					needToLoadData = true;
				}
			}
			if (forceLoadData || needToLoadData) {
				Int64 firstLoadAddress = Math.Max((Int64)firstDispAddress - 0x1000, 0);
				UInt32 loadSize = Layout.bytesPerRow * Layout.rowCount;
				if (buffer is null) {
					buffer = new();
				}
				MemoryRegion displayedRegion = MemoryRegion.FromStartAddressAndSize(firstDispAddress, loadSize);
				buffer = new(displayedRegion, Memory);
				dataAddress = (UInt32)firstLoadAddress;
				dataAddress = (UInt32)firstLoadAddress;
			}
		}

		/// <summary>Gets a data.</summary>
		/// <param name="byteAddress">Zero-based index of the data byte.</param>
		/// <param name="isImplemented">[out] True if is implemented, false if not.</param>
		/// <returns>The data.</returns>
		public UInt64 GetData(UInt32 byteAddress, out Boolean isImplemented) {
			isImplemented = false;
			return dataSizeSetting switch {
				DataSizeSettings.DataSizeByte =>
					GetDataAt(byteAddress, ref isImplemented),
				DataSizeSettings.DataSizeHWord =>
					GetDataAt(byteAddress, ref isImplemented) +
					((UInt64)GetDataAt(byteAddress + 1, ref isImplemented) << 8),
				DataSizeSettings.DataSizeWord =>
					GetDataAt(byteAddress, ref isImplemented) +
					((UInt64)GetDataAt(byteAddress + 1, ref isImplemented) << 8) +
					((UInt64)GetDataAt(byteAddress + 2, ref isImplemented) << 16) +
					((UInt64)GetDataAt(byteAddress + 3, ref isImplemented) << 24),
				DataSizeSettings.DataSizeDWord =>
				GetDataAt(byteAddress, ref isImplemented) +
					((UInt64)GetDataAt(byteAddress + 1, ref isImplemented) << 8) +
					((UInt64)GetDataAt(byteAddress + 2, ref isImplemented) << 16) +
					((UInt64)GetDataAt(byteAddress + 3, ref isImplemented) << 24) +
					((UInt64)GetDataAt(byteAddress + 4, ref isImplemented) << 32) +
					((UInt64)GetDataAt(byteAddress + 5, ref isImplemented) << 40) +
					((UInt64)GetDataAt(byteAddress + 6, ref isImplemented) << 48) +
					((UInt64)GetDataAt(byteAddress + 7, ref isImplemented) << 56),
				_ => 0,
			};
		}

		/// <summary>Gets a data.</summary>
		/// <param name="byteAddress">Zero-based index of the data byte.</param>
		/// <param name="isImplemented">[out] True if is implemented, false if not.</param>
		/// <returns>The data.</returns>
		public UInt64 GetScreenData(UInt32 byteAddress, out Boolean isImplemented) {
			isImplemented = false;
			return dataSizeSetting switch {
				DataSizeSettings.DataSizeByte =>
					GetScreenDataAt(byteAddress, ref isImplemented),
				DataSizeSettings.DataSizeHWord =>
					GetScreenDataAt(byteAddress, ref isImplemented) +
					((UInt64)GetScreenDataAt(byteAddress + 1, ref isImplemented) << 8),
				DataSizeSettings.DataSizeWord =>
					GetScreenDataAt(byteAddress, ref isImplemented) +
					((UInt64)GetScreenDataAt(byteAddress + 1, ref isImplemented) << 8) +
					((UInt64)GetScreenDataAt(byteAddress + 2, ref isImplemented) << 16) +
					((UInt64)GetScreenDataAt(byteAddress + 3, ref isImplemented) << 24),
				DataSizeSettings.DataSizeDWord =>
					GetDataAt(byteAddress, ref isImplemented) +
					((UInt64)GetScreenDataAt(byteAddress + 1, ref isImplemented) << 8) +
					((UInt64)GetScreenDataAt(byteAddress + 2, ref isImplemented) << 16) +
					((UInt64)GetScreenDataAt(byteAddress + 3, ref isImplemented) << 24) +
					((UInt64)GetScreenDataAt(byteAddress + 4, ref isImplemented) << 32) +
					((UInt64)GetScreenDataAt(byteAddress + 5, ref isImplemented) << 40) +
					((UInt64)GetScreenDataAt(byteAddress + 6, ref isImplemented) << 48) +
					((UInt64)GetScreenDataAt(byteAddress + 7, ref isImplemented) << 56),
				_ => 0,
			};
		}

		/// <summary>Data to string.</summary>
		/// <param name="dataAddress">The address of the data.</param>
		/// <param name="isImplemented">[out] True if is implemented, false if not.</param>
		/// <returns>A String.</returns>
		private String DataToString(UInt32 dataAddress, out Boolean isImplemented) {
			UInt64 data = GetScreenData(dataAddress, out isImplemented);
			String retval = dataSizeSetting switch {
				DataSizeSettings.DataSizeByte => $"{data & 0xFF:X2}",
				DataSizeSettings.DataSizeHWord => $"{data & 0xFFFF:X4}",
				DataSizeSettings.DataSizeWord => $"{data & 0xFFFFFFFF:X8}",
				DataSizeSettings.DataSizeDWord => $"{data:X16}",
				_ => String.Empty,
			};
			return retval;
		}

		/// <summary>Query if an address location exists in the memory map.</summary>
		/// <param name="address">The address.</param>
		/// <returns>True if implemented, false if not.</returns>
		public Boolean IsImplemented(UInt32 address) {
			GetData(address * Layout.bytesPerAddress, out Boolean isImplemented);
			return isImplemented;
		}

		/// <summary>Determine if a specific address is currently displaying on the control.</summary>
		/// <param name="address">The address.</param>
		/// <returns>True if the address is being displayed, false otherwise.</returns>
		public Boolean AddressIsShown(UInt32 address) {
			return ByteAddressIsShown(address * Layout.bytesPerAddress);
		}

		/// <summary>Determine if a specific byte address is currently displaying on the control.</summary>
		/// <param name="byteAddress">The byte address.</param>
		/// <returns>True if the address is being displayed, false otherwise.</returns>
		private Boolean ByteAddressIsShown(UInt32 byteAddress) {
			return (byteAddress >= CurrentByteAddress) && (byteAddress <= LastDisplayedByteAddress);
		}

		/// <summary>Data to character.</summary>
		/// <param name="dataAddress">The address of the character.</param>
		/// <param name="isImplemented">[out] True if is implemented, false if not.</param>
		/// <returns>A Char.</returns>
		private Char DataToChar(UInt32 dataAddress, out Boolean isImplemented) {
			Char retval;
			isImplemented = true;
			switch (DataSizeSetting) {
				case DataSizeSettings.DataSizeByte: {
					Byte[] bytes = new Byte[1] { (Byte)GetScreenData(dataAddress, out isImplemented) };
					if ((bytes[0] < 0x20) || (bytes[0] == 0x7F)) {
						// Replace control characters and non-printing characters with a period
						retval = '.';
					} else {
						Encoding ansiEncoding = Encoding.GetEncoding(1252);
						retval = ansiEncoding.GetChars(bytes)[0];
					}
					break;
				}
				case DataSizeSettings.DataSizeHWord: {
					retval = (Char)(GetData(dataAddress, out isImplemented) & 0xFFFF);
					break;
				}
				default: {
					// Do nothing
					return (Char)0;
				}
			}
			FormattedText formatted = new($"{retval}", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, font, fontSize, Brushes.Black);
			Size thisCharSize = new(formatted.Width, formatted.Height);
			if (Char.IsWhiteSpace(retval) || Char.IsControl(retval)) {
				// This is a control character or whitespace character, and will not print
			} else if (thisCharSize.Width > (Layout.charSize.Width * 7.0 / 4.0)) {
				// This is not a fixed-width character and may be too wide to display in the character table
				retval = '.';
			}
			return retval;
		}

		/// <summary>Col header to string.</summary>
		/// <param name="colOffset">The col offset.</param>
		/// <returns>A String.</returns>
		private String ColHeaderToString(UInt64 colOffset) {
			if (Layout.columnCount >= 0x10) {
				// It takes two characters to describe the header offset
				return $"{colOffset & 0xFF:X2}";
			} else {
				// It takes one character to describe the header offset
				return $"{colOffset & 0xF:X1}";
			}
		}

		/// <summary>Convert an address to a string.</summary>
		/// <param name="address">The address.</param>
		/// <returns>The address string.</returns>
		private String AddressToString(Int64 address) {
			String retval = addressSizeSetting switch {
				AddressSizeSettings.AddressSizeByte => $"{address & 0xFF:X2}",
				AddressSizeSettings.AddressSizeHWord => $"{address & 0xFFFF:X4}",
				AddressSizeSettings.AddressSizeWord => $"{address & 0xFFFFFFFF:X8}",
				_ => String.Empty,
			};
			return retval;
		}

		/// <summary>Opens a data file.</summary>
		/// <param name="fileName">Name of the data file.</param>
		/// <param name="dataFileFormatter">The data file formatter.</param>
		public void OpenFile(String fileName, Type dataFileFormatter) {
			//Cursor = Cursors.WaitCursor;
			//Application.DoEvents();
			Memory = null;
			if (File.Exists(fileName)) {
				DataFile file = new(fileName, dataFileFormatter);
				Memory = file.MemoryMap;
				Int32 regionCount = Memory.BlockCount;
				if (regionCount == 0) {
					CursorByteAddress = 0;
					CurrentByteAddress = 0;
				} else {
					CursorByteAddress = Memory.StartAddress;
					CurrentByteAddress = Memory.StartAddress;
				}
				buffer = null;
				LoadDataBuffer(true);
				UndoHistory.Clear();
				InvalidateVisual();
				Errors = file.Errors;
				Warnings = file.Warnings;
				if (Errors.Count > 0) {
					OpenFileErrors?.Invoke(this, new EventArgs());
				}
				OpenedFile?.Invoke(this, new EventArgs());
			}
			//Cursor = Cursors.Default;
		}

		/// <summary>Opens a data file.</summary>
		/// <param name="fileName">Name of the data file.</param>
		public void MergeFile(String fileName) {
			//Cursor = Cursors.WaitCursor;
			if (File.Exists(fileName)) {
				DataFile file = new(fileName);
				MultipleUndoAction mud = new();
				Memory.SuppressOrganize = true;
				if (undoHistoryEnabled) {
					foreach (MemoryRegion region in file.MemoryMap.Regions) {
						DataBuffer dataBeforeMerge = new(region, Memory);
						DataBuffer dataAfterMerge = new(region, file.MemoryMap);
						mud.UndoActions.Add(new MemoryChangedUndoAction(this, region.StartAddress, dataBeforeMerge, dataAfterMerge));
						Memory.Insert(file.MemoryMap.Fetch(region));
					}
				} else {
					Memory.Delete(file.MemoryMap.Regions);
					foreach (MemoryRegion region in file.MemoryMap.Regions) {
						Memory.Insert(file.MemoryMap.Fetch(region));
					}
				}
				Memory.SuppressOrganize = false;
				if (undoHistoryEnabled) {
					UndoHistory.RegisterAction(mud);
				}
				LoadDataBuffer(true);
				InvalidateVisual();
				Errors = file.Errors;
				Warnings = file.Warnings;
				if (Errors.Count > 0) {
					OpenFileErrors?.Invoke(this, new EventArgs());
				}
				OpenedFile?.Invoke(this, new EventArgs());
			}
			//Cursor = Cursors.Default;
		}

		/// <summary>Saves a data file.</summary>
		/// <param name="fileName">Name of the data file.</param>
		public void SaveFile(String fileName) {
			//Cursor = Cursors.WaitCursor;
			DataFile dataFile = new(Memory);
			dataFile.Save(fileName);
			UndoHistory.SetSavePoint();
			if ((Errors is not null) && (Errors.Count > 0)) {
				SaveFileErrors?.Invoke(this, new EventArgs());
			}
			SavedFile?.Invoke(this, new EventArgs());
			//Cursor = Cursors.Default;
		}
	}
}
