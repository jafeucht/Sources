// <copyright file="IInheritable.cs" company="Dataescher">
// Copyright (c) 2025 Dataescher. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>2/5/2025</date>
// <summary>Declares the IInheritable interface.</summary>

namespace Dataescher.Collections {
	/// <summary>An interface for an object which can be inherited by another object.</summary>
	/// <typeparam name="TParent">Type of the parent.</typeparam>
	public interface IInheritableItem<TParent> where TParent : class {
		/// <summary>Gets or sets the parent object.</summary>
		TParent Parent { get; set; }
	}
}