// <copyright file="InheritanceListDictionary.cs" company="Dataescher">
// Copyright (c) 2018-2024 Dataescher. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>1/9/2018</date>
// <summary>A collection which acts as an inheritable list or dictionary.</summary>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Dataescher.Collections {
	/// <summary>A collection which acts as an inheritable list or dictionary.</summary>
	/// <typeparam name="TKey">Type of the key.</typeparam>
	/// <typeparam name="TParent">Type of the parent.</typeparam>
	/// <typeparam name="TValue">Type of the value.</typeparam>
	/// <seealso cref="T:System.Collections.Generic.IList{TValue}"/>
	/// <seealso cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/>
	public class InheritanceListDictionary<TKey, TParent, TValue> :
		IList<TValue>,
		IDictionary<TKey, TValue>
		where TValue : IInheritableKeyItem<TKey, TParent>
		where TParent : class {
		/// <summary>(Immutable) The parent.</summary>
		private readonly TParent _parent;

		/// <summary>(Immutable) The dictionary.</summary>
		private readonly Dictionary<TKey, TValue> _dictionary;

		/// <summary>(Immutable) The list.</summary>
		private readonly List<TValue> _list;

		/// <summary>Sorts the list.</summary>
		public void Sort() {
			_list.Sort();
		}

		/// <summary>Sorts the list.</summary>
		/// <param name="comparison">The comparison.</param>
		public void Sort(Comparison<TValue> comparison) {
			_list.Sort(comparison);
		}

		/// <summary>Sorts the list.</summary>
		/// <param name="comparer">The comparer.</param>
		public void Sort(IComparer<TValue> comparer) {
			_list.Sort(comparer);
		}

		/// <summary>Sorts the list.</summary>
		/// <param name="index">The zero-based index from which to sort.</param>
		/// <param name="count">The number of entries to sort.</param>
		/// <param name="comparer">The comparer.</param>
		public void Sort(Int32 index, Int32 count, IComparer<TValue> comparer) {
			_list.Sort(index, count, comparer);
		}

		/// <summary>Gets the number of.</summary>
		public Int32 Count => _list.Count;

		/// <summary>Gets a value indicating whether this object is read only.</summary>
		public Boolean IsReadOnly => ((IList<TValue>)_list).IsReadOnly;

		/// <summary>
		///     Gets an <see cref="T:System.Collections.Generic.ICollection`1" /> containing the keys of the
		///     <see cref="T:System.Collections.Generic.IDictionary`2" />.
		/// </summary>
		public ICollection<TKey> Keys => _dictionary.Keys;

		/// <summary>
		///     Gets an <see cref="T:System.Collections.Generic.ICollection`1" /> containing the values in the
		///     <see cref="T:System.Collections.Generic.IDictionary`2" />.
		/// </summary>
		public ICollection<TValue> Values => _dictionary.Values;

		/// <summary>Gets the number of.</summary>
		/// <typeparam name="TValue">Type of the value.</typeparam>
		Int32 ICollection<TValue>.Count => throw new NotImplementedException();

		/// <summary>Gets a value indicating whether this object is read only.</summary>
		/// <typeparam name="TValue">Type of the value.</typeparam>
		Boolean ICollection<TValue>.IsReadOnly => throw new NotImplementedException();

		/// <summary>Gets or sets the element at the specified index.</summary>
		/// <param name="index">The zero-based index of the element to get or set.</param>
		/// <returns>The element at the specified index.</returns>
		public TValue this[Int32 index] { get => _list[index]; set => _list[index] = value; }

		/// <summary>Gets or sets the element at the specified index.</summary>
		/// <typeparam name="TKey">Type of the key.</typeparam>
		/// <typeparam name="TValue">Type of the value.</typeparam>
		/// <param name="key">The key.</param>
		/// <returns>The element at the specified index.</returns>
		TValue IDictionary<TKey, TValue>.this[TKey key] { get => _dictionary[key]; set => throw new NotImplementedException(); }

		/// <summary>Looks up a given key to find its associated value.</summary>
		/// <param name="key">The object to use as the key of the element to add.</param>
		/// <returns>A TValue.</returns>
		public TValue Lookup(TKey key) {
			return _dictionary.ContainsKey(key) ? _dictionary[key] : default;
		}

		/// <summary>
		///     Adds an element with the provided key and value to the
		///     <see cref="T:System.Collections.Generic.IDictionary`2" />.
		/// </summary>
		/// <param name="key">The object to use as the key of the element to add.</param>
		/// <param name="value">The object to use as the value of the element to add.</param>
		public void Add(TKey key, TValue value) {
			_dictionary.Add(key, value);
			_list.Add(value);
			value.Parent = _parent;
		}

		/// <summary>Adds item.</summary>
		/// <param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1" />.</param>
		public void Add(TValue item) {
			Add(item.Key, item);
		}

		/// <summary>Clears this object to its blank/initial state.</summary>
		public void Clear() {
			foreach (TValue thisValue in _list) {
				thisValue.Parent = null;
			}
			_list.Clear();
			_dictionary.Clear();
		}

		/// <summary>Query if this object contains the given item.</summary>
		/// <param name="item">The item to remove.</param>
		/// <returns>True if the object is in this collection, false if not.</returns>
		public Boolean Contains(TValue item) {
			return _list.Contains(item);
		}

		/// <summary>Copies to.</summary>
		/// <param name="array">The array.</param>
		/// <param name="arrayIndex">Zero-based index of the array.</param>
		public void CopyTo(TValue[] array, Int32 arrayIndex) {
			_list.CopyTo(array, arrayIndex);
		}

		/// <summary>Gets the enumerator.</summary>
		/// <returns>The enumerator.</returns>
		public IEnumerator<TValue> GetEnumerator() {
			return _list.GetEnumerator();
		}

		/// <summary>
		///     Determines the index of a specific item in the
		///     <see cref="T:System.Collections.Generic.IList`1" />.
		/// </summary>
		/// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1" />.</param>
		/// <returns>The index of <paramref name="item" /> if found in the list; otherwise, -1.</returns>
		public Int32 IndexOf(TValue item) {
			return _list.IndexOf(item);
		}

		/// <summary>
		///     Inserts an item to the <see cref="T:System.Collections.Generic.IList`1" /> at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
		/// <param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1" />.</param>
		public void Insert(Int32 index, TValue item) {
			_dictionary.Add(item.Key, item);
			_list.Insert(index, item);
			item.Parent = _parent;
		}

		/// <summary>
		///     Removes the element with the specified key from the
		///     <see cref="T:System.Collections.Generic.IDictionary`2" />.
		/// </summary>
		/// <param name="item">The item to remove.</param>
		/// <returns>
		///     true if the element is successfully removed; otherwise, false.  This method also returns false if
		///     <paramref name="item" /> was not found in the original
		///     <see cref="T:System.Collections.Generic.IDictionary`2" />.
		/// </returns>
		public Boolean Remove(TValue item) {
			return Remove(item.Key);
		}

		/// <summary>Removes the <see cref="T:System.Collections.Generic.IList`1" /> item at the specified index.</summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		public void RemoveAt(Int32 index) {
			Remove(_list[index].Key);
		}

		/// <summary>Gets the enumerator.</summary>
		/// <returns>The enumerator.</returns>
		IEnumerator IEnumerable.GetEnumerator() {
			return _list.GetEnumerator();
		}

		/// <summary>
		///     Determines whether the <see cref="T:System.Collections.Generic.IDictionary`2" /> contains an element with the
		///     specified key.
		/// </summary>
		/// <param name="key">The key to locate in the <see cref="T:System.Collections.Generic.IDictionary`2" />.</param>
		/// <returns>
		///     true if the <see cref="T:System.Collections.Generic.IDictionary`2" /> contains an element with the key;
		///     otherwise, false.
		/// </returns>
		public Boolean ContainsKey(TKey key) {
			return _dictionary.ContainsKey(key);
		}

		/// <summary>
		///     Removes the element with the specified key from the
		///     <see cref="T:System.Collections.Generic.IDictionary`2" />.
		/// </summary>
		/// <param name="key">The key of the element to remove.</param>
		/// <returns>
		///     true if the element is successfully removed; otherwise, false.  This method also returns false if
		///     <paramref name="key" /> was not found in the original
		///     <see cref="T:System.Collections.Generic.IDictionary`2" />.
		/// </returns>
		public Boolean Remove(TKey key) {
			TValue item = _dictionary[key];
			_dictionary.Remove(key);
			item.Parent = null;
			return _list.Remove(item);
		}

		/// <summary>Gets the value associated with the specified key.</summary>
		/// <param name="key">The key whose value to get.</param>
		/// <param name="value">
		///     [out] When this method returns, the value associated with the specified key, if the key is found;
		///     otherwise, the default value for the type of the <paramref name="value" />
		///     parameter. This parameter is passed uninitialized.
		/// </param>
		/// <returns>
		///     true if the object that implements
		///     <see cref="T:System.Collections.Generic.IDictionary`2" /> contains an element with the
		///     specified key; otherwise, false.
		/// </returns>
		public Boolean TryGetValue(TKey key, out TValue value) {
			return _dictionary.TryGetValue(key, out value);
		}

		/// <summary>Adds item.</summary>
		/// <param name="item">The item to remove.</param>
		public void Add(KeyValuePair<TKey, TValue> item) {
			Add(item.Key, item.Value);
		}

		/// <summary>Query if this object contains the given item.</summary>
		/// <param name="item">The item to remove.</param>
		/// <returns>True if the object is in this collection, false if not.</returns>
		public Boolean Contains(KeyValuePair<TKey, TValue> item) {
			return _dictionary.Contains(item);
		}

		/// <summary>Removes the given item.</summary>
		/// <param name="item">The item to remove.</param>
		/// <returns>True if it succeeds, false if it fails.</returns>
		public Boolean Remove(KeyValuePair<TKey, TValue> item) {
			return ((IDictionary<TKey, TValue>)_dictionary).Remove(item) && _list.Remove(item.Value);
		}

		/// <summary>Gets the enumerator.</summary>
		/// <typeparam name="TKey">Type of the key.</typeparam>
		/// <typeparam name="TValue>">Type of the value></typeparam>
		/// <returns>The enumerator.</returns>
		IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() {
			return _dictionary.GetEnumerator();
		}

		/// <summary>Copies to.</summary>
		/// <param name="array">The array.</param>
		/// <param name="arrayIndex">Zero-based index of the array.</param>
		public void CopyTo(KeyValuePair<TKey, TValue>[] array, Int32 arrayIndex) {
			((IDictionary<TKey, TValue>)_dictionary).CopyTo(array, arrayIndex);
		}

		/// <summary>
		///     Determines the index of a specific item in the
		///     <see cref="T:System.Collections.Generic.IList`1" />.
		/// </summary>
		/// <typeparam name="TValue">Type of the value.</typeparam>
		/// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1" />.</param>
		/// <returns>The index of <paramref name="item" /> if found in the list; otherwise, -1.</returns>
		Int32 IList<TValue>.IndexOf(TValue item) {
			return IndexOf(item);
		}

		/// <summary>
		///     Inserts an item to the <see cref="T:System.Collections.Generic.IList`1" /> at the specified index.
		/// </summary>
		/// <typeparam name="TValue">Type of the value.</typeparam>
		/// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
		/// <param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1" />.</param>
		void IList<TValue>.Insert(Int32 index, TValue item) {
			Insert(index, item);
		}

		/// <summary>Removes the <see cref="T:System.Collections.Generic.IList`1" /> item at the specified index.</summary>
		/// <typeparam name="TValue">Type of the value.</typeparam>
		/// <param name="index">The zero-based index of the item to remove.</param>
		void IList<TValue>.RemoveAt(Int32 index) {
			RemoveAt(index);
		}

		/// <summary>Adds item.</summary>
		/// <typeparam name="TValue">Type of the value.</typeparam>
		/// <param name="item">The item to remove.</param>
		void ICollection<TValue>.Add(TValue item) {
			Add(item);
		}

		/// <summary>Clears this object to its blank/initial state.</summary>
		/// <typeparam name="TValue">Type of the value.</typeparam>
		void ICollection<TValue>.Clear() {
			_list.Clear();
			_dictionary.Clear();
		}

		/// <summary>Query if this object contains the given item.</summary>
		/// <typeparam name="TValue">Type of the value.</typeparam>
		/// <param name="item">The item to remove.</param>
		/// <returns>True if the object is in this collection, false if not.</returns>
		Boolean ICollection<TValue>.Contains(TValue item) {
			return _list.Contains(item);
		}

		/// <summary>
		///     Copies the entire System.Collections.Generic.List`1 to a compatible one-dimensional array, starting at the
		///     beginning of the target array.
		/// </summary>
		/// <typeparam name="TValue">Type of the value.</typeparam>
		/// <param name="array">
		///     The one-dimensional System.Array that is the destination of the elements copied from
		///     System.Collections.Generic.List`1. The System.Array must have zero-based indexing.
		/// </param>
		/// <param name="arrayIndex">Zero-based index of the array.</param>
		void ICollection<TValue>.CopyTo(TValue[] array, Int32 arrayIndex) {
			_list.CopyTo(array, arrayIndex);
		}

		/// <summary>Removes the given item.</summary>
		/// <typeparam name="TValue">Type of the value.</typeparam>
		/// <param name="item">The item to remove.</param>
		/// <returns>True if it succeeds, false if it fails.</returns>
		Boolean ICollection<TValue>.Remove(TValue item) {
			return Remove(item);
		}

		/// <summary>Gets the enumerator.</summary>
		/// <typeparam name="TValue">Type of the value.</typeparam>
		/// <returns>The enumerator.</returns>
		IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() {
			return GetEnumerator();
		}

		/// <summary>
		///     Initializes a new instance of the RTP.XML.InheritanceListDictionary&lt;TKey, TParent, TValue&gt;
		///     class.
		/// </summary>
		/// <param name="Parent">The parent.</param>
		public InheritanceListDictionary(TParent Parent) {
			_parent = Parent;
			_dictionary = new Dictionary<TKey, TValue>();
			_list = new List<TValue>();
		}
	}
}