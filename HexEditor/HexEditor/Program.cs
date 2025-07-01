// <copyright file="Program.cs" company="CheckSum, LLC">
// Copyright (c) 2025 CheckSum, LLC. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>6/30/2025</date>
// <summary>Implements the program class</summary>

using Avalonia;
using System;

namespace HexEditor {
	/// <summary>A program.</summary>
	internal class Program {
		/// <summary>
		///     Initialization code. Don't use any Avalonia, third-party APIs or any SynchronizationContext-reliant code
		///     before AppMain is called: things aren't initialized yet and stuff might break.
		/// </summary>
		/// <param name="args">An array of command-line argument strings.</param>
		[STAThread]
		public static void Main(String[] args) {
			BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
		}

		/// <summary>Avalonia configuration, don't remove; also used by visual designer.</summary>
		/// <returns>An AppBuilder.</returns>
		public static AppBuilder BuildAvaloniaApp() {
			return AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
		}
	}
}
