// <copyright file="BinaryMapNode.cs" company="Dataescher">
// Copyright (c) 2024 Dataescher. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>11/4/2024</date>
// <summary>Implements the binary map node class</summary>

using System;
using System.Collections.Generic;
using System.IO;

namespace Dataescher.Collections {
	/// <summary>A binary map node.</summary>
	/// <typeparam name="T">Generic type parameter.</typeparam>
	public class BinaryMapNode<T> {
		/// <summary>(Immutable) The number of bits per level.</summary>
		public const Int32 BITS_PER_LEVEL = 4;

		/// <summary>(Immutable) The number nodes per level.</summary>
		public const Int32 NODES_PER_LEVEL = 0x1 << BITS_PER_LEVEL;

		/// <summary>(Immutable) The bit mask for the maximum level.</summary>
		public const UInt32 LEVEL_MASK_LOW = 0xFFFFFFFF >> (32 - BITS_PER_LEVEL);

		/// <summary>The bit mask for the minimum level.</summary>
		public const UInt32 LEVEL_MASK_HIGH = 0xFFFFFFFF << (32 - BITS_PER_LEVEL);

		/// <summary>(Immutable) Maximum level.</summary>
		public const Int32 MAX_LEVEL = (32 / BITS_PER_LEVEL) - 1;

		/// <summary>Gets the level.</summary>
		public Int32 Level { get; private set; }

#nullable enable
		/// <summary>Gets the value.</summary>
		public T? Value { get; internal set; }
#nullable disable

		/// <summary>The nodes.</summary>
		public BinaryMapNode<T>[] Nodes;

		/// <summary>Gets the parent.</summary>
		public Object Parent { get; private set; }

		/// <summary>Gets or sets the root key.</summary>
		private UInt32 Key { get; set; }

		/// <summary>Gets the zero-based index of this object.</summary>
		public Int32 Index => Level == 0 ? 0 : (Int32)((Key >> ((MAX_LEVEL - Level + 1) * BITS_PER_LEVEL)) & LEVEL_MASK_LOW);

		/// <summary>Gets the owner collection.</summary>
		public BinaryMap<T> Owner => Parent is BinaryMapNode<T> parent ? parent.Owner : Parent is BinaryMap<T> owner ? owner : null;

		/// <summary>Gets a value indicating whether this object is empty.</summary>
		public Boolean IsEmpty {
			get {
				if (Value is not null) {
					return false;
				} else if (Nodes is null) {
					return true;
				} else {
					for (Int32 nodeIdx = 0; nodeIdx < NODES_PER_LEVEL; nodeIdx++) {
						if (Nodes[nodeIdx] is not null) {
							return false;
						}
					}
					return true;
				}
			}
		}

		/// <summary>Gets the number of elements in this collection.</summary>
		public Int32 Count {
			get {
				if (Value is not null) {
					return 1;
				} else {
					Int32 result = 0;
					for (Int32 nodeIdx = 0; nodeIdx < NODES_PER_LEVEL; nodeIdx++) {
						if (Nodes[nodeIdx] is BinaryMapNode<T> node) {
							result += node.Count;
						}
					}
					return result;
				}
			}
		}

		/// <summary>Initializes a new instance of the <see cref="BinaryMapNode{T}"/> class.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <param name="parent">The parent.</param>
		/// <param name="level">The node level.</param>
		/// <param name="key">The key to add.</param>
		/// <param name="data">The data to emplace.</param>
		public BinaryMapNode(Object parent, Int32 level, UInt32 key, T data) {
			if (data is null) {
				throw new Exception("Cannot create a new node level with null data.");
			}
			Parent = parent;
			Value = data;
			Level = level;
			Nodes = null;
			Key = key;
		}

		/// <summary>Query if this object contains the given key.</summary>
		/// <param name="key">The key.</param>
		/// <returns>True if the object is in this collection, false if not.</returns>
		public Boolean Contains(UInt32 key) {
			if ((Value is not null) && (Key == key)) {
				return true;
			}
			Int32 nodeIdx = (Int32)((key >> (Level * BITS_PER_LEVEL)) & LEVEL_MASK_LOW);
			return (Nodes is not null) && (Nodes[nodeIdx] is BinaryMapNode<T> node) && node.Contains(key);
		}

		/// <summary>Adds the given key and data.</summary>
		/// <exception cref="ArgumentException">
		///     Thrown when one or more arguments have unsupported or illegal values.
		/// </exception>
		/// <param name="key">The key.</param>
		/// <param name="value">[out] The value.</param>
		internal void Add(UInt32 key, T value) {
			if (Value is T data) {
				if (Key == key) {
					throw new ArgumentException($"Key 0x{key:X8} already exists in this collection.", nameof(key));
				}
				// Move the data to a different level
				Value = default;
				Nodes = new BinaryMapNode<T>[NODES_PER_LEVEL];
				Byte prevNodeIdx = (Byte)((Key >> ((MAX_LEVEL - Level) * BITS_PER_LEVEL)) & LEVEL_MASK_LOW);
				Nodes[prevNodeIdx] = new(this, Level + 1, Key, data);
				if (Level == 0) {
					Key = 0;
				} else {
					Key &= 0xFFFFFFFF << ((MAX_LEVEL - (Level - 1)) * BITS_PER_LEVEL);
				}
			}
			Byte nodeIdx = (Byte)((key >> ((MAX_LEVEL - Level) * BITS_PER_LEVEL)) & LEVEL_MASK_LOW);
			if (Nodes[nodeIdx] is BinaryMapNode<T> node) {
				node.Add(key, value);
			} else {
				Nodes[nodeIdx] = new(this, Level + 1, key, value);
			}
		}

		/// <summary>Removes the given key and any associated data.</summary>
		/// <param name="key">The key.</param>
		internal void Remove(UInt32 key) {
			if (Value is not null) {
				if (Key == key) {
					// Found the item
					Value = default;
					if (Parent is BinaryMapNode<T> parent) {
						parent.Nodes[Index] = null;
					}
				}
				return;
			}
			Int32 nodeIdx = (Int32)((key >> ((MAX_LEVEL - Level) * BITS_PER_LEVEL)) & LEVEL_MASK_LOW);
			if (Nodes[nodeIdx] is BinaryMapNode<T> node) {
				node.Remove(key);
				if (node.IsEmpty) {
					Nodes[nodeIdx] = null;
				}
			}
		}

		/// <summary>Converts the binary map to a list.</summary>
		/// <param name="list">The list.</param>
		internal void ToList(List<T> list) {
			if (Value is T data) {
				list.Add(data);
			} else if (Nodes is not null) {
				foreach (BinaryMapNode<T> node in Nodes) {
					if (node is not null) {
						node.ToList(list);
					}
				}
			}
		}

		/// <summary>Attempts to get the value for a key.</summary>
		/// <param name="key">The key.</param>
		/// <param name="mode">The lookup mode.</param>
		/// <param name="value">[out] The value.</param>
		/// <returns>True if it succeeds, false if it fails.</returns>
		internal Boolean TryGetNode(UInt32 key, BinaryMapLookupMode mode, out BinaryMapNode<T> value) {
			Boolean result = false;
			value = null;
			if (Value is not null) {
				if (Key == key) {
					value = this;
					return true;
				} else if ((mode == BinaryMapLookupMode.ExactOrNextKey) && (Key > key)) {
					value = this;
					return true;
				} else if ((mode == BinaryMapLookupMode.ExactOrPreviousKey) && (Key < key)) {
					value = this;
					return true;
				} else {
					return false;
				}
			}
			Byte nodeIdx = (Byte)((key >> ((MAX_LEVEL - Level) * BITS_PER_LEVEL)) & LEVEL_MASK_LOW);
			if (Nodes[nodeIdx] is BinaryMapNode<T> node) {
				result = node.TryGetNode(key, mode, out value);
			}
			if (!result) {
				if (mode == BinaryMapLookupMode.ExactOrNextKey) {
					for (Int32 nextNodeIdx = nodeIdx + 1; nextNodeIdx < NODES_PER_LEVEL; nextNodeIdx++) {
						if (Nodes[nextNodeIdx] is BinaryMapNode<T> nextNode) {
							value = nextNode.FirstNode;
							result = true;
							break;
						}
					}
				} else if (mode == BinaryMapLookupMode.ExactOrPreviousKey) {
					for (Int32 previousNodeIdx = nodeIdx - 1; previousNodeIdx >= 0; previousNodeIdx--) {
						if (Nodes[previousNodeIdx] is BinaryMapNode<T> previousNode) {
							value = previousNode.LastNode;
							result = true;
							break;
						}
					}
				}
			}
			if (!result) {
				if (mode == BinaryMapLookupMode.ExactOrPreviousKey) {
					value = PreviousNode;
				} else if (mode == BinaryMapLookupMode.ExactOrNextKey) {
					value = NextNode;
				}
				result = value is not null;
			}
			return result;
		}

		/// <summary>Attempts to get the value for a key.</summary>
		/// <param name="key">The key.</param>
		/// <param name="mode">The lookup mode.</param>
		/// <param name="value">[out] The value.</param>
		/// <returns>True if it succeeds, false if it fails.</returns>
		internal Boolean TryGetValue(UInt32 key, BinaryMapLookupMode mode, out T value) {
			Boolean result = TryGetNode(key, mode, out BinaryMapNode<T> node);
			if (result && (node is not null)) {
				if (node.Value is T data) {
					value = data;
					return true;
				}
			}
			value = default;
			return false;
		}

		/// <summary>Gets the first item in the collection.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		public T First {
			get {
				BinaryMapNode<T> firstNode = FirstNode;
				if (firstNode is not null) {
					if (firstNode.Value is T data) {
						return data;
					}
				}
				throw new Exception("Collection is empty.");
			}
		}

		/// <summary>Gets the first item in the collection.</summary>
		public BinaryMapNode<T> FirstNode {
			get {
				if (Value is not null) {
					return this;
				} else {
					for (Int32 nodeIdx = 0; nodeIdx < NODES_PER_LEVEL; nodeIdx++) {
						if (Nodes[nodeIdx] is BinaryMapNode<T> node) {
							return node.FirstNode;
						}
					}
					return null;
				}
			}
		}

		/// <summary>Gets the last item in the collection.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		public T Last {
			get {
				BinaryMapNode<T> lastNode = LastNode;
				if (lastNode is not null) {
					if (lastNode.Value is T data) {
						return data;
					}
				}
				throw new Exception("Collection is empty.");
			}
		}

		/// <summary>Gets the last data node.</summary>
		public BinaryMapNode<T> LastNode {
			get {
				if (Value is not null) {
					return this;
				} else {
					for (Int32 nodeIdx = NODES_PER_LEVEL - 1; nodeIdx >= 0; nodeIdx--) {
						if (Nodes[nodeIdx] is BinaryMapNode<T> node) {
							return node.LastNode;
						}
					}
					return null;
				}
			}
		}

		/// <summary>Deletes this object.</summary>
		public void Delete() {
			if (Parent is BinaryMapNode<T> parentNode) {
				Int32 nodeIdx = (Int32)((Key >> ((MAX_LEVEL - parentNode.Level) * BITS_PER_LEVEL)) & LEVEL_MASK_LOW);
				parentNode.Nodes[nodeIdx] = null;
				if (parentNode.IsEmpty) {
					parentNode.Delete();
				}
			} else if (Parent is BinaryMap<T> binaryMap) {
				binaryMap._root = null;
			}
		}

		/// <summary>Move to the next item in the collection.</summary>
		public BinaryMapNode<T> NextNode {
			get {
				// Since this is a branch termination node, need to traverse backwards
				BinaryMapNode<T> curNode = this;
				Int32 curIdx = Index;
				BinaryMapNode<T> parentNode = curNode.Parent as BinaryMapNode<T>;
				while (parentNode is not null) {
					for (Int32 parentIdx = curIdx + 1; parentIdx < NODES_PER_LEVEL; parentIdx++) {
						if (parentNode.Nodes[parentIdx] is BinaryMapNode<T> node) {
							return node.FirstNode;
						}
					}
					curNode = parentNode;
					curIdx = parentNode.Index;
					parentNode = curNode.Parent as BinaryMapNode<T>;
				}
				return null;
			}
		}

		/// <summary>Move to the previous item in the collection.</summary>
		public BinaryMapNode<T> PreviousNode {
			get {
				// Since this is a branch termination node, need to traverse backwards
				BinaryMapNode<T> curNode = this;
				Int32 curIdx = Index;
				BinaryMapNode<T> parentNode = curNode.Parent as BinaryMapNode<T>;
				while (parentNode is not null) {
					for (Int32 nodeIdx = curIdx - 1; nodeIdx >= 0; nodeIdx--) {
						if (parentNode.Nodes[nodeIdx] is BinaryMapNode<T> node) {
							return node.LastNode;
						}
					}
					curNode = parentNode;
					curIdx = curNode.Index;
					parentNode = curNode.Parent as BinaryMapNode<T>;
				}
				return null;
			}
		}

		/// <summary>Print this object structure to a stream.</summary>
		/// <param name="streamWriter">The stream writer.</param>
		internal void PrintToStream(StreamWriter streamWriter) {
			String indent = new('\t', Level);
			if (Nodes is not null) {
				Int32 nodeCnt = 0;
				for (Int32 nodeIdx = 0; nodeIdx < NODES_PER_LEVEL; nodeIdx++) {
					if (Nodes[nodeIdx] is not null) {
						nodeCnt++;
					}
				}
				String addrString;
				if (Level == 0) {
					addrString = "Root";
				} else {
					UInt32 levelRootKey = Key >> ((MAX_LEVEL - (Level - 1)) * BITS_PER_LEVEL);
					addrString = "0x" + levelRootKey.ToString($"X{Level}");
				}
				streamWriter.WriteLine($"{indent}{addrString}: Level {Level} Index {Index}, {nodeCnt} node(s)");
				for (Int32 nodeIdx = 0; nodeIdx < NODES_PER_LEVEL; nodeIdx++) {
					if (Nodes[nodeIdx] is not null) {
						Nodes[nodeIdx].PrintToStream(streamWriter);
					}
				}
			} else if (Value is not null) {
				streamWriter.WriteLine($"{indent}0x{Key:X8}: {Value}");
			} else {
				streamWriter.WriteLine($"{indent}0x{Key:X8}: Level {Level} Index {Index}, INVALID NODE");
			}
		}
	}
}