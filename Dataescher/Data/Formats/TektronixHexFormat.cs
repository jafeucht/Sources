// <copyright file="TektronixHexFile.cs" company="Dataescher">
// 	Copyright (c) 2022-2024 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements a Tektronix Hex data file.</summary>

using System;
using System.IO;
using System.Text;

namespace Dataescher.Data.Formats {
	/// <summary>A Tektronix HEX file format.</summary>
	/// <seealso cref="T:Dataescher.Data.HexFileFormat"/>
	public class TektronixHexFormat : HexFileFormat {
		/// <summary>Values that represent record types.</summary>
		private enum RecordTypes : Byte {
			/// <summary>Data record.</summary>
			Data = 6,
			/// <summary>Termination record.</summary>
			Termination = 8
		}

		#region Constructors

		/// <summary>Initializes the class.</summary>
		private void InitClass() {
			// Nothing to do
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.TektronixHexFormat class.</summary>
		public TektronixHexFormat() : base() {
			InitClass();
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.TektronixHexFormat class.</summary>
		/// <param name="memoryMap">The memory map.</param>
		public TektronixHexFormat(MemoryMap memoryMap) : base(memoryMap) {
			InitClass();
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.TektronixHexFormat class.</summary>
		/// <param name="stream">The stream.</param>
		public TektronixHexFormat(Stream stream) : base() {
			InitClass();
			Load(stream);
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.TektronixHexFormat class.</summary>
		/// <param name="filename">Filename of the file.</param>
		public TektronixHexFormat(String filename) : base() {
			InitClass();
			Load(filename);
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.TektronixHexFormat class.</summary>
		/// <param name="streamReader">The stream reader.</param>
		public TektronixHexFormat(StreamReader streamReader) : base() {
			InitClass();
			Load(streamReader);
		}

		#endregion

		#region HexFileFormat base class overrides

		/// <summary>Resets the state.</summary>
		public override void ResetState() { }

		/// <summary>
		///     Applies pre-processing to the line, parsing everything except bytes representing data fields. By pre-
		///     processing the line, the data is able to be more efficiently added into the memory map.
		/// </summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <param name="lineNumber">The line number.</param>
		/// <param name="line">The line.</param>
		/// <returns>True to terminate parsing the file, false to continue.</returns>
		public override Boolean ProcessLine(Int64 lineNumber, String line) {
			UInt32 lineSize, dataSize, addressSize, address;
			Byte lineChecksum, computedChecksum;
			RecordTypes recordType;

			if (line.Length == 0) {
				return false;
			}
			if (line.Length < 8) {
				throw new Exception("Incomplete record");
			}
			if (line[0] != '%') {
				throw new Exception($"Not a valid line start for Tektronix HEX record: \'{line[0]}\'");
			}
			lineSize = GetHexNibbles(line, 1, 2);
			if (line.Length < (lineSize - 1)) {
				throw new Exception("Incomplete record");
			}
			recordType = (RecordTypes)GetHexNibbles(line, 3, 1);
			if (!Enum.IsDefined(typeof(RecordTypes), recordType)) {
				throw new Exception($"Invalid record type {recordType:X2}");
			}
			lineChecksum = (Byte)GetHexNibbles(line, 4, 2);

			addressSize = GetHexNibbles(line, 6, 1);
			if (addressSize > 8) {
				throw new Exception("Parser does not support address lengths greater than 8.");
			}
			address = GetHexNibbles(line, 7, addressSize);

			// Compute the checksum for the line so far (excluding any data)
			computedChecksum = 0;
			for (UInt32 nibbleIdx = 1; nibbleIdx <= (addressSize + 6); nibbleIdx++) {
				// The line checksum does not contain the checksum itself.
				if ((nibbleIdx < 4) || (nibbleIdx > 5)) {
					computedChecksum += (Byte)GetHexNibbles(line, nibbleIdx, 1);
				}
			}

			Int32 dataPosition = (Int32)addressSize + 7;
			dataSize = (lineSize - (addressSize + 6)) / 2;

			switch (recordType) {
				case RecordTypes.Data: {
					if (0 == dataSize) {
						throw new Exception("Invalid data record of size 0");
					}
					// To speed up data loading, determine the memory map before we begin allocating space for the data
					// Load the data later after we know the memory regions
					DataRecords.Add(
						new() {
							LineNumber = lineNumber,
							StartAddress = address,
							Size = dataSize,
							Data = line.Substring(dataPosition, (Int32)dataSize * 2),
							ComputedChecksum = computedChecksum,
							LineChecksum = lineChecksum
						}
					);
					break;
				}
				case RecordTypes.Termination: {
					if (0 != dataSize) {
						throw new Exception("Invalid data record of size. Expected 0 bytes.");
					}
					// Verify the checksum field in this line
					VerifyLineChecksum(lineNumber, computedChecksum, lineChecksum);
					return true;
				}
			}

			return false;
		}

		/// <summary>Reads hex data from a data record.</summary>
		/// <param name="record">The record.</param>
		/// <param name="memoryBlock">The memory block.</param>
		/// <param name="offset">[in,out] The offset within the hex string.</param>
		public override void ReadHexData(DataRecord record, Byte[] memoryBlock, ref Int32 offset) {
			for (UInt32 byteIdx = 0; byteIdx < record.Data.Length / 2; byteIdx++) {
				memoryBlock[offset] = (Byte)GetHexNibbles(record.Data, byteIdx * 2, 2);
				record.ComputedChecksum += (Byte)(memoryBlock[offset] & 0xF);
				record.ComputedChecksum += (Byte)((memoryBlock[offset] >> 4) & 0xF);
				offset++;
			}
		}

		/// <summary>Verify a line checksum.</summary>
		/// <param name="lineNumber">The line number.</param>
		/// <param name="computedChecksum">The computed checksum.</param>
		/// <param name="lineChecksum">The line checksum.</param>
		public override void VerifyLineChecksum(Int64 lineNumber, Byte computedChecksum, Byte lineChecksum) {
			if (computedChecksum != lineChecksum) {
				String message = $"Line {lineNumber}: Incorrect line checksum 0x{lineChecksum:X2}, expected 0x{computedChecksum:X2}";
				if (InvalidChecksumWarning) {
					Warnings.Add(message);
				} else {
					Errors.Add(message);
				}
			}
		}

		/// <summary>Saves data to the given file.</summary>
		/// <param name="streamWriter">The stream to save data to.</param>
		public override void Save(StreamWriter streamWriter) {
			foreach (MemoryBlock block in MemoryMap.Blocks) {
				UInt32 thisRegionSize = (UInt32)block.Region.Size;
				UInt32 thisAddress = block.Region.StartAddress;
				while (thisAddress <= block.Region.EndAddress) {
					StringBuilder sb = new(256);
					Byte lineChecksum = 0;
					// Determine how many bytes to write
					UInt32 writeByteCnt = Math.Min(BytesPerLine - (thisAddress % BytesPerLine), thisRegionSize);
					thisRegionSize -= writeByteCnt;
					UInt32 lineSize = (writeByteCnt * 2) + 14;
					// Print out beginning of line
					sb.Append("%");
					sb.Append(lineSize.ToString("X2"));
					sb.Append("6008");
					sb.Append(thisAddress.ToString("X8"));
					while (writeByteCnt-- > 0) {
						Byte thisChar = MemoryMap[thisAddress++];
						sb.Append(thisChar.ToString("X2"));
					}
					for (Int32 thisNibbleIdx = 1; thisNibbleIdx <= lineSize; thisNibbleIdx++) {
						lineChecksum += GetHexNibble(sb[thisNibbleIdx]);
					}
					sb[4] = Hex2Ascii((Byte)(lineChecksum >> 4));
					sb[5] = Hex2Ascii((Byte)(lineChecksum & 0x0F));
					streamWriter.WriteLine(sb.ToString());
				}
			}
			// Write termination record
			streamWriter.Write("%0E81E800000000");
		}

		#endregion
	}
}