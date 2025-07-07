using System;
using System.IO;
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

namespace Recipe__
{
    /// <summary>
    /// Interaction logic for EditValue.xaml
    /// </summary>
    public partial class EditValue : Window
    {
        private string basePath = String.Empty;
        private string browsePath = String.Empty;

        public EditValue(string in_type, string in_path, string base_path, string file_content)
        {
            InitializeComponent();

            // Name update
            if (String.IsNullOrEmpty(in_type))
            {
                this.Title = "Edit name";
                textBlock.Text = "New name";
                goButton.Visibility = Visibility.Hidden;
                //string tp = GetStringBetween(in_path, "_TP", "_HC");
                //string hc = GetStringBetween(in_path, "_HC", "_ALC");
                //string alc = GetStringBetween(in_path, "_ALC", "_DPC");
                //string dpc = GetStringBetween(in_path, "_DPC", "_IL");
                //string ww = GetStringBetween(in_path, "_IL", "_WW");
            }
            // Component recipe update
            else
            {
                textBlock.Text = "New path";
                basePath = base_path;
                updateButton.Visibility = Visibility.Hidden;

                browsePath = Path.Combine(base_path, in_path);
                browsePath = Path.GetFullPath(browsePath);
                browsePath = Path.GetDirectoryName(browsePath);
            }

            textBox.Text = in_path;
        }

        // File open
        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension
            //dlg.InitialDirectory = Path.GetDirectoryName(Path.GetFullPath(basePath + textBox.Text).ToString());
            dlg.DefaultExt = ".xml";
            dlg.Filter = "XML Files (*.xml)|*.xml";
            dlg.InitialDirectory = browsePath;

            // Display OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Absolute path of selected file
                string absolutePath = dlg.FileName;
                // Absolute path now converted to path relative to base path
                string relPath = GetRelativePath(absolutePath, basePath);
                // change text box text to relative path
                textBox.Text = relPath;
            }
        }

        private void updateButton_Click(object sender, RoutedEventArgs e)
        {
            textBox.Text = string.Empty;
            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            textBox.Text = string.Empty;
            Close();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Copied from Rick Strahl's Web Log
        // Given a base path and an absolute path, returns relative path of the latter
        public static string GetRelativePath(string fullPath, string basePath)
        {
            // Require trailing backslash for path
            if (!basePath.EndsWith("\\"))
                basePath += "\\";

            Uri baseUri = new Uri(basePath);
            Uri fullUri = new Uri(fullPath);

            Uri relativeUri = baseUri.MakeRelativeUri(fullUri);

            // Uri's use forward slashes so convert back to backward slashes
            return relativeUri.ToString().Replace("/", "\\");
        }

        // Given a string, extract text between two given sub-strings
        public static string GetStringBetween(string bigString, string from, string to)
        {
            int iFrom = bigString.IndexOf(from) + from.Length;
            int iTo = bigString.IndexOf(to);
            string containedString = bigString.Substring(iFrom, iTo - iFrom);
            MessageBox.Show(containedString);
            return containedString;
        }
    }
}
