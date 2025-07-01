// <copyright file="Memory.cs" company="Dataescher">
// Copyright (c) 2024 Dataescher. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>11/2/2024</date>
// <summary>Implements the memory class</summary>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Dataescher.Data {
	/// <summary>A memory.</summary>
	/// <seealso cref="T:ICloneable"/>
	/// <seealso cref="T:System.Collections.Generic.IEnumerable{Byte}"/>
	[Serializable]
	public class Memory : ICloneable, IEnumerable<Byte> {
		/// <summary>The data pointer.</summary>
		private IntPtr dataPtr;

		/// <summary>The size.</summary>
		public Int64 Length { get; private set; }

		/// <summary>Copies memory.</summary>
		/// <param name="dest">Destination for the.</param>
		/// <param name="src">Source for the.</param>
		/// <param name="count">Number of.</param>
		[DllImport("kernel32.dll", EntryPoint = "CopyMemory")]
		public static extern void CopyMemory(IntPtr dest, IntPtr src, UInt32 count);

		/// <summary>Initializes a new instance of the <see cref="Memory"/> class.</summary>
		public Memory() : this(0) { }

		/// <summary>Initializes a new instance of the <see cref="Memory"/> class.</summary>
		/// <exception cref="ArgumentException">
		///     Thrown when one or more arguments have unsupported or illegal values.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///     Thrown when one or more arguments are outside the required range.
		/// </exception>
		/// <param name="size">The size.</param>
		public Memory(Int64 size) {
			Length = size;
			if (size < 0) {
				throw new ArgumentException(nameof(size));
			}
			if (size > 0x100000000) {
				throw new ArgumentOutOfRangeException(nameof(size), size, "Size exceeds 4 GiB maximum value.");
			}
			dataPtr = size > 0 ? Marshal.AllocHGlobal(new IntPtr(size)) : IntPtr.Zero;
		}

		/// <summary>Finalizes an instance of the <see cref="Memory"/> class.</summary>
		~Memory() {
			Marshal.FreeHGlobal(dataPtr);
			dataPtr = IntPtr.Zero;
		}

		/// <summary>Initializes a new instance of the <see cref="Memory"/> class.</summary>
		/// <param name="data">The data.</param>
		public Memory(Byte[] data) : this(data.Length) {
			Marshal.Copy(data, 0, dataPtr, data.Length);
		}

		/// <summary>Fills the given value.</summary>
		/// <param name="value">The value.</param>
		public void Fill(Byte value) {
			Int64 bytesLeft = Length;
			Int32 fillArraySize = (Int32)Math.Min(0x1000000, Length);
			Byte[] fillArray = new Byte[fillArraySize];
			for (Int32 arrayIdx = 0; arrayIdx < fillArray.Length; arrayIdx++) {
				fillArray[arrayIdx] = value;
			}
			for (Int64 offset = 0; offset < Length; offset += fillArraySize) {
				IntPtr dest = new((Int64)dataPtr + offset);
				Int32 copySize = (Int32)Math.Min(bytesLeft, fillArraySize);
				Marshal.Copy(fillArray, 0, dest, copySize);
				bytesLeft -= copySize;
			}
		}
		/// <summary>Indexer to get or set items within this collection using array index syntax.</summary>
		/// <exception cref="ArgumentOutOfRangeException">
		///     Thrown when one or more arguments are outside the required range.
		/// </exception>
		/// <param name="index">Zero-based index of the entry to access.</param>
		/// <returns>The indexed item.</returns>
		public Byte this[UInt32 index] {
			get {
				if (index >= Length) {
					throw new ArgumentOutOfRangeException(nameof(index));
				}
				IntPtr location = new((Int64)dataPtr + index);
				return Marshal.ReadByte(location);
			}
			set {
				if (index >= Length) {
					throw new ArgumentOutOfRangeException(nameof(index));
				}
				IntPtr location = new((Int64)dataPtr + index);
				Marshal.WriteByte(location, value);
			}
		}

		/// <summary>Creates a new object that is a copy of the current instance.</summary>
		/// <returns>A new object that is a copy of this instance.</returns>
		public Object Clone() {
			Memory newMemory = new(Length);
			Copy(this, 0, newMemory, 0, Length);
			return newMemory;
		}

		/// <summary>Copies memory from one <see cref="Memory"/> object to another.</summary>
		/// <param name="source">Another instance to copy.</param>
		/// <param name="dest">Destination for the.</param>
		/// <param name="destOffset">Destination offset.</param>
		public static void Copy(Memory source, Memory dest, UInt32 destOffset) {
			Copy(source, 0, dest, destOffset, source.Length);
		}

		/// <summary>Copies memory from one <see cref="Memory"/> object to another.</summary>
		/// <exception cref="ArgumentOutOfRangeException">
		///     Thrown when one or more arguments are outside the required range.
		/// </exception>
		/// <exception cref="ArgumentNullException">Thrown when one or more required arguments are null.</exception>
		/// <param name="source">Another instance to copy.</param>
		/// <param name="srcOffset">Source offset.</param>
		/// <param name="dest">Destination for the.</param>
		/// <param name="destOffset">Destination offset.</param>
		/// <param name="size">The size.</param>
		public static void Copy(Memory source, UInt32 srcOffset, Memory dest, UInt32 destOffset, Int64 size) {
			if (size == 0) {
				return;
			}
			if (size < 0) {
				throw new ArgumentOutOfRangeException(nameof(size));
			}
			if (source is null) {
				throw new ArgumentNullException(nameof(source));
			}
			if (dest is null) {
				throw new ArgumentNullException(nameof(dest));
			}
			if ((srcOffset + size) > source.Length) {
				throw new ArgumentOutOfRangeException(nameof(source), "Memory access violation");
			}
			if ((destOffset + size) > dest.Length) {
				throw new ArgumentOutOfRangeException(nameof(dest), "Memory access violation");
			}
			IntPtr srcPtr = new((Int64)source.dataPtr + srcOffset);
			IntPtr destPtr = new((Int64)dest.dataPtr + destOffset);
			do {
				UInt32 bytesToCopy = (UInt32)Math.Min(0x1000000, size);
				CopyMemory(destPtr, srcPtr, bytesToCopy);
				srcPtr = new((Int64)srcPtr + bytesToCopy);
				destPtr = new((Int64)destPtr + bytesToCopy);
				size -= bytesToCopy;
			} while (size > 0);
		}

		/// <summary>Copies memory from a byte array to a <see cref="Memory"/> object.</summary>
		/// <param name="source">Another instance to copy.</param>
		/// <param name="dest">Destination for the.</param>
		/// <param name="destOffset">Destination offset.</param>
		public static void Copy(Byte[] source, Memory dest, UInt32 destOffset) {
			Copy(source, 0, dest, destOffset, source.Length);
		}

		/// <summary>Copies memory from a byte array to a <see cref="Memory"/> object.</summary>
		/// <exception cref="ArgumentOutOfRangeException">
		///     Thrown when one or more arguments are outside the required range.
		/// </exception>
		/// <exception cref="ArgumentNullException">Thrown when one or more required arguments are null.</exception>
		/// <param name="source">Another instance to copy.</param>
		/// <param name="srcOffset">Source offset.</param>
		/// <param name="dest">Destination for the.</param>
		/// <param name="destOffset">Destination offset.</param>
		/// <param name="size">The size.</param>
		public static void Copy(Byte[] source, Int32 srcOffset, Memory dest, UInt32 destOffset, Int32 size) {
			if (size == 0) {
				return;
			}
			if (size < 0) {
				throw new ArgumentOutOfRangeException(nameof(size));
			}
			if (source is null) {
				throw new ArgumentNullException(nameof(source));
			}
			if (dest is null) {
				throw new ArgumentNullException(nameof(dest));
			}
			if ((srcOffset + size) > source.Length) {
				throw new ArgumentOutOfRangeException(nameof(source), "Memory access violation");
			}
			if ((destOffset + size) > dest.Length) {
				throw new ArgumentOutOfRangeException(nameof(dest), "Memory access violation");
			}
			IntPtr destPtr = new((Int64)dest.dataPtr + destOffset);
			Marshal.Copy(source, srcOffset, destPtr, size);
		}

		/// <summary>Copies memory from a <see cref="Memory"/> object to a byte array.</summary>
		/// <exception cref="ArgumentOutOfRangeException">
		///     Thrown when one or more arguments are outside the required range.
		/// </exception>
		/// <exception cref="ArgumentNullException">Thrown when one or more required arguments are null.</exception>
		/// <param name="source">Another instance to copy.</param>
		/// <param name="srcOffset">Source offset.</param>
		/// <param name="dest">Destination for the.</param>
		/// <param name="destOffset">Destination offset.</param>
		/// <param name="size">The size.</param>
		public static void Copy(Memory source, UInt32 srcOffset, Byte[] dest, Int32 destOffset, Int32 size) {
			if (size == 0) {
				return;
			}
			if (size < 0) {
				throw new ArgumentOutOfRangeException(nameof(size));
			}
			if (source is null) {
				throw new ArgumentNullException(nameof(source));
			}
			if (dest is null) {
				throw new ArgumentNullException(nameof(dest));
			}
			if ((srcOffset + size) > source.Length) {
				throw new ArgumentOutOfRangeException(nameof(source), "Memory access violation");
			}
			if ((destOffset + size) > dest.Length) {
				throw new ArgumentOutOfRangeException(nameof(dest), "Memory access violation");
			}
			IntPtr srcPtr = new((Int64)source.dataPtr + srcOffset);
			Marshal.Copy(srcPtr, dest, destOffset, size);
		}

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<Byte> GetEnumerator() {
			return new MemoryEnumerator(this);
		}

		/// <summary>Returns an enumerator that iterates through a collection.</summary>
		/// <returns>
		///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
		/// </returns>
		IEnumerator IEnumerable.GetEnumerator() {
			return new MemoryEnumerator(this);
		}

		/// <summary>Returns a string that represents the current object.</summary>
		/// <returns>A string that represents the current object.</returns>
		public override String ToString() {
			String bytesPlural = (Length == 1) ? "byte" : "bytes";
			StringBuilder sb = new($"Memory @0x{dataPtr:X16} ({Length} {bytesPlural}): ");
			Int32 bytesToPrint = (Int32)Math.Min(16, Length);
			Boolean needsElipsis = Length > bytesToPrint;
			for (Int32 charIdx = 0; charIdx < bytesToPrint; charIdx++) {
				sb.Append(this[(UInt32)charIdx].ToString("X2"));
			}
			if (needsElipsis) {
				sb.Append("...");
			}
			return sb.ToString();
		}
	}
}