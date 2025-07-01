// <copyright file="InheritanceList.cs" company="Dataescher">
// 	Copyright (c) 2023-2024 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements a class which contains inheritance.</summary>

using System;
using System.Collections.Generic;

namespace Dataescher.Collections {
	/// <summary>
	///     Collection of child items. This collection automatically set the Parent property of the child items when they
	///     are added or removed.
	/// </summary>
	/// <typeparam name="TParent">Type of the parent object.</typeparam>
	/// <typeparam name="TValue">Type of the child items.</typeparam>
	/// <seealso cref="T:System.Collections.Generic.IList{TValue}"/>
	public class InheritanceList<TParent, TValue> : IList<TValue>
		where TParent : class
		where TValue : IInheritableItem<TParent> {
		/// <summary>(Immutable) The parent.</summary>
		private readonly TParent _parent;
		/// <summary>(Immutable) The collection.</summary>
		private readonly IList<TValue> _collection;

		/// <summary>Initializes a new instance of the Dataescher.InheritanceList&lt;P, T&gt; class.</summary>
		/// <param name="parent">The parent.</param>
		public InheritanceList(TParent parent) {
			_parent = parent;
			_collection = new List<TValue>();
		}

		/// <summary>Initializes a new instance of the Dataescher.InheritanceList&lt;P, T&gt; class.</summary>
		/// <param name="parent">The parent.</param>
		/// <param name="collection">The collection.</param>
		public InheritanceList(TParent parent, IList<TValue> collection) {
			_parent = parent;
			_collection = collection;
		}

		#region IList<T> Members
		/// <summary>
		///     Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1" />.
		/// </summary>
		/// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1" />.</param>
		/// <returns>The index of <paramref name="item" /> if found in the list; otherwise, -1.</returns>
		public Int32 IndexOf(TValue item) {
			return _collection.IndexOf(item);
		}

		/// <summary>
		///     Inserts an item to the <see cref="T:System.Collections.Generic.IList`1" /> at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
		/// <param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1" />.</param>
		public void Insert(Int32 index, TValue item) {
			if (item is not null) {
				item.Parent = _parent;
			}

			_collection.Insert(index, item);
		}

		/// <summary>Removes the <see cref="T:System.Collections.Generic.IList`1" /> item at the specified index.</summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		public void RemoveAt(Int32 index) {
			TValue oldItem = _collection[index];
			_collection.RemoveAt(index);
			if (oldItem is not null) {
				oldItem.Parent = null;
			}
		}

		/// <summary>Gets or sets the element at the specified index.</summary>
		/// <param name="index">The zero-based index of the element to get or set.</param>
		/// <returns>The element at the specified index.</returns>
		public TValue this[Int32 index] {
			get => _collection[index];
			set {
				TValue oldItem = _collection[index];
				if (value is not null) {
					value.Parent = _parent;
				}

				_collection[index] = value;
				if (oldItem is not null) {
					oldItem.Parent = null;
				}
			}
		}

		#endregion

		#region ICollection<T> Members
		/// <summary>Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1" />.</summary>
		/// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
		public void Add(TValue item) {
			if (item is not null) {
				item.Parent = _parent;
			}

			_collection.Add(item);
		}

		/// <summary>Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1" />.</summary>
		public void Clear() {
			foreach (TValue item in _collection) {
				if (item is not null) {
					item.Parent = null;
				}
			}
			_collection.Clear();
		}

		/// <summary>Sorts this collection using the default comparer.</summary>
		public void Sort() {
			(_collection as List<TValue>).Sort();
		}

		/// <summary>Sorts this collection using the specified comparer.</summary>
		/// <param name="comparer">The comparer.</param>
		public void Sort(IComparer<TValue> comparer) {
			(_collection as List<TValue>).Sort(comparer);
		}
		/// <summary>Sorts the collection using the specified System.Comparison.</summary>
		/// <param name="comparison">The System.Comparison to use.</param>
		public void Sort(Comparison<TValue> comparison) {
			(_collection as List<TValue>).Sort(comparison);
		}

		/// <summary>Sorts the collection over a specified range using the specified comparer.</summary>
		/// <param name="index">The start index.</param>
		/// <param name="count">The number of elements to sort.</param>
		/// <param name="comparer">The comparer.</param>
		public void Sort(Int32 index, Int32 count, IComparer<TValue> comparer) {
			(_collection as List<TValue>).Sort(index, count, comparer);
		}

		/// <summary>
		///     Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.
		/// </summary>
		/// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
		/// <returns>
		///     <see langword="true" /> if <paramref name="item" /> is found in the
		///     <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, <see langword="false" />.
		/// </returns>
		public Boolean Contains(TValue item) {
			return _collection.Contains(item);
		}

		/// <summary>
		///     Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1" /> to an
		///     <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.
		/// </summary>
		/// <param name="array">
		///     The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from
		///     <see cref="T:System.Collections.Generic.ICollection`1" />. The <see cref="T:System.Array" /> must have zero-
		///     based indexing.
		/// </param>
		/// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
		public void CopyTo(TValue[] array, Int32 arrayIndex) {
			_collection.CopyTo(array, arrayIndex);
		}

		/// <summary>
		///     Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.
		/// </summary>
		public Int32 Count => _collection.Count;

		/// <summary>
		///     Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
		/// </summary>
		public Boolean IsReadOnly => _collection.IsReadOnly;

		/// <summary>
		///     Removes the first occurrence of a specific object from the
		///     <see cref="T:System.Collections.Generic.ICollection`1" />.
		/// </summary>
		/// <param name="item">
		///     The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.
		/// </param>
		/// <returns>
		///     <see langword="true" /> if <paramref name="item" /> was successfully removed from the
		///     <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, <see langword="false" />. This method
		///     also returns <see langword="false" /> if <paramref name="item" /> is not found in the original
		///     <see cref="T:System.Collections.Generic.ICollection`1" />.
		/// </returns>
		public Boolean Remove(TValue item) {
			Boolean b = _collection.Remove(item);
			if (item is not null) {
				item.Parent = null;
			}

			return b;
		}

		#endregion

		#region IEnumerable<T> Members
		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<TValue> GetEnumerator() {
			return _collection.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members
		/// <summary>Returns an enumerator that iterates through a collection.</summary>
		/// <returns>
		///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
		/// </returns>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return (_collection as System.Collections.IEnumerable).GetEnumerator();
		}

		#endregion
	}
}