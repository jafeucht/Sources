// <copyright file="TITextFile.cs" company="Dataescher">
// 	Copyright (c) 2022-2024 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements a TI Text data file.</summary>

using System;
using System.IO;

namespace Dataescher.Data.Formats {
	/// <summary>A TI HEX file format.</summary>
	/// <seealso cref="T:Dataescher.Data.HexFileFormat"/>
	public class TITextFormat : HexFileFormat {
		/// <summary>True if we expect the end of a section.</summary>
		private Boolean ExpectEndOfSection;

		/// <summary>True if we saw the first address in the file during read.</summary>
		private Boolean SawAddress;

		/// <summary>The address currently being read from the file.</summary>
		private UInt32 address;

		#region Constructors

		/// <summary>Initializes the class.</summary>
		private void InitClass() {
			// Nothing to do
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.TITextFormat class.</summary>
		public TITextFormat() : base() {
			InitClass();
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.TITextFormat class.</summary>
		/// <param name="memoryMap">The memory map.</param>
		public TITextFormat(MemoryMap memoryMap) : base(memoryMap) {
			InitClass();
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.TITextFormat class.</summary>
		/// <param name="stream">The stream.</param>
		public TITextFormat(Stream stream) : base() {
			InitClass();
			Load(stream);
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.TITextFormat class.</summary>
		/// <param name="filename">Filename of the file.</param>
		public TITextFormat(String filename) : base() {
			InitClass();
			Load(filename);
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.TITextFormat class.</summary>
		/// <param name="streamReader">The stream reader.</param>
		public TITextFormat(StreamReader streamReader) : base() {
			InitClass();
			Load(streamReader);
		}

		#endregion

		#region HexFileFormat base class overrides

		/// <summary>Resets the state.</summary>
		public override void ResetState() {
			address = 0;
			SawAddress = false;
			ExpectEndOfSection = false;
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
			if (line.Length > 0) {
				if (line[0] == 'q') {
					// End of file record
					if (line.Length != 1) {
						throw new Exception("Invalid characters after 'q'.");
					}
					// Terminate parsing immediately
					return true;
				} else if (line[0] == '@') {
					UInt32 addrCnts = 0;
					// Address record
					address = 0;
					for (Int32 addrLoc = 1; addrLoc < line.Length; addrLoc++) {
						if (++addrCnts > 8) {
							throw new Exception("Too many characters in address.");
						}
						address = (address << 4) | GetHexNibbles(line, (UInt32)addrLoc, 1);
					}
					SawAddress = true;
					ExpectEndOfSection = false;
				} else if (line.Length >= 2) {
					Byte dataSize = (Byte)((line.Length + 1) / 3);

					if (dataSize > 0) {
						if (!SawAddress) {
							throw new Exception("Saw data record without address.");
						}
						if (ExpectEndOfSection) {
							throw new Exception("Expected end of section.");
						}
						if (dataSize > 16) {
							throw new Exception("Too many data bytes per line.");
						}
						if (dataSize < 16) {
							ExpectEndOfSection = true;
						}

						// To speed up data loading, determine the memory map before we begin allocating space for the data
						// Load the data later after we know the memory regions
						//InsertSection(thisAddr, dataSize, line, lineNumber);
						DataRecords.Add(
							new() {
								LineNumber = lineNumber,
								StartAddress = address,
								Size = dataSize,
								Data = line,
								ComputedChecksum = 0,
								LineChecksum = 0
							}
						);
						address += dataSize;
					}
				} else {
					throw new Exception($"Expected line beginning: {line}.");
				}
			}
			return false;
		}

		/// <summary>Reads hex data from a data record.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <param name="record">The record.</param>
		/// <param name="memoryBlock">The memory block.</param>
		/// <param name="offset">[in,out] The offset within the hex string.</param>
		public override void ReadHexData(DataRecord record, Byte[] memoryBlock, ref Int32 offset) {
			for (Int32 byteIdx = 0; byteIdx < ((record.Data.Length + 1) / 3); byteIdx++) {
				if (byteIdx > 0) {
					Char spaceData = record.Data[(byteIdx * 3) - 1];
					if (spaceData != ' ') {
						throw new Exception($"Expected space at position {(byteIdx * 3) - 1}, saw {(Byte)spaceData:X2}.");
					}
				}
				memoryBlock[offset] = (Byte)GetHexNibbles(record.Data, (UInt32)byteIdx * 3, 2);
				record.ComputedChecksum += memoryBlock[offset];
				offset++;
			}
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

			// Indicates whether we're looking at the first section
			Boolean bFirst = true;
			foreach (MemoryBlock block in MemoryMap.Blocks) {
				if (!bFirst) {
					streamWriter.WriteLine();
				}
				bFirst = false;
				UInt32 thisAddress = block.Region.StartAddress;
				streamWriter.Write('@');
				streamWriter.WriteLine(thisAddress.ToString("X4"));
				while (thisAddress < block.Region.EndAddress) {
					UInt32 bytesLeft = block.Region.EndAddress - thisAddress + 1;
					UInt32 writeByteCnt = Math.Min(BytesPerLine - (thisAddress % BytesPerLine), bytesLeft);
					while (writeByteCnt-- > 0) {
						streamWriter.Write(block[thisAddress++].ToString("X2"));
						if (writeByteCnt == 0) {
							streamWriter.WriteLine();
						} else {
							streamWriter.Write(' ');
						}
					}
				}
			}
			// Print the end of file character
			streamWriter.WriteLine('q');
		}

		#endregion
	}
}