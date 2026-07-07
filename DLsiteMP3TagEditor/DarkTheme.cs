using System.Runtime.InteropServices;

namespace DLsiteMP3TagEditor
{
	/// <summary>
	/// ダークモードの適用・解除を行うテーマエンジン。
	/// タイトルバー（DWM）・コンテキストメニュー（uxtheme）・スクロールバー（DarkMode_Explorer）まで含めて切り替える。
	/// </summary>
	internal static class DarkTheme
	{
		// ---- ダークテーマ配色 ----
		public static readonly Color ColorBack = Color.FromArgb(30, 30, 30);
		public static readonly Color ColorInput = Color.FromArgb(45, 45, 48);
		public static readonly Color ColorButton = Color.FromArgb(51, 51, 55);
		public static readonly Color ColorText = Color.FromArgb(230, 230, 230);
		public static readonly Color ColorBorder = Color.FromArgb(85, 85, 90);
		public static readonly Color ColorMenuBack = Color.FromArgb(37, 37, 38);
		public static readonly Color ColorMenuHover = Color.FromArgb(62, 62, 66);

		/// <summary>
		/// 現在ダークモードが適用されているかどうか
		/// </summary>
		public static bool IsDark { get; private set; }

		/// <summary>
		/// フォーム全体にテーマを適用します。
		/// </summary>
		/// <param name="form">対象フォーム</param>
		/// <param name="dark">trueでダークモード、falseでライトモード</param>
		public static void Apply(Form form, bool dark)
		{
			IsDark = dark;

			// コンテキストメニュー等のOS描画メニューをダーク化する（Win10 1903+の定番手法）
			try
			{
				SetPreferredAppMode(dark ? 2 : 0); // 2 = ForceDark / 0 = Default
				FlushMenuThemes();
			}
			catch
			{
				// undocumented APIのため、存在しない環境では何もしない
			}

			// タイトルバー
			ApplyTitleBar(form, dark);

			// メニュー・ステータスバー（全ToolStrip共通）
			ToolStripManager.Renderer = dark
				? new ToolStripProfessionalRenderer(new DarkColorTable()) { RoundedEdges = false }
				: new ToolStripProfessionalRenderer();

			// フォーム本体
			form.BackColor = dark ? ColorBack : SystemColors.Control;
			form.ForeColor = dark ? ColorText : SystemColors.ControlText;

			// 全コントロールへ再帰適用
			foreach (Control child in form.Controls)
			{
				ApplyControlTree(child, dark);
			}

			form.Invalidate(true);
		}

		/// <summary>
		/// Windows 10/11 のタイトルバーをテーマに合わせて切り替えます。
		/// </summary>
		/// <param name="form">対象フォーム</param>
		/// <param name="dark">trueでダーク</param>
		public static void ApplyTitleBar(Form form, bool dark)
		{
			try
			{
				int enabled = dark ? 1 : 0;
				// 20 = DWMWA_USE_IMMERSIVE_DARK_MODE（Win10 20H1以降）。失敗したら旧ビルド用の19を試す
				if (DwmSetWindowAttribute(form.Handle, 20, ref enabled, sizeof(int)) != 0)
				{
					DwmSetWindowAttribute(form.Handle, 19, ref enabled, sizeof(int));
				}
				// タイトルバーの再描画を促す
				form.Refresh();
			}
			catch
			{
				// 非対応環境では何もしない
			}
		}

		/// <summary>
		/// コントロールツリーへテーマを再帰適用します。
		/// </summary>
		/// <param name="control">対象コントロール</param>
		/// <param name="dark">trueでダーク</param>
		private static void ApplyControlTree(Control control, bool dark)
		{
			switch (control)
			{
				case Button button:
					if (dark)
					{
						button.FlatStyle = FlatStyle.Flat;
						button.FlatAppearance.BorderColor = ColorBorder;
						button.BackColor = ColorButton;
						button.ForeColor = ColorText;
					}
					else
					{
						button.FlatStyle = FlatStyle.Standard;
						button.BackColor = SystemColors.Control;
						button.ForeColor = SystemColors.ControlText;
						button.UseVisualStyleBackColor = true;
					}
					break;
				case TextBox textBox:
					textBox.BackColor = dark ? ColorInput : SystemColors.Window;
					textBox.ForeColor = dark ? ColorText : SystemColors.WindowText;
					SetScrollBarTheme(textBox, dark);
					break;
				case RichTextBox richText:
					richText.BackColor = dark ? ColorInput : SystemColors.Window;
					richText.ForeColor = dark ? ColorText : SystemColors.WindowText;
					SetScrollBarTheme(richText, dark);
					break;
				case NumericUpDown numeric:
					numeric.BackColor = dark ? ColorInput : SystemColors.Window;
					numeric.ForeColor = dark ? ColorText : SystemColors.WindowText;
					break;
				case CheckedListBox checkedList:
					checkedList.BackColor = dark ? ColorInput : SystemColors.Window;
					checkedList.ForeColor = dark ? ColorText : SystemColors.WindowText;
					SetScrollBarTheme(checkedList, dark);
					break;
				case ListBox listBox:
					listBox.BackColor = dark ? ColorInput : SystemColors.Window;
					listBox.ForeColor = dark ? ColorText : SystemColors.WindowText;
					SetScrollBarTheme(listBox, dark);
					break;
				case DarkDateTimePicker darkPicker:
					// ダーク時は自前描画に切り替える（ドロップダウンのカレンダーはOS描画のため対象外）
					darkPicker.DarkMode = dark;
					darkPicker.CalendarMonthBackground = dark ? ColorInput : SystemColors.Window;
					darkPicker.CalendarForeColor = dark ? ColorText : SystemColors.WindowText;
					break;
				case DateTimePicker picker:
					// 標準のDateTimePickerはOS描画のため完全なダーク化は不可。カレンダー部分のみ配色を寄せる
					picker.CalendarMonthBackground = dark ? ColorInput : SystemColors.Window;
					picker.CalendarForeColor = dark ? ColorText : SystemColors.WindowText;
					SetControlTheme(picker, dark ? "DarkMode_CFD" : "Explorer");
					break;
				case GroupBox groupBox:
					groupBox.BackColor = dark ? ColorBack : SystemColors.Control;
					groupBox.ForeColor = dark ? ColorText : SystemColors.ControlText;
					// 既定の枠線描画はライト前提のため、ダーク時はオーナードローに切り替える
					groupBox.Paint -= GroupBoxDarkPaint;
					if (dark)
					{
						groupBox.Paint += GroupBoxDarkPaint;
					}
					groupBox.Invalidate();
					break;
				case PictureBox pictureBox:
					pictureBox.BackColor = dark ? ColorInput : SystemColors.Control;
					break;
				case MenuStrip menuStrip:
					menuStrip.BackColor = dark ? ColorMenuBack : SystemColors.Control;
					ApplyToolStripItems(menuStrip.Items, dark);
					break;
				case StatusStrip statusStrip:
					statusStrip.BackColor = dark ? ColorMenuBack : SystemColors.Control;
					ApplyToolStripItems(statusStrip.Items, dark);
					break;
				default:
					// Label / CheckBox / RadioButton 等は文字色のみ変更（背景は親を継承）
					control.ForeColor = dark ? ColorText : SystemColors.ControlText;
					break;
			}

			foreach (Control child in control.Controls)
			{
				ApplyControlTree(child, dark);
			}
		}

		/// <summary>
		/// メニュー項目（ドロップダウン含む）へ文字色を再帰適用します。
		/// </summary>
		/// <param name="items">対象アイテムコレクション</param>
		/// <param name="dark">trueでダーク</param>
		private static void ApplyToolStripItems(ToolStripItemCollection items, bool dark)
		{
			foreach (ToolStripItem item in items)
			{
				item.ForeColor = dark ? ColorText : SystemColors.ControlText;
				if (item is ToolStripMenuItem menuItem)
				{
					ApplyToolStripItems(menuItem.DropDownItems, dark);
				}
			}
		}

		/// <summary>
		/// スクロールバーをテーマに合わせて切り替えます（Explorerのダークテーマを流用）。
		/// </summary>
		/// <param name="control">対象コントロール</param>
		/// <param name="dark">trueでダーク</param>
		private static void SetScrollBarTheme(Control control, bool dark)
		{
			SetControlTheme(control, dark ? "DarkMode_Explorer" : "Explorer");
		}

		/// <summary>
		/// コントロールへウィンドウテーマを適用します。
		/// </summary>
		/// <param name="control">対象コントロール</param>
		/// <param name="theme">テーマ名</param>
		private static void SetControlTheme(Control control, string theme)
		{
			try
			{
				if (control.IsHandleCreated || control.Handle != IntPtr.Zero)
				{
					SetWindowTheme(control.Handle, theme, null);
				}
			}
			catch
			{
				// 非対応環境では何もしない
			}
		}

		/// <summary>
		/// ダーク時のGroupBox描画（既定の明色枠線を塗り替える）
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private static void GroupBoxDarkPaint(object sender, PaintEventArgs e)
		{
			GroupBox groupBox = (GroupBox)sender;
			e.Graphics.Clear(ColorBack);

			Size textSize = TextRenderer.MeasureText(groupBox.Text, groupBox.Font);
			int textTop = textSize.Height / 2;
			Rectangle border = new Rectangle(0, textTop, groupBox.Width - 1, groupBox.Height - textTop - 1);

			using (Pen pen = new Pen(ColorBorder))
			{
				// 枠線（タイトル文字の部分は空ける）
				e.Graphics.DrawLine(pen, border.Left, border.Top, 6, border.Top);
				e.Graphics.DrawLine(pen, 8 + textSize.Width, border.Top, border.Right, border.Top);
				e.Graphics.DrawLine(pen, border.Left, border.Top, border.Left, border.Bottom);
				e.Graphics.DrawLine(pen, border.Right, border.Top, border.Right, border.Bottom);
				e.Graphics.DrawLine(pen, border.Left, border.Bottom, border.Right, border.Bottom);
			}
			TextRenderer.DrawText(e.Graphics, groupBox.Text, groupBox.Font, new Point(8, 0), ColorText);
		}

		/// <summary>
		/// メニュー・ステータスバー用のダーク配色テーブル
		/// </summary>
		private class DarkColorTable : ProfessionalColorTable
		{
			public override Color MenuStripGradientBegin => ColorMenuBack;
			public override Color MenuStripGradientEnd => ColorMenuBack;
			public override Color MenuItemSelected => ColorMenuHover;
			public override Color MenuItemSelectedGradientBegin => ColorMenuHover;
			public override Color MenuItemSelectedGradientEnd => ColorMenuHover;
			public override Color MenuItemPressedGradientBegin => ColorMenuBack;
			public override Color MenuItemPressedGradientEnd => ColorMenuBack;
			public override Color MenuItemBorder => ColorBorder;
			public override Color MenuBorder => ColorBorder;
			public override Color ToolStripDropDownBackground => ColorMenuBack;
			public override Color ImageMarginGradientBegin => ColorMenuBack;
			public override Color ImageMarginGradientMiddle => ColorMenuBack;
			public override Color ImageMarginGradientEnd => ColorMenuBack;
			public override Color SeparatorDark => ColorBorder;
			public override Color SeparatorLight => ColorBorder;
			public override Color StatusStripGradientBegin => ColorMenuBack;
			public override Color StatusStripGradientEnd => ColorMenuBack;
		}

		// ---- Win32 ----

		[DllImport("dwmapi.dll", PreserveSig = true)]
		private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

		[DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
		private static extern int SetWindowTheme(IntPtr hWnd, string? pszSubAppName, string? pszSubIdList);

		[DllImport("uxtheme.dll", EntryPoint = "#135", SetLastError = true)]
		private static extern int SetPreferredAppMode(int preferredAppMode);

		[DllImport("uxtheme.dll", EntryPoint = "#136", SetLastError = true)]
		private static extern void FlushMenuThemes();
	}
}
