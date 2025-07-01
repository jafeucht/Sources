// <copyright file="CopyRegion.cs" company="Dataescher">
// Copyright (c) 2024 Dataescher. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>9/28/2024</date>
// <summary>Implements the copy region class</summary>

using Dataescher.Data;
using System;

namespace Dataescher.Controls {
	public partial class HexEditorControl {
		/// <summary>(Serializable) a copy region.</summary>
		[Serializable()]
		public class CopyRegion {
			/// <summary>The buffer.</summary>
			public DataBuffer Buffer;
			/// <summary>Initializes a new instance of the <see cref="CopyRegion"/> class.</summary>
			/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
			/// <param name="source">Another instance to copy.</param>
			public CopyRegion(HexEditorControl source) {
				if (source.SelectionRange.Size <= 0x1000000) {
					source.GetData(source.SelectionRange, out Buffer);
				} else {
					// Limit copy operations to 1 MiB data
					throw new Exception("Copy operations limited to 16 MiB.");
				}
			}
		}
	}
}
