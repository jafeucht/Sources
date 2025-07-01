// <copyright file="DataBuffer.cs" company="Dataescher">
// 	Copyright (c) 2022 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements the data buffer class.</summary>

using Dataescher.Types;
using System;
using System.Text.RegularExpressions;

namespace Dataescher.Data {
	/// <summary>(Serializable) buffer for data.</summary>
	/// <seealso cref="T:ICloneable"/>
	[Serializable()]
	public class DataBuffer : ICloneable {
		/// <summary>Gets the data byte array size.</summary>
		public Int64 Size => Data.Length;
		/// <summary>Gets the implemented bit mask array size.</summary>
		public Int64 ImpSize => (Size / 8) + (((Size % 8) != 0) ? 1 : 0);
		/// <summary>The data.</summary>
		public Memory Data { get; private set; }
		/// <summary>The implemented data bit mask.</summary>
		public Mask Implemented { get; private set; }
		/// <summary>Gets information describing the blank data byte.</summary>
		public Byte BlankData { get; private set; }

		/// <summary>Initializes a new instance of the Dataescher.DataBuffer class.</summary>
		public DataBuffer() {
			BlankData = 0;
			Data = new();
			Implemented = new();
		}

		/// <summary>Query if data at specified index is implemented.</summary>
		/// <param name="index">Zero-based index of the.</param>
		/// <returns>True if implemented, false if not.</returns>
		public Boolean IsImplemented(UInt32 index) {
			return Implemented[index];
		}

		/// <summary>Initializes a new instance of the Dataescher.DataBuffer class.</summary>
		/// <param name="data">The data.</param>
		public DataBuffer(Byte[] data) {
			Data = new(data);
			Implemented = new(true);
		}

		/// <summary>Initializes a new instance of the Dataescher.DataBuffer class.</summary>
		/// <param name="memoryRegion">The memory region.</param>
		/// <param name="source">Source for the.</param>
		public DataBuffer(MemoryRegion memoryRegion, MemoryMap? source) {
			if (source is not null) {
				BlankData = source.BlankData;
				if (memoryRegion.Size > 0) {
					Data = source.Fetch(memoryRegion).Data;
					Implemented = source.ImplementedMask(memoryRegion);
					return;
				}
			}
			BlankData = 0;
			Data = new();
			Implemented = new();
		}

		/// <summary>Initializes a new instance of the Dataescher.DataBuffer class.</summary>
		/// <param name="size">The size.</param>
		/// <param name="fillValue">(Optional) The fill value.</param>
		public DataBuffer(Int64 size, Byte? fillValue = null) {
			Data = new(size);
			Implemented = new(true);
			if (fillValue is Byte fill) {
				Data.Fill(fill);
			}
			ImplementAll();
		}

		/// <summary>Creates a new object that is a copy of the current instance.</summary>
		/// <returns>A new object that is a copy of this instance.</returns>
		public Object Clone() {
			DataBuffer newBuffer = new(Data.Length) {
				Data = Data.Clone() as Memory,
				Implemented = Implemented.Clone() as Mask
			};
			return newBuffer;
		}

		/// <summary>Implement all bytes.</summary>
		public void ImplementAll() {
			Implemented = new(true);
		}

		/// <summary>Reverses the data bytes.</summary>
		public void ReverseBytes() {
			for (UInt32 firstByteIdx = 0; firstByteIdx < Data.Length / 2; firstByteIdx++) {
				UInt32 lastByteIdx = (UInt32)(Data.Length - firstByteIdx - 1);
				// Reverse the bits
				Byte byte1 = Data[firstByteIdx];
				Byte byte2 = Data[lastByteIdx];
				Data[firstByteIdx] = byte2;
				Data[lastByteIdx] = byte1;
				// Swap the implemented bit for both values
				Boolean imp1 = Implemented[firstByteIdx];
				Boolean imp2 = Implemented[lastByteIdx];
				Implemented[firstByteIdx] = imp2;
				Implemented[lastByteIdx] = imp1;
			}
		}

		/// <summary>Reverses the bits in a byte.</summary>
		/// <param name="value">The byte.</param>
		/// <returns>The byte with the bits reversed.</returns>
		private Byte ReverseByte(Byte value) {
			Byte result = 0;
			for (Int32 bitIdx = 0; bitIdx < 8; bitIdx++) {
				Int32 readBit = 1 << bitIdx;
				Int32 writeBit = 1 << (8 - bitIdx - 1);
				if ((value & readBit) != 0) {
					result = (Byte)(result | writeBit);
				}
			}
			return result;
		}

		/// <summary>Reverse the bit order in each byte.</summary>
		public void ReverseBits() {
			if ((BlankData != 0x00) && (BlankData != 0xFF)) {
				ImplementAll();
			}
			for (UInt32 byteIdx = 0; byteIdx < Data.Length; byteIdx++) {
				Data[byteIdx] = ReverseByte(Data[byteIdx]);
			}
		}

		/// <summary>Reverse the bit order in each byte.</summary>
		public void Invert() {
			ImplementAll();
			for (UInt32 byteIdx = 0; byteIdx < Data.Length; byteIdx++) {
				Data[byteIdx] = (Byte)~Data[byteIdx];
			}
		}

		/// <summary>Creates a new object from the given ASCII text.</summary>
		/// <param name="text">The text.</param>
		/// <returns>A DataBuffer.</returns>
		public static DataBuffer FromAscii(String text) {
			DataBuffer buffer;
			Char[] chars = text.ToCharArray();
			buffer = new(chars.Length);
			UInt32 bufferIdx = 0;
			foreach (Char thisChar in chars) {
				buffer.Data[bufferIdx++] = (Byte)thisChar;
			}
			return buffer;
		}

		/// <summary>Creates a new object from the given ASCII text.</summary>
		/// <param name="text">The text.</param>
		/// <returns>A DataBuffer.</returns>
		public static DataBuffer FromUnicode(String text) {
			DataBuffer buffer;
			Char[] chars = text.ToCharArray();
			buffer = new(chars.Length * 2);
			UInt32 bufferIdx = 0;
			foreach (Char thisChar in chars) {
				buffer.Data[bufferIdx++] = (Byte)thisChar;
				buffer.Data[bufferIdx++] = (Byte)(thisChar >> 8);
			}
			return buffer;
		}

		/// <summary>(Immutable) A regex string to match hex data.</summary>
		private const String hexDataRegex = @"^[0-9A-Fa-f\s]*$";

		/// <summary>Creates a new object from the given hex string.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <param name="text">The text.</param>
		/// <returns>A DataBuffer.</returns>
		public static DataBuffer FromHexString(String text) {
			Boolean startsWith0x;
			if (text.StartsWith("0x") || text.StartsWith("0X")) {
				text = text.Substring(2);
				startsWith0x = true;
			} else {
				startsWith0x = false;
			}
			Regex hexRegex = new(hexDataRegex);
			if (!hexRegex.IsMatch(text)) {
				throw new Exception("Data does not look like hex data.");
			}
			// Copy as hexadecimal. Frist, strip all whitespace characters
			text = Regex.Replace(text, @"\s+", "");
			// Append to the end an extra nibble if necessary
			if (text.Length % 2 != 0) {
				if (startsWith0x) {
					// Pad out the start of the data with zeroes
					text = "0" + text;
				} else {
					// Pad out the end of the data with ones
					text += "F";
				}
			}
			Byte[] data = Strings.ReadStringAsHexBytes(text);
			DataBuffer buffer = new(data);
			return buffer;
		}

		/// <summary>Create a data buffer from a string, automatically selecting the format.</summary>
		/// <param name="text">The text.</param>
		/// <param name="preferUnicode">(Optional) True to prefer unicode string.</param>
		/// <returns>A DataBuffer.</returns>
		public static DataBuffer FromString(String text, Boolean preferUnicode = false) {
			Boolean looksLikeHex;
			Regex hexRegex = new(hexDataRegex);
			looksLikeHex = text.StartsWith("0X") || text.StartsWith("0x") ? hexRegex.IsMatch(text.Substring(2)) : hexRegex.IsMatch(text);
			return looksLikeHex ? FromHexString(text) : preferUnicode ? FromUnicode(text) : FromAscii(text);
		}

		/// <summary>Create a data buffer with random data.</summary>
		/// <exception cref="ArgumentOutOfRangeException">
		///     Thrown when one or more arguments are outside the required range.
		/// </exception>
		/// <param name="size">The number of bytes.</param>
		/// <returns>A data buffer with random data.</returns>
		public static DataBuffer RandomDataBuffer(Int64 size) {
			if ((size < 0) || (size > 0x100000000)) {
				throw new ArgumentOutOfRangeException(nameof(size));
			}
			DataBuffer retval = new(size);
			Int64 fillIdx = 0;
			Random rnd = new();
			while (fillIdx < size) {
				retval.Data[(UInt32)fillIdx++] = (Byte)rnd.Next();
			}
			return retval;
		}
	}
}