using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for KernelEditor.xaml
    /// </summary>
    public partial class KernelEditor : Window,INotifyPropertyChanged
    {
        private ObservableCollection<ObservableCollection<KernelValue>> _KernelMatrix;
        public ObservableCollection<ObservableCollection<KernelValue>> KernelMatrix
        {
            get => _KernelMatrix;
            set
            {
                if (_KernelMatrix != value)
                {
                    _KernelMatrix = value;
                    OnPropertyChanged(nameof(KernelMatrix));
                }

            }
        }
        private BitmapImage originalImage;
        public KernelEditor(BitmapImage original)
        {
            InitializeComponent();
            KernelMatrix = new ObservableCollection<ObservableCollection<KernelValue>>();

            DataContext = this;
            this.originalImage = original;
        }

        private void KernelSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (kernelSizeComboBox.SelectedItem is ComboBoxItem selectedComboBoxItem)
            {
                // Retrieve the content of the ComboBoxItem which you have set as a string
                string content = selectedComboBoxItem.Content.ToString();

                if (int.TryParse(content, out int selectedSize))
                {
                    GenerateKernelMatrix(selectedSize);
                }
                else
                {
                    MessageBox.Show("Selected size is not a valid number.");
                }
            }
        }

        private void GenerateKernelMatrix(int size)
        {
            var newMatrix = new ObservableCollection<ObservableCollection<KernelValue>>();
            for (int i = 0; i < size; i++)
            {
                var row = new ObservableCollection<KernelValue>();
                for (int j = 0; j < size; j++)
                {
                    row.Add(new KernelValue { Value = 0 });
                }
                newMatrix.Add(row);
            }
            KernelMatrix = newMatrix;
            itemsControl.Items.Refresh();
        }

        private void Save_Button_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "JSON Files (*.json)|*.json|All files (*.*)|*.*";
            saveFileDialog.DefaultExt = ".json";
            saveFileDialog.FileName = "MyKernelFilter"; 

            // Show save file dialog box
            bool? result = saveFileDialog.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Get the selected file path
                string filePath = saveFileDialog.FileName;

                try
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string jsonString = JsonSerializer.Serialize(KernelMatrix, options);

                    // Write to file
                    File.WriteAllText(filePath, jsonString);

                    MessageBox.Show($"Kernel saved to {filePath}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save kernel to file. Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Load_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON Files (*.json)|*.json|All files (*.*)|*.*";
            openFileDialog.DefaultExt = ".json";

            bool? result = openFileDialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Get the selected file path
                string filePath = openFileDialog.FileName;

                try
                {
                    // Read from file
                    string jsonString = File.ReadAllText(filePath);

                    // Deserialize
                    KernelMatrix = JsonSerializer.Deserialize<ObservableCollection<ObservableCollection<KernelValue>>>(jsonString);

                    itemsControl.ItemsSource = KernelMatrix;

                    MessageBox.Show($"Kernel loaded from {filePath}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load kernel from file. Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private BitmapImage ProcessImageWithKernel(BitmapImage originalImage, ObservableCollection<ObservableCollection<KernelValue>> kernelMatrix)
        {
            // Convert BitmapImage to WriteableBitmap for manipulation
            WriteableBitmap writeableBitmap = new WriteableBitmap(originalImage);

            int width = writeableBitmap.PixelWidth;
            int height = writeableBitmap.PixelHeight;
            writeableBitmap.Lock();

            int kernelWidth = kernelMatrix.Count;
            int kernelHeight = kernelMatrix[0].Count;
            int kernelRadiusX = kernelWidth / 2;
            int kernelRadiusY = kernelHeight / 2;

            // Create a copy of the original bitmap to read from while writing to writeableBitmap
            WriteableBitmap originalCopy = writeableBitmap.Clone();

                for (int x = kernelRadiusX; x < width - kernelRadiusX; x++)
                {
                    for (int y = kernelRadiusY; y < height - kernelRadiusY; y++)
                    {
                        double sumR = 0, sumG = 0, sumB = 0;
                        for (int i = -kernelRadiusX; i <= kernelRadiusX; i++)
                        {
                            for (int j = -kernelRadiusY; j <= kernelRadiusY; j++)
                            {
                                var pixelColor = GetPixelColor(originalCopy, x + i, y + j);
                                double kernelValue = kernelMatrix[kernelRadiusX + i][kernelRadiusY + j].Value;
                                sumR += kernelValue * pixelColor.R;
                                sumG += kernelValue * pixelColor.G;
                                sumB += kernelValue * pixelColor.B;
                            }
                        }

                        // Clamping the sums to valid byte ranges
                        byte r = (byte)Math.Max(0, Math.Min(255, sumR));
                        byte g = (byte)Math.Max(0, Math.Min(255, sumG));
                        byte b = (byte)Math.Max(0, Math.Min(255, sumB));

                        // Update the pixel in the writeableBitmap
                        SetPixelColor(writeableBitmap, x, y, Color.FromRgb(r, g, b));
                    }
                }

            writeableBitmap.Unlock();

            // Convert the WriteableBitmap back to a BitmapImage to return
            return ConvertWriteableBitmapToBitmapImage(writeableBitmap);
        }

        private Color GetPixelColor(WriteableBitmap bitmap, int x, int y)
        {
            if (x < 0 || y < 0 || x >= bitmap.PixelWidth || y >= bitmap.PixelHeight)
                throw new ArgumentOutOfRangeException();

            // Create an array to hold the pixel's color data
            byte[] pixels = new byte[4]; // Assumes 32bpp
            Int32Rect rect = new Int32Rect(x, y, 1, 1);
            int stride = (bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7) / 8;
            bitmap.CopyPixels(rect, pixels, stride, 0);

            // pixels now holds the color data in BGRA format
            return Color.FromArgb(pixels[3], pixels[2], pixels[1], pixels[0]);
        }

        private void SetPixelColor(WriteableBitmap bitmap, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= bitmap.PixelWidth || y >= bitmap.PixelHeight)
                throw new ArgumentOutOfRangeException();

            // Convert color to BGRA format byte array
            byte[] pixels = new byte[] { color.B, color.G, color.R, color.A };
            Int32Rect rect = new Int32Rect(x, y, 1, 1);
            int stride = (bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7) / 8;
            bitmap.WritePixels(rect, pixels, stride, 0);
        }

        private BitmapImage ConvertWriteableBitmapToBitmapImage(WriteableBitmap writeableBitmap)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(writeableBitmap));
                encoder.Save(stream);
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new MemoryStream(stream.ToArray());
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }

        private void Apply_Button_Click(object sender, RoutedEventArgs e)
        {
            BitmapImage processedImage = ProcessImageWithKernel(originalImage, KernelMatrix);
            // After processing the image
            OnFilterApplied(processedImage);

        }
        public event EventHandler<BitmapImage> FilterApplied;

        protected virtual void OnFilterApplied(BitmapImage e)
        {
            FilterApplied?.Invoke(this, e);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void tbDivisor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(tbDivisor.Text, out double divisor))
            {
                for (int i = 0; i < KernelMatrix.Count; i++)
                {
                    for (int j = 0; j < KernelMatrix[i].Count; j++)
                    {
                        KernelMatrix[i][j].Value /= divisor;
                    }
                }
            }
            else if (divisor == 0)
            {
                // If the number is 0
                MessageBox.Show("The divisor cannot be 0.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
                tbDivisor.Text = ""; // Clear the invalid input
            }            
            else
            {
                MessageBox.Show("Please enter a valid number for the Divisor.");
            }
        }

        private void tbOffset_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(tbOffset.Text, out double offset))
            {
                for (int i = 0; i < KernelMatrix.Count; i++)
                {
                    for (int j = 0; j < KernelMatrix[i].Count; j++)
                    {
                        // Add each value by the coefficient
                        KernelMatrix[i][j].Value += offset;
                    }
                }
            }
            else
            {
                MessageBox.Show("Please enter a valid number for the Offset.");
            }
        }

        private void tbAnchorPoint_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(double.TryParse(tbAnchorPoint.Text, out double anchorPoint))
            {
                int anchorRow = KernelMatrix.Count / 2;
                int anchorCol = KernelMatrix.Count / 2;
                KernelMatrix[anchorRow][anchorCol].Value = anchorPoint;
            }
            else 
            { 
                
                MessageBox.Show("Please enter a valid number for the anchor point.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
                tbAnchorPoint.Text = ""; 
            }
        }
        
        private void tbCoefficient_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(tbCoefficient.Text, out double coefficient))
            {
                for (int i = 0; i < KernelMatrix.Count; i++)
                {
                    for (int j = 0; j < KernelMatrix[i].Count; j++)
                    {
                        // Multiply each value by the coefficient
                        KernelMatrix[i][j].Value *= coefficient;
                    }
                }
            }
            else
            {
                MessageBox.Show("Please enter a valid number for the coefficient.");
            }
        }
        
        private void Close_Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class FilterAppliedEventArgs : EventArgs
    {
        public ObservableCollection<ObservableCollection<KernelValue>> KernelMatrix { get; set; }
    }

    public class KernelValue : INotifyPropertyChanged
    {
        private double _Value;
        public double Value
        {
            get => _Value;
            set
            {
                _Value = value;
                OnPropertyChanged(nameof(Value));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
