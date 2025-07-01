// <copyright file="DataFileFormat.cs" company="Dataescher">
// 	Copyright (c) 2022-2024 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements the data file format class.</summary>

using System;
using System.Collections.Generic;
using System.IO;

namespace Dataescher.Data {
	/// <summary>A data file format.</summary>
	public abstract class DataFileFormat {
		/// <summary>The errors.</summary>
		public List<String> Errors;

		/// <summary>The warnings.</summary>
		public List<String> Warnings;

		/// <summary>The memory map.</summary>
		public MemoryMap MemoryMap { get; internal set; }

		/// <summary>
		///     Gets or sets a value indicating whether an invalid checksum should cause an error or a warning.
		/// </summary>
		public Boolean InvalidChecksumWarning { get; set; }

		#region Constructors

		/// <summary>Initializes a new instance of the Dataescher.Data.DataFileFormat class.</summary>
		public DataFileFormat() : this(new MemoryMap()) { }

		/// <summary>Initializes a new instance of the Dataescher.Data.DataFileFormat class.</summary>
		/// <param name="memoryMap">The memory map.</param>
		public DataFileFormat(MemoryMap memoryMap) {
			Errors = new();
			Warnings = new();
			InvalidChecksumWarning = false;
			MemoryMap = memoryMap;
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.DataFileFormat class.</summary>
		/// <param name="filename">Name of the file to load.</param>
		public DataFileFormat(String filename) : this(new MemoryMap()) {
			Load(filename);
		}

		#endregion

		/// <summary>Loads data from the given file.</summary>
		/// <param name="fileName">The name of the file to load.</param>
		public void Load(String fileName) {
			using (FileStream fileStream = File.OpenRead(fileName)) {
				Load(fileStream);
			}
		}

		/// <summary>Loads data from the given stream.</summary>
		/// <param name="stream">The stream to load data from.</param>
		public abstract void Load(Stream stream);

		/// <summary>Saves data to the given file.</summary>
		/// <param name="fileName">The name of the file to save.</param>
		public void Save(String fileName) {
			String? dirName = Path.GetDirectoryName(fileName);
			if (String.IsNullOrWhiteSpace(dirName)) {
				fileName = Path.Combine(Directory.GetCurrentDirectory(), fileName);
			} else if (!Directory.Exists(dirName)) {
				Directory.CreateDirectory(dirName);
			}
			using (FileStream fileStream = new(fileName, FileMode.Create, FileAccess.Write, FileShare.None)) {
				Save(fileStream);
			}
		}

		/// <summary>Saves data to the given stream.</summary>
		/// <param name="stream">The stream to save data to.</param>
		public abstract void Save(Stream stream);
	}
}