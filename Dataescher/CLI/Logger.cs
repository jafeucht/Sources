// <copyright file="Logger.cs" company="Dataescher">
// Copyright (c) 2024-2025 Dataescher. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>5/4/2023</date>
// <summary>Implements the logger class</summary>

using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Dataescher.CommandLineInterface {
	/// <summary>Values that represent log message severities.</summary>
	public enum Severity : Int32 {
		/// <summary>Process a note.</summary>
		Note,
		/// <summary>Process a success message.</summary>
		Success,
		/// <summary>A warning has occurred.</summary>
		Warning,
		/// <summary>An error has occurred.</summary>
		Error
	}

	/// <summary>A bit-field of flags for specifying verbosity settings./.</summary>
	[Flags]
	public enum Verbosity : Byte {
		/// <summary>No messages logged.</summary>
		None = 0,
		/// <summary>User level 1.</summary>
		Level1 = 1,
		/// <summary>User level 2.</summary>
		Level2 = 2,
		/// <summary>User level 3.</summary>
		Level3 = 4,
		/// <summary>User level 4.</summary>
		Level4 = 8,
		/// <summary>User level 5.</summary>
		Level5 = 16,
		/// <summary>Success message.</summary>
		Success = 32,
		/// <summary>Display warnings.</summary>
		Warnings = 64,
		/// <summary>Display command line processing messages.</summary>
		CommandLineProcessing = 128,
		/// <summary>Log all messages.</summary>
		All = 255
	};

	/// <summary>A logger class.</summary>
	public class Logger : IDisposable {
		/// <summary>The log counter.</summary>
		private static Int32 _logCounter;

		/// <summary>Name of the log.</summary>
		private readonly String _logName;

		/// <summary>(Immutable) The message format.</summary>
		private const String MESSAGE_FORMAT = @"${date:format=yyyyMMdd HH\:mm\:ss.fff} | ${level:uppercase=true} | ${replace:inner=${message}:searchFor=[success]:wholeWords=false:replaceWith=:ignoreCase=false}${when:when=length('${exception:format=ToString}') > 0:inner= ${exception:format=ToString}}";

		/// <summary>(Immutable) The thread lock.</summary>
		private readonly Object _threadLock = new();

		/// <summary>(Immutable) the NLog logger.</summary>
		private NLog.Logger _logger;

		/// <summary>Gets the number of warnings.</summary>
		public Int32 WarningCount { get; private set; }

		/// <summary>Gets the number of errors.</summary>
		public Int32 ErrorCount { get; private set; }

		/// <summary>Gets console window.</summary>
		/// <returns>The console window handle, zero if no console exists.</returns>
		[DllImport("kernel32.dll")]
		private static extern IntPtr GetConsoleWindow();

		/// <summary>True if console application, false if not.</summary>
		private static readonly Boolean _isConsoleApplication;

		/// <summary>Initializes static members of the CheckSum.Logger class.</summary>
		static Logger() {
			_isConsoleApplication = GetConsoleWindow() != IntPtr.Zero;
			Separator = new('=', 32);
		}

		/// <summary>(Immutable) Gets the separator string.</summary>
		public static readonly String Separator;

		/// <summary>(Immutable) The log factory.</summary>
		private readonly LogFactory _logFactory;

		/// <summary>Gets or sets the verbosity level for log messages.</summary>
		public Verbosity VerbosityLevel { get; set; }

		/// <summary>Initializes a new instance of the <see cref="Logger"/> class.</summary>
		public Logger() : this(null, Verbosity.All) { }

		/// <summary>Initializes a new instance of the <see cref="Logger"/> class.</summary>
		/// <param name="verbosity">The verbosity level.</param>
		public Logger(Verbosity verbosity) : this(null, verbosity) { }

		/// <summary>Initializes a new instance of the CheckSum.Logger class.</summary>
		/// <param name="fileName">(Optional) The text stream which to send log messages.</param>
		/// <param name="verbosity">(Optional) The verbosity level.</param>
		public Logger(String fileName, Verbosity verbosity = Verbosity.All) {
			VerbosityLevel = verbosity;
			LoggingConfiguration config = new();
			_logName = $"Logger{++_logCounter}";
			if (!String.IsNullOrEmpty(fileName)) {
				FileTarget fileTarget = new($"{_logName}_LogFile") {
					FileName = $"{fileName}",
					Layout = MESSAGE_FORMAT
				};
				config.AddTarget(fileTarget);
				config.AddRule(LogLevel.Info, LogLevel.Fatal, fileTarget);
			}

			if (_isConsoleApplication) {
				ColoredConsoleTarget consoleTarget = new($"{_logName}_Console") {
					Layout = MESSAGE_FORMAT
				};
				consoleTarget.RowHighlightingRules.Add(
					new ConsoleRowHighlightingRule(
						"contains('${message}', '[success]')",
						ConsoleOutputColor.Green,
						ConsoleOutputColor.NoChange
					)
				);
				config.AddTarget(consoleTarget);
				config.AddRule(LogLevel.Info, LogLevel.Fatal, consoleTarget);
			}
			_logFactory = new() {
				Configuration = config
			};
			_logger = _logFactory.GetLogger(_logName);
			// Write a header
			_logger.Info(Separator);
			_logger.Info($"Date: {DateTime.Now}");
			if (Assembly.GetCallingAssembly() is not Assembly entryApp) {
				throw new Exception("Cannot resolve calling assembly.");
			}
			String entryAppLocation = entryApp.Location;
			if (!String.IsNullOrWhiteSpace(entryAppLocation)) {
				FileVersionInfo entryAppVersionInfo = FileVersionInfo.GetVersionInfo(entryAppLocation);
				if (!String.IsNullOrEmpty(entryAppVersionInfo.Comments)) {
					_logger.Info(entryAppVersionInfo.Comments);
				}
				if (!String.IsNullOrEmpty(entryAppVersionInfo.ProductName)) {
					_logger.Info(entryAppVersionInfo.ProductName);
				}
				if (!String.IsNullOrEmpty(entryAppVersionInfo.CompanyName)) {
					_logger.Info(entryAppVersionInfo.CompanyName);
				}
				if (!String.IsNullOrEmpty(entryAppVersionInfo.LegalCopyright)) {
					_logger.Info(entryAppVersionInfo.LegalCopyright);
				}
				_logger.Info($"Product version: {entryAppVersionInfo.ProductVersion}");
				_logger.Info($"File version: {entryAppVersionInfo.FileVersion}");
			}
			if (Assembly.GetExecutingAssembly() is Assembly libApp && libApp != entryApp) {
				// Log the CheckSumLib version if not same as calling assembly
				String libAppLocation = libApp.Location;
				if (!String.IsNullOrWhiteSpace(libAppLocation)) {
					// Lib assembly location can be empty because it may be loaded from byte stream with Fody
					FileVersionInfo libAppVersionInfo = FileVersionInfo.GetVersionInfo(libAppLocation);
					if (libAppVersionInfo is not null) {
						_logger.Info($"CheckSumLib version: {libAppVersionInfo.ProductVersion}");
					}
				}
			}
			_logger.Info(Separator);
			OnLogMessage = null;
		}

		/// <summary>Finalizes an instance of the <see cref="Logger"/> class.</summary>
		~Logger() {
			Dispose();
		}

		/// <summary>Raises the on log message event.</summary>
		/// <param name="severity">The severity.</param>
		/// <param name="message">The message.</param>
		private void RaiseOnLogMessageEvent(Severity severity, String message) {
			if (OnLogMessage is not null) {
				Task.Run(() => OnLogMessage.Invoke(this, new(severity, message, DateTime.Now)));
			}
		}

		/// <summary>Logs a note.</summary>
		/// <param name="message">The message.</param>
		public void Note(String message) {
			Note(Verbosity.Level1, message);
		}

		/// <summary>Logs a note with specified verbosity level.</summary>
		/// <param name="verbosity">The verbosity flags.</param>
		/// <param name="message">The message.</param>
		public void Note(Verbosity verbosity, String message) {
			lock (_threadLock) {
				if (_logger is null || !VerbosityLevel.HasFlag(verbosity)) {
					return;
				}
				_logger.Info(message);
			}
			RaiseOnLogMessageEvent(Severity.Note, message);
		}

		/// <summary>Logs a note.</summary>
		/// <param name="message">The message.</param>
		public void Success(String message) {
			lock (_threadLock) {
				if (_logger is null || !VerbosityLevel.HasFlag(Verbosity.Success)) {
					return;
				}
				_logger.Info($"[success]{message}");
			}
			RaiseOnLogMessageEvent(Severity.Success, message);
		}

		/// <summary>Logs a warning.</summary>
		/// <param name="message">The message.</param>
		public void Warning(String message) {
			lock (_threadLock) {
				if (_logger is null) {
					return;
				}
				WarningCount++;
				_logger.Warn(message);
			}
			RaiseOnLogMessageEvent(Severity.Warning, message);
		}

		/// <summary>Logs an error.</summary>
		/// <param name="message">The message.</param>
		public void Error(String message) {
			lock (_threadLock) {
				if (_logger is null) {
					return;
				}
				ErrorCount++;
				_logger.Error(message);
			}
			RaiseOnLogMessageEvent(Severity.Error, message);
		}

		/// <summary>Logs a debug message.</summary>
		/// <param name="message">The message.</param>
		public void Debug(String message) {
			lock (_threadLock) {
				if (_logger is null) {
					return;
				}
				_logger.Debug(message);
			}
		}

		/// <summary>Logs a debug message.</summary>
		/// <param name="message">The message.</param>
		public void Trace(String message) {
			lock (_threadLock) {
				if (_logger is null) {
					return;
				}
				_logger.Trace(message);
			}
		}

		/// <summary>Logs an error.</summary>
		/// <param name="ex">The exception.</param>
		/// <param name="message">The message.</param>
		public void Exception(Exception ex, String message) {
			lock (_threadLock) {
				if (_logger is null) {
					return;
				}
				ErrorCount++;
				_logger.Error(ex, message);
			}
			RaiseOnLogMessageEvent(Severity.Error, message);
		}

		/// <summary>Flushes all logs.</summary>
		public static void Flush() {
			LogManager.Flush();
		}

		/// <summary>
		///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose() {
			lock (_threadLock) {
				if (_logger is null) {
					return;
				}
				_logger = null;
				_logFactory.Flush();
				_logFactory.Shutdown();
			}
		}

		/// <summary>Additional information for ITAC message events.</summary>
		public class OnLogMessageEventArgs : EventArgs {
			/// <summary>Gets the severity.</summary>
			public Severity Severity { get; private set; }

			/// <summary>Gets the message.</summary>
			public String Message { get; private set; }

			/// <summary>Gets the date/time of the event.</summary>
			public DateTime Timestamp { get; private set; }

			/// <summary>Initializes a new instance of the <see cref="OnLogMessageEventArgs"/> class.</summary>
			/// <param name="severity">The type.</param>
			/// <param name="message">The message.</param>
			/// <param name="timestamp">The timestamp Date/Time.</param>
			public OnLogMessageEventArgs(Severity severity, String message, DateTime timestamp) {
				Severity = severity;
				Message = message;
				Timestamp = timestamp;
			}
		}

		/// <summary>The on message event handler.</summary>
		public EventHandler<OnLogMessageEventArgs> OnLogMessage { get; set; }
	}
}