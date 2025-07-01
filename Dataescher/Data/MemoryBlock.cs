// <copyright file="MemoryBlock.cs" company="Dataescher">
// 	Copyright (c) 2022-2024 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements a memory block.</summary>

using System;

namespace Dataescher.Data {
	/// <summary>A memory block.</summary>
	/// <seealso cref="T:IComparable{Dataescher.Data.MemoryBlock}"/>
	public class MemoryBlock : IComparable<MemoryBlock> {
		/// <summary>The data.</summary>
		public Memory Data { get; private set; }

		/// <summary>The memory region.</summary>
		private MemoryRegion _region;

		/// <summary>Gets the region defined by this memory block.</summary>
		/// <exception cref="ArgumentNullException">Thrown when one or more required arguments are null.</exception>
		public MemoryRegion Region {
			get => _region;
			private set {
				if (value is null) {
					throw new ArgumentNullException(nameof(value));
				}
				_region = value;
			}
		}

		/// <summary>Gets a value indicating whether the empty.</summary>
		public Boolean Empty => Region.Empty;

		/// <summary>Initializes a new instance of the Dataescher.MemoryBlock class.</summary>
		public MemoryBlock() {
			Data = new Memory(0);
			_region = MemoryRegion.FromStartAddressAndSize(0, Data.Length);
		}

		/// <summary>Initializes a new instance of the Dataescher.MemoryBlock class.</summary>
		/// <param name="region">The region.</param>
		public MemoryBlock(MemoryRegion region) {
			Data = new Memory(region.Size);
			_region = region;
		}

		/// <summary>Initializes a new instance of the Dataescher.MemoryBlock class.</summary>
		/// <param name="address">The address.</param>
		/// <param name="data">The data.</param>
		public MemoryBlock(UInt32 address, Byte[] data) {
			_region = MemoryRegion.FromStartAddressAndSize(address, data.Length);
			Data = new Memory(data);
		}

		/// <summary>Initializes a new instance of the Dataescher.MemoryBlock class.</summary>
		/// <param name="address">The address.</param>
		/// <param name="data">The data.</param>
		public MemoryBlock(UInt32 address, Memory data) {
			_region = MemoryRegion.FromStartAddressAndSize(address, data.Length);
			Data = data.Clone() as Memory;
		}

		/// <summary>Initializes a new instance of the Dataescher.MemoryBlock class.</summary>
		/// <param name="region">The region.</param>
		/// <param name="data">The data.</param>
		/// <param name="dataOffset">The data offset.</param>
		internal MemoryBlock(MemoryRegion region, Memory data, UInt32 dataOffset) {
			// TODO: Do some cross-checks to ensure we don't overrun a buffer
			_region = region;
			Data = new Memory(region.Size);
			Memory.Copy(data, dataOffset, Data, 0, region.Size);
		}

		/// <summary>Initializes a new instance of the Dataescher.MemoryBlock class.</summary>
		/// <exception cref="ArgumentOutOfRangeException">
		///     Thrown when one or more arguments are outside the required range.
		/// </exception>
		/// <param name="region">The region.</param>
		/// <param name="data">The data.</param>
		/// <param name="dataOffset">The data offset.</param>
		internal MemoryBlock(MemoryRegion region, Byte[] data, Int32 dataOffset) {
			if (region.Size > 0x7FFFFFFF) {
				throw new ArgumentOutOfRangeException(nameof(region), "Range size must be less than 0x80000000.");
			}
			_region = region;
			Data = new Memory(region.Size);
			Memory.Copy(data, dataOffset, Data, 0, (Int32)region.Size);
		}

		/// <summary>Indexer to get items within this collection using array index syntax.</summary>
		/// <param name="address">The address.</param>
		/// <returns>The indexed item.</returns>
		public Byte this[UInt32 address] => Region.Contains(address)
					? Data[address - Region.StartAddress]
					: throw new Exception($"Memory block does not contain address 0x{address:X8}");

		/// <summary>Sets a byte of data.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <param name="offset">The offset.</param>
		/// <param name="data">The data.</param>
		/// <param name="dataOffset">The data offset.</param>
		/// <param name="dataSize">Size of the data.</param>
		internal void SetData(UInt32 offset, Byte[] data, UInt32 dataOffset, Int64 dataSize) {
			if ((dataOffset + dataSize) > data.Length) {
				throw new Exception("Read past the end of the array.");
			}
			if ((offset + dataSize) > Region.Size) {
				throw new Exception("Attempted to write past end of data block.");
			}
			// Copy the appropriate memory to this memory block
			for (UInt32 dataIdx = 0; dataIdx < dataSize; dataIdx++) {
				Data[offset + dataIdx] = data[dataOffset + dataIdx];
			}
		}

		/// <summary>Sets a byte of data.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <param name="offset">The offset.</param>
		/// <param name="data">The data.</param>
		/// <param name="dataOffset">The data offset.</param>
		/// <param name="dataSize">Size of the data.</param>
		internal void SetData(UInt32 offset, Memory data, UInt32 dataOffset, Int64 dataSize) {
			if ((dataOffset + dataSize) > data.Length) {
				throw new Exception("Read past the end of the array.");
			}
			if ((offset + dataSize) > Region.Size) {
				throw new Exception("Attempted to write past end of data block.");
			}
			// Copy the appropriate memory to this memory block
			Memory.Copy(data, dataOffset, Data, offset, dataSize);
		}

		/// <summary>Returns a string that represents the current object.</summary>
		/// <returns>A string that represents the current object.</returns>
		public override String ToString() {
			return $"0x{Region.StartAddress:X8}-0x{Region.EndAddress:X8} ({Region.Size} bytes)";
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
		public Int32 CompareTo(MemoryBlock? other) {
			return ((IComparable<MemoryRegion>)Region).CompareTo(other?.Region);
		}

		/// <summary>Fills.</summary>
		/// <param name="region">The region.</param>
		/// <param name="fillData">The byte to fill.</param>
		internal void Fill(MemoryRegion region, Byte fillData) {
			UInt32 fillStartAddress = Math.Max(Region.StartAddress, region.StartAddress);
			UInt32 fillEndAddress = Math.Min(Region.EndAddress, region.EndAddress);
			for (UInt32 dataIdx = fillStartAddress - Region.StartAddress; dataIdx < (fillEndAddress - Region.StartAddress + 1); dataIdx++) {
				Data[dataIdx] = fillData;
			}
		}

		/// <summary>Clears this object to its blank/initial state.</summary>
		internal void Clear() {
			Data = new();
			Region = MemoryRegion.FromStartAddressAndSize(0, 0);
		}
	}
}