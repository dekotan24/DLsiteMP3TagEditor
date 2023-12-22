using DLsiteInfoGetter;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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
			if (editFolderNameCheck.Checked)
			{
				folderNameText.Text = "[{ProductID}] [{ProductCircle}] {ProductName}";
				folderNameText.Enabled = true;
			}
			else
			{
				folderNameText.Enabled = false;
			}

			searchText.Focus();
		}

		/// <summary>
		/// �擾�����f�[�^��mp3�f�[�^�t�B�[���h�ɓK�p���܂��B
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void applyButton_Click(object sender, EventArgs e)
		{
			StringBuilder sbCVText = new StringBuilder();
			// �`�F�b�N���ꂽ�A�C�e������������
			foreach (object item in cvCheckList.CheckedItems)
			{
				// �`�F�b�N���ꂽ�A�C�e����text���o�͂���
				if (sbCVText.Length != 0)
				{
					sbCVText.Append(", ");
				}
				sbCVText.Append(item.ToString());
			}
			artistText.Text = sbCVText.ToString();
			artistText.BackColor = SystemColors.Window;

			// datetimepicker��Value�v���p�e�B����DateTime�^�̒l���擾
			DateTime date = sellTimePicker.Value;
			// ToString���\�b�h��"yyyy"�`���̕�����ɕϊ�
			string year = date.ToString("yyyy");
			// textbox��year����
			yearText.Text = year;
			yearText.BackColor = SystemColors.Window;

			genreText.Text = genreRadio.Text;
			genreText.BackColor = SystemColors.Window;

			albumText.Text = productText.Text;
			albumText.BackColor = SystemColors.Window;

			albumArtistText.Text = circleText.Text;
			albumArtistText.BackColor = SystemColors.Window;

			if (!keepOriginCheck.Checked)
			{
				mp3Picture.BackgroundImage = dlsitePicture.BackgroundImage;
			}
		}

		private async void writeButton_Click(object sender, EventArgs e)
		{
			logText.Clear();
			string newFolderPath = directoryText.Text;

			if (mp3ListBox.SelectedItems.Count == 0)
			{
				setLogMessage("�X�V�Ώۂ̃g���b�N���I������Ă��܂���B", MessageType.Error);
				MessageBox.Show("�X�V�Ώۂ̃g���b�N���I������Ă��܂���B", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// �t�H���_���X�V�����݂̂̏ꍇ�͂��ꂾ���s��
			if (editFolderNameOnlyCheck.Checked)
			{
				// �t�H���_���ύX
				if (editFolderNameCheck.Checked)
				{
					UpdateFolderName(false);
				}

				// MP3 �t�@�C���̍ēǂݍ���
				if (directoryText.Text.Length > 0)
				{
					applyMP3FilesList(directoryText.Text);
				}
				AllMP3TracksSelect();

				// �^�O����\��
				GetListBoxSelectedInfo();
				return;
			}

			if (albumText.Text.Length == 0)
			{
				DialogResult dr = MessageBox.Show("�A���o����񂪐ݒ肳��Ă��܂���B\n���s���܂����H", AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
				if (dr == DialogResult.No)
				{
					return;
				}
			}
			if (artistText.Text.Length == 0)
			{
				DialogResult dr = MessageBox.Show("�A�[�e�B�X�g��񂪐ݒ肳��Ă��܂���B\n���s���܂����H", AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
				if (dr == DialogResult.No)
				{
					return;
				}
			}

			if (editFolderNameCheck.Checked)
			{
				newFolderPath = directoryText.Text.Trim();
				// �x�[�X�p�X�`�F�b�N
				if (Directory.Exists(newFolderPath))
				{
					// �V�p�X�̍쐬
					newFolderPath = UpdateFolderName();
					if (Directory.Exists(newFolderPath))
					{
						setLogMessage($"�ύX��̃t�H���_�������ɑ��݂��܂��I - {newFolderPath}", MessageType.Error);
						MessageBox.Show($"�ύX��̃t�H���_�������ɑ��݂��܂��I\n{newFolderPath}", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}
				}
				else
				{
					setLogMessage($"�w�肵���p�X�̓t�H���_�ł͂Ȃ����A���݂��܂���B - {newFolderPath}", MessageType.Error);
					MessageBox.Show($"�w�肵���p�X�̓t�H���_�ł͂Ȃ����A���݂��܂���B\n{newFolderPath}", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
			}

			try
			{
				string writeButtonText = writeButton.Text;
				updating = true;
				writeButton.Enabled = false;

				// �o�b�N�A�b�v�����
				await BackupFilesAsync(mp3PathListBox.SelectedItems);

				// mp3PathListBox.SelectedItems �� IEnumerable<object> �ɕϊ�
				IEnumerable<object> selectedItems = mp3PathListBox.SelectedItems.Cast<object>();

				// �^�O�X�V���s
				UpdateTagInformation(selectedItems);

				// �^�O���̍X�V������Ɋ��������ꍇ�A�o�b�N�A�b�v���폜
				await DeteleBackupFiles(mp3PathListBox.SelectedItems);

				// �t�H���_���ύX
				if (editFolderNameCheck.Checked)
				{
					UpdateFolderName(false);
				}

				// �^�O�̏������݂������������Ƃ�ʒm
				MessageBox.Show("�^�O�̍X�V���������܂����B", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
				writeButton.Text = writeButtonText;
				setLogMessage("�^�O���̍X�V���������܂����B", MessageType.Info);

				// MP3 �t�@�C���̍ēǂݍ���
				if (directoryText.Text.Length > 0)
				{
					applyMP3FilesList(directoryText.Text);
				}
				AllMP3TracksSelect();

				// �^�O����\��
				GetListBoxSelectedInfo();

				updating = false;
			}
			catch (Exception ex)
			{
				// �G���[�����������ꍇ�A�S�Ă̍X�V�����𒆒f���A�������ݑO�̏�Ԃɖ߂�
				setLogMessage(ex.Message, MessageType.Error);
				MessageBox.Show("�G���[���������܂����B�X�V���������f����܂����B", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);

				// �o�b�N�A�b�v���畜��
				await RestoreFilesFromBackupAsync(mp3PathListBox.SelectedItems);

				updating = false;
			}
			finally
			{
				writeButton.Enabled = true;
			}
		}

		/// <summary>
		/// �t�@�C���̃o�b�N�A�b�v���쐬���܂�
		/// </summary>
		/// <param name="files"></param>
		/// <returns></returns>
		private async Task BackupFilesAsync(System.Windows.Forms.ListBox.SelectedObjectCollection files)
		{
			foreach (var file in files)
			{
				string filePath = file.ToString();
				string backupPath = filePath + ".bak";
				System.IO.File.Copy(filePath, backupPath, true);
			}
		}

		/// <summary>
		/// �t�@�C���̃o�b�N�A�b�v���폜���܂�
		/// </summary>
		/// <param name="files"></param>
		/// <returns></returns>
		private async Task DeteleBackupFiles(System.Windows.Forms.ListBox.SelectedObjectCollection files)
		{
			foreach (var file in files)
			{
				string filePath = file.ToString();
				string backupPath = filePath + ".bak";
				if (System.IO.File.Exists(backupPath))
				{
					System.IO.File.Delete(backupPath);
				}
				else
				{
					setLogMessage($"�o�b�N�A�b�v�t�@�C�������݂��܂���F{backupPath}", MessageType.Error);
				}
			}
		}

		/// <summary>
		/// �o�b�N�A�b�v�t�@�C�������ɕ������܂�
		/// </summary>
		/// <param name="files"></param>
		/// <returns></returns>
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
		/// �^�O�̍X�V
		/// </summary>
		/// <param name="selectedItems"></param>
		private async void UpdateTagInformation(IEnumerable<object> selectedItems)
		{
			if (selectedItems == null || !selectedItems.Any())
			{
				MessageBox.Show("�g���b�N���I������Ă��܂���I", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			int totalFiles = selectedItems.Count();
			int completedFiles = 0;

			foreach (string filePath in selectedItems)
			{
				try
				{
					var mp3 = TagLib.File.Create(filePath);

					// �^�C�g���̍X�V
					if (totalFiles == 1 || (totalFiles > 1 && !string.Equals(titleText.Text, "<�����̒l>")))
					{
						if (mp3.Tag.Title != titleText.Text)
						{
							mp3.Tag.Title = titleText.Text;
						}
					}

					// ���̃^�O���̍X�V
					mp3.Tag.Album = albumText.Text;
					mp3.Tag.AlbumArtists = new string[] { albumArtistText.Text };
					mp3.Tag.Performers = new string[] { artistText.Text };
					mp3.Tag.Genres = new string[] { genreText.Text };
					mp3.Tag.Year = Convert.ToUInt32(yearText.Text);

					if (!keepOriginCheck.Checked)
					{
						// �摜�̍X�V
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
					}

					mp3.Save();
					mp3.Dispose();

					completedFiles++;
					UpdateProgress(completedFiles, totalFiles);

				}
				catch (Exception ex)
				{
					// �G���[�����������ꍇ�A���f���A�G���[���e��\��
					RevertChanges(selectedItems);
					setLogMessage($"{ex.Message} - {Path.GetFileName(filePath)}", MessageType.Error);
					MessageBox.Show($"�^�O�̍X�V���ɃG���[���������܂����F {ex.Message}", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
			}
		}

		private string UpdateFolderName(bool genOnly = true, string baseFolderPath = "")
		{
			string resultPath = string.Empty;   // �����t���p�X
			string resultName = string.Empty;   // �����t�H���_��
			string baseFolderName = baseFolderPath.Length > 0 ? baseFolderPath : directoryText.Text.Trim();  // �x�[�X�t���p�X
			if (baseFolderName.Length == 0)
			{
				return resultName;
			}
			string replaceFolderName = folderNameText.Text.Trim();
			try
			{
				// �l����
				var checkedItems = cvCheckList.CheckedItems;
				string[] items = new string[checkedItems.Count];
				for (int i = 0; i < checkedItems.Count; i++)
				{
					items[i] = checkedItems[i].ToString();
				}
				string cvList = string.Join(",", items);

				// �t�H���_���ύX
				resultName = replaceFolderName.Replace("{ProductID}", productIDText.Text)
				.Replace("{ProductDate}", sellTimePicker.Value.ToString("yyMMdd"))
				.Replace("{ProductName}", albumText.Text)
				.Replace("{ProductCircle}", circleText.Text)
				.Replace("{ProductCVs}", cvList);
				resultName = Regex.Replace(resultName, "[\\\\/:*?\"\"<>|]", "_");

				DirectoryInfo di = new DirectoryInfo(baseFolderName);
				resultPath = di.Parent.FullName + "\\" + resultName;

				if (!genOnly)
				{
					Directory.Move(baseFolderName, resultPath);
					directoryText.Text = resultPath;
				}
			}
			catch (Exception ex)
			{
				setLogMessage($"{ex.Message}", MessageType.Error);
				MessageBox.Show($"�t�H���_���̍X�V���ɃG���[���������܂����F {ex.Message}", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return string.Empty;
			}
			return resultName;
		}

		private void RevertChanges(IEnumerable<object> selectedItems)
		{
			// �G���[�����������ꍇ�A�ύX�����ɖ߂�
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
					// �G���[���������Ă�����
				}
			}
		}

		private void UpdateProgress(int completedFiles, int totalFiles)
		{
			int progressPercentage = (int)((double)completedFiles / totalFiles * 100);
			writeButton.Text = string.Format("������({0}%)", progressPercentage);
		}


		private void directorySearchButton_Click(object sender, EventArgs e)
		{
			folderBrowserDialog1.Description = "�A�b�v���[�h����t�H���_��I��";
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
		/// ���ׂẴg���b�N��I�����܂�
		/// </summary>
		private void AllMP3TracksSelect()
		{
			if (mp3ListBox.Items.Count != 0)
			{
				// listbox�̑S�ẴA�C�e����I��
				for (int i = 0; i < mp3ListBox.Items.Count; i++)
				{
					try
					{
						mp3PathListBox.SetSelected(i, true);
					}
					catch (Exception)
					{
						// �G���[���������Ă�����
					}
				}
			}
			return;
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
		/// <paramref name="path"/>����mp3�t�@�C�����������A�g���b�N���X�g�ɔ��f���܂��B
		/// </summary>
		/// <param name="path">�����Ώۂ̃f�B���N�g��</param>
		/// <param name="listClear">true�̏ꍇ�A���X�g�̏��������s���܂�</param>
		private void applyMP3FilesList(string path, bool listClear = true)
		{
			try
			{
				if (listClear)
				{
					// ���X�g������������
					mp3ListBox.Items.Clear();
					mp3PathListBox.Items.Clear();
				}

				// �t�H���_���̂��ׂĂ�mp3�t�@�C�����擾
				string[] files = System.IO.Directory.GetFiles(path, "*.mp3", SearchOption.AllDirectories);
				// �擾�����t�@�C�������[�v
				foreach (string file in files)
				{
					// mp3ListBox�Ƀt�@�C����������ǉ�
					mp3ListBox.Items.Add(Path.GetFileName(file));
					// mp3PathListBox�Ƀt���p�X��ǉ�
					mp3PathListBox.Items.Add(file);
				}

				// �������s
				if (getProductIDFromPathCheck.Checked && Directory.Exists(path))
				{
					// ���K�\���̃p�^�[��
					string pattern = @"(RJ|VJ)\d{6,8}";

					// ���K�\���I�u�W�F�N�g�̍쐬
					Regex regex = new Regex(pattern);

					// �t�@�C���p�X�������̕����𔲂��o��
					Match match = regex.Match(path);

					// �����o��������������Ε\������
					if (match.Success)
					{
						searchText.Text = match.Value;
						if (autoSearchProductInfoFromPathCheck.Checked && searchText.Text.Length >= 6)
						{
							searchButton_Click(null, null);
						}
					}
				}
			}
			catch (Exception ex)
			{
				setLogMessage($"�t�@�C���̓ǂݍ��ݒ��ɃG���[���������܂���: {ex.Message}", MessageType.Error);
				MessageBox.Show($"�t�@�C���̓ǂݍ��ݒ��ɃG���[���������܂���: {ex.Message}", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			return;
		}

		private void searchButton_Click(object sender, EventArgs e)
		{
			logText.Clear();
			if (searchText.Text.Trim().Length < 8)
			{
				setLogMessage("URL��������ID����茅���𖞂����Ă��܂���B", MessageType.Error);
				MessageBox.Show("URL��������ID���s���ł��B\n\n��F\nID�`���F�wRJ343328�x\nURL�`���F�whttps://www.dlsite.com/home/work/=/product_id/RJ343328.html�x", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				searchText.Focus();
				return;
			}

			DLsiteItemClear();

			try
			{
				DLsiteInfo result = DLsiteInfo.GetInfo(searchText.Text.Trim());

				productIDText.Text = result.ProductId;
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
				MessageBox.Show($"�\�����ʃG���[���������܂����B\n\n{ex.Message}", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			searchText.Clear();
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
		/// ���X�g�I��l����
		/// </summary>
		/// <param name="listBox1"></param>
		/// <param name="listBox2"></param>
		private void SyncListBoxes(ListBox listBox1, ListBox listBox2)
		{
			// listbox1�̑I�����ڂ��擾
			var selectedItems1 = listBox1.SelectedItems;

			// listbox2��SelectedIndexChanged�C�x���g���ꎞ�I�ɖ�����
			if (listBox1 == mp3ListBox)
			{
				listBox2.SelectedIndexChanged -= mp3PathListBox_SelectedIndexChanged;
			}
			else
			{
				listBox2.SelectedIndexChanged -= mp3ListBox_SelectedIndexChanged;
			}

			// listbox2�̑I�����ڂ��N���A
			listBox2.ClearSelected();

			// listbox1��listbox2�̃C���f�b�N�X����v���鍀�ڂ�I��
			foreach (var item in selectedItems1)
			{
				int index = listBox1.Items.IndexOf(item);
				if (index >= 0 && index < listBox2.Items.Count)
				{
					listBox2.SetSelected(index, true);
				}
			}

			// listbox2��SelectedIndexChanged�C�x���g��L����
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
				// �I�����ꂽ�t�@�C���̃^�O�������X�g�Ɋi�[����
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
						// TagLib.File�N���X�̃C���X�^���X���쐬
						TagLib.File mp3 = TagLib.File.Create(filePath);
						// �^�O�����擾
						string title = mp3.Tag.Title;
						string artist = string.Join(", ", mp3.Tag.Performers); // �z��𕶎���ɘA��
						uint year = mp3.Tag.Year;
						string genre = string.Join(", ", mp3.Tag.Genres); // �z��𕶎���ɘA��
						string album = mp3.Tag.Album;
						string albumArtist = string.Join(", ", mp3.Tag.AlbumArtists); // �z��𕶎���ɘA��
																					  // �A���o���A�[�g���擾�i�摜��byte[]�^�Ŋi�[����Ă���j
						byte[] albumArt = null;
						if (mp3.Tag.Pictures.Length > 0) // �摜�����݂��邩�`�F�b�N
						{
							albumArt = mp3.Tag.Pictures[0].Data.Data;
						}

						// ���X�g�ɒǉ�
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
						setLogMessage($"�t�@�C�������݂��܂���: {Path.GetFileName(filePath)}", MessageType.Error);
						MessageBox.Show($"�t�@�C�������݂��܂���: {Path.GetFileName(filePath)}", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
						// �G���[���A���X�g�ēǂݍ��݂����{
						applyMP3FilesList(folderBrowserDialog1.SelectedPath);
						AllMP3TracksSelect();
						return;
					}
				}

				// �e�^�O��񂪂��ׂē������ǂ������肷��
				bool sameTitle = true; // �����l��true�Ƃ���
				for (int i = 1; i < titles.Count; i++) // 2�Ԗڂ̗v�f���珇�ɔ�r����
				{
					if (titles[i] != titles[0]) // �ŏ��̗v�f�ƈقȂ�ꍇ
					{
						sameTitle = false;
						break; // ���[�v�𔲂���
					}
				}
				bool sameArtist = true; // �����l��true�Ƃ���
				for (int i = 1; i < artists.Count; i++) // 2�Ԗڂ̗v�f���珇�ɔ�r����
				{
					if (artists[i] != artists[0]) // �ŏ��̗v�f�ƈقȂ�ꍇ
					{
						sameArtist = false;
						break; // ���[�v�𔲂���
					}
				}
				bool sameYear = true; // �����l��true�Ƃ���
				for (int i = 1; i < years.Count; i++) // 2�Ԗڂ̗v�f���珇�ɔ�r����
				{
					if (years[i] != years[0]) // �ŏ��̗v�f�ƈقȂ�ꍇ
					{
						sameYear = false;
						break; // ���[�v�𔲂���
					}
				}
				bool sameGenre = true; // �����l��true�Ƃ���
				for (int i = 1; i < genres.Count; i++) // 2�Ԗڂ̗v�f���珇�ɔ�r����
				{
					if (genres[i] != genres[0]) // �ŏ��̗v�f�ƈقȂ�ꍇ
					{
						sameGenre = false; // false�ɂ���
						break; // ���[�v�𔲂���
					}
				}
				bool sameAlbum = true; // �����l��true�Ƃ���
				for (int i = 1; i < albums.Count; i++) // 2�Ԗڂ̗v�f���珇�ɔ�r����
				{
					if (albums[i] != albums[0]) // �ŏ��̗v�f�ƈقȂ�ꍇ
					{
						sameAlbum = false;
						break; // ���[�v�𔲂���
					}
				}
				bool sameAlbumArtist = true; // �����l��true�Ƃ���
				for (int i = 1; i < albumArtists.Count; i++) // 2�Ԗڂ̗v�f���珇�ɔ�r����
				{
					if (albumArtists[i] != albumArtists[0]) // �ŏ��̗v�f�ƈقȂ�ꍇ
					{
						sameAlbumArtist = false;
						break; // ���[�v�𔲂���
					}
				}
				// �A���o���A�[�g�����ׂē������ǂ������肷��
				bool sameAlbumArt = true;
				byte[] firstAlbumArtHash = ComputeSHA256(albumArts[0]);
				for (int i = 1; i < albumArts.Count; i++)
				{
					byte[] currentAlbumArtHash = ComputeSHA256(albumArts[i]);
					// null�łȂ����Ƃ��m�F����
					if (firstAlbumArtHash == null || currentAlbumArtHash == null)
					{
						sameAlbumArt = false;
						break;
					}
					if (!firstAlbumArtHash.SequenceEqual(currentAlbumArtHash))
					{
						sameAlbumArt = false;
						break;
					}
				}

				// �e�L�X�g�{�b�N�X�ɕ\������
				if (sameTitle) // �^�C�g�������ׂē����ꍇ
				{
					titleText.Text = titles[0]; // �ŏ��̗v�f��\������
				}
				else // �^�C�g�����قȂ�ꍇ
				{
					titleText.Text = "<�����̒l>"; // <�����̒l>�ƕ\������
				}
				if (sameArtist) // �A�[�e�B�X�g�����ׂē����ꍇ
				{
					artistText.Text = artists[0]; // �ŏ��̗v�f��\������
				}
				else // �A�[�e�B�X�g���قȂ�ꍇ
				{
					artistText.Text = artists[0]; // �ŏ��̗v�f��\������
					artistText.BackColor = Color.Red;
					setLogMessage("�����̈قȂ�A�[�e�B�X�g���ݒ肳��Ă��邽�߁A��\���Ĉ�ԍŏ��ɑI�����Ă���g���b�N�̏���\�����Ă��܂��B���̂܂܏������ނƕ\������Ă���l�ŏ㏑������܂��B", MessageType.Warn);
				}
				if (sameYear) // �N�����ׂē����ꍇ
				{
					// ��\�I�ȔN�̒l������
					uint? representativeYear = years.FirstOrDefault(y => y >= 1990 && y <= 9999);
					if (!representativeYear.HasValue || representativeYear == 0)
					{
						// 1990 ���� 9999 �͈͓̔��̔N��������Ȃ��ꍇ�A�܂���0�̏ꍇ�A���݂̔N���擾
						representativeYear = (uint)DateTime.Now.Year;
						setLogMessage("�I�������g���b�N�̔N�� 1990 ���� 9999 �͈̔͊O�܂���0�̂��߁A���݂̔N��\�����Ă��܂��B", MessageType.Warn);
					}

					yearText.Value = (decimal)representativeYear;
				}
				else // �N���قȂ�ꍇ
				{
					// ��\�I�ȔN�̒l������
					uint? representativeYear = years.FirstOrDefault(y => y >= 1990 && y <= 9999);
					if (!representativeYear.HasValue || representativeYear == 0)
					{
						// 1990 ���� 9999 �͈͓̔��̔N��������Ȃ��ꍇ�A�܂���0�̏ꍇ�A���݂̔N���擾
						representativeYear = (uint)DateTime.Now.Year;
						setLogMessage("�I�������g���b�N�̔N�� 1990 ���� 9999 �͈̔͊O�܂���0�̂��߁A���݂̔N��\�����Ă��܂��B", MessageType.Warn);
					}

					yearText.Value = (decimal)representativeYear;

					yearText.BackColor = Color.Red;
					setLogMessage("�����̈قȂ�N���ݒ肳��Ă��邽�߁A��\���Ĉ�ԍŏ��ɑI�����Ă���g���b�N�̏���\�����Ă��܂��B���̂܂܏������ނƕ\������Ă���l�ŏ㏑������܂��B", MessageType.Warn);
				}
				if (sameGenre) // �W�����������ׂē����ꍇ
				{
					genreText.Text = genres[0]; // �ŏ��̗v�f��\������
				}
				else // �W���������قȂ�ꍇ
				{
					genreText.Text = genres[0]; // �ŏ��̗v�f��\������
					genreText.BackColor = Color.Red;
					setLogMessage("�����̈قȂ�W���������ݒ肳��Ă��邽�߁A��\���Ĉ�ԍŏ��ɑI�����Ă���g���b�N�̏���\�����Ă��܂��B���̂܂܏������ނƕ\������Ă���l�ŏ㏑������܂��B", MessageType.Warn);
				}
				if (sameAlbum) // �A���o�������ׂē����ꍇ
				{
					albumText.Text = albums[0]; // �ŏ��̗v�f��\������
				}
				else // �A���o�����قȂ�ꍇ
				{
					albumText.Text = albums[0]; // �ŏ��̗v�f��\������
					albumText.BackColor = Color.Red;
					setLogMessage("�����̈قȂ�A���o�����ݒ肳��Ă��邽�߁A��\���Ĉ�ԍŏ��ɑI�����Ă���g���b�N�̏���\�����Ă��܂��B���̂܂܏������ނƕ\������Ă���l�ŏ㏑������܂��B", MessageType.Warn);

				}
				if (sameAlbumArtist) // �A���o���A�[�e�B�X�g�����ׂē����ꍇ
				{
					albumArtistText.Text = albumArtists[0]; // �ŏ��̗v�f��\������
				}
				else // �A���o���A�[�e�B�X�g���قȂ�ꍇ
				{
					albumArtistText.Text = albumArtists[0]; // �ŏ��̗v�f��\������
					albumArtistText.BackColor = Color.Red;
					setLogMessage("�����̈قȂ�A���o���A�[�e�B�X�g���ݒ肳��Ă��邽�߁A��\���Ĉ�ԍŏ��ɑI�����Ă���g���b�N�̏���\�����Ă��܂��B���̂܂܏������ނƕ\������Ă���l�ŏ㏑������܂��B", MessageType.Warn);
				}
				// �s�N�`���[�{�b�N�X�ɕ\������
				if (sameAlbumArt) // �A���o���A�[�g�����ׂē����ꍇ
				{
					mp3Picture.BackgroundImage = Image.FromStream(new MemoryStream(albumArts[0])); // �ŏ��̗v�f��\������
				}
				else // �A���o���A�[�g���قȂ�ꍇ
				{
					mp3Picture.BackgroundImage = null; // �摜������
					setLogMessage("�����̈قȂ�A���o���A�[�g���ݒ肳��Ă���A�������͐ݒ肳��Ă��Ȃ����߁A�摜��\�����܂���B", MessageType.Warn);
				}
			}
			else
			{
				setLogMessage("�Y���p�X��mp3�t�@�C�����܂܂�Ă��܂���B", MessageType.Warn);
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
			// data��null�̏ꍇ�ɉ������Ȃ�
			if (data == null || data.Length == 0)
			{
				return null;
			}

			using (var sha256 = new SHA256Managed())
			{
				return sha256.ComputeHash(data);
			}
		}

		/// <summary>
		/// �}�E�X�|�C���^�̃A�C�R����ύX���܂��B
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Main_DragEnter(object sender, DragEventArgs e)
		{
			// �}�E�X�|�C���^�[�`��ύX
			//
			// DragDropEffects
			//  Copy  :�f�[�^���h���b�v��ɃR�s�[����悤�Ƃ��Ă�����
			//  Move  :�f�[�^���h���b�v��Ɉړ�����悤�Ƃ��Ă�����
			//  Scroll:�f�[�^�ɂ���ăh���b�v��ŃX�N���[�����J�n����悤�Ƃ��Ă����ԁA���邢�͌��݃X�N���[�����ł�����
			//  All   :���3��g�ݍ��킹������
			//  Link  :�f�[�^�̃����N���h���b�v��ɍ쐬����悤�Ƃ��Ă�����
			//  None  :�����Ȃ�f�[�^���h���b�v�悪�󂯕t���悤�Ƃ��Ȃ����

			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effect = DragDropEffects.Copy;
			}
			else
			{
				e.Effect = DragDropEffects.None;
			}
		}

		private void Main_DragDrop(object sender, DragEventArgs e)
		{
			// DataFormats.FileDrop��^���āAGetDataPresent()���\�b�h���Ăяo���B
			var dropTarget = (string[])e.Data.GetData(DataFormats.FileDrop, false);

			bool firstContinue = true;

			// GetData�ɂ��擾����String�^�̔z�񂩂�v�f�����o���B
			foreach (var targetFile in dropTarget)
			{
				if (firstContinue && !(Path.GetExtension(targetFile).ToLower() == ".jpg" || Path.GetExtension(targetFile).ToLower() == ".png"))
				{
					mp3ListBox.Items.Clear();
					mp3PathListBox.Items.Clear();
					firstContinue = false;
				}
				DragAndDropExec(targetFile);
			}
			// �S�g���b�N�I��
			AllMP3TracksSelect();

			// �^�O����\��
			GetListBoxSelectedInfo();
		}

		private void DragAndDropExec(string path)
		{
			if (Directory.Exists(path))
			{
				// �t�H���_
				directoryText.Text = path;
				applyMP3FilesList(path, false);
			}
			else if (System.IO.File.Exists(path))
			{
				// �t�@�C��
				if (System.IO.Path.GetExtension(path).ToLower() == ".mp3")
				{
					directoryText.Clear();

					// mp3ListBox�Ƀt�@�C����������ǉ�
					mp3ListBox.Items.Add(Path.GetFileName(path));
					// mp3PathListBox�Ƀt���p�X��ǉ�
					mp3PathListBox.Items.Add(path);
				}
				else if ((Path.GetExtension(path).ToLower() == ".jpg" || Path.GetExtension(path).ToLower() == ".png"))
				{
					if (!keepOriginCheck.Checked)
					{
						mp3Picture.BackgroundImage = System.Drawing.Image.FromFile(path);
					}
					else
					{
						setLogMessage("���摜�ێ��`�F�b�N���L���̂��߁A�摜�͕ύX����܂���B", MessageType.Info);
					}
				}
				else
				{
					setLogMessage($"�t�@�C���`�����s���ł��B�F{path}�i{Path.GetExtension(path).ToLower()}�j", MessageType.Error);
				}
			}
			else
			{
				// �t�@�C�����݂��Ȃ�
				setLogMessage($"�t�@�C�������݂��܂���F{path}", MessageType.Error);
			}

			return;
		}

		private void searchText_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == (char)Keys.Enter)
			{
				searchButton_Click(sender, e);
			}
		}

		private void directoryText_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == (char)Keys.Enter)
			{
				applyMP3FilesList(directoryText.Text.Trim());
				AllMP3TracksSelect();
			}
		}

		private void editFolderNameCheck_CheckedChanged(object sender, EventArgs e)
		{
			bool status = editFolderNameCheck.Checked;

			folderNameText.Enabled = status;
			editFolderNameOnlyCheck.Enabled = status;
			folderNameEditHelpButton.Enabled = status;
		}

		private void folderNameEditHelpButton_Click(object sender, EventArgs e)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("{ProductID}�F��iID");
			sb.Append("\n{ProductDate}�F�̔����iyyMMdd�`���j");
			sb.Append("\n{ProductName}�F��i��");
			sb.Append("\n{ProductCircle}�F�T�[�N����");
			sb.Append("\n{ProductCVs}�F���D");
			sb.Append("\n\n�\��t�H���_���F");
			sb.Append("\n" + UpdateFolderName());
			MessageBox.Show(sb.ToString(), this.Text);
		}
	}
}