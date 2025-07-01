// <copyright file="IInheritableKeyItem.cs" company="Dataescher">
// Copyright (c) 2025 Dataescher. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>2/19/2025</date>
// <summary>Declares the IInheritableKeyItem interface.</summary>

namespace Dataescher.Collections {
	/// <summary>An interface for an object which can be inherited by a dictionary type object.</summary>
	/// <typeparam name="TKey">Type of the key.</typeparam>
	/// <typeparam name="TParent">Type of the parent.</typeparam>
	public interface IInheritableKeyItem<TKey, TParent> : IInheritableItem<TParent> where TParent : class {
		/// <summary>Gets the key.</summary>
		TKey Key { get; }
	}
}