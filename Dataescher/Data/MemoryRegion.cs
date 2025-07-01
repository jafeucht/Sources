// <copyright file="MemoryRegion.cs" company="Dataescher">
// 	Copyright (c) 2022-2024 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements the memory region class.</summary>

using System;

namespace Dataescher.Data {
	/// <summary>A memory region.</summary>
	/// <seealso cref="T:IComparable{Dataescher.Data.MemoryRegion}"/>
	public class MemoryRegion : IComparable<MemoryRegion> {
		/// <summary>The address.</summary>
		public UInt32 StartAddress { get; private set; }

		/// <summary>The address.</summary>
		public UInt32 EndAddress { get; private set; }

		/// <summary>The size.</summary>
		public Int64 Size { get; private set; }

		/// <summary>Gets a value indicating whether this memory region is empty.</summary>
		public Boolean Empty => Size == 0;

		/// <summary>Initializes a new instance of the <see cref="MemoryRegion"/> class.</summary>
		public MemoryRegion() {
			Size = 0;
			StartAddress = 0;
			EndAddress = 0;
		}

		/// <summary>Create a memory region with start and end address.</summary>
		/// <param name="startAddress">The start address.</param>
		/// <param name="endAddress">The end address.</param>
		/// <returns>A MemoryRegion.</returns>
		public static MemoryRegion FromStartAndEndAddresses(UInt32 startAddress, UInt32 endAddress) {
			if (startAddress > endAddress) {
				(endAddress, startAddress) = (startAddress, endAddress);
			}
			return new() {
				StartAddress = startAddress,
				EndAddress = endAddress,
				Size = (Int64)endAddress - startAddress + 1
			};
		}

		/// <summary>Query if this memory region contains the given address.</summary>
		/// <param name="address">The UInt32 to test for containment.</param>
		/// <returns>True if the object is in this collection, false if not.</returns>
		public Boolean Contains(UInt32 address) {
			return Size != 0 && address >= StartAddress && address <= EndAddress;
		}

		/// <summary>Create a memory region with start address and size.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <param name="startAddress">The start address.</param>
		/// <param name="size">The size.</param>
		/// <returns>A MemoryRegion.</returns>
		public static MemoryRegion FromStartAddressAndSize(UInt32 startAddress, Int64 size) {
			if (startAddress + size - 1 > UInt32.MaxValue) {
				throw new Exception("Memory region extends beyond 32-bit address space.");
			}
			if (size < 0) {
				throw new Exception("Size cannot be negative.");
			}
			MemoryRegion retval = new() {
				Size = size
			};
			if (size == 0) {
				retval.StartAddress = 0;
				retval.EndAddress = 0;
			} else {
				retval.StartAddress = startAddress;
				retval.EndAddress = (UInt32)(startAddress + size - 1);
			}
			return retval;
		}

		/// <summary>Query if this object intersects the given other.</summary>
		/// <param name="other">The other.</param>
		/// <returns>True if it succeeds, false if it fails.</returns>
		public Boolean Intersects(MemoryRegion other) {
			return !Empty && !other.Empty && other.StartAddress <= EndAddress && other.EndAddress >= StartAddress;
		}

		/// <summary>Query if this memory region overlaps the given other memory region.</summary>
		/// <param name="other">An object to compare with this instance.</param>
		/// <returns>True if it overlaps, false otherwise.</returns>
		public Boolean Overlaps(MemoryRegion other) {
			return !Empty && !other.Empty && other.EndAddress >= StartAddress && other.StartAddress <= EndAddress;
		}

		/// <summary>Query if this object contains the given other.</summary>
		/// <param name="other">The other.</param>
		/// <returns>True if it succeeds, false if it fails.</returns>
		public Boolean Contains(MemoryRegion other) {
			return !Empty && other.StartAddress >= StartAddress && other.EndAddress <= EndAddress;
		}

		/// <summary>Returns the fully qualified type name of this instance.</summary>
		/// <returns>The fully qualified type name.</returns>
		public override String ToString() {
			return Empty ? "(Empty)" : $"0x{StartAddress:X8}-0x{EndAddress:X8}: {Size} bytes";
		}

		/// <summary>
		///     Compares the current instance with another object of the same type and returns an integer that indicates
		///     whether the current instance precedes, follows, or occurs in the same position in the sort order as the other
		///     object.
		/// </summary>
		/// <param name="other">An object to compare with this instance.</param>
		/// <returns>
		///     A value that indicates the relative order of the objects being compared. The return value has these meanings:
		///     
		///     Value  
		///     
		///     Meaning  
		///     
		///     Less than zero  
		///     
		///     This instance precedes <paramref name="other" /> in the sort order.  
		///     
		///     Zero  
		///     
		///     This instance occurs in the same position in the sort order as <paramref name="other" />.  
		///     
		///     Greater than zero  
		///     
		///     This instance follows <paramref name="other" /> in the sort order.
		/// </returns>
		public Int32 CompareTo(MemoryRegion other) {
			if (other is null) {
				return -1;
			}
			Int32 result = Empty.CompareTo(other.Empty);
			if (result == 0) {
				result = StartAddress.CompareTo(other.StartAddress);
				if (result == 0) {
					result = EndAddress.CompareTo(other.EndAddress);
				}
			}
			return result;
		}
	}
}