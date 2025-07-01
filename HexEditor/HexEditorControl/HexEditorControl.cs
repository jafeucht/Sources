// <copyright file="HexEditorControl.cs" company="CheckSum, LLC">
// Copyright (c) 2025 CheckSum, LLC. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>6/30/2025</date>
// <summary>Implements the hexadecimal editor control class</summary>

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Dataescher.Data;
using System;
using System.Globalization;

namespace Dataescher.Controls {
	/// <summary>A hexadecimal editor control.</summary>
	/// <seealso cref="T:UserControl"/>
	public partial class HexEditorControl : UserControl {
		/// <summary>Initializes static members of the <see cref="HexEditorControl"/> class.</summary>
		static HexEditorControl() {
#if NET6_0_OR_GREATER
			System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
#endif
		}
		/// <summary>Initializes a new instance of the <see cref="HexEditorControl"/> class.</summary>
		public HexEditorControl() {
			InitializeComponent();
			Layout.bytesPerAddress = 1;
			Layout.bytesPerCell = 1;

			// Initialize the component
			InitializeComponent();

			currentByteAddress = 0;
			cursorByteAddress = 0;
			EditMode = false;
			UndoHistory = new(this);
			Memory = new();
			Errors = new();
			Warnings = new();
			TmScrollTimer = new(25);

			inDesignMode = true;
			if (inDesignMode) {
				BorderThickness = new Thickness(1);
				BorderBrush = new SolidColorBrush(Colors.Black);
				BlankData = 0xFF;
				dataAddress = 0;
				// Select some data
				SelectionByteRegion = MemoryRegion.FromStartAndEndAddresses(7, 44);
			}
			PointerWheelChanged += OnPointerWheelChanged;
			PointerPressed += OnPointerPressed;
			PointerReleased += OnPointerReleased;
			PointerMoved += OnPointerMoved;
			AttachedToVisualTree += OnAttachedToVisualTree;
		}

		/// <summary>Raises the avalonia property changed event.</summary>
		/// <param name="change">Event information to send to registered event handlers.</param>
		/// <seealso cref="M:Avalonia.Controls.Control.OnPropertyChanged(AvaloniaPropertyChangedEventArgs)"/>
		protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
			base.OnPropertyChanged(change);

			if (change.Property == BoundsProperty) {
				Rect newSize = (Rect)change.NewValue!;
				OnSizeChanged(newSize.Size);
			}
		}
		/// <summary>Raises the pointer wheel event.</summary>
		/// <param name="sender">Source of the event.</param>
		/// <param name="e">Event information to send to registered event handlers.</param>
		private void OnPointerWheelChanged(Object sender, PointerWheelEventArgs e) {
			Int64 newByteAddress;
			Double delta = e.Delta.Y; // typically ±1 per notch

			KeyModifiers modifiers = e.KeyModifiers;
			newByteAddress = modifiers.HasFlag(KeyModifiers.Control)
				? currentByteAddress - (Layout.rowCount * (Int32)delta * Layout.bytesPerRow * 3)
				: currentByteAddress - ((Int32)delta * Layout.bytesPerRow * 3);

			CurrentByteAddress = (UInt32)Math.Min(Math.Max(newByteAddress, 0), MaxCurrentByteAddress);
		}

		/// <summary>Raises the pointer pressed event.</summary>
		/// <param name="sender">Source of the event.</param>
		/// <param name="e">Event information to send to registered event handlers.</param>
		private void OnPointerPressed(Object sender, PointerPressedEventArgs e) {
			PointerPointProperties props = e.GetCurrentPoint(this).Properties;
			Point pos = e.GetPosition(this);
			mouseDownPosition = new Point(pos.X + Layout._hOffset, pos.Y);
			EditMode = false;
			mouseDragging = false;

			if (props.PointerUpdateKind == PointerUpdateKind.RightButtonPressed) {
				// TODO: Maybe someday, show context menu
				return;
			} else if (props.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed) {
				HexEditorControlFields oldField = mouseDownField;
				mouseDownField = PointToField(mouseDownPosition);

				switch (mouseDownField) {
					case HexEditorControlFields.DataField:
					case HexEditorControlFields.DataCharacterField: {
						Point offset = (mouseDownField == HexEditorControlFields.DataField) ? Layout.dataOffset : Layout.dataCharOffset;
						Point spacing = (mouseDownField == HexEditorControlFields.DataField) ? Layout.dataSpacing : Layout.dataCharSpacing;

						mouseDownDataPosition = new(
							Math.Floor((mouseDownPosition.X - offset.X) / spacing.X),
							Math.Floor((mouseDownPosition.Y - offset.Y) / spacing.Y)
						);

						if (mouseDownDataPosition.X >= 0 &&
							mouseDownDataPosition.X < Layout.columnCount &&
							mouseDownDataPosition.Y >= 0 &&
							mouseDownDataPosition.Y < Layout.rowCount) {
							UInt32 newCursor = (UInt32)(currentByteAddress +
								(mouseDownDataPosition.X * Layout.bytesPerCell) +
								(mouseDownDataPosition.Y * Layout.bytesPerRow));

							SelectionByteRegion = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? MemoryRegion.FromStartAndEndAddresses(cursorByteAddress, newCursor) : null;

							CursorByteAddress = newCursor;
							mouseDownByteAddress = newCursor;
							mouseIsDown = true;
						} else {
							mouseDownField = HexEditorControlFields.None;
						}
						break;
					}
				}

				if (mouseDownField != oldField) {
					InvalidateVisual();
				}
			}

			e.Handled = true;
		}

		/// <summary>Raises the pointer released event.</summary>
		/// <param name="sender">Source of the event.</param>
		/// <param name="e">Event information to send to registered event handlers.</param>
		private void OnPointerReleased(Object sender, PointerReleasedEventArgs e) {
			PointerPointProperties props = e.GetCurrentPoint(this).Properties;

			if (props.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased) {
				TmScrollTimer.Enabled = false;
				mouseIsDown = false;
				mouseDragging = false;
			}

			e.Handled = true;
		}

		/// <summary>Raises the pointer event.</summary>
		/// <param name="sender">Source of the event.</param>
		/// <param name="e">Event information to send to registered event handlers.</param>
		private void OnPointerMoved(Object sender, PointerEventArgs e) {
			PointerPointProperties props = e.GetCurrentPoint(this).Properties;
			Point pos = e.GetPosition(this);
			Point dragPosition = new(pos.X + Layout._hOffset, pos.Y);

			if (mouseIsDown && props.IsLeftButtonPressed) {
				if (!mouseDragging) {
					Double dragDistance = Math.Sqrt(
						Math.Pow(mouseDownPosition.X - dragPosition.X, 2) +
						Math.Pow(mouseDownPosition.Y - dragPosition.Y, 2));

					if (dragDistance > 20) {
						mouseDragging = true;
					} else {
						return;
					}
				}

				HexEditorControlFields field = PointToField(dragPosition);
				switch (field) {
					case HexEditorControlFields.DataIndexHeaderField:
						if (mouseDownField == HexEditorControlFields.DataField) {
							moveUp = true;
							TmScrollTimer.Enabled = true;
						}
						break;

					case HexEditorControlFields.DataField:
					case HexEditorControlFields.DataCharacterField: {
						Point offset = (field == HexEditorControlFields.DataField) ? Layout.dataOffset : Layout.dataCharOffset;
						Point spacing = (field == HexEditorControlFields.DataField) ? Layout.dataSpacing : Layout.dataCharSpacing;

						mouseMoveDataPosition = new(
							Math.Floor((dragPosition.X - offset.X) / spacing.X),
							Math.Floor((dragPosition.Y - offset.Y) / spacing.Y)
						);

						if (mouseMoveDataPosition.X >= 0 &&
							mouseMoveDataPosition.X < Layout.columnCount &&
							mouseMoveDataPosition.Y >= 0 &&
							mouseMoveDataPosition.Y < Layout.rowCount) {
							UInt32 moveByteAddress = (UInt32)(CurrentByteAddress + (mouseMoveDataPosition.X * Layout.bytesPerCell) + (mouseMoveDataPosition.Y * Layout.bytesPerRow));

							cursorByteAddress = moveByteAddress;
							SelectionByteRegion = MemoryRegion.FromStartAndEndAddresses(moveByteAddress, mouseDownByteAddress);
							TmScrollTimer.Enabled = false;
						} else if (mouseMoveDataPosition.Y >= Layout.rowCount) {
							moveUp = false;
							TmScrollTimer.Enabled = true;
						} else {
							TmScrollTimer.Enabled = false;
						}

						break;
					}

					default:
						TmScrollTimer.Enabled = false;
						break;
				}
			}
			e.Handled = true;
		}

		/// <summary>Executes the 'size changed' action.</summary>
		/// <param name="newSize">Size of the new.</param>
		private void OnSizeChanged(Size newSize) {
			ComputeLayoutParameters();
			InvalidateVisual();
		}

		/// <summary>Raises the visual tree attachment event.</summary>
		/// <param name="sender">Source of the event.</param>
		/// <param name="e">Event information to send to registered event handlers.</param>
		private void OnAttachedToVisualTree(Object sender, VisualTreeAttachmentEventArgs e) {
			ComputeLayoutParameters();
			InvalidateVisual();
		}

		/// <summary>Creates a text.</summary>
		/// <param name="text">The text.</param>
		/// <param name="brush">The brush.</param>
		/// <returns>The new text.</returns>
		private FormattedText CreateText(String text, IBrush brush) {
			return new FormattedText(
				text,
				CultureInfo.CurrentCulture,
				FlowDirection.LeftToRight,
				font,
				fontSize,
				brush
			);
		}

		/// <summary>Renders the visual to a <see cref="T:Avalonia.Media.DrawingContext" />.</summary>
		/// <param name="context">The drawing context.</param>
		/// <seealso cref="M:Avalonia.Visual.Render(DrawingContext)"/>
		public override void Render(DrawingContext context) {
			base.Render(context);

			// Brushes
			SolidColorBrush normalTextBrush = new(normalTextColor);
			SolidColorBrush backgroundBrush = new(backgroundColor);
			SolidColorBrush headerRegionBrush = new(oddColumnColor);
			SolidColorBrush evenColumnBrush = new(backgroundColor);
			SolidColorBrush oddColumnBrush = new(oddColumnColor);

			if ((Bounds.Height == 0) || Double.IsNaN(Bounds.Height)) {
				// Seen this condition occur
				return;
			} else if (Layout.drawAreaWidth == 0) {
				return;
			}

			screenRegion = MemoryRegion.FromStartAddressAndSize(CurrentAddress, Layout.rowCount * Layout.bytesPerRow);
			screenData = new(screenRegion, Memory);

			// Fill background
			context.FillRectangle(backgroundBrush, new Rect(0, 0, Bounds.Width, Bounds.Height));

			// Draw header regions
			context.FillRectangle(headerRegionBrush, Layout.addressField);
			context.FillRectangle(headerRegionBrush, Layout.dataIndexField);

			// Draw cursor address string
			FormattedText formattedCursorText = CreateText(AddressToString(cursorByteAddress), normalTextBrush);
			context.DrawText(formattedCursorText, Layout.cursorAddressPosition);

			// Draw data regions
			for (UInt32 colIdx = 0; colIdx < Layout.columnCount; colIdx++) {
				Boolean bEvenIndex = (colIdx & 1) == 0;
				SolidColorBrush colBrush = bEvenIndex ? evenColumnBrush : oddColumnBrush;
				context.FillRectangle(colBrush, Layout.dataFieldColumns[colIdx]);

				for (UInt32 rowIdx = 0; rowIdx < Layout.rowCount; rowIdx++) {
					DrawData(context, colIdx, rowIdx);
				}
			}

			// Draw lines
			Pen pen = new(new SolidColorBrush(lineColor), 1);
			context.DrawLine(pen, new Point(-Layout._hOffset, 0), new Point(Layout.drawAreaWidth - Layout._hOffset, 0));
			context.DrawLine(pen, new Point(-Layout._hOffset, 0), new Point(-Layout._hOffset, Bounds.Height));
			context.DrawLine(pen, new Point(-Layout._hOffset, Layout.tDataDivY), new Point(Layout.drawAreaWidth - Layout._hOffset, Layout.tDataDivY));
			context.DrawLine(pen, new Point(Layout.lDataDivX - Layout._hOffset, 0), new Point(Layout.lDataDivX - Layout._hOffset, Bounds.Height));
			context.DrawLine(pen, new Point(Layout.rDataDivX - Layout._hOffset, 0), new Point(Layout.rDataDivX - Layout._hOffset, Bounds.Height));
			if (Layout.showDataChars) {
				context.DrawLine(pen, new Point(Layout.drawAreaWidth - Layout._hOffset, 0), new Point(Layout.drawAreaWidth - Layout._hOffset, Bounds.Height));
			}

			// Row headers
			Int64 curByteAddress = currentByteAddress;
			for (UInt32 rowIdx = 0; rowIdx < Layout.rowCount; rowIdx++) {
				if (curByteAddress > MaxByteAddress) {
					break;
				}

				FormattedText addrText = CreateText(AddressToString(curByteAddress / Layout.bytesPerAddress), normalTextBrush);
				context.DrawText(addrText, Layout.addressHeaderPoint[rowIdx]);
				curByteAddress += Layout.bytesPerRow;
			}

			// Column headers
			for (UInt32 colIdx = 0; colIdx < Layout.columnCount; colIdx++) {
				FormattedText headerText = CreateText(ColHeaderToString(colIdx), normalTextBrush);
				context.DrawText(headerText, Layout.dataIndexHeaderPoint[colIdx]);
			}
		}
		/// <summary>Draw text.</summary>
		/// <param name="context">The context.</param>
		/// <param name="text">The text.</param>
		/// <param name="brush">The brush.</param>
		/// <param name="position">The position.</param>
		private void DrawText(DrawingContext context, String text, IBrush brush, Point position) {
			FormattedText formattedText = new(
				text,
				CultureInfo.CurrentCulture,
				FlowDirection.LeftToRight,
				font,
				fontSize,
				brush);

			context.DrawText(formattedText, position);
		}

		/// <summary>Draw data.</summary>
		/// <param name="context">The context.</param>
		/// <param name="col">The col.</param>
		/// <param name="row">The row.</param>
		private void DrawData(DrawingContext context, UInt32 col, UInt32 row) {
			SolidColorBrush normalTextBrush = new(NormalTextColor);
			SolidColorBrush unimplementedTextBrush = new(unimplementedByteTextColor);
			SolidColorBrush editTextBrush = new(NormalTextColor);
			SolidColorBrush cursorTextBrush = new(Colors.White);
			SolidColorBrush cursorUnimplementedTextBrush = new(unimplementedByteTextColor);
			SolidColorBrush cursorActiveBrush = new(cursorColorActive);
			SolidColorBrush cursorPassiveBrush = new(cursorColorPassive);
			SolidColorBrush selectActiveBrush = new(selectionBackgroundColorActive);
			SolidColorBrush selectPassiveBrush = new(selectionBackgroundColorPassive);
			SolidColorBrush editBrush = new(editRegionBackgroundColor);

			UInt32 dataIdx = col + (row * Layout.columnCount);
			UInt32 dataByteAddress = currentByteAddress + (dataIdx * Layout.bytesPerCell);
			if (dataByteAddress > MaxByteAddress) {
				return;
			}

			Point dataPos = Layout.dataPosition[col, row];
			Point charPos = Layout.dataCharPosition[col, row];

			// Selection highlight
			if (SelectionByteRegion != null &&
				dataByteAddress >= SelectionByteRegion.StartAddress &&
				dataByteAddress <= SelectionByteRegion.EndAddress) {
				if (mouseDownField == HexEditorControlFields.DataCharacterField) {
					context.FillRectangle(selectPassiveBrush, Layout.dataSelectRects[col, row]);
				} else {
					context.FillRectangle(selectActiveBrush, Layout.dataSelectRects[col, row]);
				}

				if (Layout.showDataChars) {
					if (mouseDownField == HexEditorControlFields.DataCharacterField) {
						context.FillRectangle(selectActiveBrush, Layout.dataCharSelectRects[col, row]);
					} else {
						context.FillRectangle(selectPassiveBrush, Layout.dataCharSelectRects[col, row]);
					}
				}
			}

			String dataString = DataToString(dataByteAddress, out Boolean isImplemented);
			Char dataChar = Layout.showDataChars ? DataToChar(dataByteAddress, out _) : '.';

			if (CursorByteAddress == dataByteAddress) {
				// Draw cursor box
				if (mouseDownField == HexEditorControlFields.DataCharacterField) {
					context.FillRectangle(cursorPassiveBrush, Layout.dataCursorRects[col, row]);
				} else {
					context.FillRectangle(cursorActiveBrush, Layout.dataCursorRects[col, row]);
				}

				if (Layout.showDataChars) {
					if (mouseDownField == HexEditorControlFields.DataCharacterField) {
						context.FillRectangle(cursorActiveBrush, Layout.dataCharCursorRects[col, row]);
					} else {
						context.FillRectangle(cursorPassiveBrush, Layout.dataCharCursorRects[col, row]);
					}
				}
			}

			if (EditMode && EditByteAddress == dataByteAddress) {
				Rect dataSelectRect = Layout.dataSelectRects[col, row];
				Double selectWidth = Layout.strWidthPow2[Layout.strWidthPow2.Length - 1] / Math.Pow(2, Layout.strWidthPow2.Length - 1);

				Rect dataEditRect = mouseDownField == HexEditorControlFields.DataCharacterField
					? new Rect(
						dataSelectRect.X + Layout.halfCharWidth,
						dataSelectRect.Y,
						selectWidth * DataSizeBytes * 2,
						dataSelectRect.Height
					)
					: new Rect(
						dataSelectRect.X + (editPosition * selectWidth) + Layout.halfCharWidth,
						dataSelectRect.Y,
						selectWidth,
						dataSelectRect.Height
					);
				context.FillRectangle(editBrush, dataEditRect);
				DrawText(context, dataString, editTextBrush, dataPos);

				if (Layout.showDataChars) {
					context.FillRectangle(editBrush, Layout.dataCharCursorRects[col, row]);
					DrawText(context, dataChar.ToString(), editTextBrush, charPos);
				}
			} else if (CursorByteAddress == dataByteAddress) {
				if (isImplemented) {
					DrawText(context, dataString, cursorTextBrush, dataPos);
				} else {
					DrawText(context, dataString, cursorUnimplementedTextBrush, dataPos);
				}

				if (Layout.showDataChars) {
					SolidColorBrush brush = isImplemented ? cursorTextBrush : cursorUnimplementedTextBrush;
					DrawText(context, dataChar.ToString(), brush, charPos);
				}
			} else {
				SolidColorBrush brush = isImplemented ? normalTextBrush : unimplementedTextBrush;
				DrawText(context, dataString, brush, dataPos);

				if (Layout.showDataChars) {
					SolidColorBrush charBrush = isImplemented ? normalTextBrush : unimplementedTextBrush;
					DrawText(context, dataChar.ToString(), charBrush, charPos);
				}
			}
		}
	}
}