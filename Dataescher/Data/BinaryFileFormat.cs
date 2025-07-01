// <copyright file="BinaryFileFormat.cs" company="Dataescher">
// 	Copyright (c) 2022-2024 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements the binary file format class.</summary>

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Dataescher.Data {
	/// <summary>A binary file format.</summary>
	/// <seealso cref="T:Dataescher.Data.DataFileFormat"/>
	public abstract class BinaryFileFormat : DataFileFormat {
		#region Constructors

		/// <summary>Initializes the class.</summary>
		private void InitClass() {
			// Nothing to do
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.BinaryFileFormat class.</summary>
		public BinaryFileFormat() : base() {
			InitClass();
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.BinaryFileFormat class.</summary>
		/// <param name="memoryMap">The memory map.</param>
		public BinaryFileFormat(MemoryMap memoryMap) : base(memoryMap) {
			InitClass();
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.BinaryFileFormat class.</summary>
		/// <param name="stream">The stream.</param>
		public BinaryFileFormat(Stream stream) : base() {
			InitClass();
			Load(stream);
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.BinaryFileFormat class.</summary>
		/// <param name="filename">Filename of the file.</param>
		public BinaryFileFormat(String filename) : base() {
			InitClass();
			Load(filename);
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.BinaryFileFormat class.</summary>
		/// <param name="binaryReader">The binary reader.</param>
		public BinaryFileFormat(BinaryReader binaryReader) : base() {
			InitClass();
			Load(binaryReader);
		}

		#endregion

		#region DataFileFormat base class overrides

		/// <summary>Loads data from the given stream.</summary>
		/// <param name="stream">The stream to load data from.</param>
		public override void Load(Stream stream) {
			using (BinaryReader binaryReader = new(stream)) {
				Load(binaryReader);
			}
		}

		/// <summary>Loads data using the given binary reader.</summary>
		/// <param name="binaryReader">The binary reader to load.</param>
		public abstract void Load(BinaryReader binaryReader);

		/// <summary>Test if this file appears to be this file format.</summary>
		/// <param name="binaryReader">The binary reader to load.</param>
		public abstract void Test(BinaryReader binaryReader);

		/// <summary>Saves data to the given stream.</summary>
		/// <param name="stream">The stream to save data to.</param>
		public override void Save(Stream stream) {
			using (BinaryWriter binaryWriter = new(stream)) {
				Save(binaryWriter);
			}
		}

		/// <summary>Saves data using the given binary writer.</summary>
		/// <param name="binaryWriter">The binary writer to save.</param>
		public abstract void Save(BinaryWriter binaryWriter);

		/// <summary>Convert a byte array to a type.</summary>
		/// <typeparam name="T">Generic type parameter.</typeparam>
		/// <param name="reader">The reader.</param>
		/// <returns>A T.</returns>
		public static T? ByteToType<T>(BinaryReader reader) {
			Byte[] bytes = reader.ReadBytes(Marshal.SizeOf<T>());
			GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			T? theStructure = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
			handle.Free();
			return theStructure;
		}

		#endregion
	}
}