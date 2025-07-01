// <copyright file="CLI.cs" company="Dataescher">
// 	Copyright (c) 2022-2024 Dataescher. All rights reserved.
// </copyright>
// <summary>Implements a command line interface.</summary>

using Dataescher.Numbers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Dataescher.CommandLineInterface {
	/// <summary>A command line interface.</summary>
	public partial class CLI {
		/// <summary>The base type.</summary>
		private Type baseType;

		/// <summary>Look up a command handler by short switch.</summary>
		/// <param name="sw">The short switch.</param>
		/// <param name="command">[out] The handler command if the short switch is handled; null otherwise.</param>
		/// <returns>True if the short switch is handled, false otherwise.</returns>
		public Boolean TryGetHandler(Char sw, out Command command) {
			foreach (Command cmd in Commands) {
				if (cmd.Handles(sw)) {
					command = cmd;
					return true;
				}
			}
			command = null;
			return false;
		}

		/// <summary>Look up a command handler by long switch.</summary>
		/// <param name="sw">The long switch.</param>
		/// <param name="command">[out] The handler command if the long switch is handled; null otherwise.</param>
		/// <returns>True if the short switch is handled, false otherwise.</returns>
		public Boolean TryGetHandler(String sw, out Command command) {
			foreach (Command cmd in Commands) {
				if (cmd.Handles(sw)) {
					command = cmd;
					return true;
				}
			}
			command = null;
			return false;
		}

		/// <summary>Gets or sets the base type.</summary>
		/// <exception cref="ArgumentNullException">Thrown when one or more required arguments are null.</exception>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		public String BaseType {
			get => baseType.AssemblyQualifiedName;
			set {
				if (value is null) {
					throw new ArgumentNullException(nameof(value));
				}
				Type thisBaseType = Type.GetType(value);
				if (thisBaseType is null) {
					throw new Exception(String.Format($"Could not resolve type \"{value}\""));
				}
				baseType = thisBaseType;
				ReloadMethods();
			}
		}

		/// <summary>Gets or sets the commands.</summary>
		protected List<Command> Commands { get; set; }

		/// <summary>The suitable methods for handling commands.</summary>
		protected Dictionary<String, Command> HandlerMethods { get; set; }

		/// <summary>Gets or sets the output stream.</summary>
		protected Logger Logger { get; set; }

		/// <summary>Adds the methods.</summary>
		/// <param name="methods">The methods.</param>
		private void AddMethods(MethodInfo[] methods) {
			foreach (MethodInfo thisMethod in methods) {
				if (HandlerMethods.ContainsKey(thisMethod.Name)) {
					// TODO: Add override message notification
					HandlerMethods.Remove(thisMethod.Name);
				}
				Command newCommand = new(this, thisMethod);
				HandlerMethods.Add(thisMethod.Name, newCommand);
				Commands.Add(newCommand);
			}
		}

		/// <summary>Initializes a new instance of the Dataescher.CLI class.</summary>
		/// <param name="baseType">The base static class containing main.</param>
		/// <param name="logger">The logger.</param>
		public CLI(Type baseType, Logger logger) {
			Commands = new();
			HandlerMethods = new();
			if (logger is null) {
				Logger = new();
			} else {
				Logger = logger;
			}
			this.baseType = baseType;
			ReloadMethods();
		}

		/// <summary>Initializes a new instance of the <see cref="CLI"/> class.</summary>
		/// <param name="baseType">The base static class containing main.</param>
		public CLI(Type baseType) : this(baseType, null) { }

		/// <summary>Reload the potential handler methods.</summary>
		private void ReloadMethods() {
			// Find suitable handler methods
			HandlerMethods = new();

			// Get members of this class which implement Handler attribute
			MethodInfo[] defaultMethods = GetType().GetMethods()
				.Where(m => m.GetCustomAttributes(typeof(HandlerAttribute), false).Length > 0)
				//.Where(n => !n.IsStatic)
				.ToArray();
			// At these as the default methods
			AddMethods(defaultMethods);

			if (baseType is not null) {
				// Get members of the base class which implement Handler attribute
				MethodInfo[] baseMethods = baseType.GetMethods()
					.Where(m => m.GetCustomAttributes(typeof(HandlerAttribute), false).Length > 0)
					//.Where(n => n.IsStatic)
					.ToArray();
				// At these as the base methods (will override any default methods which have name conflicts)
				AddMethods(baseMethods);
			}
		}

		/// <summary>Indexer to get items within this collection using array index syntax.</summary>
		/// <param name="shortSwitch">The short switch.</param>
		/// <returns>The indexed item.</returns>
		public Command this[Char shortSwitch] {
			get {
				foreach (Command thisCommand in Commands) {
					if (thisCommand.Handles(shortSwitch)) {
						return thisCommand;
					}
				}
				return null;
			}
		}

		/// <summary>Indexer to get items within this collection using array index syntax.</summary>
		/// <param name="longSwitch">The long switch.</param>
		/// <returns>The indexed item.</returns>
		public Command this[String longSwitch] {
			get {
				foreach (Command thisCommand in Commands) {
					if (thisCommand.Handles(longSwitch)) {
						return thisCommand;
					}
				}
				return null;
			}
		}

		/// <summary>Gets help text.</summary>
		/// <param name="sb">The sb.</param>
		private void GetHelpText(StringBuilder sb) {
			sb.AppendLine("Command line usage:");
			sb.AppendLine();
			Boolean firstCommand = true;
			foreach (Command thisCommand in Commands) {
				if (!firstCommand) {
					sb.AppendLine();
				}
				firstCommand = false;
				thisCommand.GetHelpText(sb);
			}
			sb.AppendLine(Logger.Separator);
		}

		/// <summary>Gets the help text.</summary>
		public String HelpText {
			get {
				StringBuilder sb = new();
				GetHelpText(sb);
				return sb.ToString();
			}
		}

		/// <summary>The command result enumeration.</summary>
		public enum Result {
			/// <summary>The command passed.</summary>
			Ok,
			/// <summary>The command resulted in non-fatal error.</summary>
			Error,
			/// <summary>The command resulted in fatal error.</summary>
			FatalError
		}

		/// <summary>Gets the state of the command line processor.</summary>
		public Result CommandLineState { get; private set; }

		/// <summary>Process the command.</summary>
		/// <param name="command">[out] The handler command if the short switch is handled; null otherwise.</param>
		/// <param name="arguments">The arguments.</param>
		private void ProcessCommand(Command command, List<String> arguments) {
			if (command is not null) {
				if (CommandLineState != Result.FatalError) {
					// Record the logger error count before and after the command.
					Int32 errorCntBefore = Logger.ErrorCount;

					// Start the command and measure the time required to complete the command.
					Stopwatch stopWatch = Stopwatch.StartNew();
					Result result = command.Process(arguments);
					stopWatch.Stop();

					// Compute the elapsed time in nanoseconds
					Double seconds = stopWatch.ElapsedTicks * (1.0 / Stopwatch.Frequency);
					Double adjusted = ScaleFactors.GetPow10ScaleFactor(seconds, out ScaleFactor scaleFactor);
					String elapsedTimeStr = $"{adjusted} {scaleFactor.Prefix}seconds";
					Int32 errorCntAfter = Logger.ErrorCount;

					// If the error count increased, force an error state.
					Boolean sawErrors = errorCntAfter > errorCntBefore;
					if (sawErrors && (result == Result.Ok)) {
						result = Result.Error;
					}

					switch (result) {
						case Result.Ok:
							Logger.Note(Verbosity.CommandLineProcessing, $"[{command.Handler.Name}] finished successfully in {elapsedTimeStr}.");
							break;
						case Result.Error:
							if (CommandLineState == Result.Ok) {
								CommandLineState = Result.Error;
							}
							if (!sawErrors) {
								Logger.Error($"Invocation of [{command.Handler.Name}] resulted in non-fatal error.");
							}
							Logger.Note(Verbosity.CommandLineProcessing, $"[{command.Handler.Name}] finished with non-fatal error in {elapsedTimeStr}. Continuing command line processing.");
							break;
						case Result.FatalError:
							if (!sawErrors) {
								Logger.Error($"Invocation of [{command.Handler.Name}] resulted in fatal error.");
							}
							CommandLineState = Result.FatalError;
							Logger.Note(Verbosity.CommandLineProcessing, $"[{command.Handler.Name}] failed with fatal error in {elapsedTimeStr}.");
							break;
						default:
							CommandLineState = Result.FatalError;
							if (Logger is not null) {
								Logger.Error($"Result code {(Int32)result} not valid.");
								Logger.Note(Verbosity.CommandLineProcessing, $"[{command.Handler.Name}] finished with unknown code {(Int32)result} in {elapsedTimeStr}.");
							}
							break;
					}
				} else {
					Logger.Note(Verbosity.CommandLineProcessing, $"[{command.Handler.Name}] skipped because of previous fatal errors.");
				}
			}
		}

		/// <summary>Process the command line described by args.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <param name="args">The arguments.</param>
		/// <returns>A Result.</returns>
		public Result ProcessCommandLine(String[] args) {
			Stopwatch stopWatch = Stopwatch.StartNew();
			// The argument position in the args array
			Int32 argPos = 0;
			// The argument position for the current command
			Int32 commandArgPos = 0;
			Command command = null;
			List<String> arguments = new();
			CommandLineState = Result.Ok;
			if (args.Length == 0) {
				// Use the default command line to print out the help screen
				args = new String[1] { "-h" };
			}
			try {
				while ((argPos < args.Length) && (CommandLineState != Result.FatalError)) {
					String thisArg = args[argPos];
					if ((thisArg.Length > 1) && (thisArg[0] == '-')) {
						if ((thisArg.Length > 2) && (thisArg[1] == '-')) {
							if (Char.IsLetter(thisArg[2])) {
								// This is a long option
								// Process the previous command (if exists)
								ProcessCommand(command, arguments);
								if (CommandLineState == Result.FatalError) {
									break;
								}
								// Reset the command argument position
								commandArgPos = 0;
								// Proceed to next command
								command = this[thisArg.Substring(2)];
								Logger.Note(Verbosity.CommandLineProcessing, $"Saw command: \"{thisArg.Substring(2)}\"");
								if (command is null) {
									throw new Exception($"Unrecognized switch: \"{thisArg}\"");
								}
								arguments = new();
								argPos++;
								continue;
							}
						} else if (Char.IsLetter(thisArg[1]) || (thisArg[1] == '?')) {
							// This is a short option
							// Process the previous command (if exists)
							ProcessCommand(command, arguments);
							if (CommandLineState == Result.FatalError) {
								break;
							}
							// Reset the command argument position
							commandArgPos = 0;
							// Proceed to next command
							command = this[thisArg[1]];
							Logger.Note(Verbosity.CommandLineProcessing, $"Saw command: '{thisArg[1]}'");
							if (command is null) {
								throw new Exception($"Unrecognized switch: '{thisArg[1]}'");
							}
							arguments = new();
							// For short commands, the first parameter might not have a space before it.
							thisArg = thisArg.Substring(2);
							if (thisArg.Length == 0) {
								argPos++;
								continue;
							}
						}
					}
					if (command is null) {
						throw new Exception("Expected command.");
					}
					Logger.Note(Verbosity.CommandLineProcessing, $"   Parameter {commandArgPos + 1}: {thisArg}");
					arguments.Add(thisArg);
					argPos++;
					commandArgPos++;
				}
				if (CommandLineState != Result.FatalError) {
					// Process the last command (if exists)
					ProcessCommand(command, arguments);
				}
			} catch (Exception ex) {
				Logger.Error(ex.Message);
			}
			stopWatch.Stop();

			// Compute the elapsed time in nanoseconds
			Double seconds = stopWatch.ElapsedTicks * (1.0 / Stopwatch.Frequency);
			Double adjusted = ScaleFactors.GetPow10ScaleFactor(seconds, out ScaleFactor scaleFactor);
			String elapsedTimeStr = $"{adjusted} {scaleFactor.Prefix}seconds";
			Logger.Note(Verbosity.CommandLineProcessing, $"Completed in {elapsedTimeStr}");

			return CommandLineState;
		}

		#region Default Handlers

		/// <summary>Handler, called to display the help screen.</summary>
		[Handler("Display the help screen.", "-?, -h, --help")]
		public void Help() {
			Logger.Note(HelpText);
		}

		/// <summary>Handler, called to change the log destination.</summary>
		/// <param name="logPath">Full pathname of the log file.</param>
		[Handler("Set the log file.", "-l, --log")]
		public void Log(
			[Parameter("The log output.")]
			String logPath
		) {
			if (String.IsNullOrWhiteSpace(logPath)) {
				// Create a log file name based on the name of the executable
				logPath = $"{Path.GetFileName(Assembly.GetEntryAssembly().Location)}.log";
			}
			Logger = new(logPath);
		}

		/// <summary>Gets short path name.</summary>
		/// <param name="lpszLongPath">The path to convert.</param>
		/// <param name="lpszShortPath">
		/// 	Receives the 8.3 form of the filename, terminated by a null character. This string must already be
		/// 	sufficiently large to receive the 8.3 filename.
		/// </param>
		/// <param name="cchBuffer">The length of the string passed as lpszShortPath.</param>
		/// <returns>The length of the string passed as lpszShortPath.</returns>
		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		private static extern UInt32 GetShortPathName(
			[MarshalAs(UnmanagedType.LPWStr)]
			String lpszLongPath,
			[MarshalAs(UnmanagedType.LPWStr)]
			StringBuilder lpszShortPath,
			Int32 cchBuffer
		);

		/// <summary>Gets long path name.</summary>
		/// <param name="lpszShortPath">The path to convert.</param>
		/// <param name="lpszLongPath">
		/// 	Receives the 8.3 form of the filename, terminated by a null character. This string must already be
		/// 	sufficiently large to receive the 8.3 filename.
		/// </param>
		/// <param name="cchBuffer">The length of the string passed as lpszShortPath.</param>
		/// <returns>The length of the string passed as lpszShortPath.</returns>
		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		private static extern UInt32 GetLongPathName(
			[MarshalAs(UnmanagedType.LPWStr)]
			String lpszShortPath,
			[MarshalAs(UnmanagedType.LPWStr)]
			StringBuilder lpszLongPath,
			Int32 cchBuffer
		);

		/// <summary>Gets a windows physical path.</summary>
		/// <param name="path">Full pathname of the file.</param>
		/// <returns>The windows physical path.</returns>
		protected static String GetWindowsPhysicalPath(String path) {
			StringBuilder builder = new(255);
			// names with long extension can cause the short name to be actually larger than
			// the long name.
			UInt32 result = GetShortPathName(path, builder, builder.Capacity);
			if (result > 0) {
				path = builder.ToString();
				result = GetLongPathName(path, builder, builder.Capacity);
				if (result > 0) {
					if (result < builder.Capacity) {
						//Success retrieved long file name
						builder[0] = Char.ToLower(builder[0]);
						return builder.ToString(0, (Int32)result);
					}
					//Need more capacity in the buffer
					//specified in the result variable
					builder = new StringBuilder((Int32)result);
					result = GetLongPathName(path, builder, builder.Capacity);
					builder[0] = Char.ToLower(builder[0]);
					return builder.ToString(0, (Int32)result);
				}
			}
			return null;
		}

		/// <summary>Handler, called to set the working directory.</summary>
		/// <exception cref="Exception">Thrown when an exception error condition occurs.</exception>
		/// <param name="workingDirectory">Pathname of the working directory.</param>
		/// <returns>A Logger.Severity.</returns>
		[Handler("Set the working directory.", "--wd")]
		public static Severity SetWorkingDirectory(
			[Parameter("The new working directory.")]
			String workingDirectory
		) {
			String directory = GetWindowsPhysicalPath(workingDirectory);
			if (directory is null) {
				throw new Exception($"Could not resolve path: \"{workingDirectory}\"");
			}
			Directory.SetCurrentDirectory(directory);
			return Severity.Note;
		}

		/// <summary>Handler, called to set the working directory.</summary>
		[Handler("Set the logging verbosity level.", "-v, --verbosity")]
		public void SetVerbosityLevel(
			[Parameter("The verbosity level bit mask: 0 = Errors only (default), 1 = Command success message, 2 = Command processing messages, Other = user specific levels.")]
			Byte verbosityLevel
		) {
			Logger.VerbosityLevel = (Verbosity)verbosityLevel;
			Logger.Note(Verbosity.Level1, $"Verbosity level set to 0x{verbosityLevel:X2}");
		}

		/// <summary>Handler, called to retrieve a list of loaded assemblies.</summary>
		[Handler("Get a list of loaded assemblies.", "--assemblies")]
		public void GetLoadedAssemblies() {
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			Logger.Note("Loaded assemblies:");
			foreach (Assembly assembly in assemblies) {
				Logger.Note($"   {assembly.FullName}");
			}
		}

		#endregion
	}
}