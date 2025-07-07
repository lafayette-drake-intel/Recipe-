using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Text.RegularExpressions;
//using System;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Recipe__
{
    /// <summary>
    /// Interaction logic for TransferPaths.xaml
    /// </summary>
    public partial class TransferPaths : Window
    {
        string fileName;
        string fileContent;
        double checkBoxHeight = 25;
        double marginLeft = 10;
        double fixedMarginTop = 80;
        double marginTop = 25 + 80;
        List<CheckBox> checkBoxTree = new List<CheckBox>();
        Regex regex1 = new Regex(Regex.Escape("_"));
        Regex regex2 = new Regex(Regex.Escape("__"));

        // Default source and destination drive mapping
        string SourceDrive = @"\\alpfile3.al.intel.com\sdx\";
        string DestinationDrive = @"\\vmspfsfseg01\SDx1274GDXFR\";

        public TransferPaths(string fileNameIn)
        {
            InitializeComponent();
            InitializeCheckboxes(fileNameIn);
            boxDst.Text = DestinationDrive;
        }

        private void InitializeCheckboxes(string fileNameIn)
        {
            fileName = fileNameIn;
            if (!fileName.StartsWith(SourceDrive))
            {
                SourceDrive = UNCPath(Path.GetPathRoot(fileName));
                fileName = fileName.Replace(Path.GetPathRoot(fileName), SourceDrive);
            }

            boxSrc.Text = SourceDrive;
            //boxDst.Text = DestinationDrive; //Do we need to change destination drive here?

            CheckBox checkBox = new CheckBox() { Content = regex1.Replace(fileName, "__") };
            checkBox.Margin = new Thickness(marginLeft, marginTop, 0, 0);
            checkBox.Checked += Checkbox_Check;

            checkboxGrid.Children.Clear();
            checkboxGrid.Children.Add(checkBox);

            checkBoxTree.Clear();
            checkBoxTree.Add(checkBox);
        }

        // Hander transfer click
        private void Checkbox_Check(object sender, RoutedEventArgs e)
        {
            CheckBox parentCheckBox = sender as CheckBox;
            fileName = parentCheckBox.Content.ToString();
            fileName = regex2.Replace(fileName, "_");
            if (File.Exists(fileName))
            {
                parentCheckBox.Foreground = new SolidColorBrush(Colors.Green);
            }
            else
            {
                parentCheckBox.Foreground = new SolidColorBrush(Colors.Red);
                return;
            }
            int position = checkBoxTree.IndexOf(parentCheckBox);

            // Base directory for this file
            string baseDir = Path.GetDirectoryName(fileName);

            // Update fileContent
            UpdateFileContent(fileName);

            // Find all the links in fileContent
            List<string> links = ExtractLinks(fileContent);

            // For each link in the file
            foreach (string link in links)
            {
                // Combine base path and link into an absolute path
                string fullPath = Path.Combine(baseDir, link);
                fullPath = Path.GetFullPath(fullPath);

                if (!fullPath.StartsWith(SourceDrive))
                {
                    fullPath = fullPath.Replace(Path.GetPathRoot(fullPath), SourceDrive);
                }

                // Create checkbox and add to the grid
                CheckBox checkBox = new CheckBox() { Content = regex1.Replace(fullPath, "__") };
                checkBox.Checked += Checkbox_Check;

                marginLeft = parentCheckBox.Margin.Left + 15;
                marginTop = parentCheckBox.Margin.Top + (position + 1) * checkBoxHeight; // this will be fixed
                checkBox.Margin = new Thickness(marginLeft, marginTop, 0, 0);

                checkboxGrid.Children.Add(checkBox);
                checkBoxTree.Insert(++position, checkBox);
            }

            // Fix vertical position of the check boxes
            marginTop = checkBoxHeight + fixedMarginTop;
            for (int pos=0; pos < checkBoxTree.Count; pos++)
            {   
                checkBoxTree[pos].Margin = new Thickness(checkBoxTree[pos].Margin.Left, marginTop, 0, 0);
                marginTop += checkBoxHeight;
            }

            Application.Current.MainWindow = this;
            if (marginTop > Application.Current.MainWindow.Height)
            {
                Application.Current.MainWindow.Height = marginTop + checkBoxHeight + fixedMarginTop;
            }
        }

        // Transfer (copy over) selected files
        private void TransferButton_Click(object sender, RoutedEventArgs e)
        {
            string sourceFileName;
            string destFileName;
            string destFolder;
            foreach (CheckBox checkBox in checkBoxTree)
            {
                if (checkBox.IsChecked == true)
                {
                    int position = checkBoxTree.IndexOf(checkBox);
                    sourceFileName = checkBox.Content.ToString();
                    sourceFileName = regex2.Replace(sourceFileName, "_");
                    if (!File.Exists(sourceFileName))
                    {
                        MessageBox.Show("Source file\n" + sourceFileName + "\ndoesn't exist!");
                        continue;
                    }
                    destFileName = sourceFileName.Replace(SourceDrive, DestinationDrive);
                    if (File.Exists(destFileName))
                    {
                        MessageBox.Show("Destination file\n" + destFileName + "\nalready exists!");
                        continue; // Might want to do beyond compare insteady of simply exiting
                    }
                    destFolder = Path.GetDirectoryName(destFileName);
                    if (!Directory.Exists(destFolder))
                    {
                        MessageBoxResult result = MessageBox.Show("Destination folder " + destFolder + "\ndoesn't exit!\nCreate it?", 
                            "Folder doesn't exist", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            // Create the folder
                            Directory.CreateDirectory(destFolder);
                        }
                        else // No selected
                        {
                            continue;
                        }
                    }
                    File.Copy(sourceFileName, destFileName);
                }
            }
            MessageBox.Show("Done.");
        }

        static List<string> ExtractLinks(string inputText)
        {
            // Pattern to match a link
            string pattern = @"["">]([^@<>\""]*\.[a-zA-Z]\w{1,4})[<""]";

            // List of links
            List<string> links = new List<string> { };
            foreach (Match match in Regex.Matches(inputText, pattern))
            {
                // Add this link to the list
                string link = match.Value.TrimStart('\"').TrimStart('>').TrimEnd('\"').TrimEnd('<');
                // if check added on 5/24/2018. This prevents simple file names from being treatd as a path
                if (link.Contains(@"\") | link.Contains(@"/"))
                {
                    links.Add(link);
                }
            }

            return links;
        }

        // Update attributes and contents after new file open
        private void UpdateFileContent(string file_name)
        {
            // Read the file content
            string file_content;
            // Read file content
            try
            {   // If successful, assign content to file_content
                file_content = File.ReadAllText(file_name);
            }
            catch
            {   // If unsuccessful, file_content will contain "File Not Found."
                file_content = "File Not Found.";
                //// But retry as follows
                //if (file_name.StartsWith(@"I:\"))
                //{
                //    string file_name_ = file_name.Replace(@"I:\", SourceDrive);
                //    try
                //    {
                //        // If successful, assign content to file_content
                //        file_content = File.ReadAllText(file_name_);
                //    }
                //    catch
                //    {
                //        // If unsuccessful, file_content will contain "File Not Found."
                //        file_content = "File Not Found.";
                //    }
                //}
            }

            // Set the fileContent attribute
            fileContent = file_content;
        }

        // Mapped drive to UNC path
        // Copied from https://stackoverflow.com/questions/2067075/how-do-i-determine-a-mapped-drives-actual-path
        public static string UNCPath(string path)
        {
            if (!path.StartsWith(@"\\"))
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Network\\" + path[0]))
                {
                    if (key != null)
                    {
                        return key.GetValue("RemotePath").ToString() + path.Remove(0, 2).ToString();
                    }
                }
            }
            return path;
        }

        // Select Source File (and Folder)
        private void SrcBrowse_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.InitialDirectory = Path.GetDirectoryName(fileName);

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".xml";
            dlg.Filter = "XML Files (*.xml)|*.xml";
            
            // Get the selected file name and display in a TextBox 
            if (dlg.ShowDialog() == true)
            {
                // Update file name and re-initialize checkboxes
                fileName = dlg.FileName;
                InitializeCheckboxes(fileName);
            }
        }

        // Select Destination Folder
        private void DstBrowse_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog dlg = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog();
            dlg.Title = "Pick Destination Folder";
            dlg.Multiselect = false;
            dlg.IsFolderPicker = true;
            dlg.ShowPlacesList = true;
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureValidNames = true;
            dlg.InitialDirectory = DestinationDrive;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                // Update destination folder
                var folder = dlg.FileName;
                DestinationDrive = UNCPath(Path.GetPathRoot(folder));
                boxDst.Text = DestinationDrive;
            }
        }
    }
}
