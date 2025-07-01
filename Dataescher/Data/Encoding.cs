// <copyright file="Encoding.cs" company="Dataescher">
// Copyright (c) 2023-2024 Dataescher. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>5/4/2023</date>
// <summary>Implements the encoding class</summary>

using System;
using System.IO;

namespace Dataescher.Data {
	/// <summary>Various encoding extension methods.</summary>
	public static class Encoding {
		/// <summary>Get a file's encoding.</summary>
		/// <param name="filename">The path to the file.</param>
		/// <returns>The encoding.</returns>
		public static System.Text.Encoding GetEncoding(String filename) {
			// This is a direct quote from MSDN:  
			// The CurrentEncoding value can be different after the first
			// call to any Read method of StreamReader, since encoding
			// autodetection is not done until the first call to a Read method.
			System.Text.Encoding encoding = null;
			using (StreamReader reader = new(filename, System.Text.Encoding.Default, true)) {
				if (reader.Peek() >= 0) {
					reader.Read();
				}
				encoding = reader.CurrentEncoding;
				reader.Close();
			}
			return encoding;
		}
	}
}