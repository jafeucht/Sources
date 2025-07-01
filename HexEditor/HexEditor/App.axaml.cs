// <copyright file="App.axaml.cs" company="CheckSum, LLC">
// Copyright (c) 2025 CheckSum, LLC. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>6/30/2025</date>
// <summary>Implements the app.axaml class</summary>

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using TestAvaloniaApp;

namespace HexEditor {
	public partial class App : Application {
		/// <summary>Initializes the application by loading XAML etc.</summary>
		/// <seealso cref="M:Avalonia.Application.Initialize()"/>
		public override void Initialize() {
			AvaloniaXamlLoader.Load(this);
		}

		/// <summary>Executes the 'framework initialization completed' action.</summary>
		public override void OnFrameworkInitializationCompleted() {
			if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
				desktop.MainWindow = new MainWindow();
			}

			base.OnFrameworkInitializationCompleted();
		}
	}
}