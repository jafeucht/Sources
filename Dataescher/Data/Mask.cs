// <copyright file="Mask.cs" company="Dataescher">
// Copyright (c) 2025 Dataescher. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>2/4/2025</date>
// <summary>Implements a low memory footprint infinite bitmask object which can support bit operations.</summary>

using Dataescher.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataescher.Data {
	/// <summary>An object representing a low-memory bitmask of infinite length.</summary>
	/// <seealso cref="T:ICloneable"/>
	[Serializable]
	public class Mask : ICloneable {
		/// <summary>(Immutable) The maximum bit index.</summary>
		private const Int64 MAX_BIT_INDEX = 0xFFFFFFFF;

		/// <summary>(Immutable) The minimum bit index.</summary>
		private const Int64 MIN_BIT_INDEX = -0x100000000;

		/// <summary>(Immutable) The maximum 32-bit mask index.</summary>
		private const Int32 MAX_MASK_INDEX = 0x7FFFFFF;

		/// <summary>(Immutable) The minimum 32-bit mask index.</summary>
		private const Int32 MIN_MASK_INDEX = -0x8000000;

		/// <summary>
		///     Gets a value indicating the bit value for every bit not defined by the <see cref="Mask"/> object. If false,
		///     all undefined bits are zero; if true, all undefined bits are one.
		/// </summary>
		public Boolean DefaultBitState { get; private set; }

		/// <summary>
		///     (Immutable) Gets or sets the masks. This dictionary should never contain values which indicate a blank mask.
		///     - If <see cref="DefaultBitState"/> is true, should never contain a value of All 1's (0xFFFFFFFF)
		///     - If <see cref="DefaultBitState"/> is false, should never contain a value of all 0's (0x0).
		/// </summary>
		private readonly Dictionary<Int32, UInt32> _masks;

		/// <summary>Initializes a new instance of the <see cref="Mask"/> class.</summary>
		public Mask() {
			DefaultBitState = false;
			_masks = new();
		}

		/// <summary>Initializes a new instance of the <see cref="Mask"/> class.</summary>
		/// <param name="defBitState">(Optional) The default bit value.</param>
		public Mask(Boolean defBitState = false) {
			DefaultBitState = defBitState;
			_masks = new();
		}

		/// <summary>Gets a bit.</summary>
		/// <param name="bitIndex">Zero-based index of the bit to get.</param>
		/// <returns>The bit value.</returns>
		private Boolean GetBit(Int64 bitIndex) {
			ValidateBitIndex(nameof(bitIndex), bitIndex);
			// Ensure maskMod is normalized between 0 and 31
			Int32 maskMod = (Int32)(((bitIndex % 32) + 32) % 32);
			Int32 maskIdx = (Int32)((bitIndex - maskMod) / 32);
			if (_masks.TryGetValue(maskIdx, out UInt32 maskValue)) {
				UInt32 bitMask = (UInt32)(1 << maskMod);
				return (maskValue & bitMask) != 0;
			} else {
				return DefaultBitState;
			}
		}

		/// <summary>Validates a bit index is within range.</summary>
		/// <exception cref="ArgumentOutOfRangeException">
		///     Thrown when one or more arguments are outside the required range.
		/// </exception>
		/// <param name="paramName">Name of the parameter.</param>
		/// <param name="index">Zero-based index of the.</param>
		private static void ValidateBitIndex(String paramName, Int64 index) {
			if (index > MAX_BIT_INDEX) {
				throw new ArgumentOutOfRangeException(paramName, index, $"Argument is greater than maximum value {MAX_BIT_INDEX}");
			} else if (index < MIN_BIT_INDEX) {
				throw new ArgumentOutOfRangeException(paramName, index, $"Argument is less than minimum value {MIN_BIT_INDEX}");
			}
		}

		/// <summary>Sets a bit.</summary>
		/// <param name="bitIndex">Zero-based index of the bit to set.</param>
		/// <param name="value">The bit value to set.</param>
		private void SetBit(Int64 bitIndex, Boolean value) {
			ValidateBitIndex(nameof(bitIndex), bitIndex);
			// Ensure maskMod is normalized between 0 and 31
			Int32 maskMod = (Int32)(((bitIndex % 32) + 32) % 32);
			Int32 maskIdx = (Int32)((bitIndex - maskMod) / 32);
			UInt32 bitMask = (UInt32)(1 << maskMod);
			if (_masks.TryGetValue(maskIdx, out UInt32 maskValue)) {
				UInt32 newMaskValue = maskValue & ~bitMask;
				if (value) {
					newMaskValue |= bitMask;
				}
				_masks.Remove(maskIdx);
				if (DefaultBitState && (newMaskValue == 0xFFFFFFFF)) {
					// The resulting mask is a default mask value
					_masks.Remove(maskIdx);
				} else if (!DefaultBitState && (newMaskValue == 0x0)) {
					// The resulting mask is a default mask value
					_masks.Remove(maskIdx);
				} else {
					// Non-default bits are still set. Change the value
					_masks[maskIdx] = newMaskValue;
				}
			} else if (value != DefaultBitState) {
				UInt32 newMaskValue = DefaultBitState ? (0xFFFFFFFF & (~bitMask)) : bitMask;
				_masks.Add(maskIdx, newMaskValue);
			}
		}

		/// <summary>Crops the mask to a specified set of bits.</summary>
		/// <param name="lowBitIndex">The lowest bit index to include in the array.</param>
		/// <param name="highBitIndex">The highest bit index to include in the array.</param>
		public void Crop(Int64 lowBitIndex, Int64 highBitIndex) {
			ValidateBitRange(nameof(lowBitIndex), lowBitIndex, nameof(highBitIndex), highBitIndex);
			Int32 lowMod = (Int32)(((lowBitIndex % 32) + 32) % 32);
			Int32 lowIndex = (Int32)((lowBitIndex - lowMod) / 32);
			Int32 highMod = (Int32)(((highBitIndex % 32) + 32) % 32);
			Int32 highIndex = (Int32)((highBitIndex - highMod) / 32);
			if (lowMod != 0) {
				if (_masks.TryGetValue(lowIndex, out UInt32 value)) {
					UInt32 lowMask = 0xFFFFFFFF << lowMod;
					_masks[lowIndex] = DefaultBitState ? value | ~lowMask : value & lowMask;
				}
			}
			if (highMod != 31) {
				if (_masks.TryGetValue(highIndex, out UInt32 value)) {
					UInt32 highMask = 0xFFFFFFFF >> (31 - highMod);
					_masks[highIndex] = DefaultBitState ? value | ~highMask : value & highMask;
				}
			}
			List<Int32> removeKeys = new();
			foreach (Int32 key in _masks.Keys) {
				if ((key < lowIndex) || (key > highIndex)) {
					removeKeys.Add(key);
				}
			}
			foreach (Int32 removeKey in removeKeys) {
				_masks.Remove(removeKey);
			}
		}

		/// <summary>Deletes a range of bits.</summary>
		/// <param name="lowBitIndex">The lowest bit index to include in the array.</param>
		/// <param name="highBitIndex">The highest bit index to include in the array.</param>
		public void Delete(Int64 lowBitIndex, Int64 highBitIndex) {
			ValidateBitRange(nameof(lowBitIndex), lowBitIndex, nameof(highBitIndex), highBitIndex);
			Int32 lowMod = (Int32)(((lowBitIndex % 32) + 32) % 32);
			Int32 lowIndex = (Int32)((lowBitIndex - lowMod) / 32);
			Int32 highMod = (Int32)(((highBitIndex % 32) + 32) % 32);
			Int32 highIndex = (Int32)((highBitIndex - highMod) / 32);
			if (lowMod != 0) {
				if (_masks.TryGetValue(lowIndex, out UInt32 value)) {
					if ((lowIndex == highIndex) && (highMod < 31)) {
						// This is the case we're deleting bits from the middle of one mask entry
						UInt32 mask = (0xFFFFFFFF << lowMod) & (0xFFFFFFFF >> (31 - highMod));
						_masks[lowIndex++] = DefaultBitState ? value | mask : value & ~mask;
						return;
					}
					UInt32 lowMask = 0xFFFFFFFF << lowMod;
					_masks[lowIndex++] = DefaultBitState ? value | lowMask : value & ~lowMask;
				}
			}
			if (highMod != 31) {
				if (_masks.TryGetValue(highIndex, out UInt32 value)) {
					UInt32 highMask = 0xFFFFFFFF >> (31 - highMod);
					_masks[highIndex--] = DefaultBitState ? value | highMask : value & ~highMask;
				}
			}
			List<Int32> removeKeys = new();
			foreach (Int32 key in _masks.Keys) {
				if ((key >= lowIndex) && (key <= highIndex)) {
					removeKeys.Add(key);
				}
			}
			foreach (Int32 removeKey in removeKeys) {
				_masks.Remove(removeKey);
			}
		}

		/// <summary>Indexer to get or set items within this collection using array index syntax.</summary>
		/// <param name="idx">Zero-based index of the entry to access.</param>
		/// <returns>The indexed item.</returns>
		public Boolean this[Int64 idx] {
			get => GetBit(idx);
			set => SetBit(idx, value);
		}

		/// <summary>Convert this object into an array representation.</summary>
		/// <param name="lowBitIndex">The lowest bit index to include in the array.</param>
		/// <param name="highBitIndex">The highest bit index to include in the array.</param>
		/// <returns>An array that represents the data in this object.</returns>
		public UInt32[] ToArray(Int64 lowBitIndex, Int64 highBitIndex) {
			ValidateBitRange(nameof(lowBitIndex), lowBitIndex, nameof(highBitIndex), highBitIndex);
			Mask temp = lowBitIndex == 0 ? this : lowBitIndex < 0 ? ShiftLeft(this, -lowBitIndex) : ShiftRight(this, lowBitIndex);
			UInt32 defMask = DefaultBitState ? 0xFFFFFFFF : 0x0;
			Int32 resultSize = (Int32)((highBitIndex - lowBitIndex + 31) / 32);
			UInt32[] result = new UInt32[resultSize];
			for (Int32 resultIdx = 0; resultIdx < resultSize; resultIdx++) {
				result[resultIdx] = defMask;
			}
			for (Int32 idx = 0; idx < resultSize; idx++) {
				if (temp._masks.TryGetValue(idx, out UInt32 value)) {
					result[idx] = value;
				}
			}
			return result;
		}

		/// <summary>Create a <see cref="Mask"/> from an array.</summary>
		/// <param name="masks">The masks.</param>
		/// <param name="bitIndex">(Optional) The starting bit index.</param>
		/// <param name="defBitState">(Optional) The default bit value.</param>
		/// <returns>The <see cref="Mask"/>.</returns>
		public static Mask FromArray(UInt32[] masks, Int64 bitIndex = 0, Boolean defBitState = false) {
			ValidateBitIndex(nameof(bitIndex), bitIndex);
			Mask result = new(defBitState);
			Int32 idx = 0;
			// Simplified version for case data is alligned
			UInt32 defMask = defBitState ? 0xFFFFFFFF : 0x0;
			foreach (UInt32 mask in masks) {
				if (mask != defMask) {
					result._masks.Add(idx++, mask);
				}
			}
			if (bitIndex < 0) {
				result = ShiftRight(result, -bitIndex);
			} else if (bitIndex > 0) {
				result = ShiftLeft(result, bitIndex);
			}
			return result;
		}

		/// <summary>Get the lowest bit index for a bit which does not match <see cref="DefaultBitState"/>.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		public Int64 LowBitIndex {
			get {
				if (_masks.Count == 0) {
					return 0;
				}
				Int32 lowKey = _masks.Keys.Min();
				UInt32 wordLow = _masks[lowKey];
				Int32 bitLowOffset;
				for (bitLowOffset = 0; bitLowOffset < 32; bitLowOffset++) {
					if (DefaultBitState != ((wordLow & (1 << bitLowOffset)) != 0)) {
						return (lowKey * 32) + bitLowOffset;
					}
				}
				// If you make it here, something is not right about management of _masks. Values are not
				// automatically purged if they are all default.
				throw new Exception("Reached point which is illegal.");
			}
		}

		/// <summary>Get the highest bit index for a bit which does not match <see cref="DefaultBitState"/>.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		public Int64 HighBitIndex {
			get {
				if (_masks.Count == 0) {
					return 0;
				}
				Int32 highKey = _masks.Keys.Max();
				UInt32 wordHigh = _masks[highKey];
				Int32 bitHighOffset;
				for (bitHighOffset = 31; bitHighOffset >= 0; bitHighOffset--) {
					if (DefaultBitState != ((wordHigh & (1 << bitHighOffset)) != 0)) {
						return (highKey * 32) + bitHighOffset;
					}
				}
				// If you make it here, something is not right about management of _masks. Values are not
				// automatically purged if they are all default.
				throw new Exception("Reached point which is illegal.");
			}
		}

		/// <summary>
		///     Gets a value indicating whether this mask is empty, meaning all bits in mask are default value.
		/// </summary>
		public Boolean Empty => _masks.Count == 0;

		/// <summary>Returns a string that represents the current object.</summary>
		/// <returns>A string that represents the current object.</returns>
		public override String ToString() {
			if (_masks.Count == 0) {
				Int32 bitValue = DefaultBitState ? 1 : 0;
				return $"(0 bits total, default {bitValue})";
			}
			Int32 minBit = _masks.Keys.Min() * 32;
			Int32 maxBit = (_masks.Keys.Max() * 32) + 31;
			return ToString(minBit, maxBit);
		}

		/// <summary>
		///     Validate a bit range defined by <paramref name="lowBitIndex"/> and <paramref name="highBitIndex"/>.
		/// </summary>
		/// <exception cref="ArgumentException">
		///     Thrown when one or more arguments have unsupported or illegal values.
		/// </exception>
		/// <param name="lowBitIndexName">Name of the low bit index parameter.</param>
		/// <param name="lowBitIndex">The low bit index.</param>
		/// <param name="highBitIndexName">Name of the high bit index parameter.</param>
		/// <param name="highBitIndex">The high bit index.</param>
		private static void ValidateBitRange(String lowBitIndexName, Int64 lowBitIndex, String highBitIndexName, Int64 highBitIndex) {
			ValidateBitIndex(lowBitIndexName, lowBitIndex);
			ValidateBitIndex(highBitIndexName, highBitIndex);
			if (highBitIndex < lowBitIndex) {
				throw new ArgumentException($"Cannot be greater than {lowBitIndexName}", highBitIndexName);
			}
		}

		/// <summary>Returns a string that represents the current object.</summary>
		/// <param name="lowBitIndex">The start bit index.</param>
		/// <param name="highBitIndex">The end bit index.</param>
		/// <returns>A string that represents the current object.</returns>
		public String ToString(Int64 lowBitIndex, Int64 highBitIndex) {
			ValidateBitRange(nameof(lowBitIndex), lowBitIndex, nameof(highBitIndex), highBitIndex);
			const Int32 MaxDisplayBits = 256;
			StringBuilder sb = new();
			Boolean first = true;
			Int32 numBits = (Int32)(highBitIndex - lowBitIndex + 1);
			if (numBits > MaxDisplayBits) {
				// Show indication that the bitmask is truncated
				sb.Append("...");
				highBitIndex = (Int32)(lowBitIndex + MaxDisplayBits - 1);
			}
			UInt32[] masks = ToArray(lowBitIndex, highBitIndex);
			Int32 numBitsLastMask = numBits % 32;
			sb.Append($"({highBitIndex}) ");
			for (Int32 maskIdx = masks.Length - 1; maskIdx >= 0; maskIdx--) {
				UInt32 mask = masks[maskIdx];
				if (first && (numBitsLastMask != 0)) {
					mask &= ~(0xFFFFFFFF << numBitsLastMask);
					Int32 numNibbles = (numBitsLastMask + 3) / 4;
					sb.Append(mask.ToString($"X{numNibbles}"));
				} else {
					sb.Append(mask.ToString("X8"));
				}
				first = false;
			}
			sb.Append($" ({lowBitIndex})");
			Int32 defMask = DefaultBitState ? 1 : 0;
			sb.Append($" ({numBits} bit");
			if (numBits != 1) {
				sb.Append('s');
			}
			sb.Append($" total, default {defMask})");
			return sb.ToString();
		}

		/// <summary>Gets a <see cref="Byte"/> value.</summary>
		/// <param name="bitIndex">(Optional) The first bit index.</param>
		/// <returns>The <see cref="Byte"/> mask value.</returns>
		public Byte ToUInt8(Int32 bitIndex = 0) {
			return (Byte)ToArray(bitIndex, bitIndex + 7)[0];
		}

		/// <summary>Convert a <see cref="Byte"/> to a <see cref="Mask"/>.</summary>
		/// <param name="value">The value to convert.</param>
		/// <param name="bitIndex">(Optional) The start bit index.</param>
		/// <param name="defBitState">(Optional) The default bit value.</param>
		/// <returns>The resulting <see cref="Mask"/>.</returns>
		public static Mask FromUInt8(Byte value, Int32 bitIndex = 0, Boolean defBitState = false) {
			ValidateBitIndex(nameof(bitIndex), bitIndex);
			return FromArray(new UInt32[1] { defBitState ? 0xFFFFFF00 | value : value }, bitIndex, defBitState);
		}

		/// <summary>Gets a <see cref="UInt16"/> value.</summary>
		/// <param name="bitIndex">(Optional) The first bit index.</param>
		/// <returns>The <see cref="UInt16"/> mask value.</returns>
		public UInt16 ToUInt16(Int32 bitIndex = 0) {
			return (UInt16)ToArray(bitIndex, bitIndex + 15)[0];
		}

		/// <summary>Convert a <see cref="UInt16"/> to a <see cref="Mask"/>.</summary>
		/// <param name="value">The value to convert.</param>
		/// <param name="bitIndex">(Optional) The start bit index.</param>
		/// <param name="defBitState">(Optional) The default bit value.</param>
		/// <returns>The resulting <see cref="Mask"/>.</returns>
		public static Mask FromUInt16(UInt16 value, Int32 bitIndex = 0, Boolean defBitState = false) {
			return FromArray(new UInt32[1] { defBitState ? 0xFFFF0000 | value : value }, bitIndex, defBitState);
		}

		/// <summary>Gets a <see cref="UInt32"/> value.</summary>
		/// <param name="bitIndex">(Optional) The first bit index.</param>
		/// <returns>The <see cref="UInt32"/> mask value.</returns>
		public UInt32 ToUInt32(Int32 bitIndex = 0) {
			return ToArray(bitIndex, bitIndex + 31)[0];
		}

		/// <summary>Convert a <see cref="UInt32"/> to a <see cref="Mask"/>.</summary>
		/// <param name="value">The value to convert.</param>
		/// <param name="bitIndex">(Optional) The start bit index.</param>
		/// <param name="defBitState">(Optional) The default bit value.</param>
		/// <returns>The resulting <see cref="Mask"/>.</returns>
		public static Mask FromUInt32(UInt32 value, Int64 bitIndex = 0, Boolean defBitState = false) {
			ValidateBitIndex(nameof(bitIndex), bitIndex);
			return FromArray(new UInt32[1] { value }, bitIndex, defBitState);
		}

		/// <summary>Gets a <see cref="UInt64"/> value.</summary>
		/// <param name="bitIndex">(Optional) The first bit index.</param>
		/// <returns>The <see cref="UInt64"/> mask value.</returns>
		public UInt64 ToUInt64(Int64 bitIndex = 0) {
			ValidateBitIndex(nameof(bitIndex), bitIndex);
			UInt32[] result = ToArray(bitIndex, bitIndex + 63);
			return result[0] | ((UInt64)result[1] << 32);
		}

		/// <summary>Convert a <see cref="UInt64"/> to a <see cref="Mask"/>.</summary>
		/// <param name="value">The value to convert.</param>
		/// <param name="bitIndex">(Optional) The start bit index.</param>
		/// <param name="defBitState">(Optional) The default bit value.</param>
		/// <returns>The resulting <see cref="Mask"/>.</returns>
		public static Mask FromUInt64(UInt64 value, Int64 bitIndex = 0, Boolean defBitState = false) {
			ValidateBitIndex(nameof(bitIndex), bitIndex);
			return FromArray(new UInt32[2] { (UInt32)value, (UInt32)(value >> 32) }, bitIndex, defBitState);
		}

		/// <summary>Creates a new object that is a copy of the current instance.</summary>
		/// <returns>A new object that is a copy of this instance.</returns>
		public Object Clone() {
			Mask newMask = new(DefaultBitState);
			foreach (KeyValuePair<Int32, UInt32> pair in _masks) {
				newMask._masks.Add(pair.Key, pair.Value);
			}
			return newMask;
		}

		/// <summary>Bitwise 'or' operator.</summary>
		/// <param name="value1">A bit-field to process.</param>
		/// <param name="value2">One or more bits to OR into the bit-field.</param>
		/// <returns>The result of the operation.</returns>
		public static Mask operator |(Mask value1, Mask value2) {
			Mask newMask = new(value1.DefaultBitState || value2.DefaultBitState);
			// Get a collection of mask indices for both masks
			// Process keys which exist in a
			foreach (KeyValuePair<Int32, UInt32> aPair in value1._masks) {
				UInt32 aMask = aPair.Value;
				UInt32 bMask = value2.ToUInt32(aPair.Key * 32);
				UInt32 newValue = aMask | bMask;
				if (newValue != (newMask.DefaultBitState ? 0xFFFFFFFF : 0x0)) {
					newMask._masks[aPair.Key] = newValue;
				}
			}
			// Process keys which exist in b but not in a
			foreach (KeyValuePair<Int32, UInt32> bPair in value2._masks) {
				if (!value1._masks.ContainsKey(bPair.Key)) {
					UInt32 bMask = bPair.Value;
					UInt32 aMask = value1.DefaultBitState ? 0xFFFFFFFF : 0x0;
					UInt32 newValue = aMask | bMask;
					if (newValue != (newMask.DefaultBitState ? 0xFFFFFFFF : 0x0)) {
						newMask._masks[bPair.Key] = newValue;
					}
				}
			}
			return newMask;
		}

		/// <summary>Bitwise 'and' operator.</summary>
		/// <param name="value1">A bit-field to process.</param>
		/// <param name="value2">A mask of bits to apply to the bit-field.</param>
		/// <returns>The result of the operation.</returns>
		public static Mask operator &(Mask value1, Mask value2) {
			Mask newMask = new(value1.DefaultBitState && value2.DefaultBitState);
			// Get a collection of mask indices for both masks
			// Process keys which exist in a
			foreach (KeyValuePair<Int32, UInt32> aPair in value1._masks) {
				UInt32 aMask = aPair.Value;
				UInt32 bMask = value2.ToUInt32(aPair.Key * 32);
				UInt32 newValue = aMask & bMask;
				if (newValue != (newMask.DefaultBitState ? 0xFFFFFFFF : 0x0)) {
					newMask._masks[aPair.Key] = newValue;
				}
			}
			// Process keys which exist in b but not in a
			foreach (KeyValuePair<Int32, UInt32> bPair in value2._masks) {
				if (!value1._masks.ContainsKey(bPair.Key)) {
					UInt32 bMask = bPair.Value;
					UInt32 aMask = value1.DefaultBitState ? 0xFFFFFFFF : 0x0;
					UInt32 newValue = aMask & bMask;
					if (newValue != (newMask.DefaultBitState ? 0xFFFFFFFF : 0x0)) {
						newMask._masks[bPair.Key] = newValue;
					}
				}
			}
			return newMask;
		}

		/// <summary>Bitwise 'exclusive or' operator.</summary>
		/// <param name="value1">A bit-field to process.</param>
		/// <param name="value2">One or more bits to XOR against the bit-field.</param>
		/// <returns>The result of the operation.</returns>
		public static Mask operator ^(Mask value1, Mask value2) {
			Mask newMask = new(value1.DefaultBitState != value2.DefaultBitState);
			// Get a collection of mask indices for both masks
			// Process keys which exist in a
			foreach (KeyValuePair<Int32, UInt32> aPair in value1._masks) {
				UInt32 aMask = aPair.Value;
				UInt32 bMask = value2.ToUInt32(aPair.Key * 32);
				UInt32 newValue = aMask ^ bMask;
				if (newValue != (newMask.DefaultBitState ? 0xFFFFFFFF : 0x0)) {
					newMask._masks[aPair.Key] = newValue;
				}
			}
			// Process keys which exist in b but not in a
			foreach (KeyValuePair<Int32, UInt32> bPair in value2._masks) {
				if (!value1._masks.ContainsKey(bPair.Key)) {
					UInt32 bMask = bPair.Value;
					UInt32 aMask = value1.DefaultBitState ? 0xFFFFFFFF : 0x0;
					UInt32 newValue = aMask ^ bMask;
					if (newValue != (newMask.DefaultBitState ? 0xFFFFFFFF : 0x0)) {
						newMask._masks[bPair.Key] = newValue;
					}
				}
			}
			return newMask;
		}

		/// <summary>
		///     Create a mask containing zeros between <paramref name="lowBitIndex"/> and <paramref name="highBitIndex"/>.
		/// </summary>
		/// <param name="lowBitIndex">The first bit containing a zero.</param>
		/// <param name="highBitIndex">The last bit containing a zero.</param>
		/// <returns>The resulting mask.</returns>
		public static Mask Zeros(Int64 lowBitIndex, Int64 highBitIndex) {
			ValidateBitRange(nameof(lowBitIndex), lowBitIndex, nameof(highBitIndex), highBitIndex);
			Mask retval = new(true);
			Int32 lowBitMod = (Int32)(((lowBitIndex % 32) + 32) % 32);
			Int32 highBitMod = (Int32)(((highBitIndex % 32) + 32) % 32);
			Int32 lowMaskIndex = (Int32)((lowBitIndex - lowBitMod) / 32);
			Int32 highMaskIndex = (Int32)((highBitIndex - highBitMod) / 32);
			if (lowBitMod > 0) {
				if ((lowMaskIndex == highMaskIndex) && (highBitMod < 31)) {
					retval._masks.Add(lowMaskIndex, ~((0xFFFFFFFF << lowBitMod) & (0xFFFFFFFF >> (31 - highBitMod))));
					return retval;
				} else {
					retval._masks.Add(lowMaskIndex++, ~(0xFFFFFFFF << lowBitMod));
				}
			}
			if (highBitMod < 31) {
				retval._masks.Add(highMaskIndex--, ~(0xFFFFFFFF >> (31 - highBitMod)));
			}
			for (Int32 maskIndex = lowMaskIndex; maskIndex <= highMaskIndex; maskIndex++) {
				retval._masks.Add(maskIndex, 0x0);
			}
			return retval;
		}

		/// <summary>
		///     Create a mask containing ones between <paramref name="lowBitIndex"/> and <paramref name="highBitIndex"/>.
		/// </summary>
		/// <param name="lowBitIndex">The first bit containing a zero.</param>
		/// <param name="highBitIndex">The last bit containing a zero.</param>
		/// <returns>The resulting mask.</returns>
		public static Mask Ones(Int64 lowBitIndex, Int64 highBitIndex) {
			ValidateBitRange(nameof(lowBitIndex), lowBitIndex, nameof(highBitIndex), highBitIndex);
			Mask retval = new(false);
			Int32 lowBitMod = (Int32)(((lowBitIndex % 32) + 32) % 32);
			Int32 highBitMod = (Int32)(((highBitIndex % 32) + 32) % 32);
			Int32 lowMaskIndex = (Int32)((lowBitIndex - lowBitMod) / 32);
			Int32 highMaskIndex = (Int32)((highBitIndex - highBitMod) / 32);
			if (lowBitMod > 0) {
				if ((lowMaskIndex == highMaskIndex) && (highBitMod < 31)) {
					retval._masks.Add(lowMaskIndex, (0xFFFFFFFF << lowBitMod) & (0xFFFFFFFF >> (31 - highBitMod)));
					return retval;
				}
				retval._masks.Add(lowMaskIndex++, 0xFFFFFFFF << lowBitMod);
			}
			if (highBitMod < 31) {
				retval._masks.Add(highMaskIndex--, 0xFFFFFFFF >> (31 - highBitMod));
			}
			for (Int32 maskIndex = lowMaskIndex; maskIndex <= highMaskIndex; maskIndex++) {
				retval._masks.Add(maskIndex, 0xFFFFFFFF);
			}
			return retval;
		}

		/// <summary>Bitwise 'ones complement' operator.</summary>
		/// <param name="value">A Mask to process.</param>
		/// <returns>The result of the operation.</returns>
		public static Mask operator ~(Mask value) {
			Mask newMask = new(!value.DefaultBitState);
			foreach (KeyValuePair<Int32, UInt32> aPair in value._masks) {
				UInt32 newValue = ~aPair.Value;
				if (newValue != (newMask.DefaultBitState ? 0xFFFFFFFF : 0x0)) {
					newMask._masks[aPair.Key] = newValue;
				}
			}
			return newMask;
		}

		/// <summary>Bitwise left shift.</summary>
		/// <param name="value">A Mask to process.</param>
		/// <param name="shift">The shift amount.</param>
		/// <returns>The resulting Mask.</returns>
		public static Mask ShiftLeft(Mask value, Int64 shift) {
			if (shift == 0) {
				return value;
			}
			if (shift < 0) {
				return ShiftRight(value, -shift);
			}
			// Shift is always positive from now on
			Int32 shiftLower = (Int32)(shift % 32);
			Mask retval = new(value.DefaultBitState);
			if (shiftLower == 0) {
				Int32 transpose = (Int32)(shift / 32);
				// Simply transpose the indices
				foreach (KeyValuePair<Int32, UInt32> mskPair in value._masks) {
					retval._masks.Add(mskPair.Key + transpose, mskPair.Value);
				}
				return retval;
			} else {
				// More complex. Need to apply shifting.
				// transpose2 are the higher portion,transpose1 the lower
				// 
				Int32 shiftUpper = 32 - shiftLower;
				UInt32 defMask = value.DefaultBitState ? 0xFFFFFFFF : 0x0;
				Int32 transposeUpper = (Int32)((shift + 31) / 32);
				Int32 transposeLower = transposeUpper - 1;
				UInt32 maskDestLower = 0xFFFFFFFF >> shiftUpper;
				UInt32 maskOrigLower = 0xFFFFFFFF >> shiftLower;
				UInt32 maskDestUpper = ~maskDestLower;
				UInt32 maskOrigUpper = ~maskOrigLower;
				foreach (KeyValuePair<Int32, UInt32> mskPair in value._masks) {
					// Process the lower portion
					Int32 keyLower = mskPair.Key + transposeLower;
					Int32 keyUpper = mskPair.Key + transposeUpper;
					UInt32 partLower = (maskOrigLower & mskPair.Value) << shiftLower;
					UInt32 partUpper = (maskOrigUpper & mskPair.Value) >> shiftUpper;
					// Get the previous value
					if (!retval._masks.TryGetValue(keyLower, out UInt32 existingLower)) {
						existingLower = defMask;
					} else {
						retval._masks.Remove(keyLower);
					}
					if (!retval._masks.TryGetValue(keyUpper, out UInt32 existingUpper)) {
						existingUpper = defMask;
					} else {
						retval._masks.Remove(keyUpper);
					}
					UInt32 finalValueLower = (maskDestLower & existingLower) | partLower;
					if (finalValueLower != defMask) {
						retval._masks.Add(keyLower, finalValueLower);
					}
					UInt32 finalValueUpper = (maskDestUpper & existingUpper) | partUpper;
					if (finalValueUpper != defMask) {
						retval._masks.Add(keyUpper, finalValueUpper);
					}
				}
			}
			return retval;
		}

		/// <summary>Bitwise right shift.</summary>
		/// <param name="value">A Mask to process.</param>
		/// <param name="shift">The shift amount.</param>
		/// <returns>The resulting Mask.</returns>
		public static Mask ShiftRight(Mask value, Int64 shift) {
			if (shift == 0) {
				return value;
			}
			if (shift < 0) {
				return ShiftLeft(value, -shift);
			}
			// Shift is always positive from now on
			Int32 shiftUpper = (Int32)(shift % 32);
			Mask retval = new(value.DefaultBitState);
			if (shiftUpper == 0) {
				Int32 transpose = (Int32)(shift / 32);
				// Simply transpose the indices
				foreach (KeyValuePair<Int32, UInt32> mskPair in value._masks) {
					Int32 newMaskIndex = mskPair.Key - transpose;
					if ((newMaskIndex >= MIN_MASK_INDEX) && (newMaskIndex <= MAX_MASK_INDEX)) {
						retval._masks.Add(newMaskIndex, mskPair.Value);
					}
				}
				return retval;
			} else {
				// More complex. Need to apply shifting.
				// transpose2 are the higher portion,transpose1 the lower
				// 
				Int32 shiftLower = 32 - shiftUpper;
				UInt32 defMask = value.DefaultBitState ? 0xFFFFFFFF : 0x0;
				Int32 transposeLower = (Int32)((shift + 31) / 32);
				Int32 transposeUpper = transposeLower - 1;
				UInt32 maskDestUpper = 0xFFFFFFFF << shiftLower;
				UInt32 maskOrigLower = 0xFFFFFFFF >> shiftLower;
				UInt32 maskDestLower = ~maskDestUpper;
				UInt32 maskOrigUpper = ~maskOrigLower;
				foreach (KeyValuePair<Int32, UInt32> mskPair in value._masks) {
					// Process the lower portion
					Int32 keyLower = mskPair.Key - transposeLower;
					Int32 keyUpper = mskPair.Key - transposeUpper;
					UInt32 partLower = (maskOrigLower & mskPair.Value) << shiftLower;
					UInt32 partUpper = (maskOrigUpper & mskPair.Value) >> shiftUpper;
					// Get the previous value
					if (!retval._masks.TryGetValue(keyLower, out UInt32 existingLower)) {
						existingLower = defMask;
					} else {
						retval._masks.Remove(keyLower);
					}
					if (!retval._masks.TryGetValue(keyUpper, out UInt32 existingUpper)) {
						existingUpper = defMask;
					} else {
						retval._masks.Remove(keyUpper);
					}
					UInt32 finalValueLower = (maskDestLower & existingLower) | partLower;
					if (finalValueLower != defMask) {
						if ((keyLower >= MIN_MASK_INDEX) && (keyLower <= MAX_MASK_INDEX)) {
							retval._masks.Add(keyLower, finalValueLower);
						}
					}
					UInt32 finalValueUpper = (maskDestUpper & existingUpper) | partUpper;
					if (finalValueUpper != defMask) {
						if ((keyUpper >= MIN_MASK_INDEX) && (keyUpper <= MAX_MASK_INDEX)) {
							retval._masks.Add(keyUpper, finalValueUpper);
						}
					}
				}
			}
			return retval;
		}

		/// <summary>Bitwise left shift operator.</summary>
		/// <param name="value">A Mask to process.</param>
		/// <param name="shift">The shift.</param>
		/// <returns>The result of the operation.</returns>
		public static Mask operator <<(Mask value, Int32 shift) {
			return ShiftLeft(value, shift);
		}

		/// <summary>Bitwise right shift operator.</summary>
		/// <param name="value">A Mask to process.</param>
		/// <param name="shift">The shift.</param>
		/// <returns>The result of the operation.</returns>
		public static Mask operator >>(Mask value, Int32 shift) {
			return ShiftRight(value, shift);
		}

		/// <summary>Determines whether the specified object is equal to the current object.</summary>
		/// <param name="obj">The object to compare with the current object.</param>
		/// <returns>
		///     <see langword="true" /> if the specified object  is equal to the current object; otherwise, <see langword="false" />.
		/// </returns>
		public override Boolean Equals(Object obj) {
			if (obj is not Mask other) {
				return false;
			}
			if (other.DefaultBitState != DefaultBitState) {
				return false;
			}
			Int64 lowBitIndexOther = other.LowBitIndex;
			Int64 lowBitIndex = LowBitIndex;
			if (lowBitIndexOther != lowBitIndex) {
				return false;
			}
			Int64 highBitIndexOther = other.HighBitIndex;
			Int64 highBitIndex = HighBitIndex;
			if (highBitIndexOther != highBitIndex) {
				return false;
			}
			// The following might not be the fastest, but it's the easiest to implement
			for (Int64 bitIdx = lowBitIndexOther; bitIdx <= highBitIndexOther; bitIdx += 64) {
				UInt64 valOther = other.ToUInt64(bitIdx);
				UInt64 val = ToUInt64(bitIdx);
				if (valOther != val) {
					return false;
				}
			}
			return true;
		}

		/// <summary>Serves as the default hash function.</summary>
		/// <returns>A hash code for the current object.</returns>
		public override Int32 GetHashCode() {
			Int32 hash = -965995815;
			hash = (hash * -1521134295) + DefaultBitState.GetHashCode();
			List<KeyValuePair<Int32, UInt32>> sortedMaskList = _masks.OrderBy(x => x.Key).ToList();
			foreach (KeyValuePair<Int32, UInt32> maskItem in sortedMaskList) {
				hash = (hash * -1521134295) + maskItem.Key.GetHashCode();
				hash = (hash * -1521134295) + maskItem.Value.GetHashCode();
			}
			return hash;
		}

		/// <summary>Equality operator.</summary>
		/// <param name="value1">The first instance to compare.</param>
		/// <param name="value2">The second instance to compare.</param>
		/// <returns>The result of the operation.</returns>
		public static Boolean operator ==(Mask value1, Mask value2) {
			return value1.Equals(value2);
		}

		/// <summary>Inequality operator.</summary>
		/// <param name="value1">The first instance to compare.</param>
		/// <param name="value2">The second instance to compare.</param>
		/// <returns>The result of the operation.</returns>
		public static Boolean operator !=(Mask value1, Mask value2) {
			return !value1.Equals(value2);
		}

		/// <summary>Explicit cast that converts the given <see cref="Mask"/> to a <see cref="Byte"/>.</summary>
		/// <param name="value">A bit-field to process.</param>
		/// <returns>The result of the operation.</returns>
		public static explicit operator Byte(Mask value) {
			return value.ToUInt8();
		}

		/// <summary>Implicit cast that converts the given <see cref="Byte"/> to a <see cref="Mask"/>.</summary>
		/// <param name="value">True to value.</param>
		/// <returns>The result of the operation.</returns>
		public static implicit operator Mask(Byte value) {
			return FromUInt8(value);
		}

		/// <summary>Explicit cast that converts the given <see cref="Mask"/> to a <see cref="UInt16"/>.</summary>
		/// <param name="value">A bit-field to process.</param>
		/// <returns>The result of the operation.</returns>
		public static explicit operator UInt16(Mask value) {
			return value.ToUInt16();
		}

		/// <summary>Implicit cast that converts the given <see cref="UInt16"/> to a <see cref="Mask"/>.</summary>
		/// <param name="value">True to value.</param>
		/// <returns>The result of the operation.</returns>
		public static implicit operator Mask(UInt16 value) {
			return FromUInt16(value);
		}

		/// <summary>Explicit cast that converts the given <see cref="Mask"/> to a <see cref="UInt32"/>.</summary>
		/// <param name="value">A bit-field to process.</param>
		/// <returns>The result of the operation.</returns>
		public static explicit operator UInt32(Mask value) {
			return value.ToUInt32();
		}

		/// <summary>Implicit cast that converts the given <see cref="UInt32"/> to a <see cref="Mask"/>.</summary>
		/// <param name="value">True to value.</param>
		/// <returns>The result of the operation.</returns>
		public static implicit operator Mask(UInt32 value) {
			return FromUInt32(value);
		}

		/// <summary>Explicit cast that converts the given <see cref="Mask"/> to a <see cref="UInt64"/>.</summary>
		/// <param name="value">A bit-field to process.</param>
		/// <returns>The result of the operation.</returns>
		public static explicit operator UInt64(Mask value) {
			return value.ToUInt64();
		}

		/// <summary>Implicit cast that converts the given <see cref="UInt64"/> to a <see cref="Mask"/>.</summary>
		/// <param name="value">True to value.</param>
		/// <returns>The result of the operation.</returns>
		public static implicit operator Mask(UInt64 value) {
			return FromUInt64(value);
		}

		/// <summary>Gets a CSV containing values represented by this object.</summary>
		/// <param name="lowBitIndex">The start bit index.</param>
		/// <param name="highBitIndex">The end bit index.</param>
		/// <param name="useHexFormat">(Optional) True to use hexadecimal format, false to use decimal format.</param>
		/// <returns>The given data converted to a String.</returns>
		public String ToCSV(Int64 lowBitIndex, Int64 highBitIndex, Boolean useHexFormat = false) {
			ValidateBitRange(nameof(lowBitIndex), lowBitIndex, nameof(highBitIndex), highBitIndex);
			UInt32[] masks = ToArray(lowBitIndex, highBitIndex);
			String[] maskStrings = new String[masks.Length];
			for (Int32 arrayIdx = 0; arrayIdx < masks.Length; arrayIdx++) {
				maskStrings[arrayIdx] = useHexFormat ? $"0x{masks[arrayIdx]:X8}" : masks[arrayIdx].ToString();
			}
			return String.Join(",", maskStrings);
		}

		/// <summary>Convert a string containing comma-separated integers into a <see cref="Mask"/>.</summary>
		/// <param name="masksCSV">The CSV string.</param>
		/// <param name="lowBitIndex">(Optional) The start bit index.</param>
		/// <param name="defBitState">(Optional) The default bit value.</param>
		/// <returns>The <see cref="Mask"/>.</returns>
		public static Mask FromCSV(String masksCSV, Int32 lowBitIndex = 0, Boolean defBitState = false) {
			ValidateBitIndex(nameof(lowBitIndex), lowBitIndex);
			String[] maskStrings = masksCSV.Split(',');
			UInt32[] masks = new UInt32[maskStrings.Length];
			for (Int32 arrayIdx = 0; arrayIdx < masks.Length; arrayIdx++) {
				masks[arrayIdx] = IntegerParser.ParseUInt32(maskStrings[arrayIdx]);
			}
			return FromArray(masks, lowBitIndex, defBitState);
		}

		/// <summary>Count bits which are set to a specific state within a range.</summary>
		/// <param name="bitValue">The bit value.</param>
		/// <returns>The total number of bits set to the specified bit value within the specified range.</returns>
		public Int32 CountBits(Boolean bitValue) {
			return CountBits(LowBitIndex, HighBitIndex, bitValue);
		}

		/// <summary>Count bits which are set to a specific state within a range.</summary>
		/// <param name="lowBitIndex">The start bit index.</param>
		/// <param name="highBitIndex">The end bit index.</param>
		/// <param name="bitValue">The bit value.</param>
		/// <returns>The total number of bits set to the specified bit value within the specified range.</returns>
		public Int32 CountBits(Int64 lowBitIndex, Int64 highBitIndex, Boolean bitValue) {
			ValidateBitRange(nameof(lowBitIndex), lowBitIndex, nameof(highBitIndex), highBitIndex);
			Int32 result = 0;
			// Need to normalize the lowBitMod and highBitMod to between 0 and 31.
			Int32 lowBitMod = (Int32)(((lowBitIndex % 32) + 32) % 32);
			Int32 highBitMod = (Int32)(((highBitIndex % 32) + 32) % 32);
			Int32 lowMaskIndex = (Int32)((lowBitIndex - lowBitMod) / 32);
			Int32 highMaskIndex = (Int32)((highBitIndex - highBitMod) / 32);
			for (Int32 maskIndex = lowMaskIndex; maskIndex <= highMaskIndex; maskIndex++) {
				if (!_masks.TryGetValue(maskIndex, out UInt32 mask)) {
					mask = DefaultBitState ? 0xFFFFFFFF : 0x0;
				}
				Int64 maskLowBitIndex = maskIndex * 32;
				if (maskLowBitIndex < lowBitIndex) {
					// This mask overlaps start of count region
					UInt32 highMask = 0xFFFFFFFF << (Int32)(lowBitIndex - maskLowBitIndex);
					UInt32 lowMask = ~highMask;
					// Mask out the bits which shouldn't be counted
					if (bitValue) {
						mask &= highMask;
					} else {
						mask |= lowMask;
					}
				}
				Int64 maskHighBitIndex = maskLowBitIndex + 31;
				if (maskHighBitIndex > highBitIndex) {
					// This mask overlaps end of count region
					UInt32 lowMask = 0xFFFFFFFF >> (Int32)(maskHighBitIndex - highBitIndex);
					UInt32 highMask = ~lowMask;
					// Mask out the bits which shouldn't be counted
					if (bitValue) {
						mask &= lowMask;
					} else {
						mask |= highMask;
					}
				}
				// Now count the bits
				if ((bitValue && (mask == 0xFFFFFFFF)) || (!bitValue && (mask == 0x0))) {
					result += 32;
				} else {
					for (Int32 bitIdx = 0; bitIdx < 32; bitIdx++) {
						UInt32 bitMask = (UInt32)(1 << bitIdx);
						if (bitValue == ((mask & bitMask) != 0)) {
							result++;
						}
					}
				}
			}
			return result;
		}
	}
}