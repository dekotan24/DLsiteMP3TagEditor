using DLsiteInfoGetter;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using TagLib;

namespace DLsiteMP3TagEditor
{
	public partial class Main : Form
	{
		private static string AppName = "DLsite MP3 Tag Editor";
		private bool mp3InfoGetFlg = true;
		private bool updating = false;
		private enum MessageType
		{
			Info,
			Warn,
			Error
		};

		public Main()
		{
			InitializeComponent();
		}

		private void Main_Load(object sender, EventArgs e)
		{
			searchText.Focus();
		}

		/// <summary>
		/// 取得したデータをmp3データフィールドに適用します。
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void applyButton_Click(object sender, EventArgs e)
		{
			StringBuilder sbCVText = new StringBuilder();
			// チェックされたアイテムを処理する
			foreach (object item in cvCheckList.CheckedItems)
			{
				// チェックされたアイテムのtextを出力する
				if (sbCVText.Length != 0)
				{
					sbCVText.Append(", ");
				}
				sbCVText.Append(item.ToString());
			}
			artistText.Text = sbCVText.ToString();
			artistText.BackColor = SystemColors.Window;

			// datetimepickerのValueプロパティからDateTime型の値を取得
			DateTime date = sellTimePicker.Value;
			// ToStringメソッドで"yyyy"形式の文字列に変換
			string year = date.ToString("yyyy");
			// textboxにyearを代入
			yearText.Text = year;
			yearText.BackColor = SystemColors.Window;

			genreText.Text = genreRadio.Text;
			genreText.BackColor = SystemColors.Window;

			albumText.Text = productText.Text;
			albumText.BackColor = SystemColors.Window;

			albumArtistText.Text = circleText.Text;
			albumArtistText.BackColor = SystemColors.Window;

			mp3Picture.BackgroundImage = dlsitePicture.BackgroundImage;
		}

		private async void writeButton_Click(object sender, EventArgs e)
		{
			logText.Clear();
			if (mp3ListBox.SelectedItems.Count == 0)
			{
				setLogMessage("更新対象のトラックが選択されていません。", MessageType.Error);
				MessageBox.Show("更新対象のトラックが選択されていません。", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			try
			{
				string writeButtonText = writeButton.Text;
				updating = true;
				writeButton.Enabled = false;

				// バックアップを取る
				await BackupFilesAsync(mp3PathListBox.SelectedItems);

				// mp3PathListBox.SelectedItems を IEnumerable<object> に変換
				IEnumerable<object> selectedItems = mp3PathListBox.SelectedItems.Cast<object>();

				// タグ更新実行
				UpdateTagInformation(selectedItems);

				// タグ情報の更新が正常に完了した場合
				writeButton.Text = writeButtonText;
				setLogMessage("タグ情報の更新が完了しました。", MessageType.Info);

				// MP3 ファイルの再読み込み
				applyMP3FilesList(directoryText.Text);
				AllMP3TracksSelect();

				// タグ情報を表示
				GetListBoxSelectedInfo();

				updating = false;
			}
			catch (Exception ex)
			{
				// エラーが発生した場合、全ての更新処理を中断し、書き込み前の状態に戻す
				setLogMessage(ex.Message, MessageType.Error);
				MessageBox.Show("エラーが発生しました。更新処理が中断されました。", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);

				// バックアップから復元
				await RestoreFilesFromBackupAsync(mp3PathListBox.SelectedItems);

				updating = false;
			}
			finally
			{
				writeButton.Enabled = true;
			}
		}

		private async Task BackupFilesAsync(System.Windows.Forms.ListBox.SelectedObjectCollection files)
		{
			foreach (var file in files)
			{
				string filePath = file.ToString();
				string backupPath = filePath + ".bak";
				System.IO.File.Copy(filePath, backupPath, true);
			}
		}

		private async Task RestoreFilesFromBackupAsync(System.Windows.Forms.ListBox.SelectedObjectCollection files)
		{
			foreach (var file in files)
			{
				string filePath = file.ToString();
				string backupPath = filePath + ".bak";
				if (System.IO.File.Exists(backupPath))
				{
					System.IO.File.Copy(backupPath, filePath, true);
				}
			}
		}

		/// <summary>
		/// タグの更新
		/// </summary>
		/// <param name="selectedItems"></param>
		private async void UpdateTagInformation(IEnumerable<object> selectedItems)
		{
			if (selectedItems == null || !selectedItems.Any())
			{
				MessageBox.Show("トラックが選択されていません！", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			int totalFiles = selectedItems.Count();
			int completedFiles = 0;

			foreach (string filePath in selectedItems)
			{
				try
				{
					var mp3 = TagLib.File.Create(filePath);

					// タイトルの更新
					if (totalFiles == 1 || (totalFiles > 1 && !string.Equals(titleText.Text, "<複数の値>")))
					{
						if (mp3.Tag.Title != titleText.Text)
							mp3.Tag.Title = titleText.Text;
					}

					// 他のタグ情報の更新
					mp3.Tag.Album = albumText.Text;
					mp3.Tag.AlbumArtists = new string[] { albumArtistText.Text };
					mp3.Tag.Performers = new string[] { artistText.Text };
					mp3.Tag.Genres = new string[] { genreText.Text };
					mp3.Tag.Year = Convert.ToUInt32(yearText.Text);

					// 画像の更新
					if (mp3Picture.BackgroundImage != null)
					{
						var picture = new Picture();
						picture.Type = PictureType.FrontCover;
						using (var image = new MemoryStream())
						{
							mp3Picture.BackgroundImage.Save(image, System.Drawing.Imaging.ImageFormat.Jpeg);
							picture.Data = image.ToArray();
							mp3.Tag.Pictures = new IPicture[] { picture };
						}
					}


					mp3.Save();
					mp3.Dispose();

					completedFiles++;
					UpdateProgress(completedFiles, totalFiles);

				}
				catch (Exception ex)
				{
					// エラーが発生した場合、中断し、エラー内容を表示
					RevertChanges(selectedItems);
					setLogMessage($"{ex.Message} - {Path.GetFileName(filePath)}", MessageType.Error);
					MessageBox.Show($"タグの更新中にエラーが発生しました： {ex.Message}", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
			}

			// タグの書き込みが完了したことを通知
			MessageBox.Show("タグの更新が完了しました。", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void RevertChanges(IEnumerable<object> selectedItems)
		{
			// エラーが発生した場合、変更を元に戻す
			foreach (string filePath in selectedItems)
			{
				try
				{
					var mp3 = TagLib.File.Create(filePath + ".bak");
					System.IO.File.Copy(filePath + ".bak", filePath, true);
					System.IO.File.Delete(filePath + ".bak");
					mp3.Dispose();
				}
				catch (Exception)
				{
					// エラーが発生しても無視
				}
			}
		}

		private void UpdateProgress(int completedFiles, int totalFiles)
		{
			int progressPercentage = (int)((double)completedFiles / totalFiles * 100);
			writeButton.Text = string.Format("書込中({0}%)", progressPercentage);
		}


		private void directorySearchButton_Click(object sender, EventArgs e)
		{
			folderBrowserDialog1.Description = "アップロードするフォルダを選択";
			DialogResult dr = folderBrowserDialog1.ShowDialog();
			if (dr == DialogResult.Cancel)
			{
				return;
			}
			directoryText.Text = folderBrowserDialog1.SelectedPath;
			applyMP3FilesList(folderBrowserDialog1.SelectedPath);
			AllMP3TracksSelect();
		}

		/// <summary>
		/// すべてのトラックを選択します
		/// </summary>
		private void AllMP3TracksSelect()
		{
			if (mp3ListBox.Items.Count != 0)
			{
				// listboxの全てのアイテムを選択
				for (int i = 0; i < mp3ListBox.Items.Count; i++)
				{
					mp3ListBox.SetSelected(i, true);
				}
			}
		}

		private void mp3ListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!mp3InfoGetFlg)
			{
				return;
			}

			SyncListBoxes(mp3ListBox, mp3PathListBox);
			GetListBoxSelectedInfo();
		}

		private void mp3PathListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!mp3InfoGetFlg)
			{
				return;
			}

			SyncListBoxes(mp3PathListBox, mp3ListBox);
			GetListBoxSelectedInfo();
		}

		/// <summary>
		/// <paramref name="path"/>からmp3ファイルを検索し、<see cref="directoryText"/>に反映します。
		/// </summary>
		/// <param name="path"></param>
		private void applyMP3FilesList(string path)
		{
			try
			{
				// リストを初期化する
				mp3ListBox.Items.Clear();
				mp3PathListBox.Items.Clear();

				// フォルダ内のすべてのmp3ファイルを取得
				string[] files = System.IO.Directory.GetFiles(path, "*.mp3", SearchOption.AllDirectories);
				// 取得したファイルをループ
				foreach (string file in files)
				{
					// mp3ListBoxにファイル名だけを追加
					mp3ListBox.Items.Add(Path.GetFileName(file));
					// mp3PathListBoxにフルパスを追加
					mp3PathListBox.Items.Add(file);
				}
			}

			catch (Exception ex)
			{
				setLogMessage($"ファイルの読み込み中にエラーが発生しました: {ex.Message}", MessageType.Error);
				MessageBox.Show($"ファイルの読み込み中にエラーが発生しました: {ex.Message}", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void searchButton_Click(object sender, EventArgs e)
		{
			logText.Clear();
			if (searchText.Text.Trim().Length < 8)
			{
				setLogMessage("URLもしくはIDが一定桁数を満たしていません。", MessageType.Error);
				MessageBox.Show("URLもしくはIDが不正です。\n\n例：\nID形式：『RJ343328』\nURL形式：『https://www.dlsite.com/home/work/=/product_id/RJ343328.html』", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				searchText.Focus();
				return;
			}

			DLsiteItemClear();

			try
			{
				DLsiteInfo result = DLsiteInfo.GetInfo(searchText.Text.Trim());

				productText.Text = result.Title;
				circleText.Text = result.Circle;
				sellTimePicker.Value = result.SellDate;
				foreach (string va in result.VoiceActor)
				{
					cvCheckList.Items.Add(va, true);
				}
				WebClient wc = new WebClient();
				byte[] bytes = wc.DownloadData(result.ImageUrl);
				MemoryStream ms = new MemoryStream(bytes);
				Image img = Image.FromStream(ms);
				dlsitePicture.BackgroundImage = img;
			}
			catch (Exception ex)
			{
				setLogMessage(ex.Message, MessageType.Error);
				MessageBox.Show($"予期せぬエラーが発生しました。\n\n{ex.Message}", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			return;
		}

		private void setLogMessage(string message, MessageType msgType)
		{
			StringBuilder sb = new StringBuilder();
			switch (msgType)
			{
				case MessageType.Info:
					sb.Append("[Info] ");
					break;
				case MessageType.Warn:
					sb.Append("[Warn] ");
					break;
				case MessageType.Error:
				default:
					sb.Append("[Error] ");
					break;
			}
			sb.Append(message).Append(" ");
			sb.Append("[").Append(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")).Append("]");
			sb.Append("\n");

			logText.Text = logText.Text.Insert(0, sb.ToString());
			return;
		}

		/// <summary>
		/// リスト選択値同期
		/// </summary>
		/// <param name="listBox1"></param>
		/// <param name="listBox2"></param>
		private void SyncListBoxes(ListBox listBox1, ListBox listBox2)
		{
			// listbox1の選択項目を取得
			var selectedItems1 = listBox1.SelectedItems;

			// listbox2のSelectedIndexChangedイベントを一時的に無効化
			if (listBox1 == mp3ListBox)
			{
				listBox2.SelectedIndexChanged -= mp3PathListBox_SelectedIndexChanged;
			}
			else
			{
				listBox2.SelectedIndexChanged -= mp3ListBox_SelectedIndexChanged;
			}

			// listbox2の選択項目をクリア
			listBox2.ClearSelected();

			// listbox1とlistbox2のインデックスが一致する項目を選択
			foreach (var item in selectedItems1)
			{
				int index = listBox1.Items.IndexOf(item);
				if (index >= 0 && index < listBox2.Items.Count)
				{
					listBox2.SetSelected(index, true);
				}
			}

			// listbox2のSelectedIndexChangedイベントを有効化
			if (listBox1 == mp3ListBox)
			{
				listBox2.SelectedIndexChanged += mp3PathListBox_SelectedIndexChanged;
			}
			else
			{
				listBox2.SelectedIndexChanged += mp3ListBox_SelectedIndexChanged;
			}
		}

		private void GetListBoxSelectedInfo()
		{
			MP3ItemClear();
			logText.Clear();
			if (mp3ListBox.SelectedItems.Count != 0)
			{
				// 選択されたファイルのタグ情報をリストに格納する
				List<string> titles = new List<string>();
				List<string> artists = new List<string>();
				List<uint> years = new List<uint>();
				List<string> genres = new List<string>();
				List<string> albums = new List<string>();
				List<string> albumArtists = new List<string>();
				List<byte[]> albumArts = new List<byte[]>();
				foreach (var obj in mp3PathListBox.SelectedItems)
				{
					string filePath = obj.ToString();
					if (System.IO.File.Exists(filePath))
					{
						// TagLib.Fileクラスのインスタンスを作成
						TagLib.File mp3 = TagLib.File.Create(filePath);
						// タグ情報を取得
						string title = mp3.Tag.Title;
						string artist = string.Join(", ", mp3.Tag.Performers); // 配列を文字列に連結
						uint year = mp3.Tag.Year;
						string genre = string.Join(", ", mp3.Tag.Genres); // 配列を文字列に連結
						string album = mp3.Tag.Album;
						string albumArtist = string.Join(", ", mp3.Tag.AlbumArtists); // 配列を文字列に連結
																					  // アルバムアートを取得（画像はbyte[]型で格納されている）
						byte[] albumArt = null;
						if (mp3.Tag.Pictures.Length > 0) // 画像が存在するかチェック
						{
							albumArt = mp3.Tag.Pictures[0].Data.Data;
						}

						// リストに追加
						titles.Add(title);
						artists.Add(artist);
						years.Add(year);
						genres.Add(genre);
						albums.Add(album);
						albumArtists.Add(albumArtist);
						albumArts.Add(albumArt);
						mp3.Dispose();
					}
					else
					{
						setLogMessage($"ファイルが存在しません: {Path.GetFileName(filePath)}", MessageType.Error);
						MessageBox.Show($"ファイルが存在しません: {Path.GetFileName(filePath)}", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
						// エラー時、リスト再読み込みを実施
						applyMP3FilesList(folderBrowserDialog1.SelectedPath);
						AllMP3TracksSelect();
						return;
					}
				}

				// 各タグ情報がすべて同じかどうか判定する
				bool sameTitle = true; // 初期値はtrueとする
				for (int i = 1; i < titles.Count; i++) // 2番目の要素から順に比較する
				{
					if (titles[i] != titles[0]) // 最初の要素と異なる場合
					{
						sameTitle = false; // falseにする
						break; // ループを抜ける
					}
				}
				bool sameArtist = true; // 初期値はtrueとする
				for (int i = 1; i < artists.Count; i++) // 2番目の要素から順に比較する
				{
					if (artists[i] != artists[0]) // 最初の要素と異なる場合
					{
						sameArtist = false; // falseにする
						break; // ループを抜ける
					}
				}
				bool sameYear = true; // 初期値はtrueとする
				for (int i = 1; i < years.Count; i++) // 2番目の要素から順に比較する
				{
					if (years[i] != years[0]) // 最初の要素と異なる場合
					{
						sameYear = false; // falseにする
						break; // ループを抜ける
					}
				}
				bool sameGenre = true; // 初期値はtrueとする
				for (int i = 1; i < genres.Count; i++) // 2番目の要素から順に比較する
				{
					if (genres[i] != genres[0]) // 最初の要素と異なる場合
					{
						sameGenre = false; // falseにする
						break; // ループを抜ける
					}
				}
				bool sameAlbum = true; // 初期値はtrueとする
				for (int i = 1; i < albums.Count; i++) // 2番目の要素から順に比較する
				{
					if (albums[i] != albums[0]) // 最初の要素と異なる場合
					{
						sameAlbum = false; // falseにする
						break; // ループを抜ける
					}
				}
				bool sameAlbumArtist = true; // 初期値はtrueとする
				for (int i = 1; i < albumArtists.Count; i++) // 2番目の要素から順に比較する
				{
					if (albumArtists[i] != albumArtists[0]) // 最初の要素と異なる場合
					{
						sameAlbumArtist = false; // falseにする
						break; // ループを抜ける
					}
				}
				// アルバムアートがすべて同じかどうか判定する
				bool sameAlbumArt = true;
				byte[] firstAlbumArtHash = ComputeSHA256(albumArts[0]);
				for (int i = 1; i < albumArts.Count; i++)
				{
					byte[] currentAlbumArtHash = ComputeSHA256(albumArts[i]);
					if (!firstAlbumArtHash.SequenceEqual(currentAlbumArtHash))
					{
						sameAlbumArt = false;
						break;
					}
				}
				// テキストボックスに表示する
				if (sameTitle) // タイトルがすべて同じ場合
				{
					titleText.Text = titles[0]; // 最初の要素を表示する
				}
				else // タイトルが異なる場合
				{
					titleText.Text = "<複数の値>"; // <複数の値>と表示する
				}
				if (sameArtist) // アーティストがすべて同じ場合
				{
					artistText.Text = artists[0]; // 最初の要素を表示する
				}
				else // アーティストが異なる場合
				{
					artistText.Text = artists[0]; // 最初の要素を表示する
					artistText.BackColor = Color.Red;
					setLogMessage("複数の異なるアーティストが設定されているため、代表して一番最初に選択しているトラックの情報を表示しています。このまま書き込むと表示されている値で上書きされます。", MessageType.Warn);
				}
				if (sameYear) // 年がすべて同じ場合
				{
					// 代表的な年の値を決定
					uint? representativeYear = years.FirstOrDefault(y => y >= 1990 && y <= 9999);
					if (!representativeYear.HasValue || representativeYear == 0)
					{
						// 1990 から 9999 の範囲内の年が見つからない場合、または0の場合、現在の年を取得
						representativeYear = (uint)DateTime.Now.Year;
						setLogMessage("選択したトラックの年が 1990 から 9999 の範囲外または0のため、現在の年を表示しています。", MessageType.Warn);
					}

					yearText.Value = (decimal)representativeYear;
				}
				else // 年が異なる場合
				{
					// 代表的な年の値を決定
					uint? representativeYear = years.FirstOrDefault(y => y >= 1990 && y <= 9999);
					if (!representativeYear.HasValue || representativeYear == 0)
					{
						// 1990 から 9999 の範囲内の年が見つからない場合、または0の場合、現在の年を取得
						representativeYear = (uint)DateTime.Now.Year;
						setLogMessage("選択したトラックの年が 1990 から 9999 の範囲外または0のため、現在の年を表示しています。", MessageType.Warn);
					}

					yearText.Value = (decimal)representativeYear;

					yearText.BackColor = Color.Red;
					setLogMessage("複数の異なる年が設定されているため、代表して一番最初に選択しているトラックの情報を表示しています。このまま書き込むと表示されている値で上書きされます。", MessageType.Warn);
				}
				if (sameGenre) // ジャンルがすべて同じ場合
				{
					genreText.Text = genres[0]; // 最初の要素を表示する
				}
				else // ジャンルが異なる場合
				{
					genreText.Text = genres[0]; // 最初の要素を表示する
					genreText.BackColor = Color.Red;
					setLogMessage("複数の異なるジャンルが設定されているため、代表して一番最初に選択しているトラックの情報を表示しています。このまま書き込むと表示されている値で上書きされます。", MessageType.Warn);
				}
				if (sameAlbum) // アルバムがすべて同じ場合
				{
					albumText.Text = albums[0]; // 最初の要素を表示する
				}
				else // アルバムが異なる場合
				{
					albumText.Text = albums[0]; // 最初の要素を表示する
					albumText.BackColor = Color.Red;
					setLogMessage("複数の異なるアルバムが設定されているため、代表して一番最初に選択しているトラックの情報を表示しています。このまま書き込むと表示されている値で上書きされます。", MessageType.Warn);

				}
				if (sameAlbumArtist) // アルバムアーティストがすべて同じ場合
				{
					albumArtistText.Text = albumArtists[0]; // 最初の要素を表示する
				}
				else // アルバムアーティストが異なる場合
				{
					albumArtistText.Text = albumArtists[0]; // 最初の要素を表示する
					albumArtistText.BackColor = Color.Red;
					setLogMessage("複数の異なるアルバムアーティストが設定されているため、代表して一番最初に選択しているトラックの情報を表示しています。このまま書き込むと表示されている値で上書きされます。", MessageType.Warn);
				}
				// ピクチャーボックスに表示する
				if (sameAlbumArt) // アルバムアートがすべて同じ場合
				{
					mp3Picture.BackgroundImage = Image.FromStream(new MemoryStream(albumArts[0])); // 最初の要素を表示する
				}
				else // アルバムアートが異なる場合
				{
					mp3Picture.BackgroundImage = null; // 画像を消す
					setLogMessage("複数の異なるアルバムアートが設定されているため、画像を表示しません。", MessageType.Warn);
				}
			}
		}

		private void DLsiteItemClear()
		{
			productText.Text = circleText.Text = string.Empty;
			sellTimePicker.Value = DateTime.Now;
			cvCheckList.Items.Clear();
			dlsitePicture.BackgroundImage = null;
			return;
		}

		private void MP3ItemClear()
		{
			titleText.Text = artistText.Text = genreText.Text = albumText.Text = albumArtistText.Text = string.Empty;
			yearText.Value = (decimal)Convert.ToInt16(sellTimePicker.Value.ToString("yyyy"));
			mp3Picture.BackgroundImage = null;
			titleText.BackColor = artistText.BackColor = yearText.BackColor = genreText.BackColor = albumText.BackColor = albumArtistText.BackColor = SystemColors.Window;
			return;
		}

		private static byte[] ComputeSHA256(byte[] data)
		{
			using (var sha256 = new SHA256Managed())
			{
				return sha256.ComputeHash(data);
			}
		}
	}
}