// <copyright file="MotorolaHexFile.cs" company="Dataescher">
// 	Copyright (c) 2022-2024 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements a Motorola Hex data file.</summary>

using System;
using System.Collections.Generic;
using System.IO;

namespace Dataescher.Data.Formats {
	/// <summary>A Motorola HEX file format.</summary>
	/// <seealso cref="T:Dataescher.Data.HexFileFormat"/>
	public class MotorolaHexFormat : HexFileFormat {
		/// <summary>Values that represent record types.</summary>
		private enum RecordType : Byte {
			/// <summary>Vendor record.</summary>
			Vendor = 0,
			/// <summary>Data record (16-bit address).</summary>
			S1_Start = 1,
			/// <summary>Data record (24-bit address).</summary>
			S2_Start = 2,
			/// <summary>Data record (32-bit address).</summary>
			S3_Start = 3,
			/// <summary>Reserved record.</summary>
			Invalid = 4,
			/// <summary>S1/S2/S3 Record count record (16-bit count).</summary>
			Cnt16 = 5,
			/// <summary>S1/S2/S3 Record count record (24-bit count).</summary>
			Cnt24 = 6,
			/// <summary>End data record (32-bit address).</summary>
			S3_End = 7,
			/// <summary>End data record (24-bit address).</summary>
			S2_End = 8,
			/// <summary>End data record (16-bit address).</summary>
			S1_End = 9
		}

		/// <summary>Values that represent Motorola HEX file types.</summary>
		public enum MotorolaHexFileTypes : Byte {
			/// <summary>The type is automatically selected.</summary>
			Auto,
			/// <summary>Addresses are 2 bytes long.</summary>
			S19,
			/// <summary>Addresses are 3 bytes long.</summary>
			S28,
			/// <summary>Addresses are 4 bytes long.</summary>
			S37
		};

		/// <summary>(Immutable) List of sizes of the record types.</summary>
		private static readonly Dictionary<RecordType, Int32> recordAddrSizes = new() {
			{ RecordType.Vendor, 2 },
			{ RecordType.S1_Start, 2 },
			{ RecordType.S2_Start, 3 },
			{ RecordType.S3_Start, 4 },
			{ RecordType.Invalid, 0 },
			{ RecordType.Cnt16, 2 },
			{ RecordType.Cnt24, 3 },
			{ RecordType.S3_End, 4 },
			{ RecordType.S2_End, 3 },
			{ RecordType.S1_End, 2 }
		};

		/// <summary>The Motorola hex file type.</summary>
		public MotorolaHexFileTypes MotorolaHexFileType { get; set; }

		/// <summary>(Immutable) The default vendor.</summary>
		public static readonly String DefaultVendor = "https://www.dataescher.com";

		/// <summary>A running tally of data records.</summary>
		public UInt32 DataRecordCount { get; private set; }

		/// <summary>Gets or sets the vendor.</summary>
		public String Vendor { get; set; }

		#region Constructors

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.MotorolaHexFormat class.</summary>
		public MotorolaHexFormat() : base() {
			Vendor = DefaultVendor;
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.MotorolaHexFormat class.</summary>
		/// <param name="memoryMap">The memory map.</param>
		public MotorolaHexFormat(MemoryMap memoryMap) : base(memoryMap) {
			Vendor = DefaultVendor;
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.MotorolaHexFormat class.</summary>
		/// <param name="stream">The stream.</param>
		public MotorolaHexFormat(Stream stream) : this() {
			Load(stream);
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.MotorolaHexFormat class.</summary>
		/// <param name="filename">Filename of the file.</param>
		public MotorolaHexFormat(String filename) : this() {
			Load(filename);
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.MotorolaHexFormat class.</summary>
		/// <param name="streamReader">The stream reader.</param>
		public MotorolaHexFormat(StreamReader streamReader) : this() {
			Load(streamReader);
		}

		#endregion

		#region HexFileFormat base class overrides

		/// <summary>Resets the state.</summary>
		public override void ResetState() {
			DataRecordCount = 0;
			MotorolaHexFileType = MotorolaHexFileTypes.Auto;
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
			UInt32 recordSize, dataSize, address;
			Byte computedChecksum, lineChecksum;

			line = line.Trim();
			if (line.Length == 0) {
				return false;
			}
			if ((line[0] != 'S') && (line[0] != 's')) {
				throw new Exception($"Not a valid line start for Motorola HEX record: '{line[0]}'.");
			}
			if (line.Length < 10) {
				throw new Exception("Minimum size for Motorola HEX record is 10 characters");
			}
			recordType = (RecordType)GetHexNibbles(line, 1, 1);

			if ((recordType > RecordType.S1_End) || (recordType == RecordType.Invalid)) {
				throw new Exception($"Unrecognized record type: S{recordType}");
			}

			recordSize = GetHexNibbles(line, 2, 2);

			if (line.Length != (recordSize * 2) + 4) {
				throw new Exception($"Incorrect record size of {line.Length}, expected {(recordSize * 2) + 4} bytes.");
			}
			if ((recordSize - 1) < recordAddrSizes[recordType]) {
				throw new Exception("Negative data size");
			}

			lineChecksum = (Byte)GetHexNibbles(line, (recordSize * 2) + 2, 2);
			dataSize = (UInt32)(recordSize - recordAddrSizes[recordType] - 1);
			address = GetHexNibbles(line, 4, (UInt32)(2 * recordAddrSizes[recordType]));

			// Validate individual record types
			switch (recordType) {
				case RecordType.Vendor: {
					if (0 == dataSize) {
						throw new Exception("Invalid vendor record of size 0");
					}
					break;
				}
				case RecordType.S1_Start: {
					if (0 == dataSize) {
						throw new Exception("Invalid S1 data record of size 0");
					}
					if (((address + dataSize - 1) & 0xFFFF) < address) {
						throw new Exception("Address range for S1 record exceeds the bounds of 16 bit addressing");
					}
					break;
				}
				case RecordType.S2_Start: {
					if (0 == dataSize) {
						throw new Exception("Invalid S2 data record of size 0");
					}
					if (((address + dataSize - 1) & 0xFFFFFF) < address) {
						throw new Exception("Address range for S2 record exceeds the bounds of 24 bit addressing");
					}
					break;
				}
				case RecordType.S3_Start: {
					if (0 == dataSize) {
						throw new Exception("Invalid S3 data record of size 0");
					}
					if (((address + (UInt64)dataSize - 1) & 0xFFFFFFFF) < address) {
						throw new Exception("Address range for S3 record exceeds the bounds of 32 bit addressing");
					}
					break;
				}
				case RecordType.Cnt16: {
					if (3 != recordSize) {
						throw new Exception($"Invalid S5 record size of {recordSize}.");
					}
					address = GetHexNibbles(line, 4, 4);
					if (address != DataRecordCount) {
						throw new Exception($"File contains {DataRecordCount} S1/S2/S3 records, expected {address}.");
					}
					break;
				}
				case RecordType.Cnt24: {
					if (4 != recordSize) {
						throw new Exception($"Invalid S6 record size of {recordSize}.");
					}
					address = GetHexNibbles(line, 4, 6);
					if (address != DataRecordCount) {
						throw new Exception($"File contains {DataRecordCount} S1/S2/S3 records, expected {address}.");
					}
					break;
				}
				case RecordType.S1_End:
				case RecordType.S2_End:
				case RecordType.S3_End: {
					if (0 != dataSize) {
						throw new Exception("End record type expects data size of 0");
					}
					break;
				}
			}

			computedChecksum = (Byte)(recordSize + address + (address >> 8) + (address >> 16) + (address >> 24));
			Int32 dataPosition = (recordAddrSizes[recordType] * 2) + 4;
			Boolean retval = false;

			if (dataSize > 0) {
				switch (recordType) {
					case RecordType.S1_Start:
					case RecordType.S2_Start:
					case RecordType.S3_Start: {
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
						DataRecordCount++;
						return false;
					}
					case RecordType.Vendor: {
						// Record validation complete
						Char[] vendorChars = new Char[dataSize];
						for (Int32 charPos = 0; charPos < dataSize; charPos++) {
							vendorChars[charPos] = (Char)GetHexNibbles(line, (UInt32)(dataPosition + (2 * charPos)), 2);
							computedChecksum += (Byte)vendorChars[charPos];
						}
						Vendor = new String(vendorChars);
						break;
					}
					case RecordType.S1_End:
					case RecordType.S2_End:
					case RecordType.S3_End: {
						retval = true;
						break;
					}
				}
			}

			VerifyLineChecksum(lineNumber, computedChecksum, lineChecksum);

			return retval;
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

		/// <summary>Verify a line checksum.</summary>
		/// <param name="lineNumber">The line number.</param>
		/// <param name="computedChecksum">The computed checksum.</param>
		/// <param name="lineChecksum">The line checksum.</param>
		public override void VerifyLineChecksum(Int64 lineNumber, Byte computedChecksum, Byte lineChecksum) {
			if ((Byte)~computedChecksum != lineChecksum) {
				String message = $"Line {lineNumber}: Incorrect line checksum 0x{lineChecksum:X2}, expected 0x{computedChecksum:X2}";
				if (InvalidChecksumWarning) {
					Warnings.Add(message);
				} else {
					Errors.Add(message);
				}
			}
		}

		/// <summary>Saves data to the given file.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <param name="streamWriter">The stream to save data to.</param>
		public override void Save(StreamWriter streamWriter) {
			MemoryMap.Organize();

			// Determine what type of record files to output
			UInt32 endAddr = MemoryMap.EndAddress;
			Int32 addrSize;
			Byte lineChecksum;

			if (MotorolaHexFileType == MotorolaHexFileTypes.Auto) {
				// Auto-select type
				if (endAddr < 0x10000) {
					// Use 16-bit addresses (S19)
					MotorolaHexFileType = MotorolaHexFileTypes.S19;
				} else if (endAddr < 0x1000000) {
					// Use 24-bit addresses (S28)
					MotorolaHexFileType = MotorolaHexFileTypes.S28;
				} else {
					// Use 32-bit addresses (S37)
					MotorolaHexFileType = MotorolaHexFileTypes.S37;
				}
			} else if (MotorolaHexFileType > MotorolaHexFileTypes.S37) {
				throw new Exception("Invalid type parameter.");
			} else {
				if ((MotorolaHexFileType == MotorolaHexFileTypes.S19) && (endAddr >= 0x10000)) {
					throw new Exception("Cannot save as S19 type since address space extends beyond address 0xFFFF");
				} else if ((MotorolaHexFileType == MotorolaHexFileTypes.S28) && (endAddr >= 0x1000000)) {
					throw new Exception("Cannot save as S28 type since address space extends beyond address 0xFFFFFF");
				}
			}
			addrSize = recordAddrSizes[(RecordType)MotorolaHexFileType];

			if (Vendor.Length > 0xFC) {
				throw new Exception("Vendor string exceeds maximum length of 252 bytes");
			}
			DataRecordCount = 0;
			// Output the vendor record
			streamWriter.Write("S0");
			streamWriter.Write((((UInt32)Vendor.Length + 3) & 0xFF).ToString("X2"));
			streamWriter.Write("0000");
			lineChecksum = (Byte)(Vendor.Length + 3);
			for (Int32 vendorByteIdx = 0; vendorByteIdx < Vendor.Length; vendorByteIdx++) {
				lineChecksum += (Byte)Vendor[vendorByteIdx];
				streamWriter.Write(((Byte)Vendor[vendorByteIdx]).ToString("X2"));
			}
			streamWriter.WriteLine(((Byte)~lineChecksum).ToString("X2"));

			foreach (MemoryBlock block in MemoryMap.Blocks) {
				UInt32 thisRegionStartAddress = block.Region.StartAddress;
				UInt32 thisRegionSize = (UInt32)block.Region.Size;
				UInt32 thisAddress = thisRegionStartAddress;

				while (thisAddress <= block.Region.EndAddress) {
					// Determine how many bytes to write
					UInt32 writeByteCnt = Math.Min(BytesPerLine - (thisAddress % BytesPerLine), thisRegionSize);

					// Print out address
					streamWriter.Write("S");
					streamWriter.Write(((Byte)MotorolaHexFileType).ToString("X1"));
					streamWriter.Write((writeByteCnt + addrSize + 1).ToString("X2"));
					lineChecksum = (Byte)(writeByteCnt + addrSize + 1);

					switch (MotorolaHexFileType) {
						case MotorolaHexFileTypes.S19:
							streamWriter.Write((thisAddress & 0xFFFF).ToString("X4"));
							lineChecksum += (Byte)((thisAddress >> 8) + thisAddress);
							break;
						case MotorolaHexFileTypes.S28:
							streamWriter.Write((thisAddress & 0xFFFFFF).ToString("X6"));
							lineChecksum += (Byte)((thisAddress >> 16) + (thisAddress >> 8) + thisAddress);
							break;
						case MotorolaHexFileTypes.S37:
							streamWriter.Write(thisAddress.ToString("X8"));
							lineChecksum += (Byte)((thisAddress >> 24) + (thisAddress >> 16) + (thisAddress >> 8) + thisAddress);
							break;
					}
					DataRecordCount++;
					thisRegionSize -= writeByteCnt;
					while (writeByteCnt-- > 0) {
						Byte thisByte = block.Data[thisAddress - thisRegionStartAddress];
						streamWriter.Write(thisByte.ToString("X2"));
						lineChecksum += thisByte;
						thisAddress++;
					}
					streamWriter.WriteLine(((Byte)~lineChecksum).ToString("X2"));
				}
			}

			// Create a count record
			if (DataRecordCount < 0x10000) {
				// Use S5 record count
				lineChecksum = (Byte)(0x03 + (DataRecordCount & 0xFF) + ((DataRecordCount >> 8) & 0xFF));
				streamWriter.Write("S503");
				streamWriter.Write(DataRecordCount.ToString("X4"));
				streamWriter.WriteLine(((~lineChecksum) & 0xFF).ToString("X2"));
			} else if (DataRecordCount < 0x1000000) {
				// Use S6 record count
				lineChecksum = (Byte)(0x04 + (DataRecordCount & 0xFF) + ((DataRecordCount >> 8) & 0xFF) + ((DataRecordCount >> 16) & 0xFF));
				streamWriter.Write("S604");
				streamWriter.Write(DataRecordCount.ToString("X6"));
				streamWriter.WriteLine(((Byte)~lineChecksum).ToString("X2"));
			} else {
				// Too many records to use either S5 or S6 count record
			}

			// Write end of file record
			switch (MotorolaHexFileType) {
				case MotorolaHexFileTypes.S19:
					streamWriter.Write("S9030000FC");
					break;
				case MotorolaHexFileTypes.S28:
					streamWriter.Write("S804000000FB");
					break;
				case MotorolaHexFileTypes.S37:
					streamWriter.Write("S70500000000FA");
					break;
			}
		}

		#endregion
	}
}