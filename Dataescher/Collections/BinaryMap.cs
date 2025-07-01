// <copyright file="BinaryMap.cs" company="Dataescher">
// Copyright (c) 2024 Dataescher. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>11/4/2024</date>
// <summary>Implements the binary map class</summary>

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Dataescher.Collections {
	/// <summary>Values that represent lookup modes.</summary>
	public enum BinaryMapLookupMode {
		/// <summary>Look up only the exact key.</summary>
		ExactKey,
		/// <summary>Look up the key, and if doesn't exist, the previous key.</summary>
		ExactOrPreviousKey,
		/// <summary>Look up the key, and if doesn't exist, the next key.</summary>
		ExactOrNextKey
	}

	/// <summary>A binary map.</summary>
	/// <typeparam name="T">Generic type parameter.</typeparam>
	/// <seealso cref="T:System.Collections.Generic.IEnumerable{T}"/>
	public class BinaryMap<T> : IEnumerable<T> {
		/// <summary>Gets or sets the root.</summary>
		internal BinaryMapNode<T> _root;

		/// <summary>Initializes a new instance of the <see cref="BinaryMap{T}"/> class.</summary>
		public BinaryMap() {
			_root = null;
		}

		/// <summary>Gets a value indicating whether this object is empty.</summary>
		public Boolean IsEmpty => _root is null;

		/// <summary>Gets the number of elements in this collection.</summary>
		public Int32 Count { get; internal set; }

		/// <summary>Query if this object contains the given key.</summary>
		/// <param name="key">The key to remove.</param>
		/// <returns>True if the object is in this collection, false if not.</returns>
		public Boolean Contains(UInt32 key) {
			return _root is not null && _root.Contains(key);
		}
		/// <summary>Adds a key and value.</summary>
		/// <param name="key">The key to remove.</param>
		/// <param name="value">[out] The value.</param>
		public void Add(UInt32 key, T value) {
			if (_root is null) {
				_root = new(this, 0, key, value);
				Count = 1;
			} else {
				_root.Add(key, value);
				Count++;
			}
		}
		/// <summary>Converts this object to a list.</summary>
		/// <returns>This object as a List&lt;T&gt;</returns>
		public List<T> ToList() {
			List<T> retval = new();
			if (_root is not null) {
				_root.ToList(retval);
			}
			return retval;
		}

		/// <summary>Clears this object to its blank/initial state.</summary>
		public void Clear() {
			_root = null;
			Count = 0;
		}

		/// <summary>Attempts to get data node.</summary>
		/// <param name="key">The key to remove.</param>
		/// <param name="mode">The lookup mode.</param>
		/// <param name="value">[out] The value.</param>
		/// <returns>True if it succeeds, false if it fails.</returns>
		public Boolean TryGetNode(UInt32 key, BinaryMapLookupMode mode, out BinaryMapNode<T> value) {
			if (_root is null) {
				value = null;
				return false;
			}
			return _root.TryGetNode(key, mode, out value);
		}

		/// <summary>Attempts to get value a KeyValuePair&lt;UInt32,T&gt; from the given UInt32.</summary>
		/// <param name="key">The key to remove.</param>
		/// <param name="mode">The lookup mode.</param>
		/// <param name="value">[out] The value.</param>
		/// <returns>True if it succeeds, false if it fails.</returns>
		public Boolean TryGetValue(UInt32 key, BinaryMapLookupMode mode, out T value) {
			if (_root is null) {
				value = default;
				return false;
			}
			return _root.TryGetValue(key, mode, out value);
		}

		/// <summary>Gets the first.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <returns>A KeyValuePair&lt;UInt32,T&gt;?</returns>
		public T First() {
			return _root is not null ? _root.First : throw new Exception("Collection is empty.");
		}

		/// <summary>Gets the last item in the collection.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <returns>A KeyValuePair&lt;UInt32,T&gt;?</returns>
		public T Last() {
			return _root is not null ? _root.Last : throw new Exception("Collection is empty.");
		}

		/// <summary>Gets the last item in the collection.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <returns>A KeyValuePair&lt;UInt32,T&gt;?</returns>
		public BinaryMapNode<T> FirstNode() {
			return _root is not null ? _root.FirstNode : throw new Exception("Collection is empty.");
		}

		/// <summary>Gets the last item in the collection.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <returns>A KeyValuePair&lt;UInt32,T&gt;?</returns>
		public BinaryMapNode<T> LastNode() {
			return _root is not null ? _root.LastNode : throw new Exception("Collection is empty.");
		}

		/// <summary>Returns a string that represents the current object.</summary>
		/// <returns>A string that represents the current object.</returns>
		public override String ToString() {
			return _root.ToString();
		}

		/// <summary>Removes the given key and any associated data.</summary>
		/// <param name="key">The key to remove.</param>
		public void Remove(UInt32 key) {
			if (_root is not null) {
				_root.Remove(key);
				if (_root.IsEmpty) {
					_root = null;
					Count = 0;
				} else {
					Count--;
				}
			}
		}

		/// <summary>Indexer to get items within this collection using array index syntax.</summary>
		/// <param name="key">The key.</param>
		/// <returns>The indexed item.</returns>
		public T this[UInt32 key] => TryGetValue(key, BinaryMapLookupMode.ExactKey, out T value)
					? value
					: throw new IndexOutOfRangeException("Collection does not contain the provided key.");

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<T> GetEnumerator() {
			return new BinaryMapEnumerator<T>(this);
		}

		/// <summary>Returns an enumerator that iterates through a collection.</summary>
		/// <returns>
		///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
		/// </returns>
		IEnumerator IEnumerable.GetEnumerator() {
			return new BinaryMapEnumerator<T>(this);
		}

		/// <summary>Print the binary data structure to file.</summary>
		/// <param name="filePath">The file path to write to.</param>
		public void PrintToFile(String filePath) {
			if (File.Exists(filePath)) {
				File.Delete(filePath);
			}
			using (FileStream fileStream = new(filePath, FileMode.OpenOrCreate, FileAccess.Write)) {
				using (StreamWriter streamWriter = new(fileStream)) {
					if (_root is not null) {
						_root.PrintToStream(streamWriter);
					}
					fileStream.Flush();
				}
			}
		}
	}
}