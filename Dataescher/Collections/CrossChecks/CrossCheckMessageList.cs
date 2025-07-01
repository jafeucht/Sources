// <copyright file="CrossCheckMessageList.cs" company="Dataescher">
// Copyright (c) 2025 Dataescher. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>2/16/2025</date>
// <summary>Implements the cross check message collection class</summary>

using System;
using System.Collections;
using System.Collections.Generic;

namespace Dataescher.Collections.CrossChecks {
	/// <summary>Collection of cross check messages.</summary>
	/// <seealso cref="T:System.Collections.Generic.IEnumerable{Dataescher.Collections.CrossChecks.CrossCheckMessage}"/>
	public class CrossCheckMessageList : IEnumerable<CrossCheckMessage> {
		/// <summary>(Immutable) The messages.</summary>
		private readonly List<CrossCheckMessage> _messages;

		/// <summary>Initializes a new instance of the <see cref="CrossCheckMessageList"/> class.</summary>
		public CrossCheckMessageList() {
			_messages = new();
		}

		/// <summary>Clears this object to its blank/initial state.</summary>
		public void Clear() {
			_messages.Clear();
		}

		/// <summary>Report a cross-check error.</summary>
		/// <param name="message">The error message.</param>
		public void Error(String message) {
			_messages.Add(new(CrossCheckMessageType.Error, message));
		}

		/// <summary>Report a cross-check warning.</summary>
		/// <param name="message">The warning message.</param>
		public void Warning(String message) {
			_messages.Add(new(CrossCheckMessageType.Warning, message));
		}

		/// <summary>Report a cross-check error or warning.</summary>
		/// <param name="isError">True to report error, false to report warning.</param>
		/// <param name="message">The message.</param>
		public void Create(Boolean isError, String message) {
			if (isError) {
				_messages.Add(new(CrossCheckMessageType.Error, message));
			} else {
				_messages.Add(new(CrossCheckMessageType.Warning, message));
			}
		}

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<CrossCheckMessage> GetEnumerator() {
			return ((IEnumerable<CrossCheckMessage>)_messages).GetEnumerator();
		}

		/// <summary>Returns an enumerator that iterates through a collection.</summary>
		/// <returns>
		///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
		/// </returns>
		IEnumerator IEnumerable.GetEnumerator() {
			return ((IEnumerable)_messages).GetEnumerator();
		}

		/// <summary>Gets the number of errors.</summary>
		public Int32 ErrorCount {
			get {
				Int32 retval = 0;
				foreach (CrossCheckMessage message in _messages) {
					if (message.Type == CrossCheckMessageType.Error) {
						retval++;
					}
				}
				return retval;
			}
		}

		/// <summary>Gets the number of warnings.</summary>
		public Int32 WarningCount {
			get {
				Int32 retval = 0;
				foreach (CrossCheckMessage message in _messages) {
					if (message.Type == CrossCheckMessageType.Warning) {
						retval++;
					}
				}
				return retval;
			}
		}
	}
}