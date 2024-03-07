using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BitmapImage originalImage;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Upload_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedImagePath = openFileDialog.FileName;
                originalImage = new BitmapImage(new Uri(selectedImagePath));
                // Load the image and display it in the first canvas
                DisplayImage(selectedImagePath, Origin);
            }
        }

        private void DisplayImage(string imagePath, Canvas canvas)
        {

            canvas.Children.Clear();

            Image image = new Image();
            BitmapImage bitmapImage = new BitmapImage(new Uri(imagePath));
            image.Source = bitmapImage;


            image.Width = canvas.Width;
            image.Height = canvas.Height;
 
            canvas.Children.Add(image);
        }

        private void DisplayImageOnCanvas(Canvas canvas, BitmapImage image)
        {
            canvas.Children.Clear(); // Clear previous images
            Image img = new Image
            {
                Width = canvas.Width,
                Height = canvas.Height,
                Source = image
            };
            canvas.Children.Add(img);
        }


        private WriteableBitmap ApplyFilter(BitmapImage image)
        {
            WriteableBitmap writableImage = new WriteableBitmap(image);
            writableImage.Lock();


            writableImage.Unlock();
            return writableImage;
        }

        private BitmapImage GetImageFromCanvas(Canvas canvas)
        {
            foreach (var child in canvas.Children)
            {
                if (child is Image image)
                {
                    // Assuming the source was set as a BitmapImage
                    return image.Source as BitmapImage;
                }
            }
            return null; // No Image found or Source is not a BitmapImage
        }


        private void Kernel_Button_Click(object sender, RoutedEventArgs e)
        {
            if (originalImage != null)
            {
                BitmapImage originalImage = GetImageFromCanvas(Origin);
                KernelEditor kernelEditor = new KernelEditor(originalImage);
                kernelEditor.FilterApplied += KernelEditor_FilterApplied;
                bool? dialogResult = kernelEditor.ShowDialog();

                if (dialogResult == true)
                {
                    kernelEditor.Show();
                    WriteableBitmap processedImage = ApplyFilter(originalImage);          
                }
            }
            else
            {
                MessageBox.Show("Please upload an image first.");
            }
        }

        void KernelEditor_FilterApplied(object sender, BitmapImage e)
        {
            DisplayImageOnCanvas(target, e);
        }

        private void Invert_Button_Click(object sender, RoutedEventArgs e)
        {
        if(target.Children.Count>0 && target.Children[0] is Image) { 
                Image image = target.Children[0] as Image;

                // Check if the image exists
                if (image != null && image.Source != null && image.Source is BitmapSource)
                {
                    // Create a new bitmap source by inverting the colors
                    BitmapSource originalBitmap = (BitmapSource)image.Source;
                    FormatConvertedBitmap formattedBitmap = new FormatConvertedBitmap(originalBitmap, PixelFormats.Bgr32, null, 0);

                    int width = formattedBitmap.PixelWidth;
                    int height = formattedBitmap.PixelHeight;
                    int stride = width * ((formattedBitmap.Format.BitsPerPixel + 7) / 8);
                    byte[] pixels = new byte[height * stride];
                    formattedBitmap.CopyPixels(pixels, stride, 0);

                    for (int i = 0; i < pixels.Length; i += 4)
                    {
                        pixels[i] = (byte)(255 - pixels[i]);
                        pixels[i + 1] = (byte)(255 - pixels[i + 1]);
                        pixels[i + 2] = (byte)(255 - pixels[i + 2]);
                    }
                    WriteableBitmap invertedBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
                    invertedBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);

                    // Create a new Image and set its source to the inverted bitmap
                    Image invertedImage = new Image();
                    invertedImage.Source = invertedBitmap;

                    // Add the inverted image to the second canvas
                    target.Children.Clear();

                    invertedImage.Width = target.Width;
                    invertedImage.Height = target.Height;

                    target.Children.Add(invertedImage);
                }
            }
        }

        private void Bright_Button_Click(object sender, RoutedEventArgs e)
        {              
        if(target.Children.Count>0 && target.Children[0] is Image)
        { 
            Image image = target.Children[0] as Image;               
            if(image!=null && image.Source is BitmapSource)
            {
                BitmapSource bitmapSource = (BitmapSource)image.Source;
                FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap(bitmapSource,PixelFormats.Bgra32,null,0);
                int width = formatConvertedBitmap.PixelWidth;
                int height = formatConvertedBitmap.PixelHeight;
                //
                int stride = width * ((formatConvertedBitmap.Format.BitsPerPixel + 7) / 8);
                byte[] pixels = new byte[height * stride];
                formatConvertedBitmap.CopyPixels(pixels, stride, 0);
                double brightness = 50; // Increase this value to increase brightness

                for (int i = 0; i < pixels.Length; i += 4)
                {
                    double red = pixels[i + 2] + brightness;
                    double green = pixels[i + 1] + brightness;
                    double blue = pixels[i] + brightness;

                    pixels[i + 2] = (byte)Math.Min(255, Math.Max(0, red));
                    pixels[i + 1] = (byte)Math.Min(255, Math.Max(0, green));
                    pixels[i] = (byte)Math.Min(255, Math.Max(0, blue));
                }

                WriteableBitmap brightBitmap = new WriteableBitmap(formatConvertedBitmap);
                brightBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);

                Image brightImage = new Image();
                image.Source = brightBitmap;

                target.Children.Clear();
                image.Width = target.Width;
                image.Height = target.Height;
                target.Children.Add(image);
            }
        }            
        }

        private void Contrast_Button_Click(object sender, RoutedEventArgs e)
        {
            if(target.Children.Count > 0 && target.Children[0] is Image)
            {
                Image image = target.Children[0] as Image;

                if(image != null && image.Source is BitmapSource)
                {
                    BitmapSource originalSource = (BitmapSource)image.Source;
                    FormatConvertedBitmap formatBitmap = new FormatConvertedBitmap(originalSource, PixelFormats.Bgr32, null, 0);

                    double contrast = 150;

                    byte[] pixels = ApplyContrastEnhancement(formatBitmap, contrast);

                    WriteableBitmap contastEnhancementBitmap = new WriteableBitmap(formatBitmap);
                    contastEnhancementBitmap.WritePixels(new Int32Rect(0, 0, formatBitmap.PixelWidth, formatBitmap.PixelHeight), pixels, formatBitmap.PixelWidth * 4, 0);

                    image.Source = contastEnhancementBitmap;

                    target.Children.Clear();
                    image.Height = target.Height;
                    image.Width = target.Width;
                    target.Children.Add(image);
                }

            }
        }

        private byte[] ApplyContrastEnhancement(FormatConvertedBitmap originalBitmap, double contrast)
        {
            int width = originalBitmap.PixelWidth;
            int height = originalBitmap.PixelHeight;
            int stride = width * 4; // 4 bytes per pixel (Bgr32 format)
            byte[] pixels = new byte[height * stride];
            originalBitmap.CopyPixels(pixels, stride, 0);

            double factor = (259 * (contrast + 255)) / (255 * (259 - contrast));

            for (int i = 0; i < pixels.Length; i += 4)
            {
                double red = (pixels[i] - 128) * factor + 128;
                double green = (pixels[i + 1] - 128) * factor + 128;
                double blue = (pixels[i + 2] - 128) * factor + 128;

                pixels[i] = (byte)Math.Max(0, Math.Min(255, red)); // Blue
                pixels[i + 1] = (byte)Math.Max(0, Math.Min(255, green)); // Green
                pixels[i + 2] = (byte)Math.Max(0, Math.Min(255, blue)); // Red
            }

            return pixels;
        }

        private void Gamma_Button_Click(object sender, RoutedEventArgs e)
        {            
                if(target.Children.Count>0 && target.Children[0] is Image)
                {                   
                Image image = target.Children[0] as Image;
                    if(image!=null && image.Source is BitmapSource)
                    {
                        BitmapSource originalBitmap = (BitmapSource)image.Source;
                        FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap(originalBitmap, PixelFormats.Bgra32, null, 0);

                        double gamma = 0.4;
                        byte[] pixels = ApplyGammaCorrection(formatConvertedBitmap,gamma);

                        WriteableBitmap gammaCorrectionBitmap = new WriteableBitmap(formatConvertedBitmap);
                        gammaCorrectionBitmap.WritePixels(new Int32Rect(0, 0, formatConvertedBitmap.PixelWidth, formatConvertedBitmap.PixelHeight),pixels,formatConvertedBitmap.PixelWidth*4,0);


                        Image outputImage = new Image();
                        outputImage.Source = gammaCorrectionBitmap;

                        target.Children.Clear();
                        outputImage.Width = target.Width;
                        outputImage.Height = target.Height;
                        target.Children.Add(outputImage);
                    }
                }
            
        }

        private byte[] ApplyGammaCorrection(FormatConvertedBitmap originalBitmap, double gamma)
        {
            int width = originalBitmap.PixelWidth;
            int height = originalBitmap.PixelHeight;
            int stride = width * 4; // 4 bytes per pixel (Bgr32 format)
            byte[] pixels = new byte[height * stride];
            originalBitmap.CopyPixels(pixels, stride, 0);

            double gammaCorrection = 1.0 / gamma;


            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = (byte)(255 * Math.Pow(pixels[i] / 255.0, gammaCorrection)); // Blue
                pixels[i + 1] = (byte)(255 * Math.Pow(pixels[i + 1] / 255.0, gammaCorrection)); // Green
                pixels[i + 2] = (byte)(255 * Math.Pow(pixels[i + 2] / 255.0, gammaCorrection)); // Red
            }

            return pixels;
        }

        private void Save_Button_Click(object sender, RoutedEventArgs e)
        {
            if(target.Children.Count>0 && target.Children[0] is Image)
            {
                Image imageToSave = target.Children[0] as Image;

                if (imageToSave != null && imageToSave.Source is BitmapSource)
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    BitmapFrame bitmapFrame = BitmapFrame.Create((BitmapSource)imageToSave.Source);

                    encoder.Frames.Add(bitmapFrame);

                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*";

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        string fileName = saveFileDialog.FileName;

                        using (FileStream stream = new FileStream(fileName, FileMode.Create))
                        {
                            encoder.Save(stream);
                        }
                    }
                }
            }

            
        }

        private void Reset_Button_Click(object sender, RoutedEventArgs e)
        {
            if(originalImage!=null)
            {
                Image original = new Image();
                original.Source = originalImage;

                target.Children.Clear();

                original.Width = target.Width;
                original.Height = target.Height;

                target.Children.Add(original);
            }
        }

        private void Exit_Button_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Blur_Button_Click(object sender, RoutedEventArgs e)
        {
            if(target.Children.Count > 0 && target.Children[0] is Image)
            {
                Image blurImage = target.Children[0] as Image;

                if(blurImage != null && blurImage.Source is BitmapSource)
                {
                    BitmapSource originalSource = (BitmapSource)blurImage.Source;
                    FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap(originalSource, PixelFormats.Bgra32, null, 0);

                    int width = formatConvertedBitmap.PixelWidth;
                    int height = formatConvertedBitmap.PixelHeight;
                    int stride = width * ((formatConvertedBitmap.Format.BitsPerPixel + 7) / 8);
                    byte[] pixels = new byte[height * stride];
                    formatConvertedBitmap.CopyPixels(pixels, stride, 0);

                    int kernelSize = 3;

                    byte[] tempPixels = new byte[height * stride];

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            // Initialize sum and count for calculating the average
                            int sumR = 0, sumG = 0, sumB = 0;
                            int count = 0;

                            // Iterate over the pixels within the kernel
                            for (int ky = -kernelSize / 2; ky <= kernelSize / 2; ky++)
                            {
                                for (int kx = -kernelSize / 2; kx <= kernelSize / 2; kx++)
                                {
                                    // Calculate the coordinates of the current pixel in the original image
                                    int nx = x + kx;
                                    int ny = y + ky;

                                    // Check if the pixel is within the image bounds
                                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                                    {
                                        // Calculate the index of the current pixel in the pixel array
                                        int index = ny * stride + nx * 4;

                                        // Accumulate the color values
                                        sumB += pixels[index];
                                        sumG += pixels[index + 1];
                                        sumR += pixels[index + 2];

                                        // Increment the count
                                        count++;
                                    }
                                }
                            }
                            // Calculate the average color values
                            byte avgB = (byte)(sumB / count);
                            byte avgG = (byte)(sumG / count);
                            byte avgR = (byte)(sumR / count);

                            // Calculate the index of the current pixel in the temporary pixel array
                            int currentIndex = y * stride + x * 4;

                            // Update the color values in the temporary pixel array
                            tempPixels[currentIndex] = avgB;
                            tempPixels[currentIndex + 1] = avgG;
                            tempPixels[currentIndex + 2] = avgR;
                            tempPixels[currentIndex + 3] = pixels[currentIndex + 3]; // Alpha channel remains unchanged
                        }
                    }
                    WriteableBitmap blurBitmap = new WriteableBitmap(formatConvertedBitmap);
                    blurBitmap.WritePixels(new Int32Rect(0, 0, formatConvertedBitmap.PixelWidth, formatConvertedBitmap.PixelHeight), tempPixels, formatConvertedBitmap.PixelWidth * 4, 0);

                    blurImage.Source = blurBitmap;
                    target.Children.Clear();
                    blurImage.Height = target.Height;
                    blurImage.Width = target.Width;
                    target.Children.Add(blurImage);
                }
            }
        }

        private void Gaussian_Button_Click(object sender, RoutedEventArgs e)
        {
            if (target.Children.Count > 0 && target.Children[0] is Image)
            {
                Image blurImage = target.Children[0] as Image;

                if (blurImage != null && blurImage.Source is BitmapSource)
                {
                    BitmapSource originalSource = (BitmapSource)blurImage.Source;
                    FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap(originalSource, PixelFormats.Bgra32, null, 0);

                    int width = formatConvertedBitmap.PixelWidth;
                    int height = formatConvertedBitmap.PixelHeight;
                    int stride = width * ((formatConvertedBitmap.Format.BitsPerPixel + 7) / 8);
                    byte[] pixels = new byte[height * stride];
                    formatConvertedBitmap.CopyPixels(pixels, stride, 0);

                    double[,] gaussianKernel = GenerateGaussianKernel(3, 1); // Adjust kernel size and standard deviation as needed

                    // Create a temporary array to store modified pixel values
                    byte[] tempPixels = new byte[height * stride];

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            double sumR = 0, sumG = 0, sumB = 0;
                            int kernelSize = 3; // Kernel size (3x3 in this case)

                            // Iterate over the kernel
                            for (int ky = 0; ky < kernelSize; ky++)
                            {
                                for (int kx = 0; kx < kernelSize; kx++)
                                {
                                    int nx = x + kx - kernelSize / 2;
                                    int ny = y + ky - kernelSize / 2;

                                    // Check if the pixel is within the image bounds
                                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                                    {
                                        int index = ny * stride + nx * 4;
                                        double weight = gaussianKernel[ky, kx];

                                        sumB += weight * pixels[index];
                                        sumG += weight * pixels[index + 1];
                                        sumR += weight * pixels[index + 2];
                                    }
                                }
                            }

                            int currentIndex = y * stride + x * 4;

                            // Update the pixel values
                            tempPixels[currentIndex] = (byte)sumB;
                            tempPixels[currentIndex + 1] = (byte)sumG;
                            tempPixels[currentIndex + 2] = (byte)sumR;
                            tempPixels[currentIndex + 3] = pixels[currentIndex + 3]; // Alpha channel remains unchanged
                        }
                    }

                    WriteableBitmap blurBitmap = new WriteableBitmap(formatConvertedBitmap);
                    blurBitmap.WritePixels(new Int32Rect(0, 0, width, height), tempPixels, stride, 0);

                    blurImage.Source = blurBitmap;

                    target.Children.Clear();
                    blurImage.Height = target.Height;
                    blurImage.Width = target.Width;
                    target.Children.Add(blurImage);
                }
            }
        }

        private double[,] GenerateGaussianKernel(int size, double sigma)
        {
            double[,] kernel = new double[size, size];
            double sumTotal = 0;

            int kernelRadius = size / 2;
            double distance = 0;

            double calculatedEuler = 1.0 / (2.0 * Math.PI * Math.Pow(sigma, 2));

            for (int filterY = -kernelRadius; filterY <= kernelRadius; filterY++)
            {
                for (int filterX = -kernelRadius; filterX <= kernelRadius; filterX++)
                {
                    distance = ((filterX * filterX) + (filterY * filterY)) / (2 * (sigma * sigma));

                    kernel[filterY + kernelRadius, filterX + kernelRadius] = calculatedEuler * Math.Exp(-distance);

                    sumTotal += kernel[filterY + kernelRadius, filterX + kernelRadius];
                }
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    kernel[y, x] = kernel[y, x] * (1.0 / sumTotal);
                }
            }

            return kernel;
        }

        private void Sharpen_Button_Click(object sender, RoutedEventArgs e)
        {
            if (target.Children.Count > 0 && target.Children[0] is Image)
            {
                Image sharpenImage = target.Children[0] as Image;

                if (sharpenImage != null && sharpenImage.Source is BitmapSource)
                {
                    BitmapSource originalSource = (BitmapSource)sharpenImage.Source;
                    FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap(originalSource, PixelFormats.Bgra32, null, 0);

                    int width = formatConvertedBitmap.PixelWidth;
                    int height = formatConvertedBitmap.PixelHeight;
                    int stride = width * ((formatConvertedBitmap.Format.BitsPerPixel + 7) / 8);
                    byte[] pixels = new byte[height * stride];
                    formatConvertedBitmap.CopyPixels(pixels, stride, 0);

                    // Define the sharpening kernel
                    double[,] sharpenKernel = 
                        //{ { 0, -1, 0 }, { -1, 5, -1 }, { 0, -1, 0 } };
                    { { -1, -1, -1 }, { -1, 9, -1 }, { -1, -1, -1 } };
                    
                    byte[] tempPixels = new byte[height * stride];

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            double sumR = 0, sumG = 0, sumB = 0;

                            // Apply the sharpening kernel
                            for (int ky = -1; ky <= 1; ky++)
                            {
                                for (int kx = -1; kx <= 1; kx++)
                                {
                                    int nx = x + kx;
                                    int ny = y + ky;

                                    // Check if the pixel is within the image bounds
                                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                                    {
                                        int index = ny * stride + nx * 4;
                                        double weight = sharpenKernel[ky + 1, kx + 1]; // Adjusted index for kernel

                                        sumB += weight * pixels[index];
                                        sumG += weight * pixels[index + 1];
                                        sumR += weight * pixels[index + 2];
                                    }
                                }
                            }
                            int currentIndex = y * stride + x * 4;
                            // Update the pixel values
                            tempPixels[currentIndex] = (byte)Math.Min(Math.Max(sumB, 0), 255);
                            tempPixels[currentIndex + 1] = (byte)Math.Min(Math.Max(sumG, 0), 255);
                            tempPixels[currentIndex + 2] = (byte)Math.Min(Math.Max(sumR, 0), 255);
                            tempPixels[currentIndex + 3] = pixels[currentIndex + 3]; // Alpha channel remains unchanged
                        }
                    }
                    WriteableBitmap sharpenBitmap = new WriteableBitmap(formatConvertedBitmap);
                    sharpenBitmap.WritePixels(new Int32Rect(0, 0, width, height), tempPixels, stride, 0);
                    sharpenImage.Source = sharpenBitmap;
                    target.Children.Clear();
                    sharpenImage.Height = target.Height;
                    sharpenImage.Width = target.Width;
                    target.Children.Add(sharpenImage);
                }
            }
        }

        private void Edge_Button_Click(object sender, RoutedEventArgs e)
        {
            if (target.Children.Count > 0 && target.Children[0] is Image)
            {
                Image edgeImage = target.Children[0] as Image;

                if (edgeImage != null && edgeImage.Source is BitmapSource)
                {
                    BitmapSource originalSource = (BitmapSource)edgeImage.Source;
                    FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap(originalSource, PixelFormats.Bgra32, null, 0);

                    int width = formatConvertedBitmap.PixelWidth;
                    int height = formatConvertedBitmap.PixelHeight;
                    int stride = width * ((formatConvertedBitmap.Format.BitsPerPixel + 7) / 8);
                    byte[] pixels = new byte[height * stride];
                    formatConvertedBitmap.CopyPixels(pixels, stride, 0);

                    // Define the Sobel operator kernels
                    int[,] sobelX = { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
                    int[,] sobelY = { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };
                    // Define the vertical edge detection kernel
                    //int[,] Kernel = { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
                    //int[,] Kernel = { { 0, -1, 0 }, { 0, 1, 0 }, { 0, 0, 0 } };
                    int[,] Kernel = { { -1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 0 } };

                    byte[] tempPixels = new byte[height * stride];

                    for (int y = 1; y < height - 1; y++)
                    {
                        for (int x = 1; x < width - 1; x++)
                        {
                            int sumX = 0, sumY = 0, 
                                sum = 0;

                            // Apply the Sobel operator kernels
                            for (int ky = -1; ky <= 1; ky++)
                            {
                                for (int kx = -1; kx <= 1; kx++)
                                {
                                    int nx = x + kx;
                                    int ny = y + ky;
                                    int index = ny * stride + nx * 4;

                                    sumX += sobelX[ky + 1, kx + 1] * pixels[index];
                                    sumY += sobelY[ky + 1, kx + 1] * pixels[index];

                                    sum += Kernel[ky + 1, kx + 1] * pixels[index];
                                }
                            }
                            int currentIndex = y * stride + x * 4;
                            // Calculate the magnitude of the gradient
                            int magnitude = 
                                //(int)Math.Sqrt(sumX * sumX + sumY * sumY)
                                (byte)Math.Min(Math.Max(sum, 0), 255);

                            // Apply thresholding to enhance edges
                            byte edgeValue = (byte)(magnitude > 255 ? 255 : magnitude < 0 ? 0 : magnitude);

                            // Update the pixel values
                            tempPixels[currentIndex] = edgeValue;
                            tempPixels[currentIndex + 1] = edgeValue;
                            tempPixels[currentIndex + 2] = edgeValue;
                            tempPixels[currentIndex + 3] = pixels[currentIndex + 3]; // Alpha channel remains unchanged
                        }
                    }
                    WriteableBitmap edgeBitmap = new WriteableBitmap(formatConvertedBitmap);
                    edgeBitmap.WritePixels(new Int32Rect(0, 0, width, height), tempPixels, stride, 0);

                    edgeImage.Source = edgeBitmap;

                    target.Children.Clear();
                    edgeImage.Height = target.Height;
                    edgeImage.Width = target.Width;
                    target.Children.Add(edgeImage);
                }
            }
        }

        private void Emboss_Button_Click(object sender, RoutedEventArgs e)
        {
            if (target.Children.Count > 0 && target.Children[0] is Image)
            {
                Image embossImage = target.Children[0] as Image;

                if (embossImage != null && embossImage.Source is BitmapSource)
                {
                    BitmapSource originalSource = (BitmapSource)embossImage.Source;
                    FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap(originalSource, PixelFormats.Bgra32, null, 0);

                    int width = formatConvertedBitmap.PixelWidth;
                    int height = formatConvertedBitmap.PixelHeight;
                    int stride = width * ((formatConvertedBitmap.Format.BitsPerPixel + 7) / 8);
                    byte[] pixels = new byte[height * stride];
                    formatConvertedBitmap.CopyPixels(pixels, stride, 0);

                    // Define the emboss kernel
                    int[,] embossKernel = 
                        //{ { -2, -1, 0 }, { -1, 1, 1 }, { 0, 1, 2 } }
                        //{ { -1, 0, 1 }, { -1, 1, 1 }, { -1, 0, 1 } }
                    //{ { -1, -1, -1 }, { 0, 1, 0 }, { 1, 1, 1 } }
                    { { -1, -1, 0 }, { -1, 1, 1 }, { 0, 1, 1 } }
                    ;

                    byte[] tempPixels = new byte[height * stride];
                    for (int y = 1; y < height - 1; y++)
                    {
                        for (int x = 1; x < width - 1; x++)
                        {
                            int sum = 0;

                            // Apply the emboss kernel to the pixel and its neighbors
                            for (int ky = -1; ky <= 1; ky++)
                            {
                                for (int kx = -1; kx <= 1; kx++)
                                {
                                    int nx = x + kx;
                                    int ny = y + ky;
                                    int index = ny * stride + nx * 4;

                                    // Apply the kernel
                                    sum += embossKernel[ky + 1, kx + 1] * pixels[index];
                                }
                            }
                            int currentIndex = y * stride + x * 4;
                            // Update the pixel values
                            byte embossValue = (byte)Math.Min(Math.Max(sum , 0), 255); 
                            tempPixels[currentIndex] = embossValue;
                            tempPixels[currentIndex + 1] = embossValue;
                            tempPixels[currentIndex + 2] = embossValue;
                            tempPixels[currentIndex + 3] = pixels[currentIndex + 3]; 
                        }
                    }

                    WriteableBitmap embossBitmap = new WriteableBitmap(formatConvertedBitmap);
                    embossBitmap.WritePixels(new Int32Rect(0, 0, width, height), tempPixels, stride, 0);

                    embossImage.Source = embossBitmap;

                    target.Children.Clear();
                    embossImage.Height = target.Height;
                    embossImage.Width = target.Width;
                    target.Children.Add(embossImage);
                }
            }
        }

        private void Greyscale_Button_Click(object sender, RoutedEventArgs e)
        {
            if(target.Children.Count > 0 && target.Children[0] is Image)
            {
                Image greySacleImage = target.Children[0] as Image;

                if(greySacleImage != null && greySacleImage.Source is BitmapSource)
                {
                    BitmapSource originalSource = (BitmapSource)greySacleImage.Source;
                    FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap(originalSource, PixelFormats.Bgra32, null, 0);

                    int width = formatConvertedBitmap.PixelWidth;
                    int height = formatConvertedBitmap.PixelHeight;
                    int stride = width * ((formatConvertedBitmap.Format.BitsPerPixel + 7) / 8);
                    byte[] pixels = new byte[height * stride];
                    formatConvertedBitmap.CopyPixels(pixels, stride, 0);

                    byte[] tempPixels = new byte[height * stride];

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int currentIndex = y * stride + x * 4;
                            byte grayValue = (byte)((pixels[currentIndex] + pixels[currentIndex + 1] + pixels[currentIndex + 2]) / 3);
                            // Set all color channels to the grayscale value
                            tempPixels[currentIndex] = grayValue;
                            tempPixels[currentIndex + 1] = grayValue;
                            tempPixels[currentIndex + 2] = grayValue;
                            tempPixels[currentIndex + 3] = pixels[currentIndex + 3];
  
                        }
                    }
                    WriteableBitmap greyBitmap = new WriteableBitmap(formatConvertedBitmap);
                    greyBitmap.WritePixels(new Int32Rect(0, 0, width, height), tempPixels, stride, 0);

                    greySacleImage.Source = greyBitmap;

                    target.Children.Clear();

                    greySacleImage.Height = target.Height;
                    greySacleImage.Width = target.Width;
                    target.Children.Add(greySacleImage);

                }
            }
        }

        private void RGBtoHSV(int red, int green, int blue, out double hue, out double saturation, out double value)
        {
            {
            double r = red / 255.0;
            double g = green / 255.0;
            double b = blue / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));

             if (max == min)
            {
                hue = 0;
            }

            else if (max == r)
            {
                hue = 60 * ((g - b) / (max - min));
                    if (g < b) hue += 360;
                }

            else if (max == g)
            {
                hue = 60 * ((b - r) / (max - min)) + 120;
            }
            else // max == b
            {
                hue = 60 * ((r - g) / (max - min)) + 240;
            }

            // Saturation calculation
            if (max == 0)
            {
                saturation = 0;
            }
            else
            {
                saturation = 1 - (min / max);
            }

            // Value calculation
            value = max;
        }
    }

        private void Hue_Button_Click(object sender, RoutedEventArgs e)
        {
             if (target.Children.Count > 0 && target.Children[0] is Image)
                {
                    Image hueImage = target.Children[0] as Image;

                    if (hueImage != null && hueImage.Source is BitmapSource)
                    {
                        BitmapSource originalSource = (BitmapSource)hueImage.Source;
                        FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap(originalSource, PixelFormats.Bgra32, null, 0);

                        int width = formatConvertedBitmap.PixelWidth;
                        int height = formatConvertedBitmap.PixelHeight;
                        int stride = width * ((formatConvertedBitmap.Format.BitsPerPixel + 7) / 8);
                        byte[] pixels = new byte[height * stride];
                        formatConvertedBitmap.CopyPixels(pixels, stride, 0);

                        byte[] tempPixels = new byte[height * stride];

                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                int currentIndex = y * stride + x * 4;
                                byte red = pixels[currentIndex];
                                byte green = pixels[currentIndex + 1];
                                byte blue = pixels[currentIndex + 2];


                                double hue, saturation, value;
                                RGBtoHSV(red, green, blue, out hue, out saturation, out value);

                                // Set all color channels to the hue value
                                tempPixels[currentIndex] = (byte)(hue * 255 / 360);
                                tempPixels[currentIndex + 1] = (byte)(hue * 255 / 360);
                                tempPixels[currentIndex + 2] = (byte)(hue * 255 / 360);
                                tempPixels[currentIndex + 3] = pixels[currentIndex + 3];




                            }
                        }
                        WriteableBitmap greyBitmap = new WriteableBitmap(formatConvertedBitmap);
                        greyBitmap.WritePixels(new Int32Rect(0, 0, width, height), tempPixels, stride, 0);

                        hueImage.Source = greyBitmap;

                        target.Children.Clear();

                        hueImage.Height = target.Height;
                        hueImage.Width = target.Width;
                        target.Children.Add(hueImage);

                    }
                }
        }

        private void Saturation_Button_Click(object sender, RoutedEventArgs e)
        {          
            if (target.Children.Count > 0 && target.Children[0] is Image)
            {
                Image saturateImage = target.Children[0] as Image;

                if (saturateImage != null && saturateImage.Source is BitmapSource)
                {
                    BitmapSource originalSource = (BitmapSource)saturateImage.Source;
                    FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap(originalSource, PixelFormats.Bgra32, null, 0);

                    int width = formatConvertedBitmap.PixelWidth;
                    int height = formatConvertedBitmap.PixelHeight;
                    int stride = width * ((formatConvertedBitmap.Format.BitsPerPixel + 7) / 8);
                    byte[] pixels = new byte[height * stride];
                    formatConvertedBitmap.CopyPixels(pixels, stride, 0);

                    byte[] tempPixels = new byte[height * stride];

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int currentIndex = y * stride + x * 4;
                               
                            byte red = pixels[currentIndex];
                            byte green = pixels[currentIndex + 1];
                            byte blue = pixels[currentIndex + 2];

                            double hue, saturation, value;
                            RGBtoHSV(red, green, blue, out hue, out saturation, out value);

                            // Set all color channels to the saturation value
                            tempPixels[currentIndex] = (byte)(saturation * 255); ;
                            tempPixels[currentIndex + 1] = (byte)(saturation * 255 );
                            tempPixels[currentIndex + 2] = (byte)(saturation * 255);
                            tempPixels[currentIndex + 3] = pixels[currentIndex + 3];
                        }
                    }
                    WriteableBitmap saturateBitmap = new WriteableBitmap(formatConvertedBitmap);
                    saturateBitmap.WritePixels(new Int32Rect(0, 0, width, height), tempPixels, stride, 0);

                    saturateImage.Source = saturateBitmap;

                    target.Children.Clear();

                    saturateImage.Height = target.Height;
                    saturateImage.Width = target.Width;
                    target.Children.Add(saturateImage);

                }
            }           
        }

        private void Value_Button_Click(object sender, RoutedEventArgs e)
        {
            if (target.Children.Count > 0 && target.Children[0] is Image)
                {
                    Image valueImage = target.Children[0] as Image;

                    if (valueImage != null && valueImage.Source is BitmapSource)
                    {
                        BitmapSource originalSource = (BitmapSource)valueImage.Source;
                        FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap(originalSource, PixelFormats.Bgra32, null, 0);

                        int width = formatConvertedBitmap.PixelWidth;
                        int height = formatConvertedBitmap.PixelHeight;
                        int stride = width * ((formatConvertedBitmap.Format.BitsPerPixel + 7) / 8);
                        byte[] pixels = new byte[height * stride];
                        formatConvertedBitmap.CopyPixels(pixels, stride, 0);

                        byte[] tempPixels = new byte[height * stride];

                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                int currentIndex = y * stride + x * 4;                        
                                byte red = pixels[currentIndex];
                                byte green = pixels[currentIndex + 1];
                                byte blue = pixels[currentIndex + 2];

                                double hue, saturation, value;
                                RGBtoHSV(red, green, blue, out hue, out saturation, out value);
                                // Set all color channels to the value value
                                tempPixels[currentIndex] = (byte)(value * 255);
                                tempPixels[currentIndex + 1] = (byte)(value * 255);
                                tempPixels[currentIndex + 2] = (byte)(value * 255);
                                tempPixels[currentIndex + 3] = pixels[currentIndex + 3];
                            }
                        }
                        WriteableBitmap valueBitmap = new WriteableBitmap(formatConvertedBitmap);
                        valueBitmap.WritePixels(new Int32Rect(0, 0, width, height), tempPixels, stride, 0);

                        valueImage.Source = valueBitmap;

                        target.Children.Clear();

                        valueImage.Height = target.Height;
                        valueImage.Width = target.Width;
                        target.Children.Add(valueImage);

                    }
                }
        }
    }
}
