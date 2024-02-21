namespace DLsiteMP3TagEditor
{
	public partial class ImageViewer : Form
	{
		public ImageViewer(Image img)
		{
			InitializeComponent();
			this.Width = img.Width;
			this.Height = img.Height;
			pictureBox1.BackgroundImage = img;
		}
	}
}
