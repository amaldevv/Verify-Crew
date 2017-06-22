using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Shapes;
using Windows.UI;
using Windows.Storage;
using Windows.UI.ViewManagement;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Verify_Crew
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly IFaceServiceClient faceServiceClient = new FaceServiceClient("8392e94159ef475ca9c834041ddbb757", "https://westcentralus.api.cognitive.microsoft.com/face/v1.0");
        private SoftwareBitmap _bitmapSource;
        StorageFile file;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            file = await picker.PickSingleFileAsync();


            FaceCanvas.Children.Clear();
            var fileStream = await file.OpenAsync(FileAccessMode.Read);

            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(fileStream);

            BitmapTransform transform = new BitmapTransform();
            const float sourceImageHeightLimit = 1280;

            if (decoder.PixelHeight > sourceImageHeightLimit)
            {
                float scalingFactor = (float)sourceImageHeightLimit / (float)decoder.PixelHeight;
                transform.ScaledWidth = (uint)Math.Floor(decoder.PixelWidth * scalingFactor);
                transform.ScaledHeight = (uint)Math.Floor(decoder.PixelHeight * scalingFactor);
            }

            _bitmapSource = await decoder.GetSoftwareBitmapAsync(decoder.BitmapPixelFormat, BitmapAlphaMode.Premultiplied, transform, ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);

            ImageBrush brush = new ImageBrush();
            SoftwareBitmapSource bitmapSource = new SoftwareBitmapSource();
            await bitmapSource.SetBitmapAsync(_bitmapSource);
            brush.ImageSource = bitmapSource;
            brush.Stretch = Stretch.Uniform;
            FaceCanvas.Background = brush;




        }

        private async void FindFaceButton_Click(object sender, RoutedEventArgs e)
        {
            FaceRectangle[] faceRects = await UploadAndDetectFaces(file);


            if (faceRects.Length > 0)
            {
                MarkFaces(faceRects);
            }
        }

        private async Task<FaceRectangle[]> UploadAndDetectFaces(StorageFile file)
        {
            try
            {
                ApplicationView appView = ApplicationView.GetForCurrentView();
                appView.Title = "Detecting...";

                using (Stream imageFileStream = await file.OpenStreamForReadAsync())
                {
                    var faces = await faceServiceClient.DetectAsync(imageFileStream);
                    var faceRects = faces.Select(face => face.FaceRectangle);

                    appView.Title = String.Format("Detection Finished. {0} face(s) detected", faceRects.Count());
                    return faceRects.ToArray();
                }


            }
            catch (Exception ex)
            {

                return new FaceRectangle[0];
            }
        }

        private void MarkFaces(FaceRectangle[] faceRects)
        {
            SolidColorBrush lineBrush = new SolidColorBrush(Colors.Red);
            SolidColorBrush fillBrush = new SolidColorBrush(Colors.Transparent);
            double lineThickness = 2.0;

            double dpi = _bitmapSource.DpiX;
            double resizeFactor = 96 / dpi;

            if (faceRects != null)
            {
                double widthScale = _bitmapSource.PixelWidth / FaceCanvas.ActualWidth;
                double heightScale = _bitmapSource.PixelHeight / FaceCanvas.ActualHeight;

                foreach (var faceRectangle in faceRects)
                {

                    try
                    {

                        Rectangle box = new Rectangle
                        {

                            Width = (uint)(faceRectangle.Width / widthScale) - faceRectangle.Width,
                            Height = (uint)(faceRectangle.Height / heightScale),
                            Fill = fillBrush,
                            Stroke = lineBrush,
                            StrokeThickness = lineThickness,
                            Margin = new Thickness((uint)(faceRectangle.Left * resizeFactor) + faceRectangle.Width, (uint)(faceRectangle.Top / heightScale), 0, 0)

                            //new Thickness(faceRectangle.Left * resizeFactor, faceRectangle.Top * resizeFactor, faceRectangle.Width * resizeFactor,                           faceRectangle.Height * resizeFactor)
                            //new Thickness((uint)(faceRectangle.Left * resizeFactor) + faceRectangle.Width, (uint)(faceRectangle.Top / heightScale), 0, 0)
                        };


                        FaceCanvas.Children.Add(box);
                    }
                    catch (Exception)
                    { }
                }
            }
        }
    }
}
