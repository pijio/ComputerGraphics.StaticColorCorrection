using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using ComputerGraphics.StaticColorCorrection.App;

namespace ComputerGraphics.StaticColorCorrection.GUI
{
    public partial class MainForm : Form
    {
        private string _currentImagePath = string.Empty;
        private string _currentImagePath2 = string.Empty;
        private readonly double _previewRatio;
        public MainForm()
        {
            InitializeComponent();
            openFileDialog1.Filter = "Images|*.BMP;*.JPG;*.PNG|All files(*.*)|*.*";
            _previewRatio = (double)pictureBox1.Width / pictureBox1.Height;
        }

        /// <summary>
        /// Диалог для выбора фото
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            ResetInfoBox();
            if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;
            _currentImagePath = openFileDialog1.FileName;
            try
            {
                var image = ScalePictureForPictureBox(Image.FromFile(_currentImagePath));
                pictureBox1.Image = image;
            }
            catch(Exception ex)
            {
                label1.ForeColor = Color.Red;
                label1.Text = ex.Message;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ResetInfoBox();
            if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;
            _currentImagePath2 = openFileDialog1.FileName;
            try
            {
                var image = ScalePictureForPictureBox(Image.FromFile(_currentImagePath2));
                pictureBox2.Image = image;
            }
            catch (Exception ex)
            {
                label1.ForeColor = Color.Red;
                label1.Text = ex.Message;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var watcher = new Stopwatch();
            watcher.Start();
            pictureBox3.Image =
                ScalePictureForPictureBox(ColorSpaceHelper.MergePictures(new Bitmap(_currentImagePath), new Bitmap(_currentImagePath2)));
            watcher.Stop();
            var time = watcher.ElapsedMilliseconds;
        }

        private Image ScalePictureForPictureBox(Image image)
        {
            double imageRatio = (double)image.Width / image.Height;

            // Вычисляем новые размеры изображения с сохранением пропорций
            int newWidth, newHeight;
            if (imageRatio > _previewRatio)
            {
                newWidth = pictureBox1.Width;
                newHeight = (int)(pictureBox1.Width / imageRatio);
            }
            else
            {
                newWidth = (int)(pictureBox1.Height * imageRatio);
                newHeight = pictureBox1.Height;
            }
            return new Bitmap(image, newWidth, newHeight);
        }

        private void ResetInfoBox()
        {
            label1.ForeColor = Color.Black;
            label1.Text = string.Empty;
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e) { }

        private void MainForm_Load(object sender, EventArgs e) { }

        private void label1_Click(object sender, EventArgs e) { }

        private void label2_Click(object sender, EventArgs e) { }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }
    }
}
