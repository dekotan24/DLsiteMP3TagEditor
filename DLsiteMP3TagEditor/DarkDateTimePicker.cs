namespace DLsiteMP3TagEditor
{
	/// <summary>
	/// ダークモード対応のDateTimePicker。
	/// 標準のDateTimePickerはOS描画のため配色変更できないので、ダーク時のみ自前描画に切り替える。
	/// ※ドロップダウンのカレンダー部分はOS描画のため対象外。
	/// </summary>
	public class DarkDateTimePicker : DateTimePicker
	{
		private bool darkMode = false;

		/// <summary>
		/// ダークモードの有効状態。trueで自前描画（ダーク配色）、falseでOS標準描画に戻る。
		/// </summary>
		public bool DarkMode
		{
			get
			{
				return darkMode;
			}
			set
			{
				darkMode = value;
				SetStyle(ControlStyles.UserPaint, value);
				Invalidate();
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			if (!darkMode)
			{
				base.OnPaint(e);
				return;
			}

			// 背景と枠線（フォーカス時は枠色を変えて視覚フィードバックする）
			e.Graphics.Clear(DarkTheme.ColorInput);
			using (Pen pen = new Pen(Focused ? Color.FromArgb(0, 120, 215) : DarkTheme.ColorBorder))
			{
				e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
			}

			// 日付テキスト（Textプロパティは書式適用済みの表示文字列を返す）
			Rectangle textRect = new Rectangle(4, 0, Width - 24, Height);
			TextRenderer.DrawText(e.Graphics, Text, Font, textRect, DarkTheme.ColorText, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);

			// ドロップダウン矢印（▼）
			int arrowX = Width - 14;
			int arrowY = Height / 2 - 2;
			using (SolidBrush brush = new SolidBrush(DarkTheme.ColorText))
			{
				Point[] arrow = new Point[]
				{
					new Point(arrowX - 4, arrowY),
					new Point(arrowX + 4, arrowY),
					new Point(arrowX, arrowY + 4)
				};
				e.Graphics.FillPolygon(brush, arrow);
			}
		}

		protected override void OnValueChanged(EventArgs eventargs)
		{
			base.OnValueChanged(eventargs);
			// 自前描画時は値変更で明示的に再描画する
			if (darkMode)
			{
				Invalidate();
			}
		}

		protected override void OnCloseUp(EventArgs eventargs)
		{
			base.OnCloseUp(eventargs);
			if (darkMode)
			{
				Invalidate();
			}
		}

		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
			if (darkMode)
			{
				Invalidate();
			}
		}

		protected override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);
			if (darkMode)
			{
				Invalidate();
			}
		}
	}
}
