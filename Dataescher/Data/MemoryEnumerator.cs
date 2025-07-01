// <copyright file="MemoryEnumerator.cs" company="Dataescher">
// Copyright (c) 2024 Dataescher. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>11/3/2024</date>
// <summary>Implements the memory enumerator class</summary>

using System;
using System.Collections;
using System.Collections.Generic;

namespace Dataescher.Data {
	/// <summary>A memory enumerator.</summary>
	/// <seealso cref="T:System.Collections.Generic.IEnumerator{Byte}"/>
	public class MemoryEnumerator : IEnumerator<Byte> {
		/// <summary>Gets or sets the memory.</summary>
		private Memory Memory { get; set; }

		/// <summary>Gets or sets the offset.</summary>
		public UInt32 Offset { get; set; }

		/// <summary>Gets the element in the collection at the current position of the enumerator.</summary>
		public Byte Current => Memory[Offset];

		/// <summary>Advances the enumerator to the next element of the collection.</summary>
		Object IEnumerator.Current => Memory[Offset];

		/// <summary>
		///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose() { }

		/// <summary>Advances the enumerator to the next element of the collection.</summary>
		/// <returns>
		///     <see langword="true" /> if the enumerator was successfully advanced to the next element; <see langword="false" />
		///     if the enumerator has passed the end of the collection.
		/// </returns>
		public Boolean MoveNext() {
			if (Offset < Memory.Length) {
				Offset++;
				return true;
			}
			return false;
		}

		/// <summary>
		///     Sets the enumerator to its initial position, which is before the first element in the collection.
		/// </summary>
		public void Reset() {
			Offset = 0;
		}

		/// <summary>Initializes a new instance of the <see cref="MemoryEnumerator"/> class.</summary>
		/// <param name="memory">The memory.</param>
		public MemoryEnumerator(Memory memory) {
			Memory = memory;
			Offset = 0;
		}
	}
}