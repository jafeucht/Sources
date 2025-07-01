// <copyright file="LargeList.cs" company="Dataescher">
// Copyright (c) 2024 Dataescher. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>11/10/2024</date>
// <summary>Implements the large list class</summary>

using System;
using System.Collections;
using System.Collections.Generic;

namespace Dataescher.Collections {
	/// <summary>List of larges.</summary>
	/// <typeparam name="T">Generic type parameter.</typeparam>
	/// <seealso cref="T:System.Collections.Generic.IEnumerable{T}"/>
	public class LargeList<T> : IEnumerable<T> {
		/// <summary>The first.</summary>
		public LargeListItem<T> First { get; internal set; }

		/// <summary>The last.</summary>
		public LargeListItem<T> Last { get; internal set; }

		/// <summary>Gets the number of items in this collection.</summary>
		public Int64 Count { get; internal set; }

		/// <summary>Initializes a new instance of the <see cref="LargeList{T}"/> class.</summary>
		public LargeList() {
			First = null;
			Last = null;
			Count = 0;
		}

		/// <summary>Clears this object to its blank/initial state.</summary>
		public void Clear() {
			First = null;
			Last = null;
			Count = 0;
		}

		/// <summary>Empties this object.</summary>
		public Boolean IsEmpty => First is null;

		/// <summary>Adds item.</summary>
		/// <param name="item">The item to add.</param>
		public void Add(T item) {
			if ((First is null) || (Last is null)) {
				First = new LargeListItem<T>(item) {
					Owner = this,
					Next = null,
					Previous = null
				};
				Last = First;
				First.Next = null;
				Count = 1;
			} else {
				LargeListItem<T> newLast = new(item) {
					Owner = this,
					Previous = Last
				};
				Last.Next = newLast;
				newLast.Previous = Last;
				Last = newLast;
				Count++;
			}
		}

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <typeparam name="T">Generic type parameter.</typeparam>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		IEnumerator<T> IEnumerable<T>.GetEnumerator() {
			return new LargeListEnumerator<T>(this);
		}

		/// <summary>Returns an enumerator that iterates through a collection.</summary>
		/// <returns>
		///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
		/// </returns>
		IEnumerator IEnumerable.GetEnumerator() {
			return new LargeListEnumerator<T>(this);
		}
	}
}