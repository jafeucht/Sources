// <copyright file="InheritanceDictionary.cs" company="Dataescher">
// Copyright (c) 2018-2024 Dataescher. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>1/9/2018</date>
// <summary>A collection which acts as an inheritable dictionary.</summary>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Dataescher.Collections {
	/// <summary>A collection which acts as an inheritable dictionary.</summary>
	/// <typeparam name="TKey">Type of the key.</typeparam>
	/// <typeparam name="TParent">Type of the parent.</typeparam>
	/// <typeparam name="TValue">Type of the value.</typeparam>
	/// <seealso cref="T:System.Collections.Generic.IDictionary{TKey,TValue}"/>
	public class InheritanceDictionary<TKey, TParent, TValue> :
		IDictionary<TKey, TValue>
		where TValue : IInheritableKeyItem<TKey, TParent>
		where TParent : class {
		/// <summary>(Immutable) The parent.</summary>
		private readonly TParent _parent;
		/// <summary>(Immutable) The dictionary.</summary>
		private readonly Dictionary<TKey, TValue> _dictionary;

		/// <summary>Gets the number of.</summary>
		public Int32 Count => _dictionary.Count;

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

		/// <summary>
		///     Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
		/// </summary>
		public Boolean IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).IsReadOnly;

		/// <summary>Gets or sets the element at the specified index.</summary>
		/// <param name="key">The dictionary key to look up.</param>
		/// <returns>The element at the specified index.</returns>
		public TValue this[TKey key] { get => _dictionary[key]; set => _dictionary[key] = value; }

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
			value.Parent = _parent;
		}

		/// <summary>Adds item.</summary>
		/// <param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1" />.</param>
		public void Add(TValue item) {
			Add(item.Key, item);
		}

		/// <summary>Clears this object to its blank/initial state.</summary>
		public void Clear() {
			foreach (TValue thisValue in _dictionary.Values) {
				thisValue.Parent = null;
			}
			_dictionary.Clear();
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

		/// <summary>Gets the enumerator.</summary>
		/// <returns>The enumerator.</returns>
		IEnumerator IEnumerable.GetEnumerator() {
			return _dictionary.GetEnumerator();
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
			Boolean retval = _dictionary.Remove(key);
			item.Parent = null;
			return retval;
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
			return ((IDictionary<TKey, TValue>)_dictionary).Remove(item);
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
		///     Initializes a new instance of the RTP.XML.InheritanceListDictionary&lt;TKey, TParent, TValue&gt;
		///     class.
		/// </summary>
		/// <param name="Parent">The parent.</param>
		public InheritanceDictionary(TParent Parent) {
			_parent = Parent;
			_dictionary = new Dictionary<TKey, TValue>();
		}
	}
}