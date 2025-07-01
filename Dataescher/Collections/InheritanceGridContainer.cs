// <copyright file="GridContainer.cs" company="Dataescher">
// Copyright (c) 2025 Dataescher. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>5/22/2025</date>
// <summary>Implements an inheritable, enumerable 2D array container.</summary>

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace Dataescher.Collections {
	/// <summary>Container for inheritance grids.</summary>
	/// <content>A class which represents a multiple-item 2D array.</content>
	/// <typeparam name="TParent">Type of the parent.</typeparam>
	/// <typeparam name="TItem">Type of the item.</typeparam>
	/// <seealso cref="T:ICloneable"/>
	/// <seealso cref="T:System.Collections.Generic.IEnumerable{TItem}"/>
	/// <seealso cref="T:System.ComponentModel.INotifyPropertyChanged"/>
	public class InheritanceGridContainer<TParent, TItem> : ICloneable, IEnumerable<TItem>, INotifyPropertyChanged
		where TItem : class, ICloneable, IInheritableItem<TParent>
		where TParent : InheritanceGridContainer<TParent, TItem>, new() {
		/// <summary>Array of panels.</summary>
		private TItem[,] _slots;

		/// <summary>The assigned panels.</summary>
		private Dictionary<TItem, List<Point>> _assignments;

		/// <summary>(Immutable) A dictionary linking panel indexes to panels.</summary>
		private readonly List<TItem> _items;

		/// <summary>Gets the items.</summary>
		public List<TItem> Items {
			get {
				List<TItem> result = new();
				foreach (TItem item in _items) {
					result.Add(item);
				}
				return result;
			}
		}

		/// <summary>Get the index of the given item.</summary>
		/// <param name="item">The item.</param>
		/// <returns>The index of the panel in this collection. If panel not in this collection, returns -1.</returns>
		public Int32 IndexOf(TItem item) {
			return _items.IndexOf(item);
		}

		/// <summary>Validates the coordinates described by location.</summary>
		/// <exception cref="ArgumentOutOfRangeException">
		///     Thrown when one or more arguments are outside the required range.
		/// </exception>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		private void ValidateCoordinates(Int32 x, Int32 y) {
			if ((x < 0) || (x >= Width)) {
				throw new ArgumentOutOfRangeException(nameof(x));
			}
			if ((y < 0) || (y >= Height)) {
				throw new ArgumentOutOfRangeException(nameof(y));
			}
		}

		/// <summary>Query if this object contains the given item.</summary>
		/// <param name="item">The item.</param>
		/// <returns>True if the object is in this collection, false if not.</returns>
		public Boolean Contains(TItem item) {
			return _assignments.ContainsKey(item);
		}

		/// <summary>Gets the number of items assigned to this collection.</summary>
		public Int32 Count => _items.Count;

		/// <summary>Indexer to get items within this collection using array index syntax.</summary>
		/// <param name="index">Zero-based index of the entry to access.</param>
		/// <returns>The indexed item.</returns>
		public TItem this[Int32 index] => (index < 0) || (index >= _items.Count) ? throw new ArgumentOutOfRangeException(nameof(index)) : _items[index];

		/// <summary>Indexer to get or set items within this collection using array index syntax.</summary>
		/// <param name="location">The location.</param>
		/// <returns>The indexed item.</returns>
		public TItem this[Point location] {
			get => this[location.X, location.Y];
			set {
				this[location.X, location.Y] = value;
				OnPropertyChanged("Item[]");
			}
		}

		/// <summary>Indexer to get items within this collection using array index syntax.</summary>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <returns>The indexed item.</returns>
		public TItem this[Int32 x, Int32 y] {
			get {
				ValidateCoordinates(x, y);
				return _slots[x, y];
			}
			set {
				ValidateCoordinates(x, y);
				Point location = new(x, y);
				if (value is not null) {
					if (!Contains(value)) {
						_items.Add(value);
						value.Parent = this as TParent;
					}
				}
				TItem oldItem = _slots[x, y];
				if (oldItem is TItem existingItem) {
					if (existingItem == value) {
						return;
					}
					_slots[x, y] = null;
					if (_assignments.TryGetValue(existingItem, out List<Point> panelAssignments)) {
						panelAssignments.Remove(location);
					}
				}
				if (value is null) {
					if (!Contains(oldItem)) {
						// The old panel is no longer a member of the collection
						_items.Remove(oldItem);
						oldItem.Parent = null;
					}
					return;
				}
				_slots[x, y] = value;
				if (_assignments.TryGetValue(value, out List<Point> existingItemAssignments)) {
					if (!existingItemAssignments.Contains(location)) {
						existingItemAssignments.Add(location);
					}
				} else {
					_assignments.Add(value, new List<Point>() { location });
				}
				OnPropertyChanged("Item[]");
			}
		}

		/// <summary>Clears this object to its blank/initial state.</summary>
		public void Clear() {
			for (Int32 y = 0; y < Height; y++) {
				for (Int32 x = 0; x < Width; x++) {
					_slots[x, y] = null;
				}
			}
			_assignments.Clear();
			_items.Clear();
			OnPropertyChanged("Item[]");
		}

		/// <summary>Gets or sets the width.</summary>
		public Int32 Width {
			get => _slots.GetLength(0);
			set {
				Size = new(value, Height);
				OnPropertyChanged(nameof(Width));
			}
		}

		/// <summary>Gets or sets the height.</summary>
		public Int32 Height {
			get => _slots.GetLength(1);
			set {
				Size = new(Width, value);
				OnPropertyChanged(nameof(Height));
			}
		}

		/// <summary>Gets or sets the size (width, height).</summary>
		/// <exception cref="ArgumentException">
		///     Thrown when one or more arguments have unsupported or illegal values.
		/// </exception>
		public Size Size {
			get => new(Width, Height);
			set {
				if ((value.Width == Width) && (value.Height == Height)) {
					return;
				}
				if (value.Width < 0) {
					throw new ArgumentException("Width cannot be less than 0.");
				}
				if (value.Height < 0) {
					throw new ArgumentException("Height cannot be less than 0.");
				}
				Dictionary<TItem, List<Point>> newAssignments = new();
				TItem[,] oldItemArray = _slots;
				TItem[,] newItemArray = new TItem[value.Width, value.Height];
				Size intersectionSize = new(Math.Min(value.Width, Width), Math.Min(value.Height, Height));
				for (Int32 intersectY = 0; intersectY < intersectionSize.Height; intersectY++) {
					for (Int32 intersectX = 0; intersectX < intersectionSize.Width; intersectX++) {
						Point intersectPoint = new(intersectX, intersectY);
						TItem item = oldItemArray[intersectX, intersectY];
						newItemArray[intersectX, intersectY] = item;
						if (newAssignments.TryGetValue(item, out List<Point> existingItemAssignments)) {
							existingItemAssignments.Add(intersectPoint);
						} else {
							newAssignments.Add(item, new List<Point>() { intersectPoint });
						}
					}
				}
				_assignments = newAssignments;
				_slots = newItemArray;
				OnPropertyChanged(nameof(Size));
			}
		}

		/// <summary>Gets all locations occupied by the specified item.</summary>
		/// <param name="item">The item.</param>
		/// <returns>The locations.</returns>
		public List<Point> GetSlotLocations(TItem item) {
			List<Point> result = new();
			if (_assignments.TryGetValue(item, out List<Point> points)) {
				// Make a deep copy of all the assigned points
				foreach (Point point in points) {
					result.Add(new Point(point.X, point.Y));
				}
			}
			return result;
		}

		/// <summary>Gets the available slots.</summary>
		public List<Point> AvailableSlots {
			get {
				List<Point> result = new();
				for (Int32 y = 0; y < Height; y++) {
					for (Int32 x = 0; x < Width; x++) {
						if (_slots[x, y] is null) {
							result.Add(new(x, y));
						}
					}
				}
				return result;
			}
		}

		/// <summary>Gets the number of available slots.</summary>
		public Int32 AvailableSlotCount {
			get {
				Int32 result = 0;
				for (Int32 y = 0; y < Height; y++) {
					for (Int32 x = 0; x < Width; x++) {
						if (_slots[x, y] is null) {
							result++;
						}
					}
				}
				return result;
			}
		}

		/// <summary>Gets the occupied slots.</summary>
		public List<Point> OccupiedSlots {
			get {
				List<Point> result = new();
				for (Int32 y = 0; y < Height; y++) {
					for (Int32 x = 0; x < Width; x++) {
						if (_slots[x, y] is not null) {
							result.Add(new(x, y));
						}
					}
				}
				return result;
			}
		}

		/// <summary>Gets the number of occupied slots.</summary>
		public Int32 OccupiedSlotCount {
			get {
				Int32 result = 0;
				for (Int32 y = 0; y < Height; y++) {
					for (Int32 x = 0; x < Width; x++) {
						if (_slots[x, y] is not null) {
							result++;
						}
					}
				}
				return result;
			}
		}

		/// <summary>Swaps the indices for two items.</summary>
		/// <exception cref="ArgumentOutOfRangeException">
		///     Thrown when one or more arguments are outside the required range.
		/// </exception>
		/// <param name="indexA">The index a.</param>
		/// <param name="indexB">The index b.</param>
		public void Swap(Int32 indexA, Int32 indexB) {
			if ((indexA < 0) || (indexA >= _items.Count)) {
				throw new ArgumentOutOfRangeException(nameof(indexA));
			}
			if ((indexB < 0) || (indexB >= _items.Count)) {
				throw new ArgumentOutOfRangeException(nameof(indexB));
			}
		}

		/// <summary>Swaps the indices for two items.</summary>
		/// <exception cref="ArgumentException">
		///     Thrown when one or more arguments have unsupported or illegal values.
		/// </exception>
		/// <param name="itemA">The first item to process.</param>
		/// <param name="itemB">The second item to process.</param>
		public void Swap(TItem itemA, TItem itemB) {
			Int32 locA = _items.IndexOf(itemA);
			if (locA < 0) {
				throw new ArgumentException("Not part of this collection.", nameof(itemA));
			}
			Int32 locB = _items.IndexOf(itemB);
			if (locB < 0) {
				throw new ArgumentException("Not part of this collection.", nameof(itemB));
			}
			_items.Remove(itemA);
			_items.Insert(locA, itemB);
			_items.Remove(itemB);
			_items.Insert(locB, itemA);
			OnPropertyChanged("Item[]");
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="InheritanceGridContainer{TParent, TItem}"/> class.
		/// </summary>
		/// <param name="size">The size (width, height).</param>
		public InheritanceGridContainer(Size size) : this(size.Width, size.Height) { }

		/// <summary>
		///     Initializes a new instance of the <see cref="InheritanceGridContainer{TParent, TItem}"/> class.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">
		///     Thrown when one or more arguments are outside the required range.
		/// </exception>
		/// <param name="width">The width.</param>
		/// <param name="height">The height.</param>
		public InheritanceGridContainer(Int32 width, Int32 height) {
			if (width < 0) {
				throw new ArgumentOutOfRangeException(nameof(width), "Value must not be less than zero.");
			}
			if (height < 0) {
				throw new ArgumentOutOfRangeException(nameof(height), "Value must not be less than zero.");
			}
			_slots = new TItem[width, height];
			_assignments = new();
			_items = new();
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="InheritanceGridContainer{TParent, TItem}"/> class.
		/// </summary>
		public InheritanceGridContainer() {
			_slots = new TItem[0, 0];
			_assignments = new();
			_items = new();
		}

		/// <summary>Creates a new object that is a copy of the current instance.</summary>
		/// <returns>A new object that is a copy of this instance.</returns>
		public virtual Object Clone() {
			TParent newContainer = new() {
				Size = Size
			};
			foreach (TItem oldItem in _items) {
				List<Point> points = _assignments[oldItem];
				TItem newItem = oldItem.Clone() as TItem;
				List<Point> newPoints = new();
				foreach (Point pt in points) {
					newContainer._slots[pt.X, pt.Y] = newItem;
					newPoints.Add(pt);
				}
				newContainer._assignments.Add(newItem, newPoints);
				newContainer._items.Add(newItem);
				newItem.Parent = newContainer;
			}
			return newContainer;
		}

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<TItem> GetEnumerator() {
			return ((IEnumerable<TItem>)_items).GetEnumerator();
		}

		/// <summary>Returns an enumerator that iterates through a collection.</summary>
		/// <returns>
		///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
		/// </returns>
		IEnumerator IEnumerable.GetEnumerator() {
			return ((IEnumerable)_items).GetEnumerator();
		}

		/// <summary>Occurs when a property value changes.</summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>Executes the 'property changed' action.</summary>
		/// <param name="propertyName">Name of the property.</param>
		internal virtual void OnPropertyChanged(String propertyName) {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}