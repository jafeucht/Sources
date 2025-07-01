// <copyright file="ElfFormat.cs" company="Dataescher">
// Copyright (c) 2023-2024 Dataescher. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>5/4/2023</date>
// <summary>Implements the elf format class</summary>

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Dataescher.Data.Formats {
	/// <summary>An ELF (Executable and Linkable Format) format.</summary>
	/// <seealso cref="T:Dataescher.Data.BinaryFileFormat"/>
	public class ElfFormat : BinaryFileFormat {
		/// <summary>(Immutable) Standard elf file header: 0x7F, 'E', 'L', 'F'.</summary>
		public static readonly UInt32 elf_ident = 0x464C457F;

		#region ELF Enumerations

		/// <summary>Values that represent the supported ELF formats.</summary>
		public enum FormatClass : Byte {
			/// <summary>An enum constant representing the 32-bit elf format option.</summary>
			Format_32bit = 1,
			/// <summary>An enum constant representing the 64-bit elf format option.</summary>
			Format_64bit = 2
		}

		/// <summary>Values that represent data endianness.</summary>
		public enum Endian : Byte {
			/// <summary>An enum constant representing Little Endian.</summary>
			Little = 1,
			/// <summary>An enum constant representing Big Endian.</summary>
			Big = 2
		}

		/// <summary>An enumeration representing target operating system ABI.</summary>
		public enum Target_ABI : Byte {
			/// <summary>System V</summary>
			System_V = 0x00,
			/// <summary>HP-UX</summary>
			HP_UX = 0x01,
			/// <summary>NetBSD</summary>
			NetBSD = 0x02,
			/// <summary>Linux</summary>
			Linux = 0x03,
			/// <summary>GNU Hurd</summary>
			GNU_Hurd = 0x04,
			/// <summary>Solaris</summary>
			Solaris = 0x06,
			/// <summary>AIX (Monterey)</summary>
			AIX = 0x07,
			/// <summary>IRIX</summary>
			IRIX = 0x08,
			/// <summary>FreeBSD</summary>
			FreeBSD = 0x09,
			/// <summary>Tru64</summary>
			Tru64 = 0x0A,
			/// <summary>Novell Modesto</summary>
			Novell_Modesto = 0x0B,
			/// <summary>OpenBSD</summary>
			OpenBSD = 0x0C,
			/// <summary>OpenVMS</summary>
			OpenVMS = 0x0D,
			/// <summary>NonStop Kernel</summary>
			NonStop_Kernel = 0x0E,
			/// <summary>AROS</summary>
			AROS = 0x0F,
			/// <summary>FenixOS</summary>
			Fenix_OS = 0x10,
			/// <summary>Nuxi CloudABI</summary>
			CloudABI = 0x11,
			/// <summary>Stratus Technologies OpenVOS</summary>
			Stratus_Technologies_OpenVOS = 0x12
		}

		/// <summary>An enumeration representing various object file types.</summary>
		public enum Object_File_Type : UInt16 {
			/// <summary>Unknown</summary>
			ET_NONE = 0x00,
			/// <summary>Relocatable file</summary>
			ET_REL = 0x01,
			/// <summary>Executable file</summary>
			ET_EXEC = 0x02,
			/// <summary>Shared object</summary>
			ET_DYN = 0x03,
			/// <summary>Core file</summary>
			ET_CORE = 0x04,
			/// <summary>Reserved inclusive range top. Operating system specific.</summary>
			ET_LOOS = 0xFE00,
			/// <summary>Reserved inclusive range bottom. Operating system specific.</summary>
			ET_HIOS = 0xFEFF,
			/// <summary>Reserved inclusive range top. Processor specific.</summary>
			ET_LOPROC = 0xFF00,
			/// <summary>Reserved inclusive range bottom. Processor specific.</summary>
			ET_HIPROC = 0xFFFF
		}

		/// <summary>An enumeration representing various instruction set architectures.</summary>
		public enum Instruction_Set_Architecture : UInt16 {
			/// <summary>No specific instruction set</summary>
			No_specific_instruction_set = 0x00,
			/// <summary>AT&amp;T WE 32100</summary>
			AT_T_WE_32100 = 0x01,
			/// <summary>SPARC</summary>
			SPARC = 0x02,
			/// <summary>x86</summary>
			x86 = 0x03,
			/// <summary>Motorola 68000 (M68k)</summary>
			Motorola_68000_M68k = 0x04,
			/// <summary>Motorola 88000 (M88k)</summary>
			Motorola_88000_M88k = 0x05,
			/// <summary>Intel MCU</summary>
			Intel_MCU = 0x06,
			/// <summary>Intel 80860</summary>
			Intel_80860 = 0x07,
			/// <summary>MIPS</summary>
			MIPS = 0x08,
			/// <summary>IBM System/370</summary>
			IBM_System_370 = 0x09,
			/// <summary>MIPS RS3000 Little-endian</summary>
			MIPS_RS3000_Little_endian = 0x0A,
			/// <summary>Hewlett-Packard PA-RISC</summary>
			Hewlett_Packard_PA_RISC = 0x0E,
			/// <summary>Reserved for future use</summary>
			Reserved_for_future_use = 0x0F,
			/// <summary>Intel 80960</summary>
			Intel_80960 = 0x13,
			/// <summary>PowerPC</summary>
			PowerPC = 0x14,
			/// <summary>PowerPC (64-bit)</summary>
			PowerPC_64_bit = 0x15,
			/// <summary>S390, including S390x</summary>
			S390_including_S390x = 0x16,
			/// <summary>IBM SPU/SPC</summary>
			IBM_SPU_SPC = 0x17,
			/// <summary>NEC V800</summary>
			NEC_V800 = 0x24,
			/// <summary>Fujitsu FR20</summary>
			Fujitsu_FR20 = 0x25,
			/// <summary>TRW RH-32</summary>
			TRW_RH_32 = 0x26,
			/// <summary>Motorola RCE</summary>
			Motorola_RCE = 0x27,
			/// <summary>ARM (up to ARMv7/Aarch32)</summary>
			ARM_upo_ARMv7_Aarch32 = 0x28,
			/// <summary>Digital Alpha</summary>
			Digital_Alpha = 0x29,
			/// <summary>SuperH</summary>
			SuperH = 0x2A,
			/// <summary>SPARC Version 9</summary>
			SPARC_Version_9 = 0x2B,
			/// <summary>Siemens TriCore embedded processor</summary>
			Siemens_TriCore_embedded_processor = 0x2C,
			/// <summary>Argonaut RISC Core</summary>
			Argonaut_RISC_Core = 0x2D,
			/// <summary>Hitachi H8/300</summary>
			Hitachi_H8_300 = 0x2E,
			/// <summary>Hitachi H8/300H</summary>
			Hitachi_H8_300H = 0x2F,
			/// <summary>Hitachi H8S</summary>
			Hitachi_H8S = 0x30,
			/// <summary>Hitachi H8/500</summary>
			Hitachi_H8_500 = 0x31,
			/// <summary>IA-64</summary>
			IA_64 = 0x32,
			/// <summary>Stanford MIPS-X</summary>
			Stanford_MIPS_X = 0x33,
			/// <summary>Motorola ColdFire</summary>
			Motorola_ColdFire = 0x34,
			/// <summary>Motorola M68HC12</summary>
			Motorola_M68HC12 = 0x35,
			/// <summary>Fujitsu MMA Multimedia Accelerator</summary>
			Fujitsu_MMA_Multimedia_Accelerator = 0x36,
			/// <summary>Siemens PCP</summary>
			Siemens_PCP = 0x37,
			/// <summary>Sony nCPU embedded RISC processor</summary>
			Sony_nCPU_embedded_RISC_processor = 0x38,
			/// <summary>Denso NDR1 microprocessor</summary>
			Denso_NDR1_microprocessor = 0x39,
			/// <summary>Motorola Star*Core processor</summary>
			Motorola_Star_Core_processor = 0x3A,
			/// <summary>Toyota ME16 processor</summary>
			Toyota_ME16_processor = 0x3B,
			/// <summary>STMicroelectronics ST100 processor</summary>
			STMicroelectronics_ST100_processor = 0x3C,
			/// <summary>Advanced Logic Corp. TinyJ embedded processor family</summary>
			Advanced_Logic_Corp_TinyJ_embedded_processor_family = 0x3D,
			/// <summary>AMD x86-64</summary>
			AMD_x86_64 = 0x3E,
			/// <summary>Sony DSP Processor</summary>
			Sony_DSP_Processor = 0x3F,
			/// <summary>Digital Equipment Corp. PDP-10</summary>
			Digital_Equipment_Corp_PDP_10 = 0x40,
			/// <summary>Digital Equipment Corp. PDP-11</summary>
			Digital_Equipment_Corp_PDP_11 = 0x41,
			/// <summary>Siemens FX66 microcontroller</summary>
			Siemens_FX66_microcontroller = 0x42,
			/// <summary>STMicroelectronics ST9+ 8/16 bit microcontroller</summary>
			STMicroelectronics_ST9_8_16_bit_microcontroller = 0x43,
			/// <summary>STMicroelectronics ST7 8-bit microcontroller</summary>
			STMicroelectronics_ST7_8_bit_microcontroller = 0x44,
			/// <summary>Motorola MC68HC16 Microcontroller</summary>
			Motorola_MC68HC16_Microcontroller = 0x45,
			/// <summary>Motorola MC68HC11 Microcontroller</summary>
			Motorola_MC68HC11_Microcontroller = 0x46,
			/// <summary>Motorola MC68HC08 Microcontroller</summary>
			Motorola_MC68HC08_Microcontroller = 0x47,
			/// <summary>Motorola MC68HC05 Microcontroller</summary>
			Motorola_MC68HC05_Microcontroller = 0x48,
			/// <summary>Silicon Graphics SVx</summary>
			Silicon_Graphics_SVx = 0x49,
			/// <summary>STMicroelectronics ST19 8-bit microcontroller</summary>
			STMicroelectronics_ST19_8_bit_microcontroller = 0x4A,
			/// <summary>Digital VAX</summary>
			Digital_VAX = 0x4B,
			/// <summary>Axis Communications 32-bit embedded processor</summary>
			Axis_Communications_32_bit_embedded_processor = 0x4C,
			/// <summary>Infineon Technologies 32-bit embedded processor</summary>
			Infineon_Technologies_32_bit_embedded_processor = 0x4D,
			/// <summary>Element 14 64-bit DSP Processor</summary>
			Element_14_64_bit_DSP_Processor = 0x4E,
			/// <summary>LSI Logic 16-bit DSP Processor</summary>
			LSI_Logic_16_bit_DSP_Processor = 0x4F,
			/// <summary>TMS320C6000 Family</summary>
			TMS320C6000_Family = 0x8C,
			/// <summary>MCST Elbrus e2k</summary>
			MCST_Elbrus_e2k = 0xAF,
			/// <summary>ARM 64-bits (ARMv8/Aarch64)</summary>
			ARM_64_bits_ARMv8_Aarch64 = 0xB7,
			/// <summary>Zilog_Z80</summary>
			Zilog_Z80 = 0xDC,
			/// <summary>RISC-V</summary>
			RISC_V = 0xF3,
			/// <summary>Berkeley Packet Filter</summary>
			Berkeley_Packet_Filter = 0xF7,
			/// <summary>WDC 65C816</summary>
			WDC_65C816 = 0x101
		}

		/// <summary>An enumeration representing various section header types.</summary>
		public enum Section_Header_Type : UInt32 {
			/// <summary>Section header table entry unused</summary>
			SHT_NULL = 0x0,
			/// <summary>Program data</summary>
			SHT_PROGBITS = 0x1,
			/// <summary>Symbol table</summary>
			SHT_SYMTAB = 0x2,
			/// <summary>String table</summary>
			SHT_STRTAB = 0x3,
			/// <summary>Relocation entries with addends</summary>
			SHT_RELA = 0x4,
			/// <summary>Symbol hash table</summary>
			SHT_HASH = 0x5,
			/// <summary>Dynamic linking information</summary>
			SHT_DYNAMIC = 0x6,
			/// <summary>Notes</summary>
			SHT_NOTE = 0x7,
			/// <summary>Program space with no data (bss)</summary>
			SHT_NOBITS = 0x8,
			/// <summary>Relocation entries, no addends</summary>
			SHT_REL = 0x9,
			/// <summary>Reserved</summary>
			SHT_SHLIB = 0x0A,
			/// <summary>Dynamic linker symbol table</summary>
			SHT_DYNSYM = 0x0B,
			/// <summary>Array of constructors</summary>
			SHT_INIT_ARRAY = 0x0E,
			/// <summary>Array of destructors</summary>
			SHT_FINI_ARRAY = 0x0F,
			/// <summary>Array of pre-constructors</summary>
			SHT_PREINIT_ARRAY = 0x10,
			/// <summary>Section group</summary>
			SHT_GROUP = 0x11,
			/// <summary>Extended section indices</summary>
			SHT_SYMTAB_SHNDX = 0x12,
			/// <summary>Number of defined types.</summary>
			SHT_NUM = 0x13,
			/// <summary>Start OS-specific.</summary>
			SHT_LOOS = 0x60000000
		}

		/// <summary>A bit-field of flags for identifying the attributes of a section.</summary>
		[Flags]
		public enum SH_FLAGS : UInt32 {
			/// <summary>Writable</summary>
			SHF_WRITE = 0x1,
			/// <summary>Occupies memory during execution</summary>
			SHF_ALLOC = 0x2,
			/// <summary>Executable</summary>
			SHF_EXECINSTR = 0x4,
			/// <summary>Might be merged</summary>
			SHF_MERGE = 0x10,
			/// <summary>Contains null-terminated strings</summary>
			SHF_STRINGS = 0x20,
			/// <summary>'sh_info' contains SHT index</summary>
			SHF_INFO_LINK = 0x40,
			/// <summary>Preserve order after combining</summary>
			SHF_LINK_ORDER = 0x80,
			/// <summary>Non-standard OS specific handling required</summary>
			SHF_OS_NONCONFORMING = 0x100,
			/// <summary>Section is member of a group</summary>
			SHF_GROUP = 0x200,
			/// <summary>Section hold thread-local data</summary>
			SHF_TLS = 0x400,
			/// <summary>OS-specific</summary>
			SHF_MASKOS = 0xFF00000,
			/// <summary>Processor-specific</summary>
			SHF_MASKPROC = 0xF0000000,
			/// <summary>Special ordering requirement (Solaris)</summary>
			SHF_ORDERED = 0x4000000,
			/// <summary>Section is excluded unless referenced or allocated (Solaris)</summary>
			SHF_EXCLUDE = 0x8000000
		}

		/// <summary>Gets or sets the flags for sections to load.</summary>
		public SH_FLAGS SectionLoadFlags { get; set; }

		/// <summary>An enumeration representing various program segment types.</summary>
		public enum Program_Type : UInt32 {
			/// <summary>Program header table entry unused</summary>
			PT_NULL = 0,
			/// <summary>Loadable segment</summary>
			PT_LOAD = 1,
			/// <summary>Dynamic linking information</summary>
			PT_DYNAMIC = 2,
			/// <summary>Interpreter information</summary>
			PT_INTERP = 3,
			/// <summary>Auxiliary information</summary>
			PT_NOTE = 4,
			/// <summary>Reserved</summary>
			PT_SHLIB = 5,
			/// <summary>Segment containing program header table itself</summary>
			PT_PHDR = 6,
			/// <summary>Thread-Local Storage template</summary>
			PT_TLS = 7,
			/// <summary>Reserved inclusive range top. Operating system specific.</summary>
			PT_LOOS = 0x60000000,
			/// <summary>Reserved inclusive range bottom. Operating system specific.</summary>
			PT_HIOS = 0x6FFFFFFF,
			/// <summary>Reserved inclusive range top. Processor specific.</summary>
			PT_LOPROC = 0x70000000,
			/// <summary>Reserved inclusive range bottom. Processor specific.</summary>
			PT_HIPROC = 0x7FFFFFFF
		}

		#endregion

		#region ELF Structures

		/// <summary>The elf file header identifier structure.</summary>
		/// <seealso cref="T:Dataescher.Data.BinaryFileFormat"/>
		[StructLayout(LayoutKind.Explicit)]
		public unsafe struct E_Ident {
			/// <summary>0x7F followed by ELF(0x45 0x4C 0x46) in ASCII; these four bytes constitute the magic number.</summary>
			[FieldOffset(0x00)]
			public UInt32 EI_MAG;
			/// <summary>This byte is set to either 1 or 2 to signify 32- or 64-bit format, respectively.</summary>
			[FieldOffset(0x04)]
			public FormatClass EI_CLASS;
			/// <summary>
			///     This byte is set to either 1 or 2 to signify little or big endianness, respectively. This affects
			///     interpretation of multi-byte fields starting with offset 0x10.
			/// </summary>
			[FieldOffset(0x05)]
			public Endian EI_DATA;
			/// <summary>Set to 1 for the original and current version of ELF.</summary>
			[FieldOffset(0x06)]
			public Byte EI_VERSION;
			/// <summary>
			///     Identifies the target operating system ABI. It is often set to 0 regardless of the target platform.
			/// </summary>
			[FieldOffset(0x07)]
			public Target_ABI EI_OSABI;
			/// <summary>
			///     Further specifies the ABI version. Its interpretation depends on the target ABI. Linux kernel (after at least
			///     2.6) has no definition of it,[5] so it is ignored for statically-linked executables. In that case, offset and
			///     size of EI_PAD are 8.
			/// </summary>
			[FieldOffset(0x08)]
			public Byte EI_ABIVERSION;
			/// <summary>Currently unused, should be filled with zeros.</summary>
			[FieldOffset(0x09)]
			public fixed Byte EI_PAD[7];
		}

		/// <summary>32-bit format elf file header.</summary>
		/// <seealso cref="T:Dataescher.Data.BinaryFileFormat"/>
		[StructLayout(LayoutKind.Explicit)]
		public struct File_Header32 {
			/// <summary>The identifier structure.</summary>
			[FieldOffset(0x00)]
			public E_Ident e_ident;
			/// <summary>Identifies object file type.</summary>
			[FieldOffset(0x10)]
			public Object_File_Type e_type;
			/// <summary>Specifies target instruction set architecture.</summary>
			[FieldOffset(0x12)]
			public Instruction_Set_Architecture e_machine;
			/// <summary>Set to 1 for the original version of ELF.</summary>
			[FieldOffset(0x14)]
			public UInt32 e_version;
			/// <summary>
			///     This is the memory address of the entry point from where the process starts executing. This field is either
			///     32 or 64 bits long depending on the format defined earlier.
			/// </summary>
			[FieldOffset(0x18)]
			public UInt32 e_entry;
			/// <summary>
			///     Points to the start of the program header table. It usually follows the file header immediately, making the
			///     offset 0x34 or 0x40 for 32- and 64-bit ELF executables, respectively.
			/// </summary>
			[FieldOffset(0x1C)]
			public UInt32 e_phoff;
			/// <summary>Points to the start of the section header table.</summary>
			[FieldOffset(0x20)]
			public UInt32 e_shoff;
			/// <summary>Interpretation of this field depends on the target architecture.</summary>
			[FieldOffset(0x24)]
			public UInt32 e_flags;
			/// <summary>Contains the size of this header, normally 64 Bytes for 64-bit and 52 Bytes for 32-bit format.</summary>
			[FieldOffset(0x28)]
			public UInt16 e_ehsize;
			/// <summary>Contains the size of a program header table entry.</summary>
			[FieldOffset(0x2A)]
			public UInt16 e_phentsize;
			/// <summary>Contains the number of entries in the program header table.</summary>
			[FieldOffset(0x2C)]
			public UInt16 e_phnum;
			/// <summary>Contains the size of a section header table entry.</summary>
			[FieldOffset(0x2E)]
			public UInt16 e_shentsize;
			/// <summary>Contains the number of entries in the section header table.</summary>
			[FieldOffset(0x30)]
			public UInt16 e_shnum;
			/// <summary>Contains index of the section header table entry that contains the section names.</summary>
			[FieldOffset(0x32)]
			public UInt16 e_shstrndx;
		}

		/// <summary>64-bit format elf file header.</summary>
		/// <seealso cref="T:Dataescher.Data.BinaryFileFormat"/>
		[StructLayout(LayoutKind.Explicit)]
		public struct File_Header64 {
			/// <summary>The identifier structure.</summary>
			[FieldOffset(0x00)]
			public E_Ident e_ident;
			/// <summary>Identifies object file type.</summary>
			[FieldOffset(0x10)]
			public Object_File_Type e_type;
			/// <summary>Specifies target instruction set architecture.</summary>
			[FieldOffset(0x12)]
			public Instruction_Set_Architecture e_machine;
			/// <summary>Set to 1 for the original version of ELF.</summary>
			[FieldOffset(0x14)]
			public UInt32 e_version;
			/// <summary>
			///     This is the memory address of the entry point from where the process starts executing. This field is either
			///     32 or 64 bits long depending on the format defined earlier.
			/// </summary>
			[FieldOffset(0x18)]
			public UInt64 e_entry;
			/// <summary>
			///     Points to the start of the program header table. It usually follows the file header immediately, making the
			///     offset 0x34 or 0x40 for 32- and 64-bit ELF executables, respectively.
			/// </summary>
			[FieldOffset(0x20)]
			public UInt64 e_phoff;
			/// <summary>Points to the start of the section header table.</summary>
			[FieldOffset(0x28)]
			public UInt64 e_shoff;
			/// <summary>Interpretation of this field depends on the target architecture.</summary>
			[FieldOffset(0x30)]
			public UInt32 e_flags;
			/// <summary>Contains the size of this header, normally 64 Bytes for 64-bit and 52 Bytes for 32-bit format.</summary>
			[FieldOffset(0x34)]
			public UInt16 e_ehsize;
			/// <summary>Contains the size of a program header table entry.</summary>
			[FieldOffset(0x36)]
			public UInt16 e_phentsize;
			/// <summary>Contains the number of entries in the program header table.</summary>
			[FieldOffset(0x38)]
			public UInt16 e_phnum;
			/// <summary>Contains the size of a section header table entry.</summary>
			[FieldOffset(0x3A)]
			public UInt16 e_shentsize;
			/// <summary>Contains the number of entries in the section header table.</summary>
			[FieldOffset(0x3C)]
			public UInt16 e_shnum;
			/// <summary>Contains index of the section header table entry that contains the section names.</summary>
			[FieldOffset(0x3E)]
			public UInt16 e_shstrndx;
		}

		/// <summary>32-bit format elf program header.</summary>
		/// <seealso cref="T:Dataescher.Data.BinaryFileFormat"/>
		[StructLayout(LayoutKind.Explicit)]
		public struct Program_Header32 {
			/// <summary>Identifies the type of the segment.</summary>
			[FieldOffset(0x00)]
			public Program_Type p_type;
			/// <summary>Offset of the segment in the file image.</summary>
			[FieldOffset(0x04)]
			public UInt32 p_offset;
			/// <summary>Virtual address of the segment in memory.</summary>
			[FieldOffset(0x08)]
			public UInt32 p_vaddr;
			/// <summary>On systems where physical address is relevant, reserved for segment's physical address.</summary>
			[FieldOffset(0x0C)]
			public UInt32 p_paddr;
			/// <summary>Size in bytes of the segment in the file image. May be 0.</summary>
			[FieldOffset(0x10)]
			public UInt32 p_filesz;
			/// <summary>Size in bytes of the segment in memory. May be 0.</summary>
			[FieldOffset(0x14)]
			public UInt32 p_memsz;
			/// <summary>Segment-dependent flags (for 32-bit).</summary>
			[FieldOffset(0x18)]
			public UInt32 p_flags;
			/// <summary>
			///     0 and 1 specify no alignment. Otherwise should be a positive, integral power of 2, with p_vaddr equating
			///     p_offset modulus p_align.
			/// </summary>
			[FieldOffset(0x1C)]
			public UInt32 p_align;
		}

		/// <summary>64-bit format elf program header.</summary>
		/// <seealso cref="T:Dataescher.Data.BinaryFileFormat"/>
		[StructLayout(LayoutKind.Explicit)]
		public struct Program_Header64 {
			/// <summary>Identifies the type of the segment.</summary>
			[FieldOffset(0x00)]
			public Program_Type p_type;
			/// <summary>Segment-dependent flags (for 64 bit).</summary>
			[FieldOffset(0x04)]
			public UInt32 p_flags;
			/// <summary>Offset of the segment in the file image.</summary>
			[FieldOffset(0x08)]
			public UInt64 p_offset;
			/// <summary>Virtual address of the segment in memory.</summary>
			[FieldOffset(0x10)]
			public UInt64 p_vaddr;
			/// <summary>On systems where physical address is relevant, reserved for segment's physical address.</summary>
			[FieldOffset(0x18)]
			public UInt64 p_paddr;
			/// <summary>Size in bytes of the segment in the file image. May be 0.</summary>
			[FieldOffset(0x20)]
			public UInt64 p_filesz;
			/// <summary>Size in bytes of the segment in memory. May be 0.</summary>
			[FieldOffset(0x28)]
			public UInt64 p_memsz;
			/// <summary>
			///     0 and 1 specify no alignment. Otherwise should be a positive, integral power of 2, with p_vaddr equating
			///     p_offset modulus p_align.
			/// </summary>
			[FieldOffset(0x30)]
			public UInt64 p_align;
		}

		/// <summary>32-bit format section header.</summary>
		/// <seealso cref="T:Dataescher.Data.BinaryFileFormat"/>
		[StructLayout(LayoutKind.Explicit)]
		public struct Section_Header32 {
			/// <summary>An offset to a string in the .shstrtab section that represents the name of this section.</summary>
			[FieldOffset(0x00)]
			public UInt32 sh_name;
			/// <summary>Identifies the type of this header.</summary>
			[FieldOffset(0x04)]
			public Section_Header_Type sh_type;
			/// <summary>Identifies the attributes of the section.</summary>
			[FieldOffset(0x08)]
			public SH_FLAGS sh_flags;
			/// <summary>Virtual address of the section in memory, for sections that are loaded.</summary>
			[FieldOffset(0x0C)]
			public UInt32 sh_addr;
			/// <summary>Offset of the section in the file image.</summary>
			[FieldOffset(0x10)]
			public UInt32 sh_offset;
			/// <summary>Size in bytes of the section in the file image. May be 0.</summary>
			[FieldOffset(0x14)]
			public UInt32 sh_size;
			/// <summary>
			///     Contains the section index of an associated section. This field is used for several purposes, depending on
			///     the type of section.
			/// </summary>
			[FieldOffset(0x18)]
			public UInt32 sh_link;
			/// <summary>
			///     Contains extra information about the section. This field is used for several purposes, depending on the type
			///     of section.
			/// </summary>
			[FieldOffset(0x1C)]
			public UInt32 sh_info;
			/// <summary>Contains the required alignment of the section. This field must be a power of two.</summary>
			[FieldOffset(0x20)]
			public UInt32 sh_addralign;
			/// <summary>
			///     Contains the size, in bytes, of each entry, for sections that contain fixed-size entries. Otherwise, this
			///     field contains zero.
			/// </summary>
			[FieldOffset(0x24)]
			public UInt32 sh_entsize;
		}

		/// <summary>64-bit format section header.</summary>
		/// <seealso cref="T:Dataescher.Data.BinaryFileFormat"/>
		[StructLayout(LayoutKind.Explicit)]
		public struct Section_Header64 {
			/// <summary>An offset to a string in the .shstrtab section that represents the name of this section.</summary>
			[FieldOffset(0x00)]
			public UInt32 sh_name;
			/// <summary>Identifies the type of this header.</summary>
			[FieldOffset(0x04)]
			public Section_Header_Type sh_type;
			/// <summary>Identifies the attributes of the section.</summary>
			[FieldOffset(0x08)]
			public SH_FLAGS sh_flags;
			/// <summary>Virtual address of the section in memory, for sections that are loaded.</summary>
			[FieldOffset(0x10)]
			public UInt64 sh_addr;
			/// <summary>Offset of the section in the file image.</summary>
			[FieldOffset(0x18)]
			public UInt64 sh_offset;
			/// <summary>Size in bytes of the section in the file image. May be 0.</summary>
			[FieldOffset(0x20)]
			public UInt64 sh_size;
			/// <summary>
			///     Contains the section index of an associated section. This field is used for several purposes, depending on
			///     the type of section.
			/// </summary>
			[FieldOffset(0x28)]
			public UInt32 sh_link;
			/// <summary>
			///     Contains extra information about the section. This field is used for several purposes, depending on the type
			///     of section.
			/// </summary>
			[FieldOffset(0x2C)]
			public UInt32 sh_info;
			/// <summary>Contains the required alignment of the section. This field must be a power of two.</summary>
			[FieldOffset(0x30)]
			public UInt64 sh_addralign;
			/// <summary>
			///     Contains the size, in bytes, of each entry, for sections that contain fixed-size entries. Otherwise, this
			///     field contains zero.
			/// </summary>
			[FieldOffset(0x38)]
			public UInt64 sh_entsize;
		}

		#endregion

		/// <summary>Gets the endianness.</summary>
		public Endian Endianness { get; private set; }

		/// <summary>Gets the ELF format.</summary>
		public FormatClass Format { get; private set; }

		/// <summary>Gets target ABI.</summary>
		public Target_ABI TargetABI { get; private set; }

		/// <summary>Gets the type of the object file.</summary>
		public Object_File_Type ObjectFileType { get; private set; }

		/// <summary>Gets the instruction set architecture.</summary>
		public Instruction_Set_Architecture InstructionSetArchitecture { get; private set; }

		#region Constructors

		/// <summary>Initializes the class.</summary>
		private void InitClass() {
			SectionLoadFlags = 0;
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.ElfFormat class.</summary>
		public ElfFormat() : base() {
			InitClass();
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.ElfFormat class.</summary>
		/// <param name="memoryMap">The memory map.</param>
		public ElfFormat(MemoryMap memoryMap) : base(memoryMap) {
			InitClass();
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.ElfFormat class.</summary>
		/// <param name="stream">The stream.</param>
		public ElfFormat(Stream stream) : base() {
			InitClass();
			Load(stream);
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.ElfFormat class.</summary>
		/// <param name="filename">Filename of the file.</param>
		public ElfFormat(String filename) : base() {
			InitClass();
			Load(filename);
		}

		/// <summary>Initializes a new instance of the Dataescher.Data.Formats.ElfFormat class.</summary>
		/// <param name="binaryReader">The binary reader.</param>
		public ElfFormat(BinaryReader binaryReader) : base() {
			InitClass();
			Load(binaryReader);
		}

		#endregion

		/// <summary>
		///     Save to an elf file (32 bit). The 64-bit version is not implemented yet, however it shouldn't be much
		///     different than this function.
		/// </summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <param name="br">The binary reader.</param>
		private void ProcessElf32(BinaryReader br) {
			// Read in elfHeader
			File_Header32 elfHeader;

			elfHeader = ByteToType<File_Header32>(br);
			if (elfHeader.e_ident.EI_MAG != elf_ident) {
				throw new Exception("EI_MAG not correct for an elf file.");
			}
			if (elfHeader.e_ehsize != Marshal.SizeOf<File_Header32>()) {
				throw new Exception("Elf header size incorrect.");
			}

			Format = elfHeader.e_ident.EI_CLASS;
			ObjectFileType = elfHeader.e_type;
			InstructionSetArchitecture = elfHeader.e_machine;

			// Check if the file is at least large enough to fit in all the section headers, which are located towards
			// the bottom of the file
			Int64 expFileSize = elfHeader.e_shoff + (elfHeader.e_shnum * Marshal.SizeOf<Section_Header32>());
			if (br.BaseStream.Length < expFileSize) {
				throw new Exception("Incomplete elf file.");
			}

			// Cross-check the section size to ensure it is reasonable for this format
			if (elfHeader.e_shentsize != Marshal.SizeOf<Section_Header32>()) {
				throw new Exception("Section header size incorrect.");
			}

			// Navigate to the beginning of the first section and read in all sections
			br.BaseStream.Seek(elfHeader.e_shoff, SeekOrigin.Begin);
			Section_Header32[] sectionHeaders = new Section_Header32[elfHeader.e_shnum];
			for (UInt32 sectionHeaderIdx = 0; sectionHeaderIdx < elfHeader.e_shnum; sectionHeaderIdx++) {
				sectionHeaders[sectionHeaderIdx] = ByteToType<Section_Header32>(br);
			}

			// Cross-check the names table section index
			if (elfHeader.e_shstrndx >= elfHeader.e_shnum) {
				// Invalid condition - names table not an enumerated section
				throw new Exception("Invalid names section index.");
			}

			// Seek to the position of the section names, then read in the names table
			br.BaseStream.Seek(sectionHeaders[elfHeader.e_shstrndx].sh_offset, SeekOrigin.Begin);
			Byte[] sectionNamesBytes = br.ReadBytes((Int32)sectionHeaders[elfHeader.e_shstrndx].sh_size);

			// An array of loaded elf sections
			Dictionary<String, MemoryMap> ElfSections = new();

			if (SectionLoadFlags != 0) {
				// Load the sections into memory
				for (UInt32 shIdx = 0; shIdx < elfHeader.e_shnum; shIdx++) {
					if (SectionLoadFlags.HasFlag(sectionHeaders[shIdx].sh_flags)) {
						String thisSectionName = String.Empty;
						// Grab the name for this section
						for (UInt32 sectionNameIdx = sectionHeaders[shIdx].sh_name; sectionNameIdx < sectionNamesBytes.Length; sectionNameIdx++) {
							Char thisChar = (Char)sectionNamesBytes[sectionNameIdx];
							if (thisChar == '\0') {
								break;
							}
							thisSectionName += thisChar;
						}
						if (!ElfSections.TryGetValue(thisSectionName, out MemoryMap thisSectionMap)) {
							thisSectionMap = new();
							ElfSections.Add(thisSectionName, thisSectionMap);
						}

						//String thisSectionName = sectionNamesBytes[sectionHeaders[shIdx].sh_name];
						// Do not load the string table as a section
						if (sectionHeaders[shIdx].sh_size > 0) {
							// Load the data for this section (only if it is not the names table and the section is not null)
							br.BaseStream.Seek(sectionHeaders[shIdx].sh_offset, SeekOrigin.Begin);
							Byte[] thisSectionData = br.ReadBytes((Int32)sectionHeaders[shIdx].sh_size);
							thisSectionMap.Insert(sectionHeaders[shIdx].sh_addr, thisSectionData);
						}
					}
				}
			}

			// Load the program sections
			if (elfHeader.e_phentsize != Marshal.SizeOf<Program_Header32>()) {
				throw new Exception("Program header size incorrect.");
			}
			br.BaseStream.Seek(elfHeader.e_phoff, SeekOrigin.Begin);
			Program_Header32[] programHeaders = new Program_Header32[elfHeader.e_phnum];
			for (UInt32 programHeaderIdx = 0; programHeaderIdx < elfHeader.e_phnum; programHeaderIdx++) {
				programHeaders[programHeaderIdx] = ByteToType<Program_Header32>(br);
				if (programHeaders[programHeaderIdx].p_filesz > 0) {
					// Save our position, then read the program data.
					Int64 savedPosition = br.BaseStream.Position;
					br.BaseStream.Seek(programHeaders[programHeaderIdx].p_offset, SeekOrigin.Begin);
					Byte[] programData = br.ReadBytes((Int32)programHeaders[programHeaderIdx].p_filesz);
					MemoryMap.Insert(programHeaders[programHeaderIdx].p_paddr, programData);
					// Return to the previous position.
					br.BaseStream.Seek(savedPosition, SeekOrigin.Begin);
				}
			}
		}

		#region BinaryFileFormat base class overrides

		/// <summary>Loads data using the given binary reader.</summary>
		/// <param name="binaryReader">The binary reader to load.</param>
		public override void Load(BinaryReader binaryReader) {
			ProcessElf32(binaryReader);
		}

		/// <summary>Saves data using the given binary writer.</summary>
		/// <param name="binaryWriter">The binary writer to save.</param>
		public override void Save(BinaryWriter binaryWriter) {
			throw new NotImplementedException();
		}

		/// <summary>Test if this file appears to be this file format.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <param name="binaryReader">The binary reader to load.</param>
		public override void Test(BinaryReader binaryReader) {
			// Read in elfHeader
			File_Header32 elfHeader;
			elfHeader = ByteToType<File_Header32>(binaryReader);
			if (elfHeader.e_ident.EI_MAG != elf_ident) {
				throw new Exception("EI_MAG not correct for an elf file.");
			}
		}

		#endregion
	}
}