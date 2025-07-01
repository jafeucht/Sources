// <copyright file="HexEditorControl.Layout.cs" company="CheckSum, LLC">
// Copyright (c) 2025 CheckSum, LLC. All rights reserved.
// </copyright>
// <author>Jonathan Feucht</author>
// <date>6/30/2025</date>
// <summary>Implements the hexadecimal editor control. layout class</summary>

using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using System;
using System.ComponentModel;

namespace Dataescher.Controls {
	public partial class HexEditorControl {
		/// <summary>A layout parameters.</summary>
		/// <seealso cref="T:Avalonia.Controls.UserControl"/>
		private struct LayoutParameters {
			/// <summary>Gets a value indicating whether the data characters is shown.</summary>
			public Boolean showDataChars;
			/// <summary>The data character padding.</summary>
			public Double dataCharPadding;
			/// <summary>The width of a fixed-width string with character count of 2 to the power of index.</summary>
			public Double[] strWidthPow2;
			/// <summary>Size of the character.</summary>
			public Size charSize;
			/// <summary>A standard separation value of half a single fixed-width character.</summary>
			public Double halfCharWidth;
			/// <summary>The bytes per address location.</summary>
			public UInt32 bytesPerAddress;

			/// <summary>The bytes per cell.</summary>
			public UInt32 bytesPerCell;

			/// <summary>The bytes per row.</summary>
			public UInt32 bytesPerRow;

			/// <summary>The number of address locations per row, which may be different than bytes per row.</summary>
			[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
			public UInt32 addressesPerRow;
			/// <summary>Gets the number of columns.</summary>
			public UInt32 columnCount;
			/// <summary>Gets the number of rows.</summary>
			public UInt32 rowCount;
			/// <summary>The horizontal divider Y position.</summary>
			public Double tDataDivY;
			/// <summary>The vertical divider X position.</summary>
			public Double lDataDivX;
			/// <summary>Gets the div x coordinate.</summary>
			public Double rDataDivX;
			/// <summary>Width of the data character area.</summary>
			public Double dataCharFieldWidth;
			/// <summary>The draw area width.</summary>
			public Double drawAreaWidth;
			/// <summary>Get the horizontal position of a data column.</summary>
			public Double[] dataColX;
			/// <summary>Get the vertical position of a data row.</summary>
			public Double[] dataRowY;
			/// <summary>Gets the number of col header characters.</summary>
			public UInt32 numColHeaderChars;
			/// <summary>Gets the column header X coordinate for a provided column.</summary>
			public Double[] dataIndexHeaderX;
			/// <summary>Get the horizontal position of a data column.</summary>
			public Point[] dataIndexHeaderPoint;
			/// <summary>Gets left header text position.</summary>
			public Point[] addressHeaderPoint;
			/// <summary>Gets the get value position.</summary>
			public Point cursorAddressPosition;
			/// <summary>The cursor address field.</summary>
			public Rect cursorAddressField;
			/// <summary>The address field.</summary>
			public Rect addressField;
			/// <summary>The data character field.</summary>
			public Rect dataCharField;
			/// <summary>The data index field.</summary>
			public Rect dataIndexField;
			/// <summary>The data field columns.</summary>
			public Rect[] dataFieldColumns;
			/// <summary>Width of the draw area.</summary>
			/// <summary>The horizontal draw offset.</summary>
			public Double _hOffset;
			/// <summary>Gets data character x coordinate.</summary>
			public Double[] dataCharX;
			/// <summary>The data selection areas.</summary>
			public Rect[,] dataSelectRects;
			/// <summary>The data cursor areas.</summary>
			public Rect[,] dataCursorRects;
			/// <summary>The data character selection areas.</summary>
			public Rect[,] dataCharSelectRects;
			/// <summary>The data character cursor areas.</summary>
			public Rect[,] dataCharCursorRects;
			/// <summary>The data position.</summary>
			public Point[,] dataPosition;
			/// <summary>The data character position.</summary>
			public Point[,] dataCharPosition;
			/// <summary>The data offset.</summary>
			public Point dataOffset;
			/// <summary>The data spacing.</summary>
			public Point dataSpacing;
			/// <summary>The data character offset.</summary>
			public Point dataCharOffset;
			/// <summary>The data character spacing.</summary>
			public Point dataCharSpacing;
		}

		/// <summary>The layout.</summary>
		private LayoutParameters Layout;

		/// <summary>Gets or sets the horizontal draw offset.</summary>
		public Double HorizontalOffset {
			get => Layout._hOffset;
			set {
				Layout._hOffset = value;
				ComputeLayoutParameters();
			}
		}

		/// <summary>Calculates the text size.</summary>
		/// <param name="myText">my text.</param>
		/// <param name="typeface">The typeface.</param>
		/// <param name="myFontSize">Size of my font.</param>
		/// <returns>The calculated text size.</returns>
		public static Size CalculateTextSize(String myText, Typeface typeface, Double myFontSize) {
			TextShaper ts = TextShaper.Current;
			ShapedBuffer shaped = ts.ShapeText(myText, new TextShaperOptions(typeface.GlyphTypeface, myFontSize));
			ShapedTextRun run = new(shaped, new GenericTextRunProperties(typeface, myFontSize));
			return run.Size;
		}

		/// <summary>Calculates the layout parameters.</summary>
		private void ComputeLayoutParameters() {
			// Reset the data buffer
			buffer = null;
			Layout = new();
			if (Bounds.Height == 0) {
				return;
			}
			Layout.strWidthPow2 = new Double[8];

			// Compute the string widths and heights
			// Measure a single character
			FormattedText formatted = new("0", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, font, fontSize, Brushes.Black);
			Layout.charSize = new(formatted.Width, formatted.Height);
			Layout.strWidthPow2[0] = Layout.charSize.Width;
			// Measure powers of two string lengths
			Layout.charSize = CalculateTextSize("0", font, fontSize);
			Layout.strWidthPow2[0] = Layout.charSize.Width;
			for (Int32 power = 1; power < Layout.strWidthPow2.Length; power++) {
				Layout.strWidthPow2[power] = CalculateTextSize(new String('0', 1 << power), font, fontSize).Width;
			}
			Layout.halfCharWidth = Layout.strWidthPow2[0] / 2;
			Layout.bytesPerRow = (UInt32)(1 << ((Int32)rowLengthSetting + 1));
			Layout.bytesPerCell = (UInt32)(1 << (Int32)dataSizeSetting);
			Layout.bytesPerAddress = (UInt32)(1 << (Int32)dataAlignmentSetting);
			currentByteAddress -= currentByteAddress % Layout.bytesPerAddress;
			Layout.addressesPerRow = Layout.bytesPerRow / Layout.bytesPerAddress;
			if (Layout.bytesPerCell < Layout.bytesPerAddress) {
				Layout.bytesPerCell = Layout.bytesPerAddress;
			}
			if (Layout.bytesPerRow < Layout.bytesPerCell) {
				Layout.bytesPerRow = Layout.bytesPerCell;
			}
			Layout.showDataChars = (DataSizeSetting == DataSizeSettings.DataSizeByte) || (DataSizeSetting == DataSizeSettings.DataSizeHWord);
			// We need to account for the fact unicode character widths may be a bit wider (not fixed width)
			Layout.dataCharPadding = (DataSizeSetting == DataSizeSettings.DataSizeHWord) ? Layout.strWidthPow2[0] : 0;
			Layout.columnCount = Layout.bytesPerRow / Layout.bytesPerCell;
			if (Layout.columnCount == 0) {
				// Happens under certain conditions
				return;
			}
			Layout.tDataDivY = Layout.charSize.Height + 1;
			Layout.numColHeaderChars = (UInt32)((Layout.columnCount >= 0x10) ? 2 : 1);
			Layout.rowCount = (UInt32)((Bounds.Height - Layout.tDataDivY - Layout.halfCharWidth) / Layout.charSize.Height);
			if (Layout.rowCount <= 0) {
				// Happens under certain conditions
				return;
			}
			Layout.lDataDivX = Layout.strWidthPow2[0] + Layout.strWidthPow2[(Int32)addressSizeSetting + 1] + 1;
			Layout.dataColX = new Double[Layout.columnCount];
			Layout.dataIndexHeaderX = new Double[Layout.columnCount];
			Layout.dataCharX = new Double[Layout.columnCount];
			Layout.dataIndexHeaderPoint = new Point[Layout.columnCount];
			Layout.dataFieldColumns = new Rect[Layout.columnCount];
			Layout.rDataDivX = Layout.lDataDivX + Layout.halfCharWidth + (Layout.columnCount * (Layout.strWidthPow2[(Int32)dataSizeSetting + 1] + Layout.halfCharWidth));
			Layout.dataCharFieldWidth = (Layout.columnCount * (Layout.strWidthPow2[0] + Layout.dataCharPadding)) + (2 * Layout.halfCharWidth);
			Layout.drawAreaWidth = Layout.showDataChars ? (Layout.rDataDivX + Layout.dataCharFieldWidth) : Layout.rDataDivX;
			Layout.dataOffset = new(Layout.lDataDivX + Layout.halfCharWidth, Layout.tDataDivY + Layout.halfCharWidth);
			Layout.dataSpacing = new(Layout.strWidthPow2[(Int32)dataSizeSetting + 1] + Layout.halfCharWidth, Layout.charSize.Height);
			Layout.dataCharOffset = new(Layout.rDataDivX + Layout.halfCharWidth, Layout.tDataDivY + Layout.halfCharWidth);
			Layout.dataCharSpacing = new(Layout.strWidthPow2[0] + Layout.dataCharPadding, Layout.charSize.Height);
			for (UInt32 col = 0; col < Layout.columnCount; col++) {
				Layout.dataColX[col] = Layout.lDataDivX + Layout.halfCharWidth + (col * (Layout.strWidthPow2[(Int32)dataSizeSetting + 1] + Layout.halfCharWidth)) - Layout._hOffset;
				Layout.dataIndexHeaderX[col] = Layout.dataColX[col] + ((Layout.strWidthPow2[(Int32)dataSizeSetting + 1] - Layout.strWidthPow2[Layout.numColHeaderChars - 1]) / 2);
				Layout.dataIndexHeaderPoint[col] = new Point(Layout.dataIndexHeaderX[col], 1);
				Layout.dataCharX[col] = Layout.rDataDivX + Layout.halfCharWidth + (col * (Layout.strWidthPow2[0] + Layout.dataCharPadding)) - Layout._hOffset;
				Layout.dataFieldColumns[col] = new Rect(Layout.dataColX[col] - (Layout.halfCharWidth / 2), Layout.tDataDivY + 1, Layout.strWidthPow2[(Int32)dataSizeSetting + 1] + Layout.halfCharWidth, Height - Layout.tDataDivY - 1);
			}
			Layout.cursorAddressField = new Rect(-Layout._hOffset, 1, Layout.lDataDivX - 1, Layout.tDataDivY - 1);
			Layout.addressField = new Rect(-Layout._hOffset, Layout.tDataDivY + 1, Layout.lDataDivX - 1, Bounds.Height - Layout.tDataDivY - 1);
			Layout.dataCharField = new Rect(Layout.rDataDivX + 1 - Layout._hOffset, Layout.tDataDivY + 1, Layout.dataCharFieldWidth - 1, Bounds.Height - Layout.tDataDivY - 1);
			Layout.dataIndexField = new Rect(Layout.lDataDivX + 1 - Layout._hOffset, 1, Layout.rDataDivX - Layout.lDataDivX - 1, Layout.tDataDivY - 2);
			Layout.dataRowY = new Double[Layout.rowCount];
			Layout.addressHeaderPoint = new Point[Layout.rowCount];
			Layout.cursorAddressPosition = new(Layout.halfCharWidth - Layout._hOffset, 1);
			Layout.dataPosition = new Point[Layout.columnCount, Layout.rowCount];
			Layout.dataCharPosition = new Point[Layout.columnCount, Layout.rowCount];
			Layout.dataSelectRects = new Rect[Layout.columnCount, Layout.rowCount];
			Layout.dataCursorRects = new Rect[Layout.columnCount, Layout.rowCount];
			Layout.dataCharSelectRects = new Rect[Layout.columnCount, Layout.rowCount];
			Layout.dataCharCursorRects = new Rect[Layout.columnCount, Layout.rowCount];
			for (UInt32 row = 0; row < Layout.rowCount; row++) {
				Layout.dataRowY[row] = Layout.tDataDivY + Layout.halfCharWidth + (row * Layout.charSize.Height);
				Layout.addressHeaderPoint[row] = new Point(Layout.halfCharWidth - Layout._hOffset, Layout.dataRowY[row]);
				for (UInt32 col = 0; col < Layout.columnCount; col++) {
					Layout.dataPosition[col, row] = new Point(Layout.dataColX[col], Layout.dataRowY[row]);
					Layout.dataCharPosition[col, row] = new Point(Layout.dataCharX[col], Layout.dataRowY[row]);
					Layout.dataSelectRects[col, row] = new Rect(Layout.dataColX[col] - (Layout.halfCharWidth / 2), Layout.dataRowY[row], Layout.strWidthPow2[(Int32)dataSizeSetting + 1] + Layout.halfCharWidth - 1, Layout.charSize.Height - 1);
					Layout.dataCursorRects[col, row] = new Rect(Layout.dataSelectRects[col, row].X + 2, Layout.dataSelectRects[col, row].Y + 2, Layout.dataSelectRects[col, row].Width - 4, Layout.dataSelectRects[col, row].Height - 4);
					Layout.dataCharSelectRects[col, row] = new Rect(Layout.dataCharX[col], Layout.dataRowY[row], Layout.strWidthPow2[0] + Layout.dataCharPadding - 1, Layout.charSize.Height - 1);
					Layout.dataCharCursorRects[col, row] = new Rect(Layout.dataCharSelectRects[col, row].X + 2, Layout.dataCharSelectRects[col, row].Y + 2, Layout.dataCharSelectRects[col, row].Width - 4, Layout.dataCharSelectRects[col, row].Height - 4);
				}
			}
		}
	}
}
