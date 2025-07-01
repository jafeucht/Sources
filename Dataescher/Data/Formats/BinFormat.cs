// <copyright file="BinaryFile.cs" company="Dataescher">
// 	Copyright (c) 2022-2024 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements a binary data file.</summary>

using System;
using System.IO;

namespace Dataescher.Data.Formats {
	/// <summary>An Intel Hex data file.</summary>
	/// <seealso cref="T:Dataescher.Data.BinaryFileFormat"/>
	public class BinFormat : BinaryFileFormat {
		/// <summary>(Immutable) Length of the buffer.</summary>
		public readonly Int32 BufferLength = 0x10000;

		#region Constructors

		/// <summary>Initializes the class.</summary>
		private void InitClass() {
			// Nothing to do
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.BinFormat class.</summary>
		public BinFormat() : base() {
			InitClass();
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.BinFormat class.</summary>
		/// <param name="memoryMap">The memory map.</param>
		public BinFormat(MemoryMap memoryMap) : base(memoryMap) {
			InitClass();
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.BinFormat class.</summary>
		/// <param name="stream">The stream.</param>
		public BinFormat(Stream stream) : base() {
			InitClass();
			Load(stream);
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.BinFormat class.</summary>
		/// <param name="filename">Filename of the file.</param>
		public BinFormat(String filename) : base() {
			InitClass();
			Load(filename);
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.BinFormat class.</summary>
		/// <param name="binaryReader">The binary reader.</param>
		public BinFormat(BinaryReader binaryReader) : base() {
			InitClass();
			Load(binaryReader);
		}

		#endregion

		/// <summary>Loads data using the given binary reader.</summary>
		/// <param name="binaryReader">The binary reader to load.</param>
		public override void Load(BinaryReader binaryReader) {
			try {
				Int32 address = 0;
				Int32 readSize;
				Byte[]? data = new Byte[BufferLength];
				while ((readSize = binaryReader.BaseStream.Read(data, 0, BufferLength)) > 0) {
					MemoryMap.Insert((UInt32)address, data, 0, readSize);
					address += readSize;
				}
				data = null;
				MemoryMap.Organize();
			} catch (Exception ex) {
				Errors.Add(ex.Message);
			}
		}

		/// <summary>Saves data using the given binary writer.</summary>
		/// <param name="binaryWriter">The binary writer to save.</param>
		public override void Save(BinaryWriter binaryWriter) {
			UInt32 address = MemoryMap.StartAddress;
			UInt32 offset = 0;
			while (address < MemoryMap.EndAddress) {
				Int32 writeSize = (Int32)Math.Min((UInt32)BufferLength, MemoryMap.EndAddress - address + 1);
				Byte[] writeData = new Byte[writeSize];
				Memory memory = MemoryMap.Fetch(MemoryRegion.FromStartAddressAndSize(address, writeSize)).Data;
				Memory.Copy(memory, offset, writeData, 0, writeSize);
				binaryWriter.Write(writeData);
				address += (UInt32)writeSize;
				offset += (UInt32)writeSize;
			}
		}

		/// <summary>Test if this file appears to be this file format.</summary>
		/// <param name="binaryReader">The binary reader to load.</param>
		public override void Test(BinaryReader binaryReader) {
			// All files are valid binary files.
		}
	}
}