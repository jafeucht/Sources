// <copyright file="Timer.cs" company="CheckSum, LLC">
// Copyright (c) 2025 CheckSum, LLC. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>6/30/2025</date>
// <summary>Implements the timer class</summary>

using Avalonia.Threading;
using System;
using System.Timers;

namespace Dataescher {
	/// <summary>A timer.</summary>
	public class Timer {
		/// <summary>(Immutable) the timer.</summary>
		private readonly System.Timers.Timer _timer;

		/// <summary>Gets or sets the interval.</summary>
		/// <exception cref="ArgumentOutOfRangeException">
		///     Thrown when one or more arguments are outside the required range.
		/// </exception>
		public Double Interval {
			get => _timer.Interval;
			set {
				if (value < 0) {
					throw new ArgumentOutOfRangeException(nameof(value));
				}
				_timer.Interval = value;
			}
		}

		/// <summary>Initializes a new instance of the <see cref="Timer"/> class.</summary>
		/// <param name="interval">The interval.</param>
		public Timer(Double interval) {
			_timer = new System.Timers.Timer(interval);
			_timer.Elapsed += OnTimerElapsed;
		}

		/// <summary>Gets or sets a value indicating whether this object is enabled.</summary>
		public Boolean Enabled {
			get => _timer.Enabled;
			set {
				if (value) {
					_timer.Start();
				} else {
					_timer.Stop();
				}
			}
		}

		/// <summary>Occurs when Tick.</summary>
		public event EventHandler? Tick;

		/// <summary>Raises the elapsed event.</summary>
		/// <param name="sender">Source of the event.</param>
		/// <param name="e">Event information to send to registered event handlers.</param>
		private void OnTimerElapsed(Object? sender, ElapsedEventArgs e) {
			Dispatcher.UIThread.Post(() => {
				Tick?.Invoke(this, EventArgs.Empty);
			});
		}
	}
}
