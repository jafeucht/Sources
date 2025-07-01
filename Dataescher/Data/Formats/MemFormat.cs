// <copyright file="MemFile.cs" company="Dataescher">
// 	Copyright (c) 2022-2024 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements a CheckSum mem data file.</summary>

using Dataescher.Types;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Dataescher.Data.Formats {
	/// <summary>A CheckSum MEM data format.</summary>
	/// <seealso cref="T:Dataescher.Data.HexFileFormat"/>
	public class MemFormat : HexFileFormat {
		/// <summary>(Immutable) The default memory type.</summary>
		private const String DEFAULT_MEMORY_TYPE = "flash";

		/// <summary>Type of the memory.</summary>
		public String MemoryType { get; set; }

		/// <summary>True if the memory type is set, false otherwise.</summary>
		private Boolean MemoryTypeSet { get; set; }

		/// <summary>True if a data record was seen, false otherwise.</summary>
		private Boolean DataRecordFound { get; set; }

		#region Constructors

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.MemFormat class.</summary>
		public MemFormat() : base() {
			MemoryType = DEFAULT_MEMORY_TYPE;
			BytesPerLine = 1;
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.MemFormat class.</summary>
		/// <param name="memoryMap">The memory map.</param>
		public MemFormat(MemoryMap memoryMap) : base(memoryMap) {
			MemoryType = "flash";
			BytesPerLine = 1;
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.MemFormat class.</summary>
		/// <param name="stream">The stream.</param>
		public MemFormat(Stream stream) : this() {
			Load(stream);
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.MemFormat class.</summary>
		/// <param name="filename">Filename of the file.</param>
		public MemFormat(String filename) : this() {
			Load(filename);
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.MemFormat class.</summary>
		/// <param name="streamReader">The stream reader.</param>
		public MemFormat(StreamReader streamReader) : this() {
			Load(streamReader);
		}

		#endregion

		#region HexFileFormat base class overrides

		/// <summary>Resets the state.</summary>
		public override void ResetState() {
			MemoryTypeSet = false;
			DataRecordFound = false;
			BytesPerLine = 1;
		}

		/// <summary>
		///     Applies pre-processing to the line, parsing everything except bytes representing data fields. By pre-
		///     processing the line, the data is able to be more efficiently added into the memory map.
		/// </summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <param name="lineNumber">The line number.</param>
		/// <param name="line">The line.</param>
		/// <returns>True to terminate parsing the file, false to continue.</returns>
		public override Boolean ProcessLine(Int64 lineNumber, String line) {
			line = line.Trim();
			if (line.Length == 0) {
				// Ignore blank lines
				return false;
			}
			if (line[0] == '-') {
				// Lines starting with "-" character are ignored.
				return false;
			}

			if (!MemoryTypeSet) {
				Regex memTypeRegex = new("^Memory Type : ([a-zA-Z0-9_]+)$");
				Match memTypeMatch = memTypeRegex.Match(line);

				if (!memTypeMatch.Success) {
					throw new Exception("Could not parse memory type.");
				} else {
					MemoryType = memTypeMatch.Groups[1].Value;
					MemoryTypeSet = true;
				}
			} else {
				// Interpret the current line
				Regex dataRegex = new("^(0x[0-9A-Fa-f]{1,8})[\\s]+0x([0-9A-Fa-f]{1,8})$");
				Match dataMatch = dataRegex.Match(line);

				if (!dataMatch.Success) {
					throw new Exception("Could not parse data line.");
				} else {
					TypeConverter converter = DataType.Converters[typeof(UInt32)];
					String data = dataMatch.Groups[2].Value;
					if (data.Length % 2 != 0) {
						throw new Exception("Data record has odd number of characters.");
					}
					if (!DataRecordFound) {
						BytesPerLine = (UInt32)data.Length / 2;
						DataRecordFound = true;
					} else if (data.Length / 2 != BytesPerLine) {
						throw new Exception($"Expected data length of {BytesPerLine}.");
					}
					UInt32 address = (UInt32)converter.ParserFunc(dataMatch.Groups[1].Value);

					// To speed up data loading, determine the memory map before we begin allocating space for the data
					// Load the data later after we know the memory regions
					DataRecords.Add(
						new() {
							LineNumber = lineNumber,
							StartAddress = address * BytesPerLine,
							Size = BytesPerLine,
							Data = data
						}
					);
				}
			}
			return false;
		}

		/// <summary>Reads hex data from a data record.</summary>
		/// <param name="record">The record.</param>
		/// <param name="memoryBlock">The memory block.</param>
		/// <param name="offset">[in,out] The offset within the hex string.</param>
		public override void ReadHexData(DataRecord record, Byte[] memoryBlock, ref Int32 offset) {
			Int32 dataSize = record.Data.Length / 2;
			for (UInt32 byteIdx = 0; byteIdx < dataSize; byteIdx++) {
				memoryBlock[offset + dataSize - byteIdx - 1] = (Byte)GetHexNibbles(record.Data, byteIdx * 2, 2);
			}
			offset += dataSize;
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
			streamWriter.Write("Memory Type : ");
			streamWriter.WriteLine(MemoryType);
			streamWriter.WriteLine("-------------------------");
			Int64 address = 0;
			foreach (MemoryBlock block in MemoryMap.Blocks) {
				address = Math.Max(address, block.Region.StartAddress - (block.Region.StartAddress % BytesPerLine));
				while (address <= block.Region.EndAddress) {
					streamWriter.Write("0x");
					streamWriter.Write((address / BytesPerLine).ToString("X8"));
					streamWriter.Write("    0x");
					for (Int32 dataIdx = 0; dataIdx < BytesPerLine; dataIdx++) {
						streamWriter.Write(MemoryMap[(UInt32)(address + BytesPerLine - dataIdx - 1)].ToString("X2"));
					}
					address += BytesPerLine;
					streamWriter.WriteLine();
				}
			}
		}

		#endregion
	}
}