// <copyright file="CRC_Engine.cs" company="Dataescher">
// Copyright (c) 2022-2024 Dataescher. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>9/28/2024</date>
// <summary>Implements CRC engines for various data widths.</summary>

using System;
using System.IO;

#nullable enable

namespace Dataescher.Data.Integrity {
	/// <summary>An 8-bit CRC engine.</summary>
	/// <seealso cref="T:Dataescher.Data.Integrity.CRC"/>
	public class CRC8 : CRC {
		/// <summary>The CRC table.</summary>
		private Byte[]? CRC_Table { get; set; }
		/// <summary>(Immutable) The polynomial (reflected if required).</summary>
		private readonly Byte _poly;
		/// <summary>The polynomial.</summary>
		public Byte Polynomial { get; private set; }
		/// <summary>(Immutable) The initial CRC value (reflected if required).</summary>
		private readonly Byte _init;
		/// <summary>The initial CRC value.</summary>
		public Byte InitialValue { get; private set; }
		/// <summary>The final XOR value.</summary>
		public Byte XorOut { get; private set; }
		/// <summary>If reflected, CRC is lsb first; otherwise, msb first.</summary>
		public Boolean Reflected { get; private set; }
		/// <summary>The number of bits.</summary>
		public static Int32 DataBits => 8;
		/// <summary>The number of bytes.</summary>
		public static Int32 DataBytes => 1;
		/// <summary>Initializes a new instance of the <see cref="CRC8"/> class.</summary>
		/// <param name="polynomial">The polynomial.</param>
		/// <param name="initialValue">The initial value.</param>
		/// <param name="reflected">True if reflected.</param>
		/// <param name="xorOut">The exclusive-or out.</param>
		public CRC8(Byte polynomial, Byte initialValue, Boolean reflected, Byte xorOut) {
			Reflected = reflected;
			Polynomial = polynomial;
			_poly = Reflected ? Reflect(polynomial) : polynomial;
			InitialValue = initialValue;
			_init = Reflected ? Reflect(initialValue) : initialValue;
			XorOut = xorOut;
			CRC_Table = null;
		}
		/// <summary>Reverse the order of the bits in a Byte value.</summary>
		/// <param name="value">The value for which to reverse the order of the bits.</param>
		/// <returns>The Byte value with the bit order reversed.</returns>
		private static Byte Reflect(Byte value) {
			Byte b1 = 0x80;
			Byte b2 = 1;
			Byte result = 0;
			for (Int32 bitIdx = 0; bitIdx < 8; bitIdx++) {
				if ((value & b1) != 0) {
					result |= b2;
				}
				b1 >>= 1;
				b2 <<= 1;
			}
			return result;
		}
		/// <summary>Compute the CRC for an array of bytes.</summary>
		/// <param name="data">The array of bytes.</param>
		/// <returns>The CRC for the array of bytes.</returns>
		public Byte Compute(Byte[] data) {
			return Compute(new Memory(data), (Byte)(_init ^ XorOut));
		}
		/// <summary>Compute the CRC for an array of bytes.</summary>
		/// <param name="data">The array of bytes.</param>
		/// <returns>The CRC for the array of bytes.</returns>
		public Byte Compute(Memory data) {
			return Compute(data, (Byte)(_init ^ XorOut));
		}
		/// <summary>Compute the CRC for an array of bytes.</summary>
		/// <param name="data">The array of bytes.</param>
		/// <param name="initCRC">The initial CRC.</param>
		/// <returns>The CRC for the array of bytes.</returns>
		public Byte Compute(Byte[] data, Byte initCRC) {
			return Compute(new Memory(data), initCRC);
		}

		/// <summary>Calculates the CRC for a file.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <param name="filePath">Path of the file.</param>
		/// <returns>The calculated CCITT CRC.</returns>
		public Byte ComputeForFile(String filePath) {
			filePath = Path.GetFullPath(filePath);
			if (!File.Exists(filePath)) {
				throw new Exception($"File \"{filePath}\" does not exist.");
			}
			Int64 fileSize = new FileInfo(filePath).Length;
			using (FileStream fs = new(filePath, FileMode.Open, FileAccess.Read)) {
				using (BinaryReader br = new(fs)) {
					// Compute the CRC for the file
					return Compute(br.ReadBytes((Int32)fileSize), (Byte)(_init ^ XorOut));
				}
			}
		}

		/// <summary>Compute the CRC for an array of bytes.</summary>
		/// <exception cref="ArgumentOutOfRangeException">
		///     Thrown when one or more arguments are outside the required range.
		/// </exception>
		/// <param name="data">The data.</param>
		/// <param name="initCRC">The initial CRC.</param>
		/// <param name="offset">The offset into the data.</param>
		/// <param name="cnt">Number of bytes to compute CRC for.</param>
		/// <returns>The CRC for the memory.</returns>
		public Byte Compute(Byte[] data, Byte initCRC, Int32 offset, Int32 cnt) {
			UInt32 startIdx;
			UInt32 endIdx;
			if (offset >= 0) {
				startIdx = (UInt32)offset;
				if (startIdx >= data.Length) {
					throw new ArgumentOutOfRangeException(nameof(offset), "Read past end of the array.");
				}
			} else {
				startIdx = 0;
			}
			if (cnt >= 0) {
				endIdx = (UInt32)(startIdx + cnt - 1);
				if (endIdx >= data.Length) {
					throw new ArgumentOutOfRangeException(nameof(cnt), "Read past end of the array.");
				}
			} else {
				endIdx = (UInt32)(data.Length - 1);
			}
			if (CRC_Table is null) {
				// Generate the CRC look-up table
				CRC_Table = new Byte[0x100];
				for (Int32 dividend = 0; dividend < 0x100; dividend++) {
					Byte thisValue = Reflected ? (Byte)dividend : (Byte)(dividend << 0);
					for (Int32 bitIn = 0; bitIn < 8; bitIn++) {
						if (Reflected) {
							if ((thisValue & 0x1) != 0) {
								thisValue = (Byte)((thisValue >> 1) ^ _poly);
							} else {
								thisValue >>= 1;
							}
						} else {
							if ((thisValue & 0x80) != 0) {
								thisValue = (Byte)((thisValue << 1) ^ _poly);
							} else {
								thisValue <<= 1;
							}
						}
					}
					CRC_Table[dividend] = thisValue;
				}
			}
			Byte crc = (Byte)(initCRC ^ XorOut);
			if (Reflected) {
				for (UInt32 byteIdx = startIdx; byteIdx <= endIdx; byteIdx++) {
					crc = (Byte)(CRC_Table[data[byteIdx] ^ (crc & 0xFF)] ^ (crc >> 8));
				}
			} else {
				for (UInt32 byteIdx = startIdx; byteIdx <= endIdx; byteIdx++) {
					crc = (Byte)(CRC_Table[data[byteIdx] ^ ((crc & 0xFF) >> (DataBits - 8))] ^ (crc << 8));
				}
			}
			return (Byte)(crc ^ XorOut);
		}

		/// <summary>Compute the CRC for memory.</summary>
		/// <exception cref="ArgumentOutOfRangeException">
		///     Thrown when one or more arguments are outside the required range.
		/// </exception>
		/// <param name="data">The array of bytes.</param>
		/// <param name="initCRC">The initial CRC.</param>
		/// <param name="offset">(Optional) The offset into the data. If negative, use 0.</param>
		/// <param name="cnt">(Optional) Number of bytes to compute CRC for.</param>
		/// <returns>The CRC for the memory.</returns>
		public Byte Compute(Memory data, Byte initCRC, Int32 offset = -1, Int32 cnt = -1) {
			UInt32 startIdx;
			UInt32 endIdx;
			if (offset >= 0) {
				startIdx = (UInt32)offset;
				if (startIdx >= data.Length) {
					throw new ArgumentOutOfRangeException(nameof(offset), "Read past end of the array.");
				}
			} else {
				startIdx = 0;
			}
			if (cnt >= 0) {
				endIdx = (UInt32)(startIdx + cnt - 1);
				if (endIdx >= data.Length) {
					throw new ArgumentOutOfRangeException(nameof(cnt), "Read past end of the array.");
				}
			} else {
				endIdx = (UInt32)(data.Length - 1);
			}
			if (CRC_Table is null) {
				// Generate the CRC look-up table
				CRC_Table = new Byte[0x100];
				for (Int32 dividend = 0; dividend < 0x100; dividend++) {
					Byte thisValue = Reflected ? (Byte)dividend : (Byte)(dividend << 0);
					for (Int32 bitIn = 0; bitIn < 8; bitIn++) {
						if (Reflected) {
							if ((thisValue & 0x1) != 0) {
								thisValue = (Byte)((thisValue >> 1) ^ _poly);
							} else {
								thisValue >>= 1;
							}
						} else {
							if ((thisValue & 0x80) != 0) {
								thisValue = (Byte)((thisValue << 1) ^ _poly);
							} else {
								thisValue <<= 1;
							}
						}
					}
					CRC_Table[dividend] = thisValue;
				}
			}
			Byte crc = (Byte)(initCRC ^ XorOut);
			if (Reflected) {
				for (UInt32 byteIdx = startIdx; byteIdx <= endIdx; byteIdx++) {
					crc = (Byte)(CRC_Table[data[byteIdx] ^ (crc & 0xFF)] ^ (crc >> 8));
				}
			} else {
				for (UInt32 byteIdx = startIdx; byteIdx <= endIdx; byteIdx++) {
					crc = (Byte)(CRC_Table[data[byteIdx] ^ ((crc & 0xFF) >> (DataBits - 8))] ^ (crc << 8));
				}
			}
			return (Byte)(crc ^ XorOut);
		}
		/// <summary>Calculates the CRC and returns a string representation.</summary>
		/// <param name="data">The data.</param>
		/// <returns>The calculated CRC as a string.</returns>
		public override String ComputeCRC(Memory data) {
			return $"0x{Compute(data):X2}";
		}

		/// <summary>Calculates the CRC and returns a string representation.</summary>
		/// <param name="data">The data.</param>
		/// <returns>The calculated CRC as a string.</returns>
		public override String ComputeCRC(Byte[] data) {
			return $"0x{Compute(new Memory(data)):X2}";
		}
	}
	/// <summary>An 16-bit CRC engine.</summary>
	/// <seealso cref="T:Dataescher.Data.Integrity.CRC"/>
	public class CRC16 : CRC {
		/// <summary>The CRC table.</summary>
		private UInt16[]? CRC_Table { get; set; }
		/// <summary>(Immutable) The polynomial (reflected if required).</summary>
		private readonly UInt16 _poly;
		/// <summary>The polynomial.</summary>
		public UInt16 Polynomial { get; private set; }
		/// <summary>(Immutable) The initial CRC value (reflected if required).</summary>
		private readonly UInt16 _init;
		/// <summary>The initial CRC value.</summary>
		public UInt16 InitialValue { get; private set; }
		/// <summary>The final XOR value.</summary>
		public UInt16 XorOut { get; private set; }
		/// <summary>If reflected, CRC is lsb first; otherwise, msb first.</summary>
		public Boolean Reflected { get; private set; }
		/// <summary>The number of bits.</summary>
		public static Int32 DataBits => 16;
		/// <summary>The number of bytes.</summary>
		public static Int32 DataBytes => 2;
		/// <summary>Initializes a new instance of the <see cref="CRC16"/> class.</summary>
		/// <param name="polynomial">The polynomial.</param>
		/// <param name="initialValue">The initial value.</param>
		/// <param name="reflected">True if reflected.</param>
		/// <param name="xorOut">The exclusive-or out.</param>
		public CRC16(UInt16 polynomial, UInt16 initialValue, Boolean reflected, UInt16 xorOut) {
			Reflected = reflected;
			Polynomial = polynomial;
			_poly = Reflected ? Reflect(polynomial) : polynomial;
			InitialValue = initialValue;
			_init = Reflected ? Reflect(initialValue) : initialValue;
			XorOut = xorOut;
			CRC_Table = null;
		}
		/// <summary>Reverse the order of the bits in a UInt16 value.</summary>
		/// <param name="value">The value for which to reverse the order of the bits.</param>
		/// <returns>The UInt16 value with the bit order reversed.</returns>
		private static UInt16 Reflect(UInt16 value) {
			UInt16 b1 = 0x8000;
			UInt16 b2 = 1;
			UInt16 result = 0;
			for (Int32 bitIdx = 0; bitIdx < 16; bitIdx++) {
				if ((value & b1) != 0) {
					result |= b2;
				}
				b1 >>= 1;
				b2 <<= 1;
			}
			return result;
		}
		/// <summary>Compute the CRC for an array of bytes.</summary>
		/// <param name="data">The array of bytes.</param>
		/// <returns>The CRC for the array of bytes.</returns>
		public UInt16 Compute(Byte[] data) {
			return Compute(new Memory(data), (UInt16)(_init ^ XorOut));
		}
		/// <summary>Compute the CRC for an array of bytes.</summary>
		/// <param name="data">The array of bytes.</param>
		/// <returns>The CRC for the array of bytes.</returns>
		public UInt16 Compute(Memory data) {
			return Compute(data, (UInt16)(_init ^ XorOut));
		}
		/// <summary>Compute the CRC for an array of bytes.</summary>
		/// <param name="data">The array of bytes.</param>
		/// <param name="initCRC">The initial CRC.</param>
		/// <returns>The CRC for the array of bytes.</returns>
		public UInt16 Compute(Byte[] data, UInt16 initCRC) {
			return Compute(new Memory(data), initCRC);
		}

		/// <summary>Calculates the CRC for a file.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <param name="filePath">Path of the file.</param>
		/// <returns>The calculated CCITT CRC.</returns>
		public UInt16 ComputeForFile(String filePath) {
			filePath = Path.GetFullPath(filePath);
			if (!File.Exists(filePath)) {
				throw new Exception($"File \"{filePath}\" does not exist.");
			}
			Int64 fileSize = new FileInfo(filePath).Length;
			using (FileStream fs = new(filePath, FileMode.Open, FileAccess.Read)) {
				using (BinaryReader br = new(fs)) {
					// Compute the CRC for the file
					return Compute(br.ReadBytes((Int32)fileSize), (UInt16)(_init ^ XorOut));
				}
			}
		}

		/// <summary>Compute the CRC for an array of bytes.</summary>
		/// <exception cref="ArgumentOutOfRangeException">
		///     Thrown when one or more arguments are outside the required range.
		/// </exception>
		/// <param name="data">The data.</param>
		/// <param name="initCRC">The initial CRC.</param>
		/// <param name="offset">The offset into the data.</param>
		/// <param name="cnt">Number of bytes to compute CRC for.</param>
		/// <returns>The CRC for the memory.</returns>
		public UInt16 Compute(Byte[] data, UInt16 initCRC, Int32 offset, Int32 cnt) {
			UInt32 startIdx;
			UInt32 endIdx;
			if (offset >= 0) {
				startIdx = (UInt32)offset;
				if (startIdx >= data.Length) {
					throw new ArgumentOutOfRangeException(nameof(offset), "Read past end of the array.");
				}
			} else {
				startIdx = 0;
			}
			if (cnt >= 0) {
				endIdx = (UInt32)(startIdx + cnt - 1);
				if (endIdx >= data.Length) {
					throw new ArgumentOutOfRangeException(nameof(cnt), "Read past end of the array.");
				}
			} else {
				endIdx = (UInt32)(data.Length - 1);
			}
			if (CRC_Table is null) {
				// Generate the CRC look-up table
				CRC_Table = new UInt16[0x100];
				for (Int32 dividend = 0; dividend < 0x100; dividend++) {
					UInt16 thisValue = Reflected ? (UInt16)dividend : (UInt16)(dividend << 8);
					for (Int32 bitIn = 0; bitIn < 8; bitIn++) {
						if (Reflected) {
							if ((thisValue & 0x1) != 0) {
								thisValue = (UInt16)((thisValue >> 1) ^ _poly);
							} else {
								thisValue >>= 1;
							}
						} else {
							if ((thisValue & 0x8000) != 0) {
								thisValue = (UInt16)((thisValue << 1) ^ _poly);
							} else {
								thisValue <<= 1;
							}
						}
					}
					CRC_Table[dividend] = thisValue;
				}
			}
			UInt16 crc = (UInt16)(initCRC ^ XorOut);
			if (Reflected) {
				for (UInt32 byteIdx = startIdx; byteIdx <= endIdx; byteIdx++) {
					crc = (UInt16)(CRC_Table[data[byteIdx] ^ (crc & 0xFF)] ^ (crc >> 8));
				}
			} else {
				for (UInt32 byteIdx = startIdx; byteIdx <= endIdx; byteIdx++) {
					crc = (UInt16)(CRC_Table[data[byteIdx] ^ ((crc & 0xFF00) >> (DataBits - 8))] ^ (crc << 8));
				}
			}
			return (UInt16)(crc ^ XorOut);
		}

		/// <summary>Compute the CRC for memory.</summary>
		/// <exception cref="ArgumentOutOfRangeException">
		///     Thrown when one or more arguments are outside the required range.
		/// </exception>
		/// <param name="data">The array of bytes.</param>
		/// <param name="initCRC">The initial CRC.</param>
		/// <param name="offset">(Optional) The offset into the data. If negative, use 0.</param>
		/// <param name="cnt">(Optional) Number of bytes to compute CRC for.</param>
		/// <returns>The CRC for the memory.</returns>
		public UInt16 Compute(Memory data, UInt16 initCRC, Int32 offset = -1, Int32 cnt = -1) {
			UInt32 startIdx;
			UInt32 endIdx;
			if (offset >= 0) {
				startIdx = (UInt32)offset;
				if (startIdx >= data.Length) {
					throw new ArgumentOutOfRangeException(nameof(offset), "Read past end of the array.");
				}
			} else {
				startIdx = 0;
			}
			if (cnt >= 0) {
				endIdx = (UInt32)(startIdx + cnt - 1);
				if (endIdx >= data.Length) {
					throw new ArgumentOutOfRangeException(nameof(cnt), "Read past end of the array.");
				}
			} else {
				endIdx = (UInt32)(data.Length - 1);
			}
			if (CRC_Table is null) {
				// Generate the CRC look-up table
				CRC_Table = new UInt16[0x100];
				for (Int32 dividend = 0; dividend < 0x100; dividend++) {
					UInt16 thisValue = Reflected ? (UInt16)dividend : (UInt16)(dividend << 8);
					for (Int32 bitIn = 0; bitIn < 8; bitIn++) {
						if (Reflected) {
							if ((thisValue & 0x1) != 0) {
								thisValue = (UInt16)((thisValue >> 1) ^ _poly);
							} else {
								thisValue >>= 1;
							}
						} else {
							if ((thisValue & 0x8000) != 0) {
								thisValue = (UInt16)((thisValue << 1) ^ _poly);
							} else {
								thisValue <<= 1;
							}
						}
					}
					CRC_Table[dividend] = thisValue;
				}
			}
			UInt16 crc = (UInt16)(initCRC ^ XorOut);
			if (Reflected) {
				for (UInt32 byteIdx = startIdx; byteIdx <= endIdx; byteIdx++) {
					crc = (UInt16)(CRC_Table[data[byteIdx] ^ (crc & 0xFF)] ^ (crc >> 8));
				}
			} else {
				for (UInt32 byteIdx = startIdx; byteIdx <= endIdx; byteIdx++) {
					crc = (UInt16)(CRC_Table[data[byteIdx] ^ ((crc & 0xFF00) >> (DataBits - 8))] ^ (crc << 8));
				}
			}
			return (UInt16)(crc ^ XorOut);
		}
		/// <summary>Calculates the CRC and returns a string representation.</summary>
		/// <param name="data">The data.</param>
		/// <returns>The calculated CRC as a string.</returns>
		public override String ComputeCRC(Memory data) {
			return $"0x{Compute(data):X4}";
		}

		/// <summary>Calculates the CRC and returns a string representation.</summary>
		/// <param name="data">The data.</param>
		/// <returns>The calculated CRC as a string.</returns>
		public override String ComputeCRC(Byte[] data) {
			return $"0x{Compute(new Memory(data)):X4}";
		}
	}
	/// <summary>An 32-bit CRC engine.</summary>
	/// <seealso cref="T:Dataescher.Data.Integrity.CRC"/>
	public class CRC32 : CRC {
		/// <summary>The CRC table.</summary>
		private UInt32[]? CRC_Table { get; set; }
		/// <summary>(Immutable) The polynomial (reflected if required).</summary>
		private readonly UInt32 _poly;
		/// <summary>The polynomial.</summary>
		public UInt32 Polynomial { get; private set; }
		/// <summary>(Immutable) The initial CRC value (reflected if required).</summary>
		private readonly UInt32 _init;
		/// <summary>The initial CRC value.</summary>
		public UInt32 InitialValue { get; private set; }
		/// <summary>The final XOR value.</summary>
		public UInt32 XorOut { get; private set; }
		/// <summary>If reflected, CRC is lsb first; otherwise, msb first.</summary>
		public Boolean Reflected { get; private set; }
		/// <summary>The number of bits.</summary>
		public static Int32 DataBits => 32;
		/// <summary>The number of bytes.</summary>
		public static Int32 DataBytes => 4;
		/// <summary>Initializes a new instance of the <see cref="CRC32"/> class.</summary>
		/// <param name="polynomial">The polynomial.</param>
		/// <param name="initialValue">The initial value.</param>
		/// <param name="reflected">True if reflected.</param>
		/// <param name="xorOut">The exclusive-or out.</param>
		public CRC32(UInt32 polynomial, UInt32 initialValue, Boolean reflected, UInt32 xorOut) {
			Reflected = reflected;
			Polynomial = polynomial;
			_poly = Reflected ? Reflect(polynomial) : polynomial;
			InitialValue = initialValue;
			_init = Reflected ? Reflect(initialValue) : initialValue;
			XorOut = xorOut;
			CRC_Table = null;
		}
		/// <summary>Reverse the order of the bits in a UInt32 value.</summary>
		/// <param name="value">The value for which to reverse the order of the bits.</param>
		/// <returns>The UInt32 value with the bit order reversed.</returns>
		private static UInt32 Reflect(UInt32 value) {
			UInt32 b1 = 0x80000000;
			UInt32 b2 = 1;
			UInt32 result = 0;
			for (Int32 bitIdx = 0; bitIdx < 32; bitIdx++) {
				if ((value & b1) != 0) {
					result |= b2;
				}
				b1 >>= 1;
				b2 <<= 1;
			}
			return result;
		}
		/// <summary>Compute the CRC for an array of bytes.</summary>
		/// <param name="data">The array of bytes.</param>
		/// <returns>The CRC for the array of bytes.</returns>
		public UInt32 Compute(Byte[] data) {
			return Compute(new Memory(data), _init ^ XorOut);
		}
		/// <summary>Compute the CRC for an array of bytes.</summary>
		/// <param name="data">The array of bytes.</param>
		/// <returns>The CRC for the array of bytes.</returns>
		public UInt32 Compute(Memory data) {
			return Compute(data, _init ^ XorOut);
		}
		/// <summary>Compute the CRC for an array of bytes.</summary>
		/// <param name="data">The array of bytes.</param>
		/// <param name="initCRC">The initial CRC.</param>
		/// <returns>The CRC for the array of bytes.</returns>
		public UInt32 Compute(Byte[] data, UInt32 initCRC) {
			return Compute(new Memory(data), initCRC);
		}

		/// <summary>Calculates the CRC for a file.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <param name="filePath">Path of the file.</param>
		/// <returns>The calculated CCITT CRC.</returns>
		public UInt32 ComputeForFile(String filePath) {
			filePath = Path.GetFullPath(filePath);
			if (!File.Exists(filePath)) {
				throw new Exception($"File \"{filePath}\" does not exist.");
			}
			Int64 fileSize = new FileInfo(filePath).Length;
			using (FileStream fs = new(filePath, FileMode.Open, FileAccess.Read)) {
				using (BinaryReader br = new(fs)) {
					// Compute the CRC for the file
					return Compute(br.ReadBytes((Int32)fileSize), _init ^ XorOut);
				}
			}
		}

		/// <summary>Compute the CRC for an array of bytes.</summary>
		/// <exception cref="ArgumentOutOfRangeException">
		///     Thrown when one or more arguments are outside the required range.
		/// </exception>
		/// <param name="data">The data.</param>
		/// <param name="initCRC">The initial CRC.</param>
		/// <param name="offset">The offset into the data.</param>
		/// <param name="cnt">Number of bytes to compute CRC for.</param>
		/// <returns>The CRC for the memory.</returns>
		public UInt32 Compute(Byte[] data, UInt32 initCRC, Int32 offset, Int32 cnt) {
			UInt32 startIdx;
			UInt32 endIdx;
			if (offset >= 0) {
				startIdx = (UInt32)offset;
				if (startIdx >= data.Length) {
					throw new ArgumentOutOfRangeException(nameof(offset), "Read past end of the array.");
				}
			} else {
				startIdx = 0;
			}
			if (cnt >= 0) {
				endIdx = (UInt32)(startIdx + cnt - 1);
				if (endIdx >= data.Length) {
					throw new ArgumentOutOfRangeException(nameof(cnt), "Read past end of the array.");
				}
			} else {
				endIdx = (UInt32)(data.Length - 1);
			}
			if (CRC_Table is null) {
				// Generate the CRC look-up table
				CRC_Table = new UInt32[0x100];
				for (Int32 dividend = 0; dividend < 0x100; dividend++) {
					UInt32 thisValue = Reflected ? (UInt32)dividend : (UInt32)(dividend << 24);
					for (Int32 bitIn = 0; bitIn < 8; bitIn++) {
						if (Reflected) {
							if ((thisValue & 0x1) != 0) {
								thisValue = (thisValue >> 1) ^ _poly;
							} else {
								thisValue >>= 1;
							}
						} else {
							if ((thisValue & 0x80000000) != 0) {
								thisValue = (thisValue << 1) ^ _poly;
							} else {
								thisValue <<= 1;
							}
						}
					}
					CRC_Table[dividend] = thisValue;
				}
			}
			UInt32 crc = initCRC ^ XorOut;
			if (Reflected) {
				for (UInt32 byteIdx = startIdx; byteIdx <= endIdx; byteIdx++) {
					crc = CRC_Table[data[byteIdx] ^ (crc & 0xFF)] ^ (crc >> 8);
				}
			} else {
				for (UInt32 byteIdx = startIdx; byteIdx <= endIdx; byteIdx++) {
					crc = CRC_Table[data[byteIdx] ^ ((crc & 0xFF000000) >> (DataBits - 8))] ^ (crc << 8);
				}
			}
			return crc ^ XorOut;
		}

		/// <summary>Compute the CRC for memory.</summary>
		/// <exception cref="ArgumentOutOfRangeException">
		///     Thrown when one or more arguments are outside the required range.
		/// </exception>
		/// <param name="data">The array of bytes.</param>
		/// <param name="initCRC">The initial CRC.</param>
		/// <param name="offset">(Optional) The offset into the data. If negative, use 0.</param>
		/// <param name="cnt">(Optional) Number of bytes to compute CRC for.</param>
		/// <returns>The CRC for the memory.</returns>
		public UInt32 Compute(Memory data, UInt32 initCRC, Int32 offset = -1, Int32 cnt = -1) {
			UInt32 startIdx;
			UInt32 endIdx;
			if (offset >= 0) {
				startIdx = (UInt32)offset;
				if (startIdx >= data.Length) {
					throw new ArgumentOutOfRangeException(nameof(offset), "Read past end of the array.");
				}
			} else {
				startIdx = 0;
			}
			if (cnt >= 0) {
				endIdx = (UInt32)(startIdx + cnt - 1);
				if (endIdx >= data.Length) {
					throw new ArgumentOutOfRangeException(nameof(cnt), "Read past end of the array.");
				}
			} else {
				endIdx = (UInt32)(data.Length - 1);
			}
			if (CRC_Table is null) {
				// Generate the CRC look-up table
				CRC_Table = new UInt32[0x100];
				for (Int32 dividend = 0; dividend < 0x100; dividend++) {
					UInt32 thisValue = Reflected ? (UInt32)dividend : (UInt32)(dividend << 24);
					for (Int32 bitIn = 0; bitIn < 8; bitIn++) {
						if (Reflected) {
							if ((thisValue & 0x1) != 0) {
								thisValue = (thisValue >> 1) ^ _poly;
							} else {
								thisValue >>= 1;
							}
						} else {
							if ((thisValue & 0x80000000) != 0) {
								thisValue = (thisValue << 1) ^ _poly;
							} else {
								thisValue <<= 1;
							}
						}
					}
					CRC_Table[dividend] = thisValue;
				}
			}
			UInt32 crc = initCRC ^ XorOut;
			if (Reflected) {
				for (UInt32 byteIdx = startIdx; byteIdx <= endIdx; byteIdx++) {
					crc = CRC_Table[data[byteIdx] ^ (crc & 0xFF)] ^ (crc >> 8);
				}
			} else {
				for (UInt32 byteIdx = startIdx; byteIdx <= endIdx; byteIdx++) {
					crc = CRC_Table[data[byteIdx] ^ ((crc & 0xFF000000) >> (DataBits - 8))] ^ (crc << 8);
				}
			}
			return crc ^ XorOut;
		}
		/// <summary>Calculates the CRC and returns a string representation.</summary>
		/// <param name="data">The data.</param>
		/// <returns>The calculated CRC as a string.</returns>
		public override String ComputeCRC(Memory data) {
			return $"0x{Compute(data):X8}";
		}

		/// <summary>Calculates the CRC and returns a string representation.</summary>
		/// <param name="data">The data.</param>
		/// <returns>The calculated CRC as a string.</returns>
		public override String ComputeCRC(Byte[] data) {
			return $"0x{Compute(new Memory(data)):X8}";
		}
	}
	/// <summary>An 64-bit CRC engine.</summary>
	/// <seealso cref="T:Dataescher.Data.Integrity.CRC"/>
	public class CRC64 : CRC {
		/// <summary>The CRC table.</summary>
		private UInt64[]? CRC_Table { get; set; }
		/// <summary>(Immutable) The polynomial (reflected if required).</summary>
		private readonly UInt64 _poly;
		/// <summary>The polynomial.</summary>
		public UInt64 Polynomial { get; private set; }
		/// <summary>(Immutable) The initial CRC value (reflected if required).</summary>
		private readonly UInt64 _init;
		/// <summary>The initial CRC value.</summary>
		public UInt64 InitialValue { get; private set; }
		/// <summary>The final XOR value.</summary>
		public UInt64 XorOut { get; private set; }
		/// <summary>If reflected, CRC is lsb first; otherwise, msb first.</summary>
		public Boolean Reflected { get; private set; }
		/// <summary>The number of bits.</summary>
		public static Int32 DataBits => 64;
		/// <summary>The number of bytes.</summary>
		public static Int32 DataBytes => 8;
		/// <summary>Initializes a new instance of the <see cref="CRC64"/> class.</summary>
		/// <param name="polynomial">The polynomial.</param>
		/// <param name="initialValue">The initial value.</param>
		/// <param name="reflected">True if reflected.</param>
		/// <param name="xorOut">The exclusive-or out.</param>
		public CRC64(UInt64 polynomial, UInt64 initialValue, Boolean reflected, UInt64 xorOut) {
			Reflected = reflected;
			Polynomial = polynomial;
			_poly = Reflected ? Reflect(polynomial) : polynomial;
			InitialValue = initialValue;
			_init = Reflected ? Reflect(initialValue) : initialValue;
			XorOut = xorOut;
			CRC_Table = null;
		}
		/// <summary>Reverse the order of the bits in a UInt64 value.</summary>
		/// <param name="value">The value for which to reverse the order of the bits.</param>
		/// <returns>The UInt64 value with the bit order reversed.</returns>
		private static UInt64 Reflect(UInt64 value) {
			UInt64 b1 = 0x8000000000000000;
			UInt64 b2 = 1;
			UInt64 result = 0;
			for (Int32 bitIdx = 0; bitIdx < 64; bitIdx++) {
				if ((value & b1) != 0) {
					result |= b2;
				}
				b1 >>= 1;
				b2 <<= 1;
			}
			return result;
		}
		/// <summary>Compute the CRC for an array of bytes.</summary>
		/// <param name="data">The array of bytes.</param>
		/// <returns>The CRC for the array of bytes.</returns>
		public UInt64 Compute(Byte[] data) {
			return Compute(new Memory(data), _init ^ XorOut);
		}
		/// <summary>Compute the CRC for an array of bytes.</summary>
		/// <param name="data">The array of bytes.</param>
		/// <returns>The CRC for the array of bytes.</returns>
		public UInt64 Compute(Memory data) {
			return Compute(data, _init ^ XorOut);
		}
		/// <summary>Compute the CRC for an array of bytes.</summary>
		/// <param name="data">The array of bytes.</param>
		/// <param name="initCRC">The initial CRC.</param>
		/// <returns>The CRC for the array of bytes.</returns>
		public UInt64 Compute(Byte[] data, UInt64 initCRC) {
			return Compute(new Memory(data), initCRC);
		}

		/// <summary>Calculates the CRC for a file.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <param name="filePath">Path of the file.</param>
		/// <returns>The calculated CCITT CRC.</returns>
		public UInt64 ComputeForFile(String filePath) {
			filePath = Path.GetFullPath(filePath);
			if (!File.Exists(filePath)) {
				throw new Exception($"File \"{filePath}\" does not exist.");
			}
			Int64 fileSize = new FileInfo(filePath).Length;
			using (FileStream fs = new(filePath, FileMode.Open, FileAccess.Read)) {
				using (BinaryReader br = new(fs)) {
					// Compute the CRC for the file
					return Compute(br.ReadBytes((Int32)fileSize), _init ^ XorOut);
				}
			}
		}

		/// <summary>Compute the CRC for an array of bytes.</summary>
		/// <exception cref="ArgumentOutOfRangeException">
		///     Thrown when one or more arguments are outside the required range.
		/// </exception>
		/// <param name="data">The data.</param>
		/// <param name="initCRC">The initial CRC.</param>
		/// <param name="offset">The offset into the data.</param>
		/// <param name="cnt">Number of bytes to compute CRC for.</param>
		/// <returns>The CRC for the memory.</returns>
		public UInt64 Compute(Byte[] data, UInt64 initCRC, Int32 offset, Int32 cnt) {
			UInt32 startIdx;
			UInt32 endIdx;
			if (offset >= 0) {
				startIdx = (UInt32)offset;
				if (startIdx >= data.Length) {
					throw new ArgumentOutOfRangeException(nameof(offset), "Read past end of the array.");
				}
			} else {
				startIdx = 0;
			}
			if (cnt >= 0) {
				endIdx = (UInt32)(startIdx + cnt - 1);
				if (endIdx >= data.Length) {
					throw new ArgumentOutOfRangeException(nameof(cnt), "Read past end of the array.");
				}
			} else {
				endIdx = (UInt32)(data.Length - 1);
			}
			if (CRC_Table is null) {
				// Generate the CRC look-up table
				CRC_Table = new UInt64[0x100];
				for (Int32 dividend = 0; dividend < 0x100; dividend++) {
					UInt64 thisValue = Reflected ? (UInt64)dividend : (UInt64)(dividend << 56);
					for (Int32 bitIn = 0; bitIn < 8; bitIn++) {
						if (Reflected) {
							if ((thisValue & 0x1) != 0) {
								thisValue = (thisValue >> 1) ^ _poly;
							} else {
								thisValue >>= 1;
							}
						} else {
							if ((thisValue & 0x8000000000000000) != 0) {
								thisValue = (thisValue << 1) ^ _poly;
							} else {
								thisValue <<= 1;
							}
						}
					}
					CRC_Table[dividend] = thisValue;
				}
			}
			UInt64 crc = initCRC ^ XorOut;
			if (Reflected) {
				for (UInt32 byteIdx = startIdx; byteIdx <= endIdx; byteIdx++) {
					crc = CRC_Table[data[byteIdx] ^ (crc & 0xFF)] ^ (crc >> 8);
				}
			} else {
				for (UInt32 byteIdx = startIdx; byteIdx <= endIdx; byteIdx++) {
					crc = CRC_Table[data[byteIdx] ^ ((crc & 0xFF00000000000000) >> (DataBits - 8))] ^ (crc << 8);
				}
			}
			return crc ^ XorOut;
		}

		/// <summary>Compute the CRC for memory.</summary>
		/// <exception cref="ArgumentOutOfRangeException">
		///     Thrown when one or more arguments are outside the required range.
		/// </exception>
		/// <param name="data">The array of bytes.</param>
		/// <param name="initCRC">The initial CRC.</param>
		/// <param name="offset">(Optional) The offset into the data. If negative, use 0.</param>
		/// <param name="cnt">(Optional) Number of bytes to compute CRC for.</param>
		/// <returns>The CRC for the memory.</returns>
		public UInt64 Compute(Memory data, UInt64 initCRC, Int32 offset = -1, Int32 cnt = -1) {
			UInt32 startIdx;
			UInt32 endIdx;
			if (offset >= 0) {
				startIdx = (UInt32)offset;
				if (startIdx >= data.Length) {
					throw new ArgumentOutOfRangeException(nameof(offset), "Read past end of the array.");
				}
			} else {
				startIdx = 0;
			}
			if (cnt >= 0) {
				endIdx = (UInt32)(startIdx + cnt - 1);
				if (endIdx >= data.Length) {
					throw new ArgumentOutOfRangeException(nameof(cnt), "Read past end of the array.");
				}
			} else {
				endIdx = (UInt32)(data.Length - 1);
			}
			if (CRC_Table is null) {
				// Generate the CRC look-up table
				CRC_Table = new UInt64[0x100];
				for (Int32 dividend = 0; dividend < 0x100; dividend++) {
					UInt64 thisValue = Reflected ? (UInt64)dividend : (UInt64)(dividend << 56);
					for (Int32 bitIn = 0; bitIn < 8; bitIn++) {
						if (Reflected) {
							if ((thisValue & 0x1) != 0) {
								thisValue = (thisValue >> 1) ^ _poly;
							} else {
								thisValue >>= 1;
							}
						} else {
							if ((thisValue & 0x8000000000000000) != 0) {
								thisValue = (thisValue << 1) ^ _poly;
							} else {
								thisValue <<= 1;
							}
						}
					}
					CRC_Table[dividend] = thisValue;
				}
			}
			UInt64 crc = initCRC ^ XorOut;
			if (Reflected) {
				for (UInt32 byteIdx = startIdx; byteIdx <= endIdx; byteIdx++) {
					crc = CRC_Table[data[byteIdx] ^ (crc & 0xFF)] ^ (crc >> 8);
				}
			} else {
				for (UInt32 byteIdx = startIdx; byteIdx <= endIdx; byteIdx++) {
					crc = CRC_Table[data[byteIdx] ^ ((crc & 0xFF00000000000000) >> (DataBits - 8))] ^ (crc << 8);
				}
			}
			return crc ^ XorOut;
		}
		/// <summary>Calculates the CRC and returns a string representation.</summary>
		/// <param name="data">The data.</param>
		/// <returns>The calculated CRC as a string.</returns>
		public override String ComputeCRC(Memory data) {
			return $"0x{Compute(data):X16}";
		}

		/// <summary>Calculates the CRC and returns a string representation.</summary>
		/// <param name="data">The data.</param>
		/// <returns>The calculated CRC as a string.</returns>
		public override String ComputeCRC(Byte[] data) {
			return $"0x{Compute(new Memory(data)):X16}";
		}
	}
}

