// <copyright file="DataFile.cs" company="Dataescher">
// 	Copyright (c) 2022-2024 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements the data file class.</summary>

using Dataescher.Data.Formats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dataescher.Data {
	/// <summary>A data file.</summary>
	public class DataFile {
		/// <summary>Gets the memory.</summary>
		public MemoryMap MemoryMap { get; private set; }

		/// <summary>Gets the format used during the last load / save.</summary>
		public Type FormatType { get; private set; }

		/// <summary>Gets the errors from the last file open or save operation.</summary>
		public List<String> Errors { get; private set; }

		/// <summary>Gets the warnings from the last file open or save operation.</summary>
		public List<String> Warnings { get; private set; }

		/// <summary>Initializes a new instance of the Dataescher.Data.DataFile class.</summary>
		public DataFile() {
			FormatType = null;
			MemoryMap = new();
			Errors = new List<String>();
			Warnings = new List<String>();
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.DataFile class.</summary>
		/// <exception cref="NullReferenceException">Thrown when a value was unexpectedly null.</exception>
		/// <param name="memory">The memory.</param>
		public DataFile(MemoryMap memory) : this() {
			if (memory is null) {
				throw new NullReferenceException("Memory cannot be null.");
			}
			MemoryMap = memory;
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.DataFile class.</summary>
		/// <param name="filename">The path to the file.</param>
		/// <param name="dataFileFormatter">(Optional) The data file formatter.</param>
		public DataFile(String filename, Type dataFileFormatter = null) : this() {
			Load(filename, dataFileFormatter);
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.DataFile class.</summary>
		/// <param name="stream">.</param>
		/// <param name="dataFileFormatter">.</param>
		public DataFile(Stream stream, Type dataFileFormatter) : this() {
			Load(stream, dataFileFormatter);
		}

		/// <summary>
		///     Detect type from file extension. If the file type cannot be detected, by default return binary file type.
		/// </summary>
		/// <param name="filename">The path to the file.</param>
		/// <returns>The detected type.</returns>
		public static Type DetectTypeFromFileExtension(String filename) {
			String ext = Path.GetExtension(filename).Trim('.').ToUpper();
			return ext switch {
				"AHEX" or "AHX" => typeof(ActelHexFormat),
				"BIN" or "DAT" => typeof(BinFormat),
				"ELF" => typeof(ElfFormat),
				"C" or "CPP" or "H" => typeof(CArrayFormat),
				"HEX" or "IHEX" => typeof(IntelHexFormat),
				"MEM" => typeof(MemFormat),
				"MHEX" or "MOT" or "S19" or "S28" or "S37" or "SREC" => typeof(MotorolaHexFormat),
				"TEK" => typeof(TektronixHexFormat),
				"TXT" => typeof(TITextFormat),
				_ => typeof(BinFormat)
			};
		}

		/// <summary>
		///     Detect type from file contents. This routine will attempt to detect file encoding, then load the first 256
		///     bytes from the file; after which, it will run a test for invalid control characters which indicate it is
		///     likely a text or binary file. If it appears to be a text file, it will then try to identify the first non-
		///     blank line of the file from the first 256 bytes. Using this first line, it will then try to parse the first
		///     line using all of the file format classes. If none of the text-based formats can parse the line, it will then
		///     open the file using a binary reader, and test the file against all the binary-based formats, excluding binary
		///     format. If none of these succeed, the resulting type will be considered binary.
		/// </summary>
		/// <param name="filename">The path to the file.</param>
		/// <returns>The detected file format type, binary format type if there appears to be no good match.</returns>
		public static Type DetectTypeFromFileContents(String filename) {
			// Test if the file appears to be a text-based file, try to parse the first line, then
			// run the first line through all the format
			if (HexFileFormat.TestTextFormat(filename, out List<String> firstLines)) {
				IEnumerable<HexFileFormat> hexExporters = typeof(HexFileFormat)
					.Assembly.GetTypes()
					.Where(t => t.IsSubclassOf(typeof(HexFileFormat)) && !t.IsAbstract)
					.Select(t => (HexFileFormat)Activator.CreateInstance(t));

				foreach (HexFileFormat hexFormat in hexExporters) {
					if (hexFormat is not null) {
						try {
							Int64 lineNum = 1;
							foreach (String line in firstLines) {
								// Run this line through the ASCII format parsers to determine if it is a match
								hexFormat.ProcessLine(lineNum++, line);
							}
							// If no exceptions, this is a match
							return hexFormat.GetType();
						} catch (Exception) { }
					}
				}
			}

			IEnumerable<BinaryFileFormat> binaryExporters = typeof(BinaryFileFormat)
				.Assembly.GetTypes()
				.Where(t => t.IsSubclassOf(typeof(BinaryFileFormat)) && !t.IsAbstract)
				.Select(t => (BinaryFileFormat)Activator.CreateInstance(t));

			foreach (BinaryFileFormat binFormat in binaryExporters) {
				if (binFormat is not null) {
					if (binFormat.GetType() != typeof(BinFormat)) {
						try {
							BinaryFileFormat binaryFormatMatch = null;
							using (FileStream fileStream = new(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
								using (BinaryReader binaryReader = new(fileStream)) {
									// Run this file through the binary format parsers to determine if it is a match
									binFormat.Test(binaryReader);
									// If no exceptions, this is a match
									binaryFormatMatch = binFormat;
								}
								fileStream.Close();
							}
							if (binaryFormatMatch is not null) {
								return binaryFormatMatch.GetType();
							}
						} catch (Exception) { }
					}
				}
			}

			return typeof(BinFormat);
		}

		/// <summary>Loads the given file.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <param name="filename">The path to the file.</param>
		/// <param name="dataFileFormatter">
		///     (Optional) The data file formatter. If null, the format is automatically selected based on file extension.
		/// </param>
		public void Load(String filename, Type dataFileFormatter = null) {
			if (dataFileFormatter is null) {
				dataFileFormatter = DetectTypeFromFileContents(filename);
			}
			if (Activator.CreateInstance(dataFileFormatter) is DataFileFormat dataFileFormat) {
				dataFileFormat.Load(filename);
				MemoryMap = dataFileFormat.MemoryMap;
				FormatType = dataFileFormatter;
				Errors = dataFileFormat.Errors;
				Warnings = dataFileFormat.Warnings;
			} else {
				throw new Exception($"Type {dataFileFormatter.Name} is not a member of {typeof(HexFileFormat).Name}");
			}
		}

		/// <summary>Loads the stream file.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <param name="stream">The stream to write to.</param>
		/// <param name="dataFileFormatter">
		///     (Optional) The data file formatter. If null, the format is automatically selected based on file extension.
		/// </param>
		public void Load(Stream stream, Type dataFileFormatter) {
			if (dataFileFormatter is null) {
				throw new Exception("Data file formmater cannot be null.");
			}
			if (Activator.CreateInstance(dataFileFormatter) is DataFileFormat dataFileFormat) {
				dataFileFormat.Load(stream);
				MemoryMap = dataFileFormat.MemoryMap;
				FormatType = dataFileFormatter;
				Errors = dataFileFormat.Errors;
				Warnings = dataFileFormat.Warnings;
			}
		}

		/// <summary>Saves the given file.</summary>
		/// <param name="filename">The path to the file.</param>
		/// <param name="dataFileFormatter">
		///     (Optional) The data file formatter. If null, the format is automatically selected based on file extension.
		/// </param>
		public void Save(String filename, Type dataFileFormatter = null) {
			if (dataFileFormatter is null) {
				dataFileFormatter = DetectTypeFromFileExtension(filename);
			}
			if (Activator.CreateInstance(dataFileFormatter) is DataFileFormat dataFileFormat) {
				dataFileFormat.MemoryMap = MemoryMap;
				dataFileFormat.Save(filename);
				FormatType = dataFileFormatter;
				Errors = dataFileFormat.Errors;
				Warnings = dataFileFormat.Warnings;
			}
		}

		/// <summary>Saves the given file.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <param name="stream">The stream to write to.</param>
		/// <param name="dataFileFormatter">
		///     (Optional) The data file formatter. If null, the format is automatically selected based on file extension.
		/// </param>
		public void Save(Stream stream, Type dataFileFormatter) {
			if (dataFileFormatter is null) {
				throw new Exception("Data file formmater cannot be null.");
			}
			if (Activator.CreateInstance(dataFileFormatter) is DataFileFormat dataFileFormat) {
				dataFileFormat.MemoryMap = MemoryMap;
				dataFileFormat.Save(stream);
				FormatType = dataFileFormatter;
				Errors = dataFileFormat.Errors;
				Warnings = dataFileFormat.Warnings;
			}
		}
	}
}