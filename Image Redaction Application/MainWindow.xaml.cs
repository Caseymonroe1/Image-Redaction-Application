using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;
//using OpenCvSharp;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Cuda;
using System.Drawing;
using OpenCvSharp;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using System.Text;
//next step is updating the .exe file so it works
//then add more documentation to github, with gifs of the face redaction doing well

namespace Image_Redaction_Application
{
    public partial class MainWindow : System.Windows.Window
    {
        //global variables being initialized
        //isErasing starts off false, changes to true when erase is toggled
        private Emgu.CV.CascadeClassifier _faceCascade;
        private Emgu.CV.CascadeClassifier _faceCascadeSide;
        private bool isErasing = false;
        private SprayPaintData sprayPaintData = new SprayPaintData();
        //random variable and sizes for the spray paint brush
        private Random random = new Random();
        //variables for the image and file path
        private BitmapImage modifiedImage;
        private string filename;
        private string fileNameExtension;
        SolidColorBrush PaintColor = new SolidColorBrush();

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                _faceCascadeSide = new Emgu.CV.CascadeClassifier("haarcascade_profileface.xml");
                _faceCascade = new Emgu.CV.CascadeClassifier("haarcascade_frontalface_default.xml");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading frontal face cascade classifier: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                RedactFaces.IsEnabled = false;

            }


        }
        //move these to a separate file
        [Serializable]
        //spraraypaintData class to hold ellipses from spray paint effect and store them
        public class SprayPaintData
        {
            public List<SprayEllipse> Ellipses { get; set; } = new List<SprayEllipse>();
        }

        [Serializable]
        //this is the class for individual ellipses being put in the JSON
        public class SprayEllipse
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public double Opacity { get; set; }
            public required SolidColorBrush EllipseColor { get; set; }
        }
        // gonna finish this later
        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get the selected filter
            var selectedFilter = (sender as ComboBox).SelectedItem as ComboBoxItem;
            if (selectedFilter != null)
            {
                ApplyFilterToImage(selectedFilter.Content.ToString());
            }
        }

        private void ApplyFilterToImage(string filterName)
        {
            // Implement the logic to apply the selected filter to the image
            // For example, adjust the image's properties based on the filter
            if (filterName == "1")
            {

            }
            // After applying the filter, update the metadata display
            Console.WriteLine("apply filter to image works");
        }

        
        //hardcoded values for bitmap dimensions and formats, consider making them configurable
        private void SaveModifiedImage(string filePath)
        {
            //visual will be used to combine the image with the canvas
            DrawingVisual visual = new DrawingVisual();
            //using context to start the drawing on visual
            using (DrawingContext context = visual.RenderOpen())
            {
                //creating copy of the image for spray paint to be drawn onto
                context.DrawImage(modifiedImage, new System.Windows.Rect(new System.Windows.Size(Image.Width, Image.Height)));

                //drawing the spray paint onto the image copy, 
                foreach (SprayEllipse sprayEllipse in sprayPaintData.Ellipses)
                {
                    SolidColorBrush brush = sprayEllipse.EllipseColor;
                    //drawing the ellipses from spraypaint data onto the image
                    //these ellipses will be permanent, but the spray paint will be able to be edited
                    //by opening up the original image again
                    context.DrawEllipse(brush, null, new System.Windows.Point(sprayEllipse.X, sprayEllipse.Y), 2, 2);
                }
            }

            // Create a RenderTargetBitmap to convert the visual to a BitmapImage
            //converting the visual into a BitmapImage using a renderTargetBitmap
            RenderTargetBitmap renderTarget = new RenderTargetBitmap(
                (int)Image.Width, (int)Image.Height, 96, 96, PixelFormats.Pbgra32);
            //rendering the visual
            renderTarget.Render(visual);

            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderTarget));



            //saving the modified image and writing it into the file
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    encoder.Save(fs);
                }
            }
            catch (IOException ex)
            {
                // Handle the exception (e.g., log, display an error message)
                Console.WriteLine($"Error saving file: {ex.Message}");
            }



        }
        private void RevealMetadata_Click(object sender, RoutedEventArgs e)
        {
            //checks to make sure that an image has been loaded in
            
            if (modifiedImage is BitmapImage bitmapImage && bitmapImage.UriSource != null) // Ensure an image is loaded
            {
                MetadataWindow metadataWindow = new MetadataWindow();
                string metadata = LoadImageMetadata(fileNameExtension); // Your method to load metadata
                metadataWindow.SetMetadataText(metadata);
                metadataWindow.ShowDialog(); // Show the window as a modal dialog
            }
            else
            {
                MessageBox.Show("No image loaded.");
            }


        }
        private string LoadImageMetadata(string imagePath)
        {
            var directories = ImageMetadataReader.ReadMetadata(imagePath);
            StringBuilder sb = new StringBuilder();

            foreach (var directory in directories)
            {
                foreach (var tag in directory.Tags)
                    sb.AppendLine($"{directory.Name} - {tag.Name}: {tag.Description}");
            }

            return sb.ToString();
        }
        private void SaveSprayPaint(string filePath)
        {

            //writes all ellipses into the json file for the image
            string json = JsonConvert.SerializeObject(sprayPaintData, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            //checks to make sure that an image has been loaded in

            if (modifiedImage is BitmapImage bitmapImage && bitmapImage.UriSource != null)
            {
                //constructing path for spray paint JSON
                string sprayPaintFilePath = System.IO.Path.Combine(Environment.CurrentDirectory, $"{filename}-edited.json");
                SaveSprayPaint(sprayPaintFilePath);
                //have user choose where to save a PNG copy of the image with spray paint applied
                string modifiedImageFilePath = GetSaveFilePath("PNG Files (*.png)|*.png", "Select Image File");
                //checking to make sure that a valid path was chosen before saving the image

                if (modifiedImageFilePath != null)
                {

                    SaveModifiedImage(modifiedImageFilePath);
                }
            }

        }



        private string GetSaveFilePath(string filter, string title)
        {
            //this is used to let the user choose where to save their image, then return the path
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = filter;
            saveFileDialog.Title = title;

            if (saveFileDialog.ShowDialog() == true)
            {
                return saveFileDialog.FileName;
            }
            else { return null; }
        }
        private void LoadSprayPaint(string filePath)
        {
            if (File.Exists(filePath))
            {
                Debug.WriteLine(filePath + "in load spray paint");
                string json = File.ReadAllText(filePath);
                sprayPaintData = JsonConvert.DeserializeObject<SprayPaintData>(json);

                // Add loaded ellipses to ImageControl
                if (sprayPaintData != null)
                {
                    foreach (var ellipseData in sprayPaintData.Ellipses)
                    {
                        System.Windows.Shapes.Ellipse smallEllipse = new System.Windows.Shapes.Ellipse
                        {
                            Width = 3,
                            Height = 3,
                            Fill = ellipseData.EllipseColor,
                            Margin = new Thickness(ellipseData.X, ellipseData.Y, 0, 0),
                            Opacity = ellipseData.Opacity
                        };

                        ImageControl.Children.Add(smallEllipse);
                    }
                }
            }
            else
            {
                Debug.WriteLine("file does not exist!!" + filePath);
            }
        }


        private void SprayPaintFaces(Emgu.CV.Mat image, System.Drawing.Rectangle[] faces)
        {
            SolidColorBrush PaintColor = new SolidColorBrush();
            PaintColor.Color = System.Windows.Media.Color.FromRgb((byte)RedSlider.Value, (byte)GreenSlider.Value, (byte)BlueSlider.Value);

            foreach (var face in faces)
            {
                System.Windows.Shapes.Ellipse faceEllipse = new()
                {
                    Width = face.Width,
                    Height = face.Height,
                    Fill = PaintColor,
                    Margin = new Thickness(face.X + (face.Width / 2) / 2, face.Y + (face.Height / 2) / 2, 0, 0),

                    Opacity = 1.0
                };

                ImageControl.Children.Add(faceEllipse);
                sprayPaintData.Ellipses.Add(new SprayEllipse
                {
                    X = face.X,
                    Y = face.Y,
                    Width = face.Width,
                    Height = face.Height,
                    Opacity = OpacitySlider.Value,
                    EllipseColor = PaintColor
                });
            }
        }
        private void RedactFaces_Click(object sender, RoutedEventArgs e)
        {
            using (var image = CvInvoke.Imread(fileNameExtension))
            {
                if (image.IsEmpty)
                {
                    MessageBox.Show("Failed to load the image.");
                    return;
                }
                CvInvoke.Resize(image, image, new System.Drawing.Size((int)Image.Width, (int)Image.Height));

                using (var gray = new Emgu.CV.UMat())
                {
                    CvInvoke.CvtColor(image, gray, ColorConversion.Bgr2Gray);
                    // Detect faces
                    var faces = _faceCascade.DetectMultiScale(gray, 1.1, 10, new System.Drawing.Size(20, 20), System.Drawing.Size.Empty);
                    var faces2 = _faceCascadeSide.DetectMultiScale(gray, 1.1, 10, new System.Drawing.Size(20, 20), System.Drawing.Size.Empty);
                    SprayPaintFaces(image, faces);
                    SprayPaintFaces(image, faces2);
                    Debug.WriteLine("this finished");
                }
            }
        }
        private void LoadImage_Click(object sender, RoutedEventArgs e)
        {

            //Resetting the canvas and saving a copy of the image being edited
            //This is only done when there is already an image loaded in and loadimage_click is called again
            if (modifiedImage is BitmapImage bitmapImage && bitmapImage.UriSource != null)
            {
                //creating paths for the image file and ellipses JSON
                string sprayPaintFilePath = System.IO.Path.Combine(Environment.CurrentDirectory, $"{filename}-edited.json");
                SaveSprayPaint(sprayPaintFilePath);
                string ImagePaintFilePath = System.IO.Path.Combine(Environment.CurrentDirectory, $"{filename}-painted.png");
                SaveModifiedImage(ImagePaintFilePath);
                //resetting the canvas
                Image.Source = null;
                EraseAll_Click(sender, e);
            }
            //opening file to be loaded in
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All Files|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                BitmapImage OriginalImage = new BitmapImage(new Uri(openFileDialog.FileName));
                Image.Source = OriginalImage;
                modifiedImage = new BitmapImage(new Uri(openFileDialog.FileName));



            }

            if (openFileDialog.FileName != "")
            {
                fileNameExtension = System.IO.Path.GetFullPath(openFileDialog.FileName);
                System.Diagnostics.Debug.WriteLine(fileNameExtension);
                filename = System.IO.Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                string EllipsesFile = System.IO.Path.Combine(Environment.CurrentDirectory, $"{filename}-edited.json");
                Debug.WriteLine(EllipsesFile + "is the path in loadimage_click");
                LoadSprayPaint(EllipsesFile);
                Debug.WriteLine("first file name: " + filename);
                ImageControl.Width = Image.Width;
                ImageControl.Height = Image.Height;
            }
        }

        private void ImageControl_MouseDown(object sender, MouseButtonEventArgs e)
        {

            System.Windows.Point position = e.GetPosition(ImageControl);
            if (isErasing)
            {
                EraseAtPosition(position);
            }
            else
            {
                SprayPaint(position);
            }
        }



        private void ImageControl_MouseMove(object sender, MouseEventArgs e)
        {
            System.Windows.Point position = e.GetPosition(ImageControl);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (isErasing)
                {
                    EraseAtPosition(position);
                }
                else
                {
                    SprayPaint(position);
                }
            }
        }


        private void EraseAll_Click(object sender, RoutedEventArgs e)
        {

            //erase all button, it clears out the list of ellipses and removes all spray paint from the image canvas
            sprayPaintData.Ellipses.Clear();

            // Create a list to hold non-ellipse children that you want to keep
            var BlankImageControl = ImageControl.Children.OfType<Border>().FirstOrDefault();
            // Clear all children
            ImageControl.Children.Clear();

            // Add back the non-ellipse children
            if (BlankImageControl != null)
            {
                //BlankImageControl.Child = null;
                ImageControl.Children.Add(BlankImageControl);
            }

        }
        private HitTestResultBehavior HitTestCallback(HitTestResult result)
        {
            //checking if the result is an ellipse
            if (result.VisualHit is System.Windows.Shapes.Ellipse ellipse)
            {
                //removing the ellipse from the image canvas
                ImageControl.Children.Remove(ellipse);
                //creating a point to get the point of the ellipse, then using it to remove the ellipse from the sprayPaintData list
                System.Windows.Point ellipsePosition = new System.Windows.Point(Canvas.GetLeft(ellipse), Canvas.GetTop(ellipse));
                //checking for nulls
                if (sprayPaintData != null)
                {
                    //looking in sprayPaintData for ellipses that should be removed
                    SprayEllipse sprayEllipseToRemove = sprayPaintData.Ellipses.FirstOrDefault(e => e.X == ellipsePosition.X && e.Y == ellipsePosition.Y);
                    if (sprayEllipseToRemove != null)
                    {
                        sprayPaintData.Ellipses.Remove(sprayEllipseToRemove);
                    }
                }
            }

            return HitTestResultBehavior.Continue;
        }
        private void EraseAtPosition(System.Windows.Point position)
        {
            //using half of the eraser to create a rectangle shape for the eraser
            //halferaser is used to minimize the number of divides
            double halfEraser = EraserSizeSlider.Value / 2;
            System.Windows.Rect hitTestRect = new System.Windows.Rect(position.X - halfEraser, position.Y - halfEraser, EraserSizeSlider.Value, EraserSizeSlider.Value);
            //using a hit test to remove ellipses in boundaries of eraser
            GeometryHitTestParameters hitTestParams = new GeometryHitTestParameters(new RectangleGeometry(hitTestRect));
            HitTestResultCallback callback = new HitTestResultCallback(HitTestCallback);
            VisualTreeHelper.HitTest(ImageControl, null, callback, hitTestParams);
        }
        private void SprayPaint(System.Windows.Point position)
        {

            //SolidColorBrush PaintColor = new SolidColorBrush();
            PaintColor.Color = System.Windows.Media.Color.FromRgb((byte)RedSlider.Value, (byte)GreenSlider.Value, (byte)BlueSlider.Value);
            //this is the number of small ellipses used for the spray paint effect
            int numSmallEllipses = 10 + (15 - (int)DensitySlider.Value);
            for (int i = 0; i < numSmallEllipses; i++)
            {
                //math to randomly distribute smaller circle in the shape of a larger circle
                //double angleOffset = random.NextDouble() * Math.PI;
                //using the offset to distribute in a circle shape
                double angle = (Math.PI * i) / numSmallEllipses + (random.NextDouble() * Math.PI);
                //brushdensity is multiplied by 3 and subtracted from the maximum brush size to adjust the overall size of the distribution of the circles
                //while it is smaller, the circles are more compact, and if it is bigger they are less compact
                double xOffset = position.X + random.Next(1, (int)(33 - DensitySlider.Value * 3)) * Math.Cos(angle);
                double yOffset = position.Y + random.Next(1, (int)(33 - DensitySlider.Value * 3)) * Math.Sin(angle);
                //creating ellipse that is a circle to be added to the canvas and spraypaintdata list
                System.Windows.Shapes.Ellipse smallEllipse = new()
                {
                    Width = 7,
                    Height = 7,
                    Fill = PaintColor,
                    Margin = new Thickness(xOffset, yOffset, 0, 0),
                    Opacity = OpacitySlider.Value
                };
                //adding this to the image canvas
                ImageControl.Children.Add(smallEllipse);
                //adding these to the list of ellipses that will be saved into a JSON file
                sprayPaintData.Ellipses.Add(new SprayEllipse
                {
                    X = xOffset,
                    Y = yOffset,
                    Opacity = OpacitySlider.Value,
                    EllipseColor = PaintColor
                });
            }
        }

        //Constructors for different buttons
        private void DensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { }
        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { }
        private void RedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { }
        private void GreenSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { }
        private void BlueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { }
        private void EraserSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { }
        private void EraseButton_Click(object sender, RoutedEventArgs e)
        {
            //erase is a toggle button, click it to change from spray paint to erase
            if (!isErasing)
            {
                ImageControl.Cursor = Cursors.IBeam;
            }
            else
            {
                ImageControl.Cursor = Cursors.Pen;
            }
            isErasing = !isErasing;
        }
    }
}
