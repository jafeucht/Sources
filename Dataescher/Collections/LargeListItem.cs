// <copyright file="LargeListItem.cs" company="Dataescher">
// Copyright (c) 2024 Dataescher. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>11/10/2024</date>
// <summary>Implements the large list item class</summary>

namespace Dataescher.Collections {
	/// <summary>A large list item.</summary>
	/// <typeparam name="T">Generic type parameter.</typeparam>
	public class LargeListItem<T> {
		/// <summary>Gets the next item.</summary>
		public LargeListItem<T> Next { get; internal set; }

		/// <summary>Gets the previous item.</summary>
		public LargeListItem<T> Previous { get; internal set; }

		/// <summary>Gets the owner.</summary>
		public LargeList<T> Owner { get; internal set; }

		/// <summary>Gets or sets the value.</summary>
		public T Value { get; set; }

		/// <summary>Deletes this object from the list.</summary>
		public void Delete() {
			if (Previous is not null) {
				Previous.Next = Next;
			}
			if (Next is not null) {
				Next.Previous = Previous;
			}
			if (Owner is not null) {
				Owner.Count--;
				Owner = null;
			}
		}

		/// <summary>Initializes a new instance of the <see cref="LargeListItem{T}"/> class.</summary>
		/// <param name="value">The value.</param>
		public LargeListItem(T value) {
			Value = value;
			Next = null;
			Previous = null;
		}
	}
}