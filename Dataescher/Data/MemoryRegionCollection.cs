// <copyright file="MemoryRegionCollection.cs" company="Dataescher">
// Copyright (c) 2024 Dataescher. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>11/2/2024</date>
// <summary>Implements the memory region collection class</summary>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Dataescher.Data {
	/// <summary>Collection of memory regions.</summary>
	/// <seealso cref="T:System.Collections.Generic.IEnumerable{Dataescher.Data.MemoryRegion}"/>
	public class MemoryRegionCollection : IEnumerable<MemoryRegion> {
		/// <summary>The regions.</summary>
		private SortedList<UInt32, MemoryRegion> regions;

		/// <summary>Initializes a new instance of the <see cref="MemoryRegionCollection"/> class.</summary>
		public MemoryRegionCollection() {
			regions = new();
		}

		/// <summary>Gets the number of regions in this collection.</summary>
		public Int32 Count => regions.Count;

		/// <summary>Gets the first memory region in this collection.</summary>
		/// <returns>The first memory region.</returns>
		public MemoryRegion First() {
			return regions.Count == 0 ? null : regions.First().Value;
		}

		/// <summary>Gets the last memory region in this collection.</summary>
		/// <returns>The last memory region.</returns>
		public MemoryRegion Last() {
			return regions.Count == 0 ? null : regions.Last().Value;
		}

		/// <summary>Adds region.</summary>
		/// <param name="region">The region to add.</param>
		public void Insert(MemoryRegion region) {
			// Find any existing region
			if (regions.TryGetValue(region.StartAddress, out MemoryRegion existingRegion)) {
				// Another region at the start address exists
				if (existingRegion.EndAddress >= region.EndAddress) {
					// The existing regions do not need to be altered since there is complete overlap.
					return;
				}
				regions.Remove(region.StartAddress);
			}
			regions.Add(region.StartAddress, region);
		}

		/// <summary>Indexer to get items within this collection using array index syntax.</summary>
		/// <param name="index">Zero-based index of the entry to access.</param>
		/// <returns>The indexed item.</returns>
		public MemoryRegion this[Int32 index] => regions.Values[index];

		/// <summary>Deletes the given region.</summary>
		/// <param name="deleteRegion">The region to delete.</param>
		public void Delete(MemoryRegion deleteRegion) {
			MemoryRegionCollection removeRegions = new();
			MemoryRegionCollection addRegions = new();
			foreach (MemoryRegion existingRegion in this) {
				if (existingRegion.EndAddress < deleteRegion.StartAddress) {
					continue;
				}
				if (existingRegion.StartAddress > deleteRegion.EndAddress) {
					break;
				}
				// Add region to remove list
				removeRegions.Insert(existingRegion);
				if (existingRegion.StartAddress >= deleteRegion.StartAddress) {
					if (existingRegion.EndAddress > deleteRegion.EndAddress) {
						// Deleting only the first part of the existing region. Save the last part.
						addRegions.Insert(MemoryRegion.FromStartAndEndAddresses(deleteRegion.EndAddress + 1, existingRegion.EndAddress));
					}
				} else {
					// Overlap of only the first portion. Truncate the existing region.
					addRegions.Insert(MemoryRegion.FromStartAndEndAddresses(existingRegion.StartAddress, deleteRegion.StartAddress - 1));
					if (existingRegion.EndAddress > deleteRegion.EndAddress) {
						// No overlap of the last portion. Need to split existing region in two
						addRegions.Insert(MemoryRegion.FromStartAndEndAddresses(deleteRegion.EndAddress + 1, existingRegion.EndAddress));
					}
				}
			}
			foreach (MemoryRegion removeRegion in removeRegions) {
				regions.Remove(removeRegion.StartAddress);
			}
			foreach (MemoryRegion addRegion in addRegions) {
				regions.Add(addRegion.StartAddress, addRegion);
			}
		}

		/// <summary>Crops to the given region.</summary>
		/// <param name="cropRegion">The region to crop to.</param>
		public void Crop(MemoryRegion cropRegion) {
			MemoryRegionCollection removeRegions = new();
			MemoryRegionCollection addRegions = new();
			foreach (MemoryRegion existingRegion in this) {
				if (existingRegion.EndAddress < cropRegion.StartAddress) {
					removeRegions.Insert(existingRegion);
					continue;
				}
				if (existingRegion.EndAddress > cropRegion.EndAddress) {
					removeRegions.Insert(existingRegion);
					continue;
				}
				if (existingRegion.StartAddress >= cropRegion.StartAddress) {
					if (existingRegion.EndAddress > cropRegion.EndAddress) {
						// Deleting only the first part of the existing region. Save the last part.
						removeRegions.Insert(existingRegion);
						addRegions.Insert(MemoryRegion.FromStartAndEndAddresses(existingRegion.StartAddress, cropRegion.EndAddress));
					}
				} else {
					// Overlap of only the first portion. Truncate the existing region.
					removeRegions.Insert(existingRegion);
					if (existingRegion.EndAddress > cropRegion.EndAddress) {
						addRegions.Insert(MemoryRegion.FromStartAndEndAddresses(cropRegion.StartAddress, cropRegion.EndAddress));
					} else {
						addRegions.Insert(MemoryRegion.FromStartAndEndAddresses(cropRegion.StartAddress, existingRegion.EndAddress));
					}
				}
			}
			foreach (MemoryRegion removeRegion in removeRegions) {
				regions.Remove(removeRegion.StartAddress);
			}
			foreach (MemoryRegion addRegion in addRegions) {
				regions.Add(addRegion.StartAddress, addRegion);
			}
		}

		/// <summary>Organizes this object.</summary>
		public void Organize() {
			List<MemoryRegion> removeRegions = new();
			MemoryRegion firstRegion = null;
			MemoryRegion lastRegion = null;
			MemoryRegion finalRegion = regions.Last().Value;
			foreach (MemoryRegion region in regions.Values) {
				Boolean isFinal = region == finalRegion;
				if (firstRegion is null) {
					firstRegion = region;
					lastRegion = region;
					removeRegions.Clear();
					continue;
				}
				if (lastRegion.Overlaps(region)) {
					lastRegion = region;
					if (lastRegion != firstRegion) {
						removeRegions.Add(firstRegion);
					}
					removeRegions.Add(lastRegion);
					if (!isFinal) {
						continue;
					}
				} else if (!isFinal) {
					continue;
				}
				foreach (MemoryRegion removeRegion in removeRegions) {
					regions.Remove(removeRegion.StartAddress);
				}
				removeRegions.Clear();
				regions.Add(firstRegion.StartAddress, MemoryRegion.FromStartAndEndAddresses(firstRegion.StartAddress, lastRegion.EndAddress));
				firstRegion = region;
				lastRegion = region;
			}
		}

		/// <summary>Gets memory regions which overlap the specified region.</summary>
		/// <param name="region">The region.</param>
		/// <returns>The overlapping regions.</returns>
		public MemoryRegionCollection GetOverlappingRegions(MemoryRegion region) {
			MemoryRegionCollection retval = new();
			foreach (MemoryRegion existingRegion in this) {
				if (existingRegion.EndAddress < region.StartAddress) {
					continue;
				}
				if (existingRegion.StartAddress > region.EndAddress) {
					return retval;
				}
				retval.Insert(existingRegion);
			}
			return retval;
		}

		/// <summary>Gets memory regions which overlap the specified region.</summary>
		/// <param name="region">The region.</param>
		/// <returns>The overlapping regions.</returns>
		public MemoryRegionCollection GetIntersectingRegions(MemoryRegion region) {
			MemoryRegionCollection retval = new();
			foreach (MemoryRegion existingRegion in this) {
				if (existingRegion.EndAddress < region.StartAddress) {
					continue;
				}
				if (existingRegion.StartAddress > region.EndAddress) {
					return retval;
				}
				retval.Insert(MemoryRegion.FromStartAndEndAddresses(Math.Max(existingRegion.StartAddress, region.StartAddress), Math.Min(existingRegion.EndAddress, region.EndAddress)));
			}
			return retval;
		}

		/// <summary>Invert the regions.</summary>
		public void Invert() {
			SortedList<UInt32, MemoryRegion> newRegions = new();
			Int64 address = 0;
			foreach (KeyValuePair<UInt32, MemoryRegion> region in regions) {
				if (region.Key > address) {
					newRegions.Add((UInt32)address, MemoryRegion.FromStartAndEndAddresses((UInt32)address, region.Key - 1));
				}
				address = region.Value.EndAddress + 1;
			}
			if (address < 0x100000000) {
				newRegions.Add((UInt32)address, MemoryRegion.FromStartAndEndAddresses((UInt32)address, 0xFFFFFFFF));
			}
			regions = newRegions;
		}

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<MemoryRegion> GetEnumerator() {
			return regions.Values.GetEnumerator();
		}

		/// <summary>Returns an enumerator that iterates through a collection.</summary>
		/// <returns>
		///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
		/// </returns>
		IEnumerator IEnumerable.GetEnumerator() {
			return regions.Values.GetEnumerator();
		}
	}
}