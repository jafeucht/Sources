// <copyright file="HandlerAttribute.cs" company="Dataescher">
// 	Copyright (c) 2022-2024 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements the handler attribute class.</summary>

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Dataescher.CommandLineInterface {
	/// <summary>Attribute for handler.</summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	public class HandlerAttribute : Attribute {
		/// <summary>Gets or sets the description.</summary>
		public String Description { get; set; }

		/// <summary>The short switches.</summary>
		public List<Char> ShortSwitches { get; private set; }

		/// <summary>The long switches.</summary>
		public List<String> LongSwitches { get; private set; }

		/// <summary>Initializes a new instance of the Dataescher.HandlerAttribute class.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <param name="description">The description.</param>
		/// <param name="switches">The switches.</param>
		public HandlerAttribute(String description, String switches) {
			LongSwitches = new();
			ShortSwitches = new();
			Regex sRegex = new(@"(?<![A-Za-z0-9_-])-([^\s,-]+)");
			Regex lRegex = new(@"--([^\s,]+)");
			Match sMatch = sRegex.Match(switches);
			Match lMatch = lRegex.Match(switches);
			while (sMatch.Success) {
				Char sw = sMatch.Groups[1].Value.ToString()[0];
				if (!(sw == '?' || sw >= 'A' && sw <= 'Z' || sw >= 'a' && sw <= 'z')) {
					throw new Exception($"\"-{sw}\" is not a valid short switch.");
				}
				ShortSwitches.Add(sw);
				sMatch = sMatch.NextMatch();
			}
			while (lMatch.Success) {
				String sw = lMatch.Groups[1].Value.ToString();
				if (!Regex.IsMatch(sw, "^[a-zA-Z-_?][a-zA-Z0-9-_?]*$")) {
					throw new Exception($"\"--{sw}\" not a valid long switch.");
				}
				LongSwitches.Add(sw);
				lMatch = lMatch.NextMatch();
			}
			Description = description;
		}
	}
}