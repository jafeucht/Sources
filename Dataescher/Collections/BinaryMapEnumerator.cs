// <copyright file="BinaryMapEnumerator.cs" company="Dataescher">
// Copyright (c) 2024 Dataescher. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>11/4/2024</date>
// <summary>Implements the binary map enumerator class</summary>

using System;
using System.Collections;
using System.Collections.Generic;

namespace Dataescher.Collections {
	/// <summary>A binary map enumerator.</summary>
	/// <typeparam name="T">Generic type parameter.</typeparam>
	/// <seealso cref="T:System.Collections.Generic.IEnumerator{T}"/>
	public class BinaryMapEnumerator<T> : IEnumerator<T> {
		/// <summary>The current.</summary>
		private BinaryMapNode<T> _current;

		/// <summary>True if is reset, false if not.</summary>
		private Boolean _isReset;

		/// <summary>The map.</summary>
		private BinaryMap<T> _map;

		/// <summary>Gets the element in the collection at the current position of the enumerator.</summary>
		public T Current {
			get {
				if (_current is not null) {
					if (_current.Value is T data) {
						return data;
					}
				}
				return default;
			}
		}

		/// <summary>Gets the element in the collection at the current position of the enumerator.</summary>
		Object IEnumerator.Current => Current;

		/// <summary>
		///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose() {
			_current = null;
			_map = null;
		}

		/// <summary>Advances the enumerator to the next element of the collection.</summary>
		/// <returns>
		///     <see langword="true" /> if the enumerator was successfully advanced to the next element; <see langword="false" />
		///     if the enumerator has passed the end of the collection.
		/// </returns>
		public Boolean MoveNext() {
			if (_isReset) {
				_isReset = false;
				_current = _map._root is not null ? _map._root.FirstNode : null;
			} else if (_current is not null) {
				_current = _current.NextNode;
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

		/// <summary>Initializes a new instance of the <see cref="BinaryMapEnumerator{T}"/> class.</summary>
		/// <param name="map">The map.</param>
		public BinaryMapEnumerator(BinaryMap<T> map) {
			_map = map;
			_current = null;
			_isReset = true;
		}
	}
}