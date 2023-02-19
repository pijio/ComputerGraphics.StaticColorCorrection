using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ComputerGraphics.StaticColorCorrection.App;

namespace ComputerGraphics.StaticColorCorrection.GUI
{
    public partial class MainForm : Form
    {
        private string _currentImagePath = string.Empty;
        private string _currentImagePath2 = string.Empty;
        private readonly double _previewRatio;
        private double? _customContrast = null;
        private readonly bool[] _includedSpaces = { false, false }; // 1 - lab, 2 - hsl
        private bool _useCustomContrast = false;
        private readonly Label[] _labels;
        public MainForm()
        {
            InitializeComponent();
            _labels = new[] { label1, label2, label3, label4, label5, label6 };
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
            ResetInfoBox(1);
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
                label4.ForeColor = Color.Red;
                label4.Text = ex.Message;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ResetInfoBox(2);
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
                label4.ForeColor = Color.Red;
                label4.Text = ex.Message;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ResetInfoBox(3);
            try
            {
                var watcher = new Stopwatch();
                watcher.Start();
                var merge = ColorSpaceHelper.MergePictures(new Bitmap(_currentImagePath),
                    new Bitmap(_currentImagePath2), _includedSpaces, _customContrast.HasValue && _useCustomContrast ? _customContrast.Value : (double?)null);
                pictureBox3.Image =
                    ScalePictureForPictureBox(merge[0]);
                pictureBox4.Image =
                    ScalePictureForPictureBox(merge[1]);
                watcher.Stop();
                var time = watcher.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                label5.ForeColor = Color.Red;
                label5.Text = ex.Message;
            }
        }

        private Image ScalePictureForPictureBox(Image image)
        {
            if (image == null)
                return null;
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

        private void ResetInfoBox(int no)
        {
            var labelTochange = _labels[no - 1];
            labelTochange.ForeColor = Color.Black;
            labelTochange.Text = string.Empty;
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

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            _includedSpaces[1] = !_includedSpaces[1];
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            _includedSpaces[0] = !_includedSpaces[0];
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            _useCustomContrast = !_useCustomContrast;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if(double.TryParse(textBox2.Text, out var val))
            {
                _customContrast = val;
            }
            else
            {
                _customContrast = null;
            }
        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            label7.Text = string.Empty;
            label8.Text = string.Empty;
            if (string.IsNullOrEmpty(_currentImagePath) && string.IsNullOrEmpty(_currentImagePath2))
            {
                return;
            }

            if (_includedSpaces.All(x => x == false))
            {
                return;
            }

            label7.Text = "Бенчмарк запущен.";
            var ms = 0d;
            var testsCount = 100;
            var watcher = new Stopwatch();
            for (int i = 0; i < testsCount; i++)
            {
                watcher.Start();
                ColorSpaceHelper.MergePictures(new Bitmap(_currentImagePath),
                    new Bitmap(_currentImagePath2), _includedSpaces, _customContrast.HasValue && _useCustomContrast ? _customContrast.Value : (double?)null);
                watcher.Stop();
                ms += watcher.ElapsedMilliseconds;
                watcher.Reset();
            }

            label7.Text = "Бенчмарк завершен.";
            label8.Text = $"Среднее время преобразования при заданных параметрах: {(double)ms / testsCount} мс.";
        }
    }
}
