// <copyright file="CRC.cs" company="Dataescher">
// Copyright (c) 2024 Dataescher. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>9/28/2024</date>
// <summary>Implements the CRC class</summary>

using System;
using System.Collections.Generic;

namespace Dataescher.Data.Integrity {
	/// <summary>A CRC engine base class.</summary>
	public abstract class CRC {
		/// <summary>Calculates the CRC and returns a string representation.</summary>
		/// <param name="data">The data.</param>
		/// <returns>The calculated CRC as a string.</returns>
		public abstract String ComputeCRC(Memory data);

		/// <summary>Calculates the CRC and returns a string representation.</summary>
		/// <param name="data">The data.</param>
		/// <returns>The calculated CRC as a string.</returns>
		public abstract String ComputeCRC(Byte[] data);

		/// <summary>Gets the CRC engines.</summary>
		public static Dictionary<String, CRC> CRC_Engines { get; private set; }

		/// <summary>Initializes static members of the <see cref="CRC"/> class.</summary>
		static CRC() {
			CRC_Engines = new() {
				{ "CRC-8/AUTOSAR", new CRC8(0x2F, 0xFF, false, 0xFF) },
				{ "CRC-8/BLUETOOTH", new CRC8(0xA7, 0x00, true, 0x00) },
				{ "CRC-8/CDMA2000", new CRC8(0x9B, 0xFF, false, 0x00) },
				{ "CRC-8/DARC", new CRC8(0x39, 0x00, true, 0x00) },
				{ "CRC-8/DVB-S2", new CRC8(0xD5, 0x00, false, 0x00) },
				{ "CRC-8/GSM-A", new CRC8(0x1D, 0x00, false, 0x00) },
				{ "CRC-8/GSM-B", new CRC8(0x49, 0x00, false, 0xFF) },
				{ "CRC-8/HITAG", new CRC8(0x1D, 0xFF, false, 0x00) },
				{ "CRC-8/I-432-1", new CRC8(0x07, 0x00, false, 0x55) },
				{ "CRC-8/I-CODE", new CRC8(0x1D, 0xFD, false, 0x00) },
				{ "CRC-8/LTE", new CRC8(0x9B, 0x00, false, 0x00) },
				{ "CRC-8/MAXIM-DOW", new CRC8(0x31, 0x00, true, 0x00) },
				{ "CRC-8/MIFARE-MAD", new CRC8(0x1D, 0xC7, false, 0x00) },
				{ "CRC-8/NRSC-5", new CRC8(0x31, 0xFF, false, 0x00) },
				{ "CRC-8/OPENSAFETY", new CRC8(0x2F, 0x00, false, 0x00) },
				{ "CRC-8/ROHC", new CRC8(0x07, 0xFF, true, 0x00) },
				{ "CRC-8/SAE-J1850", new CRC8(0x1D, 0xFF, false, 0xFF) },
				{ "CRC-8/SMBUS", new CRC8(0x07, 0x00, false, 0x00) },
				{ "CRC-8/TECH-3250", new CRC8(0x1D, 0xFF, true, 0x00) },
				{ "CRC-8/WCDMA", new CRC8(0x9B, 0x00, true, 0x00) },
				{ "CRC-16/ARC", new CRC16(0x8005, 0x0000, true, 0x0000) },
				{ "CRC-16/CDMA2000", new CRC16(0xC867, 0xFFFF, false, 0x0000) },
				{ "CRC-16/CMS", new CRC16(0x8005, 0xFFFF, false, 0x0000) },
				{ "CRC-16/DDS-110", new CRC16(0x8005, 0x800D, false, 0x0000) },
				{ "CRC-16/DECT-R", new CRC16(0x0589, 0x0000, false, 0x0001) },
				{ "CRC-16/DECT-X", new CRC16(0x0589, 0x0000, false, 0x0000) },
				{ "CRC-16/DNP", new CRC16(0x3D65, 0x0000, true, 0xFFFF) },
				{ "CRC-16/EN-13757", new CRC16(0x3D65, 0x0000, false, 0xFFFF) },
				{ "CRC-16/GENIBUS", new CRC16(0x1021, 0xFFFF, false, 0xFFFF) },
				{ "CRC-16/GSM", new CRC16(0x1021, 0x0000, false, 0xFFFF) },
				{ "CRC-16/IBM-3740", new CRC16(0x1021, 0xFFFF, false, 0x0000) },
				{ "CRC-16/IBM-SDLC", new CRC16(0x1021, 0xFFFF, true, 0xFFFF) },
				{ "CRC-16/ISO-IEC-14443-3-A", new CRC16(0x1021, 0xC6C6, true, 0x0000) },
				{ "CRC-16/KERMIT", new CRC16(0x1021, 0x0000, true, 0x0000) },
				{ "CRC-16/LJ1200", new CRC16(0x6F63, 0x0000, false, 0x0000) },
				{ "CRC-16/M17", new CRC16(0x5935, 0xFFFF, false, 0x0000) },
				{ "CRC-16/MAXIM-DOW", new CRC16(0x8005, 0x0000, true, 0xFFFF) },
				{ "CRC-16/MCRF4XX", new CRC16(0x1021, 0xFFFF, true, 0x0000) },
				{ "CRC-16/MODBUS", new CRC16(0x8005, 0xFFFF, true, 0x0000) },
				{ "CRC-16/NRSC-5", new CRC16(0x080B, 0xFFFF, true, 0x0000) },
				{ "CRC-16/OPENSAFETY-A", new CRC16(0x5935, 0x0000, false, 0x0000) },
				{ "CRC-16/OPENSAFETY-B", new CRC16(0x755B, 0x0000, false, 0x0000) },
				{ "CRC-16/PROFIBUS", new CRC16(0x1DCF, 0xFFFF, false, 0xFFFF) },
				{ "CRC-16/RIELLO", new CRC16(0x1021, 0xB2AA, true, 0x0000) },
				{ "CRC-16/SPI-FUJITSU", new CRC16(0x1021, 0x1D0F, false, 0x0000) },
				{ "CRC-16/T10-DIF", new CRC16(0x8BB7, 0x0000, false, 0x0000) },
				{ "CRC-16/TELEDISK", new CRC16(0xA097, 0x0000, false, 0x0000) },
				{ "CRC-16/TMS37157", new CRC16(0x1021, 0x89EC, true, 0x0000) },
				{ "CRC-16/UMTS", new CRC16(0x8005, 0x0000, false, 0x0000) },
				{ "CRC-16/USB", new CRC16(0x8005, 0xFFFF, true, 0xFFFF) },
				{ "CRC-16/XMODEM", new CRC16(0x1021, 0x0000, false, 0x0000) },
				{ "CRC-32/AIXM", new CRC32(0x814141AB, 0x00000000, false, 0x00000000) },
				{ "CRC-32/AUTOSAR", new CRC32(0xF4ACFB13, 0xFFFFFFFF, true, 0xFFFFFFFF) },
				{ "CRC-32/BASE91-D", new CRC32(0xA833982B, 0xFFFFFFFF, true, 0xFFFFFFFF) },
				{ "CRC-32/BZIP2", new CRC32(0x04C11DB7, 0xFFFFFFFF, false, 0xFFFFFFFF) },
				{ "CRC-32/CD-ROM-EDC", new CRC32(0x8001801B, 0x00000000, true, 0x00000000) },
				{ "CRC-32/CKSUM", new CRC32(0x04C11DB7, 0x00000000, false, 0xFFFFFFFF) },
				{ "CRC-32/ISCSI", new CRC32(0x1EDC6F41, 0xFFFFFFFF, true, 0xFFFFFFFF) },
				{ "CRC-32/ISO-HDLC", new CRC32(0x04C11DB7, 0xFFFFFFFF, true, 0xFFFFFFFF) },
				{ "CRC-32/JAMCRC", new CRC32(0x04C11DB7, 0xFFFFFFFF, true, 0x00000000) },
				{ "CRC-32/MEF", new CRC32(0x741B8CD7, 0xFFFFFFFF, true, 0x00000000) },
				{ "CRC-32/MPEG-2", new CRC32(0x04C11DB7, 0xFFFFFFFF, false, 0x00000000) },
				{ "CRC-32/XFER", new CRC32(0x000000AF, 0x00000000, false, 0x00000000) }
			};
		}
	}
}