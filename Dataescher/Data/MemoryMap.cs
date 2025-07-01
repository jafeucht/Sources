// <copyright file="MemoryMap.cs" company="Dataescher">
// 	Copyright (c) 2022-2024 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements the memory map class.</summary>

using Dataescher.Collections;
using Dataescher.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dataescher.Data {
	/// <summary>Map of memory blocks.</summary>
	public class MemoryMap {
		/// <summary>(Immutable) The memory blocks.</summary>
		internal readonly BinaryMap<MemoryBlock> Blocks;

		/// <summary>The default value for bytes which are not implemented in this memory map.</summary>
		public Byte BlankData { get; set; }

		/// <summary>Gets the regions defined by this memory block.</summary>
		public MemoryRegionCollection Regions {
			get {
				MemoryRegionCollection retval = new();
				foreach (MemoryBlock memoryBlock in Blocks) {
					retval.Insert(memoryBlock.Region);
				}
				return retval;
			}
		}

		/// <summary>Gets the start address.</summary>
		public UInt32 StartAddress => Blocks.Count > 0 ? Blocks.First().Region.StartAddress : 0;

		/// <summary>Gets the end address.</summary>
		public UInt32 EndAddress => Blocks.Count > 0 ? Blocks.Last().Region.EndAddress : 0;

		/// <summary>Gets the span of the memory map.</summary>
		public UInt32 Span => EndAddress - StartAddress + 1;

		/// <summary>Gets the number of bytes implemented by this memory map.</summary>
		public Int64 Size {
			get {
				Int64 result = 0;
				foreach (MemoryBlock mb in Blocks) {
					result += mb.Region.Size;
				}
				return result;
			}
		}

		/// <summary>Clears this object to its blank/initial state.</summary>
		public void Clear() {
			Blocks.Clear();
		}

		/// <summary>Gets the number of blocks.</summary>
		public Int32 BlockCount {
			get {
				Organize();
				return Blocks.Count;
			}
		}

		/// <summary>Gets a value indicating whether this object is empty.</summary>
		public Boolean IsEmpty => BlockCount == 0;

		/// <summary>Keeps track of every time a new block is added, which might require re-organization.</summary>
		private Boolean _isOrganized;

		/// <summary>True to suppress organizing, false otherwise.</summary>
		private Boolean _suppressOrganize;

		/// <summary>True to suppress organizing, false otherwise.</summary>
		public Boolean SuppressOrganize {
			get => _suppressOrganize;
			set {
				if (_suppressOrganize != value) {
					_suppressOrganize = value;
					Organize();
				}
			}
		}

		/// <summary>Initializes a new instance of the Dataescher.MemoryMap class.</summary>
		public MemoryMap() {
			Blocks = new();
			BlankData = 0xFF;
			_isOrganized = true;
		}

		/// <summary>Inserts a section of data.</summary>
		/// <param name="address">The address.</param>
		/// <param name="data">The data.</param>
		public void Insert(UInt32 address, DataBuffer data) {
			Insert(address, data.Data, data.Implemented);
		}

		/// <summary>Inserts a section of data.</summary>
		/// <param name="address">The address.</param>
		/// <param name="data">The data.</param>
		public void Insert(UInt32 address, Memory data) {
			Int64 byteCount = Math.Min(data.Length, 0x100000000 - address);
			// Simply add the memory region
			Insert(MemoryRegion.FromStartAddressAndSize(address, byteCount), data, 0);
			Organize();
		}

		/// <summary>Inserts a section of data.</summary>
		/// <param name="address">The address.</param>
		/// <param name="data">The data.</param>
		/// <param name="impMask">The implemented bit mask.</param>
		public void Insert(UInt32 address, Memory data, Mask impMask) {
			Int64 byteCount = Math.Min(data.Length, 0x100000000 - address);
			UInt32 lastAddress = (UInt32)(address + byteCount - 1);
			// Add only the implemented regions
			Boolean inImpRegion = false;
			UInt32 impStart = 0;
			for (UInt32 curAddress = address; curAddress <= lastAddress; curAddress++) {
				if (impMask[curAddress - address]) {
					if (!inImpRegion) {
						inImpRegion = true;
						impStart = curAddress;
					}
				} else {
					if (inImpRegion) {
						inImpRegion = false;
						Int64 impSize = curAddress - impStart + 1;
						// Add this region
						Insert(MemoryRegion.FromStartAddressAndSize(impStart, impSize), data, address - impStart);
					}
				}
			}
			if (inImpRegion) {
				Int64 impSize = lastAddress - impStart + 1;
				// Add this region
				Insert(MemoryRegion.FromStartAddressAndSize(impStart, impSize), data, address - impStart);
			}
			Organize();
		}

		/// <summary>Inserts a section of data.</summary>
		/// <param name="address">The address.</param>
		/// <param name="data">The data.</param>
		/// <param name="impMask">(Optional) The implemented bit mask.</param>
		public void Insert(UInt32 address, Byte[] data, Byte[] impMask = null) {
			Insert(MemoryRegion.FromStartAddressAndSize(address, data.Length), ref data, 0);
			Organize();
			if (impMask is not null) {
				Int64 byteCount = Math.Min(data.Length, impMask.Length * 8);
				Int32 impMaskByteCnt = (Int32)((byteCount + 7) / 8);
				Boolean inNotImpRegion = false;
				UInt32 impStartAddr = 0;
				UInt32 impEndAddr = 0;

				for (Int32 impMaskByteIdx = 0; impMaskByteIdx < impMaskByteCnt; impMaskByteIdx++) {
					Byte impMaskByte = impMask[impMaskByteIdx];
					for (Int32 impMaskBitIdx = 0; impMaskBitIdx < 8; impMaskBitIdx++) {
						if ((impMaskByte & (1 << impMaskBitIdx)) == 0) {
							if (!inNotImpRegion) {
								impStartAddr = address;
								inNotImpRegion = true;
							}
							impEndAddr = address;
						} else if (inNotImpRegion) {
							// Imp the section
							Delete(MemoryRegion.FromStartAddressAndSize(impStartAddr, impEndAddr - impStartAddr + 1));
							inNotImpRegion = false;
						}
						address++;
						byteCount--;
						if (byteCount == 0) {
							break;
						}
					}
					if (byteCount == 0) {
						break;
					}
				}
				if (inNotImpRegion) {
					// Imp the last region
					Delete(MemoryRegion.FromStartAndEndAddresses(impStartAddr, impEndAddr));
				}
			}
		}

		/// <summary>Inserts a part of another memory map.</summary>
		/// <param name="other">The other memory map.</param>
		/// <param name="region">The region.</param>
		public void Insert(MemoryMap other, MemoryRegion region) {
			// TODO: This could use optimization
			foreach (MemoryBlock block in other.Blocks) {
				MemoryRegion blockRegion = block.Region;
				if (blockRegion.Overlaps(region)) {
					if (region.StartAddress <= blockRegion.StartAddress) {
						UInt32 offset = blockRegion.StartAddress - region.StartAddress;
						UInt32 size = (UInt32)Math.Min(region.Size - offset, blockRegion.Size);
						Insert(blockRegion.StartAddress, block.Data, 0, size);
					} else { // region.StartAddress > block.Region.StartAddress
						UInt32 offset = region.StartAddress - blockRegion.StartAddress;
						UInt32 size = (UInt32)Math.Min(blockRegion.EndAddress - region.StartAddress + 1, region.Size);
						Insert(region.StartAddress, block.Data, offset, size);
					}
				}
			}
		}

		/// <summary>Get a list of memory regions which intersect a specified memory region.</summary>
		/// <param name="region">The region.</param>
		/// <returns>A list of memory regions.</returns>
		public MemoryRegionCollection IntersectRegions(MemoryRegion region) {
			MemoryRegionCollection intersectRegions = new();
			MemoryRegionCollection currentRegions = Regions;
			foreach (MemoryRegion thisRegion in currentRegions) {
				if (thisRegion.Intersects(region)) {
					intersectRegions.Insert(MemoryRegion.FromStartAndEndAddresses(Math.Max(thisRegion.StartAddress, region.StartAddress), Math.Min(thisRegion.EndAddress, region.EndAddress)));
				}
			}
			return intersectRegions;
		}

		/// <summary>Get a list of implemented memory regions which do not intersect the specified region.</summary>
		/// <param name="region">The region.</param>
		/// <returns>A list of memory regions.</returns>
		public MemoryRegionCollection NonIntersectRegions(MemoryRegion region) {
			MemoryRegionCollection currentRegions = Regions;
			currentRegions.Delete(region);
			return currentRegions;
		}

		/// <summary>Offset all data up or down by a specified number of bytes.</summary>
		/// <param name="offset">The offset.</param>
		/// <param name="moveUp">True to move up, otherwise, move down.</param>
		public void OffsetAllData(UInt32 offset, Boolean moveUp) {
			MemoryRegionCollection intersectRegions = moveUp
				? IntersectRegions(MemoryRegion.FromStartAndEndAddresses(0, offset - 1))
				: IntersectRegions(MemoryRegion.FromStartAndEndAddresses(0xFFFFFFFF - offset, 0xFFFFFFFF));
			foreach (MemoryRegion intersectRegion in intersectRegions) {
				Delete(intersectRegion);
			}
			List<MemoryBlock> newBlocks = new();
			foreach (MemoryBlock block in Blocks) {
				UInt32 newAddress = moveUp ? block.Region.StartAddress - offset : block.Region.StartAddress + offset;
				newBlocks.Add(new MemoryBlock(newAddress, block.Data));
			}
			Blocks.Clear();
			foreach (MemoryBlock block in newBlocks) {
				Blocks.Add(block.Region.StartAddress, block);
			}
		}

		/// <summary>Inserts a section of data, keeping the memory map organized.</summary>
		/// <exception cref="ArgumentOutOfRangeException">
		///     Thrown when one or more arguments are outside the required range.
		/// </exception>
		/// <param name="address">The address.</param>
		/// <param name="data">The data.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="size">The size.</param>
		public void Insert(UInt32 address, Memory data, UInt32 offset, Int64 size) {
			if ((size + offset) > data.Length) {
				throw new ArgumentOutOfRangeException(nameof(data), "Length and offset parameters exceed data length.");
			}
			Insert(MemoryRegion.FromStartAddressAndSize(address, size), data, offset);
		}

		/// <summary>Inserts a section of data, keeping the memory map organized.</summary>
		/// <exception cref="ArgumentOutOfRangeException">
		///     Thrown when one or more arguments are outside the required range.
		/// </exception>
		/// <param name="address">The address.</param>
		/// <param name="data">The data.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="size">The size.</param>
		public void Insert(UInt32 address, Byte[] data, Int32 offset, Int32 size) {
			if ((size + offset) > data.Length) {
				throw new ArgumentOutOfRangeException(nameof(data), "Length and offset parameters exceed data length.");
			}
			Insert(MemoryRegion.FromStartAddressAndSize(address, size), ref data, offset);
		}

		/// <summary>Inserts a section of data.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <param name="address">The address.</param>
		/// <param name="data">The data.</param>
		public void Insert(UInt32 address, Object data) {
			if (data is null) {
				return;
			}
			TypeConverter converter = DataType.LookupConverter(data.GetType());
			if (converter is null) {
				throw new Exception($"A binary converter does not exist for type {data.GetType().FullName}.");
			}
			Byte[] byteArray = converter.ToByteFunc(data);
			Insert(address, byteArray);
		}

		/// <summary>Inserts a memory block.</summary>
		/// <param name="block">The memory block to add.</param>
		public void Insert(MemoryBlock block) {
			Insert(block.Region.StartAddress, block.Data);
		}
		/// <summary>Inserts a section of data, keeping the memory map organized.</summary>
		/// <param name="region">The address region.</param>
		/// <param name="data">The data.</param>
		/// <param name="dataOffset">The data offset.</param>
		private void Insert(MemoryRegion region, Memory data, UInt32 dataOffset) {
			List<MemoryBlock> removeBlocks = new();
			MemoryRegion thisRegion = region;
			// Look up the appropriate memory data node
			if (!Blocks.TryGetNode(region.StartAddress, BinaryMapLookupMode.ExactOrPreviousKey, out BinaryMapNode<MemoryBlock> memoryBlockNode)) {
				Blocks.TryGetNode(region.StartAddress, BinaryMapLookupMode.ExactOrNextKey, out memoryBlockNode);
			}
			while ((memoryBlockNode is not null) && (memoryBlockNode.Value is MemoryBlock otherBlock)) {
				MemoryRegion otherRegion = otherBlock.Region;
				if (otherRegion.EndAddress < thisRegion.StartAddress) {
					// Condition 1: No intersection. Do nothing.
				} else if (thisRegion.EndAddress < otherRegion.StartAddress) {
					// Condition 2: No intersection. Do nothing.
					break;
				} else if (thisRegion.StartAddress < otherRegion.StartAddress) {
					if (otherRegion.EndAddress > thisRegion.EndAddress) {
						// Condition 3. Partial overlap at front of other block. Overwrite the data in the other block.
						otherBlock.SetData(0, data, dataOffset + (otherRegion.StartAddress - thisRegion.StartAddress), thisRegion.EndAddress - otherRegion.StartAddress + 1);
						thisRegion = MemoryRegion.FromStartAddressAndSize(thisRegion.StartAddress, otherRegion.StartAddress - thisRegion.StartAddress);
					} else {
						// Condition 4. Total overlap. Remove the old memory region
						removeBlocks.Add(otherBlock);
					}
				} else if (thisRegion.EndAddress > otherRegion.EndAddress) {
					// Condition 5: Partial overlap at end of other block. Overwrite the data in the other block.
					otherBlock.SetData(thisRegion.StartAddress - otherRegion.StartAddress, data, dataOffset, otherRegion.EndAddress - thisRegion.StartAddress + 1);
					dataOffset += otherRegion.EndAddress - thisRegion.StartAddress + 1;
					thisRegion = MemoryRegion.FromStartAddressAndSize(otherRegion.EndAddress + 1, thisRegion.EndAddress - otherRegion.EndAddress);
				} else {
					// Condition 6: Total overlap.
					otherBlock.SetData(thisRegion.StartAddress - otherRegion.StartAddress, data, dataOffset, (UInt32)thisRegion.Size);
					// No more data to add. We can exit the loop at this point
					thisRegion = null;
					break;
				}
				memoryBlockNode = memoryBlockNode.NextNode;
			}
			// Remove any blocks marked for removal
			foreach (MemoryBlock removeBlock in removeBlocks) {
				Blocks.Remove(removeBlock.Region.StartAddress);
			}
			if (thisRegion is not null) {
				Blocks.Add(thisRegion.StartAddress, new MemoryBlock(thisRegion, data, dataOffset));
				_isOrganized = false;
			}
		}

		/// <summary>Inserts a section of data, keeping the memory map organized.</summary>
		/// <param name="region">The address region.</param>
		/// <param name="data">[in,out] The data.</param>
		/// <param name="dataOffset">The data offset.</param>
		private void Insert(MemoryRegion region, ref Byte[] data, Int32 dataOffset) {
			List<MemoryBlock> removeBlocks = new();
			MemoryRegion thisRegion = region;
			// Look up the appropriate memory data node
			if (!Blocks.TryGetNode(region.StartAddress, BinaryMapLookupMode.ExactOrPreviousKey, out BinaryMapNode<MemoryBlock> memoryBlockNode)) {
				Blocks.TryGetNode(region.StartAddress, BinaryMapLookupMode.ExactOrNextKey, out memoryBlockNode);
			}
			while ((memoryBlockNode is not null) && (memoryBlockNode.Value is MemoryBlock otherBlock)) {
				MemoryRegion otherRegion = otherBlock.Region;
				if (otherRegion.EndAddress < thisRegion.StartAddress) {
					// Condition 1: No intersection. Do nothing.
				} else if (thisRegion.EndAddress < otherRegion.StartAddress) {
					// Condition 2: No intersection. Break since we've already processed intersecting regions.
					break;
				} else if (thisRegion.StartAddress < otherRegion.StartAddress) {
					if (otherRegion.EndAddress > thisRegion.EndAddress) {
						// Condition 3. Partial overlap at front of other block. Overwrite the data in the other block.
						otherBlock.SetData(0, data, (UInt32)dataOffset + (otherRegion.StartAddress - thisRegion.StartAddress), thisRegion.EndAddress - otherRegion.StartAddress + 1);
						thisRegion = MemoryRegion.FromStartAddressAndSize(thisRegion.StartAddress, otherRegion.StartAddress - thisRegion.StartAddress);
					} else {
						// Condition 4. Total overlap. Remove the old memory region
						removeBlocks.Add(otherBlock);
					}
				} else if (thisRegion.EndAddress > otherRegion.EndAddress) {
					// Condition 5: Partial overlap at end of other block. Overwrite the data in the other block.
					otherBlock.SetData(thisRegion.StartAddress - otherRegion.StartAddress, data, (UInt32)dataOffset, otherRegion.EndAddress - thisRegion.StartAddress + 1);
					dataOffset += (Int32)(otherRegion.EndAddress - thisRegion.StartAddress + 1);
					thisRegion = MemoryRegion.FromStartAddressAndSize(otherRegion.EndAddress + 1, thisRegion.EndAddress - otherRegion.EndAddress);
				} else {
					// Condition 6: Total overlap.
					otherBlock.SetData(thisRegion.StartAddress - otherRegion.StartAddress, data, (UInt32)dataOffset, (UInt32)thisRegion.Size);
					// No more data to add. We can exit the loop at this point
					thisRegion = null;
					break;
				}
				memoryBlockNode = memoryBlockNode.NextNode;
			}
			// Remove any blocks marked for removal
			foreach (MemoryBlock removeBlock in removeBlocks) {
				Blocks.Remove(removeBlock.Region.StartAddress);
			}
			if (thisRegion is not null) {
				Blocks.Add(thisRegion.StartAddress, new MemoryBlock(thisRegion, data, dataOffset));
				_isOrganized = false;
			}
		}

		/// <summary>Fetches a memory block providing the memory region.</summary>
		/// <param name="region">The region to fetch.</param>
		/// <returns>A MemoryBlock.</returns>
		public MemoryBlock Fetch(MemoryRegion region) {
			// Sort and defragment the memory first
			Organize();
			MemoryBlock retval = new(region);
			retval.Fill(region, BlankData);
			UInt32 currentAddress = region.StartAddress;
			// Look up the appropriate memory data node
			if (!Blocks.TryGetNode(region.StartAddress, BinaryMapLookupMode.ExactOrPreviousKey, out BinaryMapNode<MemoryBlock> memoryBlockNode)) {
				Blocks.TryGetNode(region.StartAddress, BinaryMapLookupMode.ExactOrNextKey, out memoryBlockNode);
			}
			while ((memoryBlockNode is not null) && (memoryBlockNode.Value is MemoryBlock block)) {
				if (block.Region.Overlaps(region)) {
					// Fill in blank data
					if (block.Region.StartAddress > currentAddress) {
						retval.Fill(MemoryRegion.FromStartAddressAndSize(currentAddress, block.Region.StartAddress - currentAddress), BlankData);
					}
					// This has a problem if region starts in middle of block.Region
					if (currentAddress > block.Region.StartAddress) {
						retval.SetData(
							currentAddress - retval.Region.StartAddress,
							block.Data,
							currentAddress - block.Region.StartAddress,
							Math.Min(block.Region.EndAddress, retval.Region.EndAddress) - currentAddress + 1
						);
					} else {
						retval.SetData(
							block.Region.StartAddress - retval.Region.StartAddress,
							block.Data,
							0,
							Math.Min(block.Region.EndAddress, retval.Region.EndAddress) - block.Region.StartAddress + 1
						);
					}
					currentAddress = block.Region.EndAddress + 1;
					if (currentAddress > retval.Region.EndAddress) {
						break;
					}
				}
				if (currentAddress > retval.Region.EndAddress) {
					break;
				}
				memoryBlockNode = memoryBlockNode.NextNode;
			}
			// Fill any data after the last region
			if (Blocks.IsEmpty) {
				retval.Fill(region, BlankData);
			} else {
				MemoryBlock lastBlock = Blocks.Last();
				if (lastBlock.Region.EndAddress < region.StartAddress) {
					retval.Fill(region, BlankData);
				} else if (lastBlock.Region.EndAddress < region.EndAddress) {
					retval.Fill(MemoryRegion.FromStartAddressAndSize(lastBlock.Region.EndAddress + 1, retval.Region.EndAddress - lastBlock.Region.EndAddress), BlankData);
				}
			}
			return retval;
		}

		/// <summary>Get a memory map which contains only a portion of the memory as defined by a memory region.</summary>
		/// <param name="region">The memory region.</param>
		/// <returns>A MemoryMap which contains only data specified by region.</returns>
		public MemoryMap Filter(MemoryRegion region) {
			MemoryMap result = new();
			result.Insert(this, region);
			return result;
		}

		/// <summary>Query if this memory map overlaps the given region.</summary>
		/// <param name="region">The region to check.</param>
		/// <returns>True if overlap is detected, false otherwise.</returns>
		public Boolean Overlaps(MemoryRegion region) {
			if (Blocks.TryGetValue(region.StartAddress, BinaryMapLookupMode.ExactOrPreviousKey, out MemoryBlock prevBlock)) {
				if (prevBlock.Region.Overlaps(region)) {
					return true;
				}
			}
			if (Blocks.TryGetValue(region.StartAddress, BinaryMapLookupMode.ExactOrNextKey, out MemoryBlock nextBlock)) {
				if (nextBlock.Region.Overlaps(region)) {
					return true;
				}
			}
			return false;
		}

		/// <summary>Sort and defragment the memory blocks.</summary>
		internal void Organize() {
			if (_isOrganized || _suppressOrganize) {
				return;
			}
			_isOrganized = true;
			if (Blocks.Count <= 1) {
				// Nothing to do
				return;
			}
			// Merge blocks which are adjacent
			BinaryMapNode<MemoryBlock> firstNode = Blocks.FirstNode();
			while ((firstNode is not null) && (firstNode.Value is MemoryBlock firstBlock)) {
				MemoryRegion mergeRegion = null;
				List<MemoryBlock> mergeBlocks = new();
				BinaryMapNode<MemoryBlock> lastNode = firstNode;
				MemoryBlock lastBlock = firstBlock;
				// Figure out how many consecutive memory regions we can merge. Merging them all
				// at the same time is a lot faster than merging them one at a time.
				while (true) {
					MemoryBlock prevBlock = lastBlock;
					lastNode = lastNode.NextNode;
					if ((lastNode is null) || (lastNode.Value is not MemoryBlock newLastBlock)) {
						break;
					}
					lastBlock = newLastBlock;
					if (lastBlock.Region.StartAddress == (prevBlock.Region.EndAddress + 1)) {
						mergeRegion = MemoryRegion.FromStartAddressAndSize(firstBlock.Region.StartAddress, (Int64)lastBlock.Region.EndAddress - firstBlock.Region.StartAddress + 1);
						mergeBlocks.Add(lastBlock);
					} else {
						break;
					}
				}
				if (mergeRegion is not null) {
					// Create a new array which merges all the data from the consecutive memory regions
					MemoryBlock mergedBlock = new(mergeRegion);
					mergedBlock.SetData(0, firstBlock.Data, 0, firstBlock.Region.Size);
					foreach (MemoryBlock mergeBlock in mergeBlocks) {
						mergedBlock.SetData(mergeBlock.Region.StartAddress - mergeRegion.StartAddress, mergeBlock.Data, 0, mergeBlock.Region.Size);
						Blocks.Remove(mergeBlock.Region.StartAddress);
					}
					Blocks.Remove(firstBlock.Region.StartAddress);
					// Replace the first block with the new block
					Blocks.Add(mergedBlock.Region.StartAddress, mergedBlock);
				}
				firstNode = lastNode;
			}
		}

		/// <summary>Query if this object contains the given address.</summary>
		/// <param name="address">The address.</param>
		/// <returns>True if the memory map implements the address, false otherwise.</returns>
		public Boolean Contains(UInt32 address) {
			// Look up the appropriate memory data node
			if (Blocks.TryGetNode(address, BinaryMapLookupMode.ExactOrPreviousKey, out BinaryMapNode<MemoryBlock> memoryBlockNode)) {
				if (memoryBlockNode.Value is MemoryBlock block) {
					if (block.Region.StartAddress <= address) {
						if (block.Region.EndAddress >= address) {
							return true;
						}
					}
				}
			}
			return false;
		}

		/// <summary>Deletes multiple memory regions (optimized).</summary>
		/// <param name="deleteRegions">The delete regions to delete.</param>
		public void Delete(MemoryRegionCollection deleteRegions) {
			if (deleteRegions is null || deleteRegions.Count == 0 || (Blocks.Count == 0)) {
				return;
			}
			Dictionary<UInt32, MemoryBlock> removeBlocks = new();
			Dictionary<UInt32, MemoryBlock> addBlocks = new();
			UInt32 firstDeleteRegionStartAddress = deleteRegions.First().StartAddress;
			// Look up the appropriate memory data node
			if (!Blocks.TryGetNode(firstDeleteRegionStartAddress, BinaryMapLookupMode.ExactOrPreviousKey, out BinaryMapNode<MemoryBlock> memoryBlockNode)) {
				if (!Blocks.TryGetNode(firstDeleteRegionStartAddress, BinaryMapLookupMode.ExactOrNextKey, out memoryBlockNode)) {
					return;
				}
			}
			Int32 curDeleteRegionIdx = 0;
			MemoryRegion curDeleteRegion = deleteRegions[0];
			Int32 lastDeleteRegionIdx = deleteRegions.Count - 1;
			MemoryRegion lastDeleteRegion = deleteRegions[lastDeleteRegionIdx];
			while ((memoryBlockNode is not null) && (memoryBlockNode.Value is MemoryBlock existingBlock)) {
				MemoryRegion existingRegion = existingBlock.Region;
				if (existingRegion.StartAddress > lastDeleteRegion.EndAddress) {
					break;
				}
				if (existingRegion.EndAddress < curDeleteRegion.StartAddress) {
					// This existing region is before the current delete region
					memoryBlockNode = memoryBlockNode.NextNode;
					continue;
				}
				Int64 curAddress = existingRegion.StartAddress;
				Boolean existingRegionRemoved = false;
				Boolean deleteRegionComplete = false;
				do {
					MemoryRegion nextDeleteRegion;
					// Find the next delete region with end address >= current address
					while (curDeleteRegion.EndAddress < curAddress) {
						curDeleteRegionIdx++;
						if (curDeleteRegionIdx > lastDeleteRegionIdx) {
							// Last delete region has been processed.
							curDeleteRegionIdx = lastDeleteRegionIdx;
							deleteRegionComplete = true;
							break;
						}
						curDeleteRegion = deleteRegions[curDeleteRegionIdx];
					}
					if (deleteRegionComplete) {
						break;
					}
					// At this point, we know deleteRegion.EndAddress >= curAddress
					// Check that the delete region even intersects the existing region
					if (curDeleteRegion.StartAddress > existingRegion.EndAddress) {
						// There is no overlap. Continue to the next region.
						break;
					}
					// Confirmed intersection. Add the existing block to a list of blocks which should be deleted.
					if (!existingRegionRemoved) {
						removeBlocks.Add(existingRegion.StartAddress, existingBlock);
						existingRegionRemoved = true;
					}
					// Check if the region should be removed entirely.
					if ((curDeleteRegion.StartAddress <= existingRegion.StartAddress) && (curDeleteRegion.EndAddress >= existingRegion.EndAddress)) {
						// Complete overlap of existing region by delete region. Continue to the next existing region.
						break;
					}
					// Check if the delete block does not overlap the start of the existing region
					if (curAddress < curDeleteRegion.StartAddress) {
						addBlocks.Add((UInt32)curAddress, Fetch(MemoryRegion.FromStartAndEndAddresses((UInt32)curAddress, curDeleteRegion.StartAddress - 1)));
					}
					curAddress = (Int64)curDeleteRegion.EndAddress + 1;
					// Check of the delete block does not overlap the end of the existing region
					if ((curAddress > curDeleteRegion.EndAddress) && (curAddress < existingRegion.EndAddress)) {
						UInt32 nextAddress;
						Int32 nextDeleteRegionIdx = curDeleteRegionIdx + 1;
						if (nextDeleteRegionIdx <= lastDeleteRegionIdx) {
							nextDeleteRegion = deleteRegions[nextDeleteRegionIdx];
							if (nextDeleteRegion.StartAddress <= existingRegion.EndAddress) {
								// The next delete region overlaps this existing region, too.
								nextAddress = nextDeleteRegion.StartAddress - 1;
							} else {
								nextAddress = existingRegion.EndAddress;
							}
						} else {
							nextAddress = existingRegion.EndAddress;
						}
						addBlocks.Add((UInt32)curAddress, Fetch(MemoryRegion.FromStartAndEndAddresses((UInt32)curAddress, nextAddress)));
						curAddress = (Int64)nextAddress + 1;
					}
				} while (curAddress <= existingRegion.EndAddress);
				memoryBlockNode = memoryBlockNode.NextNode;
			}
			// Figure out which portions of the removed regions need to be added back
			foreach (MemoryBlock removeBlock in removeBlocks.Values) {
				Blocks.Remove(removeBlock.Region.StartAddress);
			}
			foreach (MemoryBlock addBlock in addBlocks.Values) {
				Blocks.Add(addBlock.Region.StartAddress, addBlock);
			}
		}

		/// <summary>Deletes the given region.</summary>
		/// <param name="deleteRegion">The region to delete.</param>
		public void Delete(MemoryRegion deleteRegion) {
			MemoryRegionCollection deleteRegions = new();
			deleteRegions.Insert(deleteRegion);
			Delete(deleteRegions);
		}

		/// <summary>Crops to the specified regions.</summary>
		/// <param name="cropRegions">The crop regions.</param>
		public void Crop(MemoryRegionCollection cropRegions) {
			cropRegions.Invert();
			Delete(cropRegions);
		}

		/// <summary>Crops to the specified region.</summary>
		/// <param name="cropRegion">The crop region.</param>
		public void Crop(MemoryRegion cropRegion) {
			MemoryRegionCollection cropRegions = new();
			cropRegions.Insert(cropRegion);
			cropRegions.Invert();
			Delete(cropRegions);
		}

		/// <summary>Get a bit mask of implemented bits.</summary>
		/// <param name="region">The region for which to produce the bit mask.</param>
		/// <returns>The bit mask array.</returns>
		public Mask ImplementedMask(MemoryRegion region) {
			Mask retval = new(false);
			// Look up the appropriate memory data node
			if (!Blocks.TryGetNode(region.StartAddress, BinaryMapLookupMode.ExactOrPreviousKey, out BinaryMapNode<MemoryBlock> memoryBlockNode)) {
				Blocks.TryGetNode(region.StartAddress, BinaryMapLookupMode.ExactOrNextKey, out memoryBlockNode);
			}
			while (memoryBlockNode is not null) {
				if (memoryBlockNode.Value is MemoryBlock block) {
					if (block.Region.StartAddress > region.EndAddress) {
						break;
					}
					if (block.Region.Overlaps(region)) {
						// Only look at overlapping region
						UInt32 startAddr = Math.Max(region.StartAddress, block.Region.StartAddress);
						UInt32 endAddr = Math.Min(region.EndAddress, block.Region.EndAddress);
						for (Int64 addr = startAddr; addr <= endAddr; addr++) {
							retval[addr - region.StartAddress] = true;
						}
					}
				}
				memoryBlockNode = memoryBlockNode.NextNode;
			}
			return retval;
		}

		/// <summary>Returns a string that represents the current object.</summary>
		/// <returns>A string that represents the current object.</returns>
		public override String ToString() {
			Organize();
			StringBuilder sb = new();
			foreach (MemoryBlock memoryBlock in Blocks) {
				sb.AppendLine(memoryBlock.ToString());
			}
			return sb.ToString();
		}

		/// <summary>Indexer to get items within this collection using array index syntax.</summary>
		/// <param name="address">The address.</param>
		/// <returns>The indexed item.</returns>
		public Byte this[UInt32 address] =>
				// Look up the appropriate memory data node
				!Blocks.TryGetNode(address, BinaryMapLookupMode.ExactOrPreviousKey, out BinaryMapNode<MemoryBlock> memoryBlockNode)
					? BlankData
					: (memoryBlockNode is not null) && (memoryBlockNode.Value is MemoryBlock block) && block.Region.Contains(address)
						? block[address]
						: BlankData;
	}
}