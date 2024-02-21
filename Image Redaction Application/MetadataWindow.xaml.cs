using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
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
using System.Windows.Shapes;

namespace Image_Redaction_Application
{
    /// <summary>
    /// Interaction logic for MetadataWindow.xaml
    /// </summary>
    public partial class MetadataWindow : Window
    {
        public MetadataWindow()
        {
            InitializeComponent();
        }

        public void SetMetadataText(string metadata)
        {
            string jpegInfo = "";
            string JfifInfo = "";
            string ExifInfo = "";
            string ICCProfileInfo = "";
            string FileTypeInfo = "";
            string FileInfo = "";
            string Photoshop = "";
            string Huffman = "";

            string[] parsemetadata=metadata.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach(string line in parsemetadata)
            {
                if (line.Length < 7)
                {
                    continue;
                }
                string firstCheck = line.Substring(0, 4);
                string SecondCheck = line.Substring(0, 6);
                if(firstCheck.Equals("JPEG")){
                    jpegInfo += line + "\n";
                }
                else if (firstCheck.Equals("JFIF")){
                    JfifInfo += line + "\n";
                }
                else if (firstCheck.Equals("Exif"))
                {
                    ExifInfo += line + "\n";
                }
                else if (firstCheck.Equals("ICC "))
                {
                    ICCProfileInfo += line + "\n";
                }
                else if (firstCheck.Equals("Phot"))
                {
                    Photoshop += line + "\n";
                }
                else if (firstCheck.Equals("Huff"))
                {
                    Huffman += line + "\n";
                }
                else if(SecondCheck.Equals("File T"))
                {
                    FileTypeInfo += line + "\n";
                }
                else
                {
                    FileInfo += line + "\n";
                } 
            }
            JpegTextBox.Text = jpegInfo;
            JfifTextBox.Text = JfifInfo;
            ExifTextBox.Text = ExifInfo;
            ICCProfileTextBox.Text = ICCProfileInfo;
            FileTypeTextBox.Text = FileTypeInfo;
            FileInfoTextBox.Text = FileInfo;
            PhotoshopTextBox.Text = Photoshop;
            HuffmanTextBox.Text = Huffman;

        }

        private void Describer1_Copy_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
