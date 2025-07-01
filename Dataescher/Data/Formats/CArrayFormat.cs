// <copyright file="MemFile.cs" company="Dataescher">
// 	Copyright (c) 2022-2024 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements a c code file.</summary>

using Dataescher.Types;
using System;
using System.Collections.Generic;
using System.IO;

namespace Dataescher.Data.Formats {
	/// <summary>A c code file format.</summary>
	/// <seealso cref="T:Dataescher.Data.HexFileFormat"/>
	public class CArrayFormat : HexFileFormat {
		/// <summary>
		///     Gets or sets the width setting of the data (0 = byte, 1 = half-word, 2 = word, 3 = double-word, etc.)
		/// </summary>
		public Byte DataWidth { get; set; }

		/// <summary>
		///     Gets or sets the alignment setting of the data (0 = byte, 1 = half-word, 2 = word, 3 = double-word, etc.)
		/// </summary>
		public Byte Alignment { get; set; }

		/// <summary>Gets the alignment size in bytes.</summary>
		public UInt32 AlignmentSizeBytes => (UInt32)Math.Pow(2, Alignment);

		/// <summary>Gets the variable size in bytes.</summary>
		public UInt32 VarSizeBytes => (UInt32)Math.Pow(2, DataWidth);

		/// <summary>Gets the variable size in bits.</summary>
		public UInt32 VarSizeBits => VarSizeBytes * 8;

		/// <summary>Gets or sets the name of the array.</summary>
		public String ArrayName { get; set; }

		/// <summary>Values that represents endianness.</summary>
		public enum Endian {
			/// <summary>An enum constant representing the big option.</summary>
			Big,
			/// <summary>An enum constant representing the little option.</summary>
			Little
		}

		/// <summary>The endianness.</summary>
		public Endian Endianness { get; set; }

		#region Constructors

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.CFormat class.</summary>
		public CArrayFormat() : base() {
			Alignment = 0;
			DataWidth = 0;
			ArrayName = String.Empty;
			Endianness = Endian.Big;
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.CFormat class.</summary>
		/// <param name="memoryMap">The memory map.</param>
		public CArrayFormat(MemoryMap memoryMap) : base(memoryMap) {
			Alignment = 0;
			DataWidth = 0;
			ArrayName = String.Empty;
			Endianness = Endian.Big;
		}

		#endregion

		#region HexFileFormat base class overrides

		/// <summary>Resets the state.</summary>
		public override void ResetState() { }

		/// <summary>
		///     Applies pre-processing to the line, parsing everything except bytes representing data fields. By pre-
		///     processing the line, the data is able to be more efficiently added into the memory map.
		/// </summary>
		/// <param name="lineNumber">The line number.</param>
		/// <param name="line">The line.</param>
		/// <returns>True to terminate parsing the file, false to continue.</returns>
		public override Boolean ProcessLine(Int64 lineNumber, String line) {
			throw new NotImplementedException();
		}

		/// <summary>Reads hex data from a data record.</summary>
		/// <param name="record">The record.</param>
		/// <param name="memoryBlock">The memory block.</param>
		/// <param name="offset">[in,out] The offset within the hex string.</param>
		public override void ReadHexData(DataRecord record, Byte[] memoryBlock, ref Int32 offset) {
			throw new NotImplementedException();
		}

		/// <summary>Verify a line checksum.</summary>
		/// <param name="lineNumber">The line number.</param>
		/// <param name="computedChecksum">The computed checksum.</param>
		/// <param name="lineChecksum">The line checksum.</param>
		public override void VerifyLineChecksum(Int64 lineNumber, Byte computedChecksum, Byte lineChecksum) {
			// This format includes no checksum information
		}

		/// <summary>Saves data to the given file.</summary>
		/// <param name="streamWriter">The stream to save data to.</param>
		public override void Save(StreamWriter streamWriter) {
			MemoryMap.Organize();
			List<MemoryRegion> dataRegions = new();
			// First, pad out the data so it is aligned to <dataWidth>
			Int32 blockIdx = 0;
			List<MemoryBlock> blocks = MemoryMap.Blocks.ToList();
			while (blockIdx < blocks.Count) {
				MemoryBlock block = blocks[blockIdx];
				UInt32 curAlignedStartAddress = block.Region.StartAddress & ~(VarSizeBytes - 1);
				UInt32 curAlignedEndAddress = block.Region.EndAddress | (VarSizeBytes - 1);

				// Detect if the current aligned end address collides with the next aligned start address
				Int32 nextBlockIdx = blockIdx + 1;
				while (nextBlockIdx <= MemoryMap.Blocks.Count) {
					if (nextBlockIdx == MemoryMap.Blocks.Count) {
						dataRegions.Add(MemoryRegion.FromStartAndEndAddresses(curAlignedStartAddress, curAlignedEndAddress));
						blockIdx = nextBlockIdx + 1;
						break;
					} else {
						MemoryBlock nextBlock = blocks[nextBlockIdx];
						UInt32 nextAlignedStartAddress = nextBlock.Region.EndAddress & ~(VarSizeBytes - 1);
						UInt32 nextAlignedEndAddress = nextBlock.Region.EndAddress | (VarSizeBytes - 1);
						if (curAlignedEndAddress >= nextAlignedStartAddress - 1) {
							curAlignedEndAddress = nextAlignedEndAddress;
						} else {
							dataRegions.Add(MemoryRegion.FromStartAndEndAddresses(curAlignedStartAddress, curAlignedEndAddress));
							blockIdx = nextBlockIdx;
							break;
						}
						nextBlockIdx++;
					}
				}
			}

			if (ArrayName is null) {
				ArrayName = String.Empty;
			}
			String arrayName = Strings.SafeName(ArrayName.ToUpper());

			UInt32 regionIdx = 0;

			foreach (MemoryRegion dataRegion in dataRegions) {
				// Determine what type of record files to output
				UInt32 thisRegionStartAddress = dataRegion.StartAddress;
				UInt32 thisRegionEndAddress = dataRegion.EndAddress;
				UInt32 thisRegionSize = (UInt32)(dataRegion.Size / VarSizeBytes);
				UInt32 thisAddress = thisRegionStartAddress;
				Boolean newRegion = true;

				String thisRegionName = $"{arrayName}Region{regionIdx}";
				streamWriter.Write($"const uint{VarSizeBits}_t {thisRegionName}[{thisRegionSize}] = {{");

				while (thisAddress <= thisRegionEndAddress) {
					if ((thisAddress % BytesPerLine == 0) || newRegion) {
						streamWriter.WriteLine();
						streamWriter.Write($"\t/* 0x{thisAddress / AlignmentSizeBytes:X8} */ ");
						newRegion = false;
					}

					streamWriter.Write("0x");
					for (UInt32 varSizeIdx = 0; varSizeIdx < VarSizeBytes; varSizeIdx++) {
						Byte thisValue = Endianness == Endian.Big ? MemoryMap[thisAddress + (VarSizeBytes - varSizeIdx - 1)] : MemoryMap[thisAddress + varSizeIdx];
						streamWriter.Write(thisValue.ToString("X2"));
					}
					thisAddress += VarSizeBytes;
					if (thisAddress <= thisRegionEndAddress) {
						streamWriter.Write(",");
					}
					if (thisAddress % BytesPerLine != 0) {
						streamWriter.Write(" ");
					}
				}
				streamWriter.WriteLine();
				streamWriter.WriteLine("};");
				streamWriter.WriteLine();
				regionIdx++;
			}

			streamWriter.WriteLine($"#define {arrayName}SECTIONCNT {dataRegions.Count}");
			streamWriter.WriteLine();
			streamWriter.WriteLine($"typedef struct {arrayName}MemoryRegion_t {{");
			streamWriter.WriteLine($"\tuint{VarSizeBits}_t address;");
			streamWriter.WriteLine($"\tuint{VarSizeBits}_t size;");
			streamWriter.WriteLine($"\tuint{VarSizeBits}_t* data;");
			streamWriter.WriteLine($"}} {arrayName}MemoryRegion;");
			streamWriter.WriteLine();
			streamWriter.WriteLine($"const {arrayName}MemoryRegion {arrayName}MemoryMap[{arrayName}SECTIONCNT] = {{");

			// Print out the final array
			regionIdx = 0;
			foreach (MemoryRegion dataRegion in dataRegions) {
				if (dataRegions.IndexOf(dataRegion) != 0) {
					streamWriter.WriteLine(",");
				}
				String thisRegionName = $"{arrayName}Region{regionIdx}";
				streamWriter.Write($"\t{{ 0x{dataRegion.StartAddress / AlignmentSizeBytes:X8}, 0x{dataRegion.Size / VarSizeBytes:X8}, (uint{VarSizeBits}_t*){thisRegionName} }}");
				regionIdx++;
			}
			streamWriter.WriteLine();
			streamWriter.WriteLine("};");
			streamWriter.WriteLine();
		}

		#endregion
	}
}