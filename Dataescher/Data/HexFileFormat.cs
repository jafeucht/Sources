// <copyright file="HexFileFormat.cs" company="Dataescher">
// 	Copyright (c) 2022-2024 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements an abstract hex file.</summary>

using Dataescher.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dataescher.Data {
	/// <summary>A hex file format.</summary>
	/// <seealso cref="T:Dataescher.Data.DataFileFormat"/>
	public abstract class HexFileFormat : DataFileFormat {
		/// <summary>A data record.</summary>
		/// <seealso cref="T:IComparable{Dataescher.Data.HexFileFormat.DataRecord}"/>
		public class DataRecord : IComparable<DataRecord> {
			/// <summary>The line number.</summary>
			public Int64 LineNumber { get; set; }
			/// <summary>The start address.</summary>
			public UInt32 StartAddress { get; set; }
			/// <summary>The start address.</summary>
			public UInt32 Size { get; set; }
			/// <summary>The start address.</summary>
			public UInt32 EndAddress => StartAddress + Size - 1;
			/// <summary>The data.</summary>
			public String Data { get; set; } = String.Empty;
			/// <summary>The computed checksum.</summary>
			public Byte ComputedChecksum { get; set; }
			/// <summary>The line checksum.</summary>
			public Byte LineChecksum { get; set; }

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
			public Int32 CompareTo(DataRecord other) {
				return StartAddress.CompareTo(other?.StartAddress);
			}

			/// <summary>Returns a string that represents the current object.</summary>
			/// <returns>A string that represents the current object.</returns>
			public override String ToString() {
				return $"Line {LineNumber}: 0x{StartAddress:X8}-0x{EndAddress:X8} ({Size} bytes)";
			}
		}

		/// <summary>Size of the buffer.</summary>
		protected Int32 BufferSize => 1024;

		/// <summary>The bytes per line.</summary>
		public UInt32 BytesPerLine { get; set; }

		#region Constructors

		/// <summary>Initializes a new instance of the Dataescher.Data.HexFileFormat class.</summary>
		public HexFileFormat() : base() {
			DataRecords = new();
			BytesPerLine = 16;
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.HexFileFormat class.</summary>
		/// <param name="memoryMap">The memory map.</param>
		public HexFileFormat(MemoryMap memoryMap) : base(memoryMap) {
			DataRecords = new();
			BytesPerLine = 16;
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.HexFileFormat class.</summary>
		/// <param name="stream">The stream to load data from.</param>
		public HexFileFormat(Stream stream) : this() {
			Load(stream);
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.HexFileFormat class.</summary>
		/// <param name="filename">Filename of the file.</param>
		public HexFileFormat(String filename) : this() {
			Load(filename);
		}

		#endregion

		/// <summary>Loads data from the given stream.</summary>
		/// <param name="stream">The stream to load.</param>
		public void Load(StreamReader stream) {
			ResetState();
			Errors = new();
			Warnings = new();
			String line;
			Int64 lineNumber = 0;
			MemoryMap.SuppressOrganize = true;
			while ((line = stream.ReadLine()) is not null) {
				lineNumber++;
				try {
					if (ProcessLine(lineNumber, line)) {
						break;
					}
					if (DataRecords.Count >= 0x1000000) {
						// Transfer data records to memory map to reduce memory footprint
						DataRecordsToMemoryMap();
					}
				} catch (Exception ex) {
					Errors.Add($"Line {lineNumber}: {ex.Message}");
				}
			}
			// Transfer data records to memory map
			DataRecordsToMemoryMap();
			MemoryMap.SuppressOrganize = false;
		}

		/// <summary>Loads data from the given stream.</summary>
		/// <param name="stream">The stream to load data from.</param>
		public override void Load(Stream stream) {
			// Test the encoding first
			System.Text.Encoding encoding;
			Int64 savedPosition = stream.Position;
			using (StreamReader testReader = new(stream, System.Text.Encoding.Default, true, BufferSize, true)) {
				stream.Seek(0, SeekOrigin.Begin);
				if (testReader.Peek() >= 0) {
					testReader.Read();
				}
				encoding = testReader.CurrentEncoding;
				stream.Seek(savedPosition, SeekOrigin.Begin);
				testReader.Close();
			}

			using (StreamReader streamReader = new(stream, encoding, true, BufferSize)) {
				Load(streamReader);
			}
		}

		/// <summary>Saves data to the given stream.</summary>
		/// <param name="stream">The stream to save data to.</param>
		public override void Save(Stream stream) {
			// Save file as UTF8 WITHOUT byte order mark. Many hex parsers (including the one in the MultiWriter dll's) cannot interpret a byte order mark properly.
			using (StreamWriter streamWriter = new(stream, new UTF8Encoding(false), BufferSize, true)) {
				Save(streamWriter);
			}
		}

		/// <summary>Saves data to the given stream.</summary>
		/// <param name="stream">The stream to save data to.</param>
		public abstract void Save(StreamWriter stream);

		/// <summary>The data lines.</summary>
		protected LargeList<DataRecord> DataRecords { get; private set; }

		/// <summary>Resets the state.</summary>
		public abstract void ResetState();

		/// <summary>Convert data records to a memory map.</summary>
		public void DataRecordsToMemoryMap() {
			// Locate contiguous line sections
			if ((DataRecords is null) || DataRecords.IsEmpty) {
				return;
			}
			LargeListItem<DataRecord> firstNode = DataRecords.First;
			while ((firstNode is not null) && (firstNode.Value is DataRecord firstRecord)) {
				DataRecord prevRecord = firstRecord;
				LargeListItem<DataRecord> lastNode = firstNode;
				DataRecord lastRecord = firstRecord;
				LargeListItem<DataRecord> curNode = firstNode.Next;
				while ((curNode is not null) && (curNode.Value is DataRecord curRecord)) {
					if (curRecord.StartAddress == (prevRecord.StartAddress + prevRecord.Size)) {
						lastNode = curNode;
						lastRecord = curRecord;
					} else {
						break;
					}
					prevRecord = curRecord;
					curNode = curNode.Next;
				}
				// Generate a new memory block, then combine all the sections into one block
				Byte[] memoryBlock = new Byte[lastRecord.StartAddress + lastRecord.Size - firstRecord.StartAddress];
				UInt32 memoryAddress = firstRecord.StartAddress;
				Int32 offset = 0;
				LargeListItem<DataRecord> thisNode = firstNode;
				firstNode = lastNode.Next;
				while (true) {
					DataRecord thisRecord = thisNode.Value;
					ReadHexData(thisRecord, memoryBlock, ref offset);
					VerifyLineChecksum(thisRecord.LineNumber, thisRecord.ComputedChecksum, thisRecord.LineChecksum);
					if (thisNode == lastNode) {
						thisNode.Delete();
						break;
					} else {
						LargeListItem<DataRecord> nextNode = thisNode.Next;
						thisNode = nextNode;
					}
				}
				MemoryMap.Insert(memoryAddress, memoryBlock);
			}
			DataRecords = new();
		}

		/// <summary>Verify a line checksum.</summary>
		/// <param name="lineNumber">The line number.</param>
		/// <param name="computedChecksum">The computed checksum.</param>
		/// <param name="lineChecksum">The line checksum.</param>
		public abstract void VerifyLineChecksum(Int64 lineNumber, Byte computedChecksum, Byte lineChecksum);

		/// <summary>Reads hex data from a data record.</summary>
		/// <param name="record">The record.</param>
		/// <param name="memoryBlock">The memory block.</param>
		/// <param name="offset">[in,out] The offset within the hex string.</param>
		public abstract void ReadHexData(DataRecord record, Byte[] memoryBlock, ref Int32 offset);

		/// <summary>
		///     Applies pre-processing to the line, parsing everything except bytes representing data fields. By pre-
		///     processing the line, the data is able to be more efficiently added into the memory map.
		/// </summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <param name="lineNumber">The line number.</param>
		/// <param name="line">The line.</param>
		/// <returns>True to terminate parsing the file, false to continue.</returns>
		public abstract Boolean ProcessLine(Int64 lineNumber, String line);

		/// <summary>Convert a hex nibble to a 4-bit value.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <param name="thisHexChar">this hexadecimal character.</param>
		/// <returns>The hexadecimal nibble.</returns>
		public static Byte GetHexNibble(Char thisHexChar) {
			return thisHexChar switch {
				'0' or '1' or '2' or '3' or '4' or '5' or '6' or '7' or '8' or '9' => (Byte)(thisHexChar - '0'),
				'A' or 'B' or 'C' or 'D' or 'E' or 'F' => (Byte)(thisHexChar - 'A' + 10),
				'a' or 'b' or 'c' or 'd' or 'e' or 'f' => (Byte)(thisHexChar - 'a' + 10),
				_ => throw new Exception($"Invalid hex character: {thisHexChar:X}")
			};
		}

		/// <summary>Convert a number to a hex string.</summary>
		/// <param name="hexString">The hex string.</param>
		/// <param name="offset">The offset within the hex string.</param>
		/// <param name="bytes">The number of bytes to parse.</param>
		/// <returns>The translated value.</returns>
		public static UInt32 GetHexNibbles(String hexString, UInt32 offset, UInt32 bytes) {
			UInt32 result = 0;
			while (bytes-- > 0) {
				result = (result << 4) | GetHexNibble(hexString[(Int32)offset++]);
			}
			return result;
		}

		/// <summary>Convert a hex value to ASCII character.</summary>
		/// <param name="data">The hex value.</param>
		/// <returns>The ASCII character.</returns>
		public Char Hex2Ascii(Byte data) {
			return data > 15 ? '\0' : data >= 10 ? (Char)(data - 10 + 'A') : (Char)(data + '0');
		}

		/// <summary>Tests a file to see if it appears to be a text-based format.</summary>
		/// <param name="filename">Filename of the file.</param>
		/// <param name="firstLines">[out] The first few lines from the file to test with the file formatter classes.</param>
		/// <returns>True if we can parse a text-based line from the file, false otherwise.</returns>
		public static Boolean TestTextFormat(String filename, out List<String> firstLines) {
			const Int32 NUM_LINES = 5;
			String thisLine;
			firstLines = new();
			Int32 curLineIndex = 0;
			Boolean passesTextTest = false;
			StringBuilder sb = new();

			// Open a string snippet
			System.Text.Encoding encoding = Data.Encoding.GetEncoding(filename);
			using (StreamReader sr = new(filename, encoding)) {
				while (true) {
					if (sr.EndOfStream) {
						if (sb.Length > 0) {
							thisLine = sb.ToString().Trim();
							if (thisLine.Length > 0) {
								firstLines.Add(thisLine);
							}
						}
						passesTextTest = firstLines.Count > 0;
						break;
					}
					Char[] thisChar = new Char[1];
					Int32 charsRead = sr.Read(thisChar, 0, 1);
					if (thisChar[0] < ' ') {
						if ((thisChar[0] == '\n') || (thisChar[0] == '\r')) {
							// Char matches CR or LF, indicating new line
							if (sb.Length > 0) {
								thisLine = sb.ToString().Trim();
								if (thisLine.Length > 0) {
									firstLines.Add(thisLine);
									if (++curLineIndex >= NUM_LINES) {
										passesTextTest = true;
										break;
									}
								}
								sb = new();
								thisLine = null;
							}
						} else if (thisChar[0] == '\t') {
							sb.Append(thisChar[0]);
						} else {
							passesTextTest = false;
							break;

						}
					} else {
						sb.Append(thisChar);
					}
				}
				sr.Close();
			}
			return passesTextTest;
		}
	}
}