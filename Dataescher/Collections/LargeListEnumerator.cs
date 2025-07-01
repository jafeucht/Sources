// <copyright file="LargeListEnumerator.cs" company="Dataescher">
// Copyright (c) 2024 Dataescher. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>11/10/2024</date>
// <summary>Implements the large list enumerator class</summary>

using System;
using System.Collections;
using System.Collections.Generic;

namespace Dataescher.Collections {
	/// <summary>A large list enumerator.</summary>
	/// <typeparam name="T">Generic type parameter.</typeparam>
	/// <seealso cref="T:System.Collections.Generic.IEnumerator{T}"/>
	public class LargeListEnumerator<T> : IEnumerator<T> {
		/// <summary>The list.</summary>
		private LargeList<T> _list;

		/// <summary>The current.</summary>
		private LargeListItem<T> _current;

		/// <summary>True if is reset, false if not.</summary>
		private Boolean _isReset;

		/// <summary>Gets the element in the collection at the current position of the enumerator.</summary>
		public T Current => _current is not null ? _current.Value : default;

		/// <summary>Gets the element in the collection at the current position of the enumerator.</summary>
		Object IEnumerator.Current => Current;

		/// <summary>
		///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose() {
			_current = null;
			_list = null;
		}

		/// <summary>Advances the enumerator to the next element of the collection.</summary>
		/// <returns>
		///     <see langword="true" /> if the enumerator was successfully advanced to the next element; <see langword="false" />
		///     if the enumerator has passed the end of the collection.
		/// </returns>
		public Boolean MoveNext() {
			if (_isReset) {
				_isReset = false;
				_current = _list.First is not null ? _list.First : null;
			} else if (_current is not null) {
				_current = _current.Next;
			}
			return _current is not null;
		}

		/// <summary>Deletes the current node and move next.</summary>
		/// <returns>True if it succeeds, false if it fails.</returns>
		public Boolean DeleteAndMoveNext() {
			if (_isReset) {
				_isReset = false;
				_current = _list.First is not null ? _list.First : null;
			} else if (_current is not null) {
				if (_list.First == _current) {
					_list.First = _current.Next;
				}
				if (_list.Last == _current) {
					_list.First = null;
				}
				_current = _current.Next;
			}
			return _current is not null;
		}

		/// <summary>
		///     Sets the enumerator to its initial position, which is before the first element in the collection.
		/// </summary>
		public void Reset() {
			_current = null;
			_isReset = true;
		}

		/// <summary>Initializes a new instance of the <see cref="LargeListEnumerator{T}"/> class.</summary>
		/// <param name="list">The list.</param>
		public LargeListEnumerator(LargeList<T> list) {
			_list = list;
			_current = null;
			_isReset = true;
		}
	}
}