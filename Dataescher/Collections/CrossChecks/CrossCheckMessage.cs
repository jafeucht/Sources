// <copyright file="CrossCheckMessage.cs" company="Dataescher">
// Copyright (c) 2025 Dataescher. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>2/17/2025</date>
// <summary>Implements the cross-check message class</summary>

using System;

namespace Dataescher.Collections.CrossChecks {
	/// <summary>A cross-check message.</summary>
	public class CrossCheckMessage {
		/// <summary>Gets the cross-check message type.</summary>
		public CrossCheckMessageType Type { get; private set; }

		/// <summary>Gets the cross-check message.</summary>
		public String Text { get; private set; }

		/// <summary>Initializes a new instance of the <see cref="CrossCheckMessage"/> class.</summary>
		/// <param name="type">The type.</param>
		/// <param name="message">The message.</param>
		public CrossCheckMessage(CrossCheckMessageType type, String message) {
			Type = type;
			Text = message;
		}

		/// <summary>Returns a string that represents the current object.</summary>
		/// <returns>A string that represents the current object.</returns>
		public override String ToString() {
			return Type switch {
				CrossCheckMessageType.Warning => $"Warning: {Text}",
				CrossCheckMessageType.Error => $"ERROR: {Text}",
				_ => Text,
			};
		}
	}
}