// <copyright file="ParameterAttribute.cs" company="Dataescher">
// 	Copyright (c) 2022-2024 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements the parameter attribute class.</summary>

using System;

namespace Dataescher.CommandLineInterface {
	/// <summary>Attribute for parameter.</summary>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
	public class ParameterAttribute : Attribute {
		/// <summary>Gets or sets the description.</summary>
		public String Description { get; set; }

		/// <summary>Initializes a new instance of the Dataescher.ParameterAttribute class.</summary>
		/// <param name="description">The description.</param>
		public ParameterAttribute(String description) {
			Description = description;
		}
	}
}