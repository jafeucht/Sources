// <copyright file="ActelHexFile.cs" company="Dataescher">
// 	Copyright (c) 2022-2024 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements a Actel Hex data file.</summary>

using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Dataescher.Data.Formats {
	/// <summary>A Actel HEX file format.</summary>
	/// <seealso cref="T:Dataescher.Data.HexFileFormat"/>
	public class ActelHexFormat : HexFileFormat {
		/// <summary>The data size in bytes.</summary>
		private UInt32 dataSizeBytes;
		/// <summary>True if the first line is being read.</summary>
		private Boolean _firstReadLine;

		#region Constructors

		/// <summary>Initializes the class.</summary>
		private void InitClass() {
			dataSizeBytes = 2;
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.ActelHexFormat class.</summary>
		public ActelHexFormat() : base() {
			InitClass();
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.ActelHexFormat class.</summary>
		/// <param name="memoryMap">The memory map.</param>
		public ActelHexFormat(MemoryMap memoryMap) : base(memoryMap) {
			InitClass();
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.ActelHexFormat class.</summary>
		/// <param name="stream">The stream.</param>
		public ActelHexFormat(Stream stream) : base() {
			InitClass();
			Load(stream);
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.ActelHexFormat class.</summary>
		/// <param name="filename">Filename of the file.</param>
		public ActelHexFormat(String filename) : base() {
			InitClass();
			Load(filename);
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.ActelHexFormat class.</summary>
		/// <param name="streamReader">The stream reader.</param>
		public ActelHexFormat(StreamReader streamReader) : base() {
			InitClass();
			Load(streamReader);
		}

		#endregion

		#region HexFileFormat base class overrides

		/// <summary>Resets the state.</summary>
		public override void ResetState() {
			dataSizeBytes = 0;
			_firstReadLine = true;
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
			Regex regex = new("^([0-9A-Fa-f]{1,8}):([0-9A-Fa-f]{1,8})$");

			if (String.IsNullOrEmpty(line)) {
				// Ignore empty lines. Simply exit the routine, no error.
				return false;
			}

			// Interpret the current line
			Match match = regex.Match(line);
			if (!match.Success) {
				// Line does not match proper format
				throw new Exception("Line is invalid format.");
			}

			String addressStr = match.Groups[1].Value;
			String dataStr = match.Groups[2].Value;
			UInt32 dataStrLen = (UInt32)dataStr.Length;

			if (0 != (dataStrLen & 0x1)) {
				// If odd number of characters, generate a failure message
				throw new Exception($"Line contains odd number ({dataStrLen}) data characters.");
			}

			UInt32 dataSize = dataStrLen / 2;

			if (dataSizeBytes > 8) {
				// If odd number of characters, generate a failure message
				throw new Exception($"Line contains {dataSizeBytes} bytes, which exceeds maximum data width of 8 bytes.");
			}

			if (!_firstReadLine) {
				if (dataSize != dataSizeBytes) {
					// Throw error. Expect all data fields in the file to be the same width
					throw new Exception($"Expect all entries to have data width of {dataSizeBytes} bytes");
				}
			} else {
				dataSizeBytes = dataSize;
				_firstReadLine = false;
			}

			if (!UInt32.TryParse(addressStr, NumberStyles.HexNumber | NumberStyles.AllowHexSpecifier, null, out UInt32 address)) {
				throw new Exception("Could not parse address.");
			}

			DataRecords.Add(
				new() {
					LineNumber = lineNumber,
					StartAddress = address * dataSizeBytes,
					Size = dataSizeBytes,
					Data = dataStr
				}
			);
			return false;
		}

		/// <summary>Reads hex data from a data record.</summary>
		/// <param name="record">The record.</param>
		/// <param name="memoryBlock">The memory block.</param>
		/// <param name="offset">[in,out] The offset within the hex string.</param>
		public override void ReadHexData(DataRecord record, Byte[] memoryBlock, ref Int32 offset) {
			for (UInt32 byteIdx = 0; byteIdx < record.Data.Length / 2; byteIdx++) {
				memoryBlock[offset + dataSizeBytes - 1 - byteIdx] = (Byte)GetHexNibbles(record.Data, byteIdx * 2, 2);
			}
			offset += (Int32)dataSizeBytes;
		}

		/// <summary>Verify a line checksum.</summary>
		/// <param name="lineNumber">The line number.</param>
		/// <param name="computedChecksum">The computed checksum.</param>
		/// <param name="lineChecksum">The line checksum.</param>
		public override void VerifyLineChecksum(Int64 lineNumber, Byte computedChecksum, Byte lineChecksum) {
			// Nothing to do
		}

		/// <summary>Saves data to the given file.</summary>
		/// <param name="streamWriter">The stream to save data to.</param>
		public override void Save(StreamWriter streamWriter) {
			MemoryMap.Organize();
			String dataFormat = $"X{dataSizeBytes * 2}";
			foreach (MemoryBlock block in MemoryMap.Blocks) {   // Check if the section is selected for this memory block.
				UInt32 byteAddress = block.Region.StartAddress;
				UInt32 thisAddress = block.Region.StartAddress / dataSizeBytes;
				while (byteAddress <= block.Region.EndAddress) {
					UInt64 thisData = 0;
					// Print out the current data and address in format <Address>:<Data>
					streamWriter.Write(thisAddress.ToString("X8"));
					streamWriter.Write(":");
					for (UInt32 dataByteIdx = 0; dataByteIdx < dataSizeBytes; dataByteIdx++) {
						thisData |= (UInt64)MemoryMap[byteAddress++] << (Int32)(8 * dataByteIdx);
					}
					streamWriter.WriteLine(thisData.ToString(dataFormat));
					thisAddress = byteAddress / dataSizeBytes;
				}
			}
		}

		#endregion
	}
}