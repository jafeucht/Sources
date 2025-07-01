// <copyright file="IntelHexFile.cs" company="Dataescher">
// 	Copyright (c) 2022-2024 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements an Intel Hex data file.</summary>

using System;
using System.IO;

namespace Dataescher.Data.Formats {
	/// <summary>An Intel Hex data file.</summary>
	/// <seealso cref="T:Dataescher.Data.HexFileFormat"/>
	public class IntelHexFormat : HexFileFormat {
		/// <summary>Values that represent line types.</summary>
		public enum RecordType : Byte {
			/// <summary>Data record.</summary>
			Data,
			/// <summary>End of file record.</summary>
			EndOfFile,
			/// <summary>Extended segment address record.</summary>
			ExtendedSegmentAddress,
			/// <summary>Start segment address record.</summary>
			StartSegmentAddress,
			/// <summary>Extended linear address record.</summary>
			ExtendedLinearAddress,
			/// <summary>Start linear address record.</summary>
			StartLinearAddress
		}

		/// <summary>The extent linear address.</summary>
		private UInt16 extLinearAddress;
		/// <summary>The extent segment address.</summary>
		private UInt32 extSegmentAddress;

#pragma warning disable IDE0052 // Remove unread private members
		/// <summary>The start linear address.</summary>
		private UInt32 startLinearAddress;
		/// <summary>The start segment address.</summary>
		private UInt32 startSegmentAddress;
#pragma warning restore IDE0052 // Remove unread private members

		#region Constructors

		/// <summary>Initializes the class.</summary>
		private void InitClass() {
			// Nothing to do
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.IntelHexFormat class.</summary>
		public IntelHexFormat() : base() {
			InitClass();
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.IntelHexFormat class.</summary>
		/// <param name="memoryMap">The memory map.</param>
		public IntelHexFormat(MemoryMap memoryMap) : base(memoryMap) {
			InitClass();
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.IntelHexFormat class.</summary>
		/// <param name="stream">The stream.</param>
		public IntelHexFormat(Stream stream) : base() {
			InitClass();
			Load(stream);
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.IntelHexFormat class.</summary>
		/// <param name="filename">Filename of the file.</param>
		public IntelHexFormat(String filename) : base() {
			InitClass();
			Load(filename);
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.IntelHexFormat class.</summary>
		/// <param name="streamReader">The stream reader.</param>
		public IntelHexFormat(StreamReader streamReader) : base() {
			InitClass();
			Load(streamReader);
		}

		#endregion

		#region HexFileFormat base class overrides

		/// <summary>Resets the state.</summary>
		public override void ResetState() {
			extLinearAddress = 0;
			extSegmentAddress = 0;
			startLinearAddress = 0;
			startSegmentAddress = 0;
		}

		/// <summary>Reads hex data from a data record.</summary>
		/// <param name="record">The record.</param>
		/// <param name="memoryBlock">The memory block.</param>
		/// <param name="offset">[in,out] The offset within the hex string.</param>
		public override void ReadHexData(DataRecord record, Byte[] memoryBlock, ref Int32 offset) {
			for (UInt32 byteIdx = 0; byteIdx < record.Data.Length / 2; byteIdx++) {
				memoryBlock[offset] = (Byte)GetHexNibbles(record.Data, byteIdx * 2, 2);
				record.ComputedChecksum += memoryBlock[offset];
				offset++;
			}
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
			RecordType recordType;
			UInt32 address;
			UInt32 dataSize;
			Byte computedChecksum;
			Byte lineChecksum;

			line = line.Trim();
			if (line.Length == 0) {
				return false;
			}
			if (line[0] != ':') {
				throw new Exception($"Invalid line start for Intel HEX format: '{line[0]}'.");
			}
			if (line.Length < 11) {
				throw new Exception("Minimum size for Intel HEX record is 11 characters");
			}
			dataSize = GetHexNibbles(line, 1, 2);
			UInt32 recordSize = (dataSize + 1) * 2;
			if (line.Length != (recordSize + 9)) {
				throw new Exception($"Incorrect record size of {line.Length}, expected {recordSize + 9} bytes.");
			}
			recordType = (RecordType)GetHexNibbles(line, 7, 2);
			if (recordType > RecordType.StartLinearAddress) {
				throw new Exception($"Unrecognized record type: S{recordType}");
			}
			address = GetHexNibbles(line, 3, 4);
			switch (recordType) {
				case RecordType.Data:
					if (dataSize == 0) {
						throw new Exception("Data record with length 0 not allowed.");
					}
					// TODO: Check minAddr/maxAddr constraints
					break;
				case RecordType.EndOfFile:
					if (dataSize != 0) {
						throw new Exception("Invalid data size for EndOfFile record type.");
					}
					if (address != 0) {
						throw new Exception("Invalid address for EndOfFile record type.");
					}
					// TODO: Inform the processor to quit pre-processing lines
					break;
				case RecordType.ExtendedSegmentAddress:
					if (dataSize != 2) {
						throw new Exception("Invalid data size for ExtendedSegmentAddress record type.");
					}
					if (address != 0) {
						throw new Exception("Invalid address for ExtendedSegmentAddress record type.");
					}
					break;
				case RecordType.StartSegmentAddress:
					if (dataSize != 4) {
						throw new Exception("Invalid data size for StartSegmentAddress record type.");
					}
					if (address != 0) {
						throw new Exception("Invalid address for StartSegmentAddress record type.");
					}
					break;
				case RecordType.ExtendedLinearAddress:
					if (dataSize != 2) {
						throw new Exception("Invalid data size for ExtendedLinearAddress record type.");
					}
					if (address != 0) {
						throw new Exception("Invalid address for ExtendedLinearAddress record type.");
					}
					break;
				case RecordType.StartLinearAddress:
					if (dataSize != 4) {
						throw new Exception("Invalid data size for StartLinearAddress record type.");
					}
					if (address != 0) {
						throw new Exception("Invalid address for StartLinearAddress record type.");
					}
					break;
			}
			address += ((UInt32)extLinearAddress << 16) + (extSegmentAddress * 16);

			// Now process record data, delaying if record type is Data
			computedChecksum = (Byte)(dataSize + address + (address >> 8) + (Byte)recordType);
			lineChecksum = (Byte)GetHexNibbles(line, (dataSize * 2) + 9, 2);
			Int32 dataPosition = 9;

			if (recordType == RecordType.Data) {
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
				// Skip processing of data for now until we have a clearer picture of the
				// final memory organization.
				return false;
			}

			// Process other record types here
			// All other record types have data size of 4 or less
			UInt32 recordData = GetHexNibbles(line, (UInt32)dataPosition, dataSize * 2);
			computedChecksum += (Byte)(
				(recordData & 0xFF) +
				((recordData >> 8) & 0xFF) +
				((recordData >> 16) & 0xFF) +
				((recordData >> 24) & 0xFF)
			);
			VerifyLineChecksum(lineNumber, computedChecksum, lineChecksum);

			switch (recordType) {
				case RecordType.EndOfFile:
					return true;
				case RecordType.ExtendedSegmentAddress:
					extLinearAddress = 0;
					extSegmentAddress = (UInt16)recordData;
					break;
				case RecordType.StartSegmentAddress:
					startSegmentAddress = recordData;
					break;
				case RecordType.ExtendedLinearAddress:
					extSegmentAddress = 0;
					extLinearAddress = (UInt16)recordData;
					break;
				case RecordType.StartLinearAddress:
					startLinearAddress = recordData;
					break;
			}
			return false;
		}

		/// <summary>Verify a line checksum.</summary>
		/// <param name="lineNumber">The line number.</param>
		/// <param name="computedChecksum">The computed checksum.</param>
		/// <param name="lineChecksum">The line checksum.</param>
		public override void VerifyLineChecksum(Int64 lineNumber, Byte computedChecksum, Byte lineChecksum) {
			if ((Byte)(-computedChecksum) != lineChecksum) {
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
			UInt32 extLinearAddress = 0;
			MemoryMap.Organize();
			foreach (MemoryBlock block in MemoryMap.Blocks) {
				Byte lineChecksum;
				UInt32 thisRegionStartAddress = block.Region.StartAddress;
				Int64 thisRegionSize = block.Region.Size;
				Int64 thisAddress = thisRegionStartAddress;

				while (thisAddress <= block.Region.EndAddress) {
					if ((thisAddress >> 16) != extLinearAddress) {
						// Write extended address record
						extLinearAddress = (UInt32)(thisAddress & 0xFFFF0000) >> 16;
						lineChecksum = (Byte)(0x100 - (0x06 + (extLinearAddress & 0xFF) + ((extLinearAddress >> 8) & 0xFF)));
						streamWriter.Write(":02000004");
						streamWriter.Write(extLinearAddress.ToString("X4"));
						streamWriter.WriteLine(lineChecksum.ToString("X2"));
					}
					// Determine how many bytes to write
					UInt32 writeByteCnt = (UInt32)Math.Min(Math.Min(BytesPerLine - (thisAddress % BytesPerLine), thisRegionSize), 0x10000 - (thisAddress & 0xFFFF));
					// Print out address
					//sb.Append($":{writeByteCnt:X2}{thisAddress & 0xFFFF:X4}00");
					streamWriter.Write(":");
					streamWriter.Write(writeByteCnt.ToString("X2"));
					streamWriter.Write((thisAddress & 0xFFFF).ToString("X4"));
					streamWriter.Write("00");
					lineChecksum = (Byte)(0x100 - (writeByteCnt + (thisAddress & 0xFF) + ((thisAddress >> 8) & 0xFF)));
					thisRegionSize -= writeByteCnt;
					while (writeByteCnt-- > 0) {
						Byte thisByte = block[(UInt32)thisAddress];
						streamWriter.Write(thisByte.ToString("X2"));
						lineChecksum -= thisByte;
						thisAddress++;
					}
					streamWriter.WriteLine(lineChecksum.ToString("X2"));
				}
			}

			// Write end of hexFile record
			streamWriter.Write(":00000001FF");
		}

		#endregion
	}
}