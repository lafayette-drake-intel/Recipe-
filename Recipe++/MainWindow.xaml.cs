using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Data;
using TitanLoader;
using System.Xml;

namespace Recipe__
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Contains full path of current file
        private string fileName;
        // Contains raw content of current file
        private string fileContent;

        //private string file_name;
        //private string file_content;
        private string updated_file_content;

        // Linked list for containng links in the current file
        private LinkedList<string> linkList = new LinkedList<string> { };
        // Position of current file in the linked list
        private int currentPosition = -1;

        // This is a placeholder for saved file
        private string newFileName = String.Empty;

        // XIU DataTable
        DataTable allXiuTableFiltered = new DataTable("XIUTable");
        public MainWindow()
        {
            InitializeComponent();
            backButton.IsEnabled = false;
            forwardButton.IsEnabled = false;

            Disable10Buttons();
        }

        // Shows Approved / Reject icon
        private void HandleKeyDownEvent(object sender, KeyEventArgs e)
        {
            string imageFile = String.Empty;
            BitmapImage image = new BitmapImage();
            if ((e.Key == Key.A) &&
                (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {

                image.BeginInit();
                Uri uri = new Uri(@"pack://application:,,,/Resources/approved.jpg");
                image.UriSource = uri;
                image.DecodePixelHeight = 1000;
                image.EndInit();
                ImageBrush imageBrush = new ImageBrush(image);
                imageBrush.Stretch = Stretch.None;
                imageBrush.AlignmentX = AlignmentX.Center;
                imageBrush.AlignmentY = AlignmentY.Bottom;
                recipeText.Background = imageBrush;
            }
            if ((e.Key == Key.R) &&
                (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {

                image.BeginInit();
                Uri uri = new Uri(@"pack://application:,,,/Resources/reject.jpg");
                image.UriSource = uri;
                image.DecodePixelHeight = 1000;
                image.EndInit();
                ImageBrush imageBrush = new ImageBrush(image);
                imageBrush.Stretch = Stretch.None;
                imageBrush.AlignmentX = AlignmentX.Center;
                imageBrush.AlignmentY = AlignmentY.Bottom;
                recipeText.Background = imageBrush;
            }
            if ((e.Key == Key.X) &&
                (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                recipeText.Background = Brushes.Lavender;
            }
        }

        // On Enter press in recipe name text field
        public void OnEnterDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                recipeName.Text = recipeName.Text.Trim();
                Boolean result = File.Exists(recipeName.Text);
                if (result == true)
                {
                    // Get the file name
                    fileName = recipeName.Text;

                    currentPosition = 0;

                    // Add first element to linked list
                    linkList.AddFirst(fileName);

                    // Remove all elements after the current
                    while (linkList.Count > currentPosition + 1)
                    {
                        linkList.RemoveLast();
                    }

                    // Update all
                    UpdateAll(fileName);

                    if (currentPosition == 0)
                    {
                        backButton.IsEnabled = false;
                    }
                    else
                    {
                        backButton.IsEnabled = true;
                    }
                    if (currentPosition < linkList.Count - 1)
                    {
                        forwardButton.IsEnabled = true;
                    }
                    else
                    {
                        forwardButton.IsEnabled = false;
                    }

                    // Enable 10 buttons
                    Enable10Buttons();
                }
            }
        }

        // File open
        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".xml";
            dlg.Filter = "XML Files (*.xml)|*.xml";

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Get the file name
                fileName = dlg.FileName;

                currentPosition = 0;

                // Add first element to linked list
                linkList.AddFirst(fileName);

                // Remove all elements after the current
                while (linkList.Count > currentPosition + 1)
                {
                    linkList.RemoveLast();
                }

                // Update all
                UpdateAll(fileName);

                if (currentPosition == 0)
                {
                    backButton.IsEnabled = false;
                }
                else
                {
                    backButton.IsEnabled = true;
                }
                if (currentPosition < linkList.Count - 1)
                {
                    forwardButton.IsEnabled = true;
                }
                else
                {
                    forwardButton.IsEnabled = false;
                }

                // Enable 10 buttons
                Enable10Buttons();
            }
        }

        // Drag and drop file open
        private void OnFileDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            // Get the file name
            fileName = files[0];

            currentPosition = 0;
            // Add first element to linked list
            linkList.AddFirst(fileName);

            // Remove all elements after the current
            while (linkList.Count > currentPosition + 1)
            {
                linkList.RemoveLast();
            }

            // Update all
            UpdateAll(fileName);

            if (currentPosition == 0)
            {
                backButton.IsEnabled = false;
            }
            else
            {
                backButton.IsEnabled = true;
            }
            if (currentPosition < linkList.Count - 1)
            {
                forwardButton.IsEnabled = true;
            }
            else
            {
                forwardButton.IsEnabled = false;
            }

            // Enable 10 buttons
            Enable10Buttons();
        }

        // Given lot ID and sort operation, pull latest recipe from MARS
        private void Amct_Click(object sender, RoutedEventArgs e)
        {
            AMCT amctWindow = new AMCT();
            //amctWindow.Owner = this;
            amctWindow.Show();
        }

        private void Xiu_Click(object sender, RoutedEventArgs e)
        {
            string xiuprefix = ExtractXiuprefix(fileContent);
            if (!String.IsNullOrEmpty(xiuprefix)) {

                xiuprefix = xiuprefix.Substring(10, xiuprefix.Length - 11);
                MessageBox.Show(xiuprefix);
            }
            else {
                MessageBox.Show("No xiuprefix found");
            }
            return;
            List<string> testProgramPaths = ExtractTestProgramPath(fileContent, fileName);
            string viprSetupFile = Path.GetFullPath(testProgramPaths[0] + @"\Modules\TPI_VIPR\InputFiles\vipr.setup");
            //MessageBox.Show(viprSetupFile);
            // Read file content
            List<string> xius = new List<string>();
            if (File.Exists(viprSetupFile))
            {
                try
                {   // If read is successful
                    string viprFileContent = File.ReadAllText(viprSetupFile);
                    foreach (string line in viprFileContent.Split('\n'))
                    {
                        if (line.StartsWith("START XIU"))
                        {
                            string[] words = line.Split();
                            if (words.Length > 2)
                            {
                                xius.Add(line.Split()[2]);
                            }
                        }
                    }
                }
                catch
                {
                    MessageBox.Show("Problem with reading vipr setup file.");
                    return;
                }
            }
            MessageBox.Show("Total XIU patterns found: " + xius.Count.ToString() + "\n" +
                    String.Join("\n", xius));
            PullXius(xiuprefix);
        }

        // Given lot ID and sort operation, pull latest recipe from MARS
        private void Lot2Recipe_Click(object sender, RoutedEventArgs e)
        {
            string recipe = "Recipe Not Found"; // Start with a default value

            // Get lot ID and sort operation from a new window
            LotToRecipe lotWindow = new LotToRecipe();
            lotWindow.Owner = this;
            lotWindow.ShowDialog();
            string lotID = lotWindow.lot.Text.Trim().ToUpper();
            string sortOper = lotWindow.operation.Text.Trim().ToUpper();

            // Lot ID must be 9 character long
            if (lotID.Length != 9)
            {
                return;
            }
            // Lot ID must contain only alphanumeric characters
            if (!lotID.All(char.IsLetterOrDigit))
            {
                return;
            }
            // Lot ID must start with one of five characters
            char[] startChars = new char[] { '4', 'A', 'C', 'D', 'H', 'L', 'N', 'Z' };
            if (!startChars.Contains(lotID[0]))
            {
                MessageBox.Show("Lot ID must start with one of 4, A, C, D, H, L, N, Z");
                return;
            }
            // Only these sort operations are allowed
            string[] allowedOperations = new string[] { "129839", "119368",     // SDS and SDT
                                                        "178475", "178477", "178479", "178481",     // CLT
                                                        "132319", "132320", "132321", "132323", "132324", "132325"      // kappa
            };
            if (!allowedOperations.Contains(sortOper))
            {
                sortOper = "129839";
            }

            // SQL query to pull manifest given a lot ID
            string query = @"/*BEGIN SQL*/
                            SELECT 
                                      lrc.lot AS lot
                                     ,lrc.operation AS operation
                                     ,lrc.oper_short_desc AS oper_short_desc
                                     ,lrc.product AS product
                                     ,lrc.route AS route
                                     ,leh.entity AS entity
                                     ,To_Char(lrc.movein_date,'yyyy-mm-dd hh24:mi:ss') AS movein_date
                                     ,To_Char(lrc.out_date,'yyyy-mm-dd hh24:mi:ss') AS out_date
                                     ,lwr.recipe AS recipe
                            FROM 
                                   F_LOT_RUN_CARD lrc
                            LEFT JOIN F_LOTENTITYHIST leh ON leh.lotoperkey = lrc.lotoperkey
                            LEFT JOIN F_Lot_Wafer_Recipe lwr ON lwr.recipe_id=leh.lot_recipe_id
                            WHERE
                                      lrc.lot In ('" + lotID + @"')
                             AND      lrc.operation In ('" + String.Join("', '", allowedOperations) + @"')
                            /*END SQL*/";
            string dataSource = "D1D_PROD_XEUS";

            DataTable loadTable = UBER.GetDataTable(dataSource, query);

            // Parse pulled data and extract recipe
            try
            {
                // Save query output to a csv file
                TitanDataAccess.ExportToCSV(loadTable, @"C:\Temp\ltlLoadTableTemp.csv", false);
                // Read csv file content
                var dataLines = File.ReadAllText(@"C:\Temp\ltlLoadTableTemp.csv").Split('\n');
                // Search for matching recipe in each line
                foreach (var dataLine in dataLines)
                {
                    // Remove double quotes
                    string thisLine = dataLine.Replace("\"", "");
                    // Find recipe in each line
                    if (thisLine.StartsWith(lotID) && thisLine.Contains(sortOper))
                    {
                        // Split line on comma
                        string[] columns = thisLine.Split(',');
                        string entity = columns[5];
                        string indate = columns[6];
                        string manifest = columns[8];
                        // Can't be recipe
                        if (entity.StartsWith("TFS") || String.IsNullOrWhiteSpace(indate) || String.IsNullOrWhiteSpace(manifest) || manifest == "NULL RECIPE")
                        {
                            continue;
                        }
                        // Yes, this is a recipe
                        else
                        {
                            // Keep replacing so that we get the latest value of recipe
                            recipe = columns[8].Trim();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Fail message
                MessageBox.Show($"Issues with recipe pull.\n {ex}");
            }

            if (File.Exists(recipe))
            {
                // Open document
                recipe = recipe.Replace("/", "\\");
                recipe = recipe.Replace("\\\\alpfile3.al.intel.com\\sdx_ODS", "J:");
                recipe = recipe.Replace("\\\\alpfile3.al.intel.com\\sdx_ods", "J:");
                recipe = recipe.Replace("\\\\alpfile3.al.intel.com\\sdx", "I:");
            }
            fileName = recipe;

            currentPosition = 0;
            // Add first element to linked list
            linkList.AddFirst(fileName);

            // Remove all elements after the current
            while (linkList.Count > currentPosition + 1)
            {
                linkList.RemoveLast();
            }

            // Update all
            UpdateAll(fileName);

            if (currentPosition == 0)
            {
                backButton.IsEnabled = false;
            }
            else
            {
                backButton.IsEnabled = true;
            }
            if (currentPosition < linkList.Count - 1)
            {
                forwardButton.IsEnabled = true;
            }
            else
            {
                forwardButton.IsEnabled = false;
            }

            // Enable 10 buttons ***Check if file exists?***
            if (File.Exists(fileName))
            {
                Enable10Buttons();
            }
            else
            {
                Disable10Buttons();
            }
        }

        // Hander transfer click
        private void Transfer_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show(fileName);
            TransferPaths transferWindow = new TransferPaths(fileName);
            transferWindow.Owner = this;
            transferWindow.Show();
        }

        // Check recipe component paths
        private void Recipe_Click(object sender, RoutedEventArgs e)
        {
            // Replace all the links with clikable links
            ClickifyLinks(fileContent, true);
        }

        // Verify well-formed xml
        private void Xml_Click()
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(fileName);
                recipeText.Inlines.Add(new Run($"\n\nWell-formed Xml!") { Foreground = Brushes.Green, FontWeight = FontWeights.Bold });
            }
            catch (XmlException e)
            {
                string error = e.ToString();
                recipeText.Inlines.Add(new Run($"\n\n" + error + $"\n") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
            }
        }

        // Verify checksum matches
        private void Checksum_Click(object sender, RoutedEventArgs e)
        {
            //Extract checksum
            string extracted_checksum = ExtractChecksum(fileContent);

            // Extract portion of text for which checksum is to be generated
            int split_position = fileContent.IndexOf("/>") + 2;
            string md5_input_string = fileContent.Substring(split_position, fileContent.Length - split_position);

            MD5 md5Hash = MD5.Create();
            bool checksum_match = VerifyMd5Hash(md5Hash, md5_input_string, extracted_checksum);

            //MessageBox.Show(checksum_match.ToString());
            if (checksum_match)
            {
                //recipeText.Text += $"\n\nChecksum is Correct!";
                recipeText.Inlines.Add(new Run($"\nChecksum is Correct!") { Foreground = Brushes.Green, FontWeight = FontWeights.Bold });
            }
            else
            {
                //recipeText.Text += $"\n\nChecksum Does NOT Match!";
                recipeText.Inlines.Add(new Run($"\nChecksum Does NOT Match!") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
            }
        }

        // Check all the GUIDs
        private void GUID_Click(object sender, RoutedEventArgs e)
        {
            List<string> guids = ExtractGUIDs(fileContent);

            // Print total number of GUIDs found
            recipeText.Inlines.Add(new Run($"\n{guids.Count()} GUIDs found.") { Foreground = Brushes.Black });

            // For each GUID, print if it is valid
            bool all_guids_valid = true;
            foreach (var stringGuid in guids)
            {
                Guid newGuid;
                if (Guid.TryParse(stringGuid, out newGuid))
                {
                    //recipeText.Inlines.Add(new Run($"\n\t{stringGuid} is a valid GUID.") { Foreground = Brushes.Green});
                }
                else
                {
                    all_guids_valid = false;
                    recipeText.Inlines.Add(new Run($"\n\t{stringGuid} is NOT a valid GUID.") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
                }
            }
            if (all_guids_valid)
            {
                //recipeText.Text += $"\nAll GUIDs are valid!";
                recipeText.Inlines.Add(new Run($"\nAll GUIDs are valid!") { Foreground = Brushes.Green, FontWeight = FontWeights.Bold });
            }
            else
            {
                //recipeText.Text += $"\nNOT alll GUIDs are valid!";
                recipeText.Inlines.Add(new Run($"\nNOT alll GUIDs are valid!") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
            }

            // Print if all are unique GUIDs
            List<string> guid_set = guids.Distinct().ToList();
            if (guids.Count() == guid_set.Count())
            {
                //recipeText.Text += $"\nAll GUIDs are unique!";
                recipeText.Inlines.Add(new Run($"\nAll GUIDs are unique!") { Foreground = Brushes.Green, FontWeight = FontWeights.Bold });
            }
            else
            {
                //recipeText.Text += $"\nNOT alll GUIDs are unique!";
                recipeText.Inlines.Add(new Run($"\nNOT alll GUIDs are unique!") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
            }
        }

        // Do all the checks
        private void All_Click(object sender, RoutedEventArgs e)
        {
            Recipe_Click(sender, e);
            Xml_Click();
            Checksum_Click(sender, e);
            GUID_Click(sender, e);
        }

        // Verify LTL upload
        private void LTL_Click(object sender, RoutedEventArgs e)
        {
            // Extract and test test program
            List<string> testPrograms = ExtractTestPrograms(fileContent, fileName);
            //if (String.IsNullOrEmpty(testProgram))
            if (testPrograms.Count == 0)
            {
                recipeText.Inlines.Add(new Run("\nNo Test Programs extracted!") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
                return;
            }
            // Start the query
            recipeText.Inlines.Add(new Run($"\nChecking LTL upload for {String.Join(" and ", testPrograms)}\n")
            {
                Foreground = Brushes.Green,
                FontWeight = FontWeights.Bold
            });

            // List of databases
            List<string> dataSources = new List<string> { "D1D_PROD_XEUS", "F24_PROD_XEUS", "F28_PROD_XEUS", "F32_PROD_XEUS" };

            // Flag specifying whether to print header
            Boolean printHeader = true;

            // Run for each database
            foreach (string dataSource in dataSources)
            {
                // Query text
                string query = @"SELECT
                max(date_time_created) as date_last_uploaded,
                min(date_time_created) as date_first_uploaded,
                program_name,
                devrevstep,
                rollup_formula_set_name as rfs_name
                FROM A_DB_SPEC_SCHEDULE DBS, A_DB_SPEC_ATTRIBUTE_VALUE DBSAV, A_DB_SPEC_ATTRIBUTE DBSA
                    WHERE PROGRAM_NAME In ('" + String.Join("', '", testPrograms) + @"')
                    AND(DBSAV.SPEC_ID = DBS.SPEC_ID) AND(DBSA.ATTRIBUTE_ID = DBSAV.ATTRIBUTE_ID)
                GROUP BY program_name, devrevstep, rollup_formula_set_name
                ORDER BY program_name";
                // Run query
                DataTable loadTable = UBER.GetDataTable(dataSource, query);
                try
                {
                    // Save query output to a csv file
                    TitanDataAccess.ExportToCSV(loadTable, @"C:\Temp\ltlLoadTableTemp" + ".csv", false);
                    // Read csv file content
                    var dataLines = File.ReadAllText(@"C:\Temp\ltlLoadTableTemp" + ".csv").Split('\n');
                    foreach (var dataLine in dataLines)
                    {
                        if (String.IsNullOrWhiteSpace(dataLine))
                        {
                            continue;
                        }
                        if (dataLine.ToUpper().StartsWith("DATE") && printHeader == false)
                        {
                            continue;
                        }
                        foreach (var data in dataLine.Trim().Split(','))
                        {
                            recipeText.Inlines.Add(new Run($" {data.Trim()}\t") { Foreground = Brushes.Green, FontSize = 12 });
                        }
                        recipeText.Inlines.Add(new Run($"\n") { Foreground = Brushes.Green, FontSize = 12 });
                        printHeader = false;
                    }
                }
                catch (Exception ex)
                {
                    // Fail message
                    recipeText.Inlines.Add(new Run($"LTL Check Failed:\n {ex}") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
                }
            }
        }

        static string Update_GUIDs(string updated_file_content)
        {
            // Extract the list of GUIDs in the file content
            List<string> guids = ExtractGUIDs(updated_file_content);

            // Replace each GUID with a newly generated one
            foreach (string guid in guids)
            {
                // For GUID update: in case there is a duplicate, replace one at a time.
                var regex = new Regex(guid);
                updated_file_content = regex.Replace(updated_file_content, Guid.NewGuid().ToString().ToUpper(), 1);
            }

            // Return updated file content
            return updated_file_content;
        }

        static string Update_Checksum(string updated_file_content)
        {
            // Extract checksum
            string extracted_checksum = ExtractChecksum(updated_file_content);

            // Generate new checksum
            // Extract portion of text for which checksum is to be generated
            int split_position = updated_file_content.IndexOf("/>") + 2;
            string md5_input_string = updated_file_content.Substring(split_position, updated_file_content.Length - split_position);

            // Hash the input.
            MD5 md5Hash = MD5.Create();
            string hashOfInput = GetMd5Hash(md5Hash, md5_input_string);

            // Replace the checksum
            updated_file_content = Regex.Replace(updated_file_content, extracted_checksum, hashOfInput);

            // Return updated file content
            return updated_file_content;
        }

        // Update author
        static string Update_Author(string input)
        {
            // pattern (without out '?' this pattern is greedy and replaces any following attributes)
            string pattern = "author=\"" + ".*?" + "\"";
            // replacement
            string replacement = "author=\"" + UserPrincipal.Current.EmailAddress + "\"";
            // Define the regular experssion
            Regex rx = new Regex(pattern);
            // Replace pattern with replacement
            string result = rx.Replace(input, replacement);

            // Return updated file content
            return result;
        }

        // Update asof
        static string Update_Asof(string input)
        {
            // pattern
            string pattern = "asof=\"" + ".*" + "\"";
            // replacement
            string replacement = "asof=\"" + DateTime.Now.ToString(new CultureInfo("en-US")) + "\"";
            // Define the regular experssion
            Regex rx = new Regex(pattern);
            // Replace pattern with replacement
            string result = rx.Replace(input, replacement);

            // Return updated file content
            return result;
        }

        // Update all the GUIDs
        private void Update_GUIDs_Click(object sender, RoutedEventArgs e)
        {
            // Update file content and text of recipeText
            fileContent = Update_GUIDs(fileContent);

            // Replace all the links with clikable links
            ClickifyLinks(fileContent);
        }

        // Update the checksum
        private void Update_Checksum_Click(object sender, RoutedEventArgs e)
        {
            // Update file content and text of recipeText
            fileContent = Update_Checksum(fileContent);

            // Replace all the links with clikable links
            ClickifyLinks(fileContent);
        }

        // Generate Beyond Compare text report
        private void Beyond_Compare(object sender, RoutedEventArgs e)
        {
            // Left file
            string leftFile = fileName;
            // Right file
            string rightFile = newFileName;

            // If left, or right, or both files do not exist, retrun
            if (!File.Exists(leftFile) | !File.Exists(rightFile))
            {
                return;
            }

            // Beyond compare exe
            string beyondCompare = @"C:\Program Files (x86)\Beyond Compare 3\BComp";
            // Beyond compare script that will be used later
            string myLine = @"text-report layout:side-by-side options:display-all output-to:%3 output-options:wrap-word,html-color %1 %2";
            string bcScript = @"C:\TEMP\bcscript.txt";
            // Output file
            string myDesktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string outFile = myDesktop + @"\CompareReport.html";
            // Write Beyond Compare script
            using (StreamWriter file = new StreamWriter(bcScript))
            {
                file.WriteLine(myLine);
            }
            // Create command to run in cmd
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = beyondCompare;
            startInfo.Arguments = "@\"" + bcScript + "\" \"" + leftFile + "\" \"" + rightFile + "\" \"" + outFile + "\"";
            // Run command
            Process.Start(startInfo);
        }

        // Handle SaveAs click
        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            // Create SaveFileDialog
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

            // Set filter for file extension and default file extension
            dlg.FileName = "";
            dlg.InitialDirectory = Path.GetDirectoryName(fileName);
            dlg.DefaultExt = ".xml";
            dlg.Filter = "XML Files (*.xml)|*.xml";
            dlg.OverwritePrompt = true;

            // Display SaveFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            // Save the file
            if (result == true)
            {
                // Update file name
                fileName = dlg.FileName;

                // Update Author
                fileContent = Update_Author(fileContent);

                // Update Asof
                fileContent = Update_Asof(fileContent);

                // Update GUIDs
                fileContent = Update_GUIDs(fileContent);

                //Update Checksum
                fileContent = Update_Checksum(fileContent);

                // Replace all the links with clikable links
                ClickifyLinks(fileContent);

                // Write updated file content to the opened file
                File.WriteAllText(fileName, fileContent);
                newFileName = dlg.FileName; // Used in Beyond Compare
                recipeName.Text = dlg.FileName;
            }
        }

        // Handle OpenInNotepad++ click
        private void OpenInNotepad_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(fileName) & File.Exists(fileName))
            {
                Process.Start("notepad++.exe", "\""+fileName+ "\"");
            }
        }

        // Right click to edit identity name 
        // There is a different method to handle hyperlink right click
        private void TextBlock_RightMouseDown(object sender, MouseEventArgs e)
        {
            // Copy file content to a new string
            updated_file_content = fileContent;
            //recipeText.Text = file_content;

            // Replace all the links with clikable links
            ClickifyLinks(fileContent);
            //return;

            string recipe_type = String.Empty; // Sent to EditValue
            string recipe_path = String.Empty; // Received from EditValue

            string lineLeft, lineRight;
            string wordLeft = String.Empty;
            string wordRight = String.Empty;
            string clickedWord = String.Empty;

            // Clicked character position in the text block
            TextBlock textBlock = sender as TextBlock;
            Point mousePoint = Mouse.GetPosition(textBlock);
            TextPointer charPosition = textBlock.GetPositionFromPoint(mousePoint, true);

            // Left portion of text block from clicked character
            string textLeft = charPosition.GetTextInRun(LogicalDirection.Backward);
            // Right portion of text block from clicked character
            string textRight = charPosition.GetTextInRun(LogicalDirection.Forward);

            // If text contains multiple lines, split off the last line
            lineLeft = textLeft.Split('\n').Last();

            // Extract left part of the clicked word
            if (lineLeft.ToLower().Contains("<identity") && lineLeft.ToLower().Contains("name=\""))
            {
                string[] stringSeparators = new string[] { "name=\"" };
                wordLeft = lineLeft.Split(stringSeparators, StringSplitOptions.None).Last();
                if (wordLeft.Contains("\""))
                {
                    wordLeft = String.Empty;
                }
            }

            // Extract right part of the clicked word
            if (!String.IsNullOrEmpty(wordLeft))
            {
                // If text contains multiple lines, split off the first line
                lineRight = textRight.Split('\n').First();
                wordRight = lineRight.Split('\"').First();
            }

            // Hand over to EditValue
            if (!String.IsNullOrEmpty(wordLeft) && !String.IsNullOrEmpty(wordRight))
            {
                // Clicked word
                recipe_path = wordLeft + wordRight;
                
                // Popup window for editing path
                string manifest_dir = Path.GetDirectoryName(fileName);
                EditValue editWindow = new EditValue(recipe_type, recipe_path, manifest_dir, updated_file_content);
                editWindow.Owner = this;
                editWindow.ShowDialog();

                // If we have new name or path, update file content
                string new_path = editWindow.textBox.Text;
                // Update recipe content if necessary
                if (String.IsNullOrEmpty(new_path) | new_path == recipe_path)
                {
                    return;
                }
                else
                {
                    Boolean updateFile = true;
                    if (updateFile == true)
                    {
                        updated_file_content = updated_file_content.Replace(recipe_path, new_path);
                    }
                }
            }

            // Update file contect and text of recipeText
            fileContent = updated_file_content;

            // Replace all the links with clikable links
            ClickifyLinks(fileContent);
        }

        // Extract all the links
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

        // Extract the checksum
        static string ExtractChecksum(string input)
        {
            // Define the regular experssion for text withing double quotes
            Regex rx = new Regex("(?<=\")[\\w]+(?!=\")");

            // Match the regex against the input string
            Match match = rx.Match(input);

            // Convert the match to a string
            string checksum = match.ToString();

            return checksum;
        }

        // Extract all the GUIDs from input string
        static List<string> ExtractGUIDs(string input_string)
        {
            // Extract all the guids in file_content
            List<string> guids = new List<string>();
            foreach (string line in input_string.Split('\n'))
            {
                if (line.Contains("id="))
                {
                    int s_ind = line.IndexOf("id=") + 4;
                    if (s_ind != -1 && line.Length > s_ind + 36)
                    {
                        string rest_line = line.Substring(s_ind, line.Length - s_ind);
                        int s_len = rest_line.IndexOf('"');
                        if (s_len != -1)
                        {
                            string guid = line.Substring(s_ind, s_len);
                            guids.Add(guid);
                        }
                    }
                }
            }
            return guids;
        }

        static string ExtractXiuprefix(string input_string)
        {
            // Extract all the guids in file_content
            string xiuprefix = String.Empty;
            foreach (string line in input_string.Split('\n'))
            {
                if (line.Contains("<ProcessStep "))
                {
                    // Define the regular experssion
                    // Regex rx = new Regex("xiuprefix=\"" + ".*?" + "\"");
                    Regex rx = new Regex("xiuregex=\"" + ".*?" + "\"");

                    // Match the regex against the input string
                    Match match = rx.Match(line);

                    // Convert the match to a string
                    xiuprefix = match.ToString();
                    break;
                }
            }
            return xiuprefix;
        }

        public void PullXius(string xiuprefix)
        {
            if (allXiuTableFiltered.Rows.Count == 0)
            {
                // Query text
                string query = @"SELECT
                *
                From F_Entity
                Where 
                    Entity like 'IC%'
                AND ENTITY_GROUP = 'ProbeCard'";
                string dataSource = "D1D_PROD_XEUS";

                // Run query
                DataTable xiuTable = UBER.GetDataTable(dataSource, query);
                string cols = String.Empty;
                for (int k = 0; k < xiuTable.Columns.Count; k++)
                {
                    cols += xiuTable.Columns[k].ColumnName.ToString() + ",";
                }
                // Use filters
                var rows0 = xiuTable.AsEnumerable()
                    .Where(row => row.Field<String>("GENERAL_TYPE") == "ProbeCard" &&
                                  row.Field<String>("CURRENT_SITE") == "STT" &&
                                  row.Field<String>("PARENT_STATE") != "Archivable" &&
                                  row.Field<String>("PARENT_STATE") != "Retired");
                if (rows0.Any())
                {
                    allXiuTableFiltered = rows0.CopyToDataTable();
                }
                else
                {
                    MessageBox.Show("Filtered Table is empty!");
                    return;
                }
            }

            //string thisXiu = String.Empty;
            //string filteredXius = xiuprefix + ":\n";
            //string pattern = xiuprefix.Replace('?', '.');
            //pattern = pattern.TrimEnd('.');
            //pattern += ".+";
            ////MessageBox.Show(pattern);
            //Regex regex = new Regex(pattern);
            //foreach (DataRow row in allXiuTableFiltered.Rows)
            //{
            //    thisXiu = row["PARENT_ENTITY"].ToString();
            //    if (regex.IsMatch(thisXiu))
            //    {
            //        filteredXius += thisXiu + "\n";
            //    }
            //}
            //MessageBox.Show(filteredXius);

            string thisXiu = String.Empty;
            List<string> filteredXius = new List<string>();
            //string pattern = xiuprefix.Replace('?', '.');
            //pattern = pattern.TrimEnd('.');
            //pattern += ".+";
            //MessageBox.Show(pattern);
            //Regex regex = new Regex(pattern);
            Regex regex = new Regex(xiuprefix);
            foreach (DataRow row in allXiuTableFiltered.Rows)
            {
                thisXiu = row["PARENT_ENTITY"].ToString();
                if (regex.IsMatch(thisXiu))
                {
                    filteredXius.Add(thisXiu);
                }
            }
            if (filteredXius.Count > 0)
            {
                MessageBox.Show("Total XIU found: " + filteredXius.Count.ToString() + "\n" +
                    String.Join("\n", filteredXius));
            }
            else
            {
                MessageBox.Show("No matching XIUs found.");
            }
        }

        //// This method will extract the Test Prgrom names from the manifest string
        //static List<string> ExtractTestPrograms(string input, string manifestpath)
        //{
        //    List<string> testPrograms = new List<string> { };

        //    // If file content is blank
        //    if (String.IsNullOrEmpty(input))
        //    {
        //        return testPrograms;
        //    }
        //    string searchTerm = "directPath=\"";
        //    foreach (string line in input.Split('\n'))
        //    {
        //        // Handle type="TP" line
        //        if (line.Contains("type=\"TP\"") && line.Contains(searchTerm) && line.Contains("\""))
        //        {
        //            int startIndex = line.IndexOf(searchTerm);
        //            int stopIndex = line.LastIndexOf("\"");
        //            string testProgramPath = line.Substring(startIndex + searchTerm.Length, stopIndex - (startIndex + searchTerm.Length));

        //            if (!String.IsNullOrEmpty(testProgramPath) && testProgramPath.Contains("\\") && testProgramPath.Contains("_"))
        //            {
        //                startIndex = testProgramPath.LastIndexOf("\\");
        //                stopIndex = testProgramPath.IndexOf("_");
        //                string testProgram = testProgramPath.Substring(startIndex + 1, stopIndex - (startIndex + 1));
        //                testPrograms.Add(testProgram);
        //            }
        //        }
        //        // Handle type="VPS" line
        //        if (line.Contains("type=\"VPS\""))
        //        {
        //            List<string> links = ExtractLinks(line);
        //            if (links.Count > 0)
        //            {
        //                string vpsPath = Path.GetFullPath(Path.GetDirectoryName(manifestpath) + "\\" + links[0]);
        //                if (File.Exists(vpsPath))
        //                {
        //                    var vpsLines = File.ReadAllText(vpsPath).Split('\n'); ;
        //                    foreach (var vpsLine in vpsLines)
        //                    {
        //                        if (vpsLine.Contains("name=\"TestProgram\""))
        //                        {
        //                            string[] stringSeparators = new string[] { "value=\"" };
        //                            string valueAndTail = vpsLine.Split(stringSeparators, StringSplitOptions.None).Last();
        //                            string testProgram = valueAndTail.Split('\"').First();
        //                            if (!String.IsNullOrEmpty(testProgram))
        //                            {
        //                                testPrograms.Add(testProgram);
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    return testPrograms;
        //}

        // This method will extract the Test Prgrom names from the manifest string
        static List<string> ExtractTestProgramPath(string input, string manifestpath)
        {
            List<string> testProgramPaths = new List<string> { };

            // If file content is blank
            if (String.IsNullOrEmpty(input))
            {
                return testProgramPaths;
            }

            // Now for each line in the manifest
            foreach (string line in input.Split('\n'))
            {
                // Handle type="TP" line
                if (line.IndexOf("ComponentRecipe", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (line.IndexOf("order=", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (line.IndexOf("type=\"TP\"", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            if (line.IndexOf("directPath=\"", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                // Relative TP path in manifest
                                List<string> tpLinks = ExtractLinks(line);
                                if (tpLinks.Count > 0)
                                {
                                    // Full TP path in manifest
                                    string tpPath = Path.GetFullPath(Path.GetDirectoryName(manifestpath) + "\\" + tpLinks[0]);
                                    if (File.Exists(tpPath))
                                    {
                                        // Read TP xml content and extract links
                                        List<string> linksInTpXml = ExtractLinks(File.ReadAllText(tpPath));
                                        List<string> folders = new List<string>();
                                        foreach (string link in linksInTpXml)
                                        {
                                            // Find last folder path from link
                                            string folder = Path.GetDirectoryName(link);
                                            if (Directory.Exists(folder))
                                            {
                                                testProgramPaths.Add(folder);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return testProgramPaths;
        }

        // This method will extract the Test Prgrom names from the manifest string
        static List<string> ExtractTestPrograms(string input, string manifestpath)
        {
            List<string> testPrograms = new List<string> { };

            // If file content is blank
            if (String.IsNullOrEmpty(input))
            {
                return testPrograms;
            }
            foreach (string line in input.Split('\n'))
            {
                // Handle type="TP" line
                if (line.IndexOf("ComponentRecipe", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (line.IndexOf("order=", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (line.IndexOf("type=\"TP\"", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            if (line.IndexOf("directPath=\"", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                // Relative TP path in manifest
                                List<string> tpLinks = ExtractLinks(line);
                                if (tpLinks.Count > 0)
                                {
                                    // Full TP path in manifest
                                    string tpPath = Path.GetFullPath(Path.GetDirectoryName(manifestpath) + "\\" + tpLinks[0]);
                                    if (File.Exists(tpPath))
                                    {
                                        // Read TP xml content and extract links
                                        List<string> links = ExtractLinks(File.ReadAllText(tpPath));
                                        List<string> names = new List<string>();
                                        foreach (string link in links)
                                        {
                                            // Find last folder name from link
                                            string name = Path.GetFileName(Path.GetDirectoryName(link));
                                            names.Add(name);
                                        }
                                        int numNames = names.Distinct().ToList().Count;
                                        // If all folder names are same, assume TP == folder name
                                        if (numNames == 1)
                                        {
                                            testPrograms.Add(names[0]);
                                        }
                                        // If all folder names are not same
                                        else if (numNames > 1)
                                        {
                                            // Extract TPL file name without extension and use that as TP if it is not empty
                                            string name = Path.GetFileNameWithoutExtension(links[0]);
                                            if (!String.IsNullOrEmpty(name))
                                            {
                                                testPrograms.Add(name);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // Handle type="VPS" line
                        else if (line.Contains("type=\"VPS\""))
                        {
                            List<string> links = ExtractLinks(line);
                            if (links.Count > 0)
                            {
                                string vpsPath = Path.GetFullPath(Path.GetDirectoryName(manifestpath) + "\\" + links[0]);
                                if (File.Exists(vpsPath))
                                {
                                    var vpsLines = File.ReadAllText(vpsPath).Split('\n'); ;
                                    foreach (var vpsLine in vpsLines)
                                    {
                                        if (vpsLine.Contains("name=\"TestProgram\""))
                                        {
                                            string[] stringSeparators = new string[] { "value=\"" };
                                            string valueAndTail = vpsLine.Split(stringSeparators, StringSplitOptions.None).Last();
                                            string testProgram = valueAndTail.Split('\"').First();
                                            if (!String.IsNullOrEmpty(testProgram))
                                            {
                                                testPrograms.Add(testProgram);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return testPrograms;
        }

        // Generate the checksum given an input text
        static string GetMd5Hash(MD5 md5Hash, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        // Verify a hash against a string.
        static bool VerifyMd5Hash(MD5 md5Hash, string input, string hash)
        {
            // Hash the input.
            string hashOfInput = GetMd5Hash(md5Hash, input);

            // Create a StringComparer an compare the hashes.
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;

            if (0 == comparer.Compare(hashOfInput, hash))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // This method will search for a specified word (string) starting at a specified position.
        TextPointer FindWordFromPosition(TextPointer position, string word)
        {
            while (position != null)
            {
                if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string textRun = position.GetTextInRun(LogicalDirection.Forward);

                    // Find the starting index of any substring that matches "word".
                    int indexInRun = textRun.IndexOf(word);
                    if (indexInRun >= 0)
                    {
                        position = position.GetPositionAtOffset(indexInRun);
                        break;
                    }
                }
                else
                    position = position.GetNextContextPosition(LogicalDirection.Forward);
            }

            // position will be null if "word" is not found.
            return position;
        }

        // When a link is clicked, naviated to the file
        private void HyperlinkLeftClicked(object sender, RequestNavigateEventArgs e)
        {
            var hyperlink = sender as Hyperlink;
            string fullPath = hyperlink.NavigateUri.LocalPath;

            // Remove all elements after the current
            while (linkList.Count > currentPosition + 1)
            {
                linkList.RemoveLast();
            }

            currentPosition++;
            // Add clicked element to list and update position
            linkList.AddLast(fullPath);
            // Call UpdateAll so everyting is update
            UpdateAll(fullPath);

            if (currentPosition == 0)
            {
                backButton.IsEnabled = false;
            }
            else
            {
                backButton.IsEnabled = true;
            }
            if (currentPosition < linkList.Count - 1)
            {
                forwardButton.IsEnabled = true;
            }
            else
            {
                forwardButton.IsEnabled = false;
            }

            // Mark event handled
            e.Handled = true;
        }

        // Handle recipe / hyperlink right click
        // There is a different method to handle identity name right click
        private void HyperlinkRightClicked(object sender, MouseButtonEventArgs e)
        {
            // Copy file content to a new string
            updated_file_content = fileContent;

            var hyperlink = sender as Hyperlink;
            var run = hyperlink.Inlines.FirstOrDefault() as Run;
            string relativePath = run.Text;
            string recipe_type = "recipe";

            // Highlight all matching paths
            foreach (Inline inline in recipeText.Inlines.ToList())
            {
                Hyperlink hyperlinkx = inline as Hyperlink;
                if (hyperlinkx != null)
                {
                    Run runx = hyperlinkx.Inlines.FirstOrDefault() as Run;
                    if (runx.Text == relativePath)
                    {
                        runx.Background = Brushes.Yellow;
                    }
                }
            }

            // Popup window for editing path
            string basePath = Path.GetDirectoryName(fileName);
            EditValue editWindow = new EditValue(recipe_type, relativePath, basePath, String.Empty);
            editWindow.Owner = this;
            editWindow.ShowDialog();

            string new_path = editWindow.textBox.Text;
            if (String.IsNullOrEmpty(new_path) | new_path == relativePath)
            {
                return;
            }
            else
            {
                Boolean updateFile = true;
                if (!String.IsNullOrEmpty(recipe_type))
                {
                    // Combine manifest_dir and new_path into a full path
                    string component_fullpath = Path.Combine(basePath, new_path);
                    // If path contains double backslashes
                    if (component_fullpath.Contains(@"\\"))
                    {
                        MessageBoxResult result = MessageBox.Show(@"Path contains '\\'. Replace with '\'?", "Confirmation", 
                                                                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            new_path = new_path.Replace(@"\\", @"\");
                        }
                    }
                    // If file does not exist
                    if (!File.Exists(component_fullpath))
                    {
                        MessageBoxResult result = MessageBox.Show("Recipe does not exist. Ok to edit?", "Confirmation", 
                                                                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.No)
                        {
                            updateFile = false;
                        }
                    }
                }
                if (updateFile == true)
                {
                    updated_file_content = updated_file_content.Replace(relativePath, new_path);
                    fileContent = updated_file_content;

                    // Replace all the links with clikable links
                    ClickifyLinks(fileContent);

                    // Find all the links
                    List<string> recipeLinks = new List<string>();
                    foreach (Inline inline in recipeText.Inlines.ToList())
                    {
                        Hyperlink hyperlinkx = inline as Hyperlink;
                        if (hyperlinkx != null)
                        {
                            Run runx = hyperlinkx.Inlines.FirstOrDefault() as Run;
                            recipeLinks.Add(runx.Text);
                        }
                    }

                    // Pick all the duplicate links
                    var duplicateLinks = from x in recipeLinks
                                         group recipeLinks by x into grouped
                                         where grouped.Count() > 1
                                         select grouped.Key;
                    
                    // Highlight all duplicate links
                    foreach (Inline inline in recipeText.Inlines.ToList())
                    {
                        Hyperlink hyperlinkx = inline as Hyperlink;
                        if (hyperlinkx != null)
                        {
                            Run runx = hyperlinkx.Inlines.FirstOrDefault() as Run;
                            if (duplicateLinks.Contains(runx.Text) == true)
                            {
                                runx.Background = Brushes.Cyan;
                            }
                        }
                    }
                }
            }

            // Mark event handled
            e.Handled = true;
        }

        // Handle Back button click (Decrement currentPosition)
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            // Make sure index is not negative
            if (currentPosition <= 0)
            {
                return;
            }
            --currentPosition;
            string fullPath = linkList.ElementAt(currentPosition);
            UpdateAll(fullPath);
            if (currentPosition == 0)
            {
                backButton.IsEnabled = false;
            }
            else
            {
                backButton.IsEnabled = true;
            }
            if (currentPosition < linkList.Count - 1)
            {
                forwardButton.IsEnabled = true;
            }
            else
            {
                forwardButton.IsEnabled = false;
            }
        }

        // Handle Forward button click (Increment currentPosition)
        private void Forward_Click(object sender, RoutedEventArgs e)
        {
            // Make sure index does not exceed length
            if (currentPosition >= linkList.Count - 1)
            {
                forwardButton.IsEnabled = false;
                return;
            }

            ++currentPosition;
            string fullPath = linkList.ElementAt(currentPosition);
            UpdateAll(fullPath);

            if (currentPosition == 0)
            {
                backButton.IsEnabled = false;
            }
            else
            {
                backButton.IsEnabled = true;
            }
            if (currentPosition < linkList.Count - 1)
            {
                forwardButton.IsEnabled = true;
            }
            else
            {
                forwardButton.IsEnabled = false;
            }
        }

        //Replace all the links with clickable links
        private void ClickifyLinks(string inputText, Boolean checkPath=false)
        {
            // Start afresh
            recipeText.Inlines.Clear();

            // Base directory for this file
            string baseDir = Path.GetDirectoryName(fileName);

            // Find all the links in fileContent
            List<string> links = ExtractLinks(fileContent);

            Boolean checkPathPassed = true;

            // For each link in the file
            foreach (string link in links)
            {
                // Combine base path and link into an absolute path
                string fullPath = Path.Combine(baseDir, link);
                
                // HC recipe has (or, used to have) I:\ mapped to I:\recipe, so do replacement here
                if (fullPath.StartsWith("I:\\127"))
                {
                    fullPath = fullPath.Replace("127", "recipe\\127");
                }
                if (fullPath.StartsWith("J:\\127"))
                {
                    fullPath = fullPath.Replace("127", "recipe\\127");
                }

                //if (fullPath.StartsWith("\\\\alpfile15.al.intel.com\\sdx"))
                //{
                //    fullPath = fullPath.Replace("\\\\alpfile15.al.intel.com\\sdx\\", "I:\\");
                //}
                //if (fullPath.StartsWith("\\\\alpfile3.al.intel.com\\sdx"))
                //{
                //    fullPath = fullPath.Replace("\\\\alpfile15.al.intel.com\\sdx_ODS\\", "J:\\");
                //    fullPath = fullPath.Replace("\\\\alpfile15.al.intel.com\\sdx_ods\\", "J:\\");
                //}

                // link is used as separator for string splitting
                string[] stringSeparators = new string[] { link };

                // Split inputText into left and right parts
                string[] leftRight = inputText.Split(stringSeparators, 2, StringSplitOptions.None);

                // Add left part to the text block
                recipeText.Inlines.Add(new Run(leftRight[0]));

                // Turn link into a clickable one
                Hyperlink hyperlink = new Hyperlink();
                // What you see or the blue text
                hyperlink.Inlines.Add(link);
                // The full path behind the blue text
                hyperlink.NavigateUri = new Uri(fullPath, UriKind.RelativeOrAbsolute);
                // Method that handles the left click on the link
                hyperlink.RequestNavigate += HyperlinkLeftClicked;
                // Method that handles the right click on the link
                hyperlink.MouseRightButtonDown += HyperlinkRightClicked;
                // Finally add it to the text block
                recipeText.Inlines.Add(hyperlink);

                if (checkPath)
                {
                    stringSeparators = new string[] { "\n" };
                    leftRight = leftRight[1].Split(stringSeparators, 2, StringSplitOptions.None);
                    recipeText.Inlines.Add(new Run(leftRight[0].TrimEnd()));

                    //if (!fullPath.Contains(@"\\") && File.Exists(fullPath))
                    if (File.Exists(fullPath))
                    {
                        recipeText.Inlines.Add(new Run($" PASS\n") { Foreground = Brushes.Green, FontWeight = FontWeights.Bold });
                    }
                    else
                    {
                        checkPathPassed = false;
                        recipeText.Inlines.Add(new Run($" FAIL\n") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
                    }
                }


                // Assign right part back to inputText for ruther processing
                inputText = leftRight[1];
            }
            // Add rest of the text (containing no link) to recipeText
            recipeText.Inlines.Add(new Run(inputText));

            if (checkPath)
            {
                if (checkPathPassed)
                {
                    recipeText.Inlines.Add(new Run($"\nAll paths are correct!") { Foreground = Brushes.Green, FontWeight = FontWeights.Bold });
                }
                else
                {
                    recipeText.Inlines.Add(new Run($"\nSome paths failed check!") { Foreground = Brushes.Red, FontWeight = FontWeights.Bold });
                }
            }
        }

        // Update attributes and contents after new file open
        private void UpdateAll(string file_name)
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
                // But retry as follows
                if (file_name.StartsWith("I:"))
                {
                    string file_name_ = file_name.Replace("I:", "\\\\alpfile3.al.intel.com\\sdx");
                    try
                    {
                        // If successful, assign content to file_content
                        file_content = File.ReadAllText(file_name_);
                    }
                    catch
                    {
                        // If unsuccessful, file_content will contain "File Not Found."
                        file_content = "File Not Found.";
                    }
                }
                if (file_name.StartsWith("J:"))
                {
                    string file_name_ = file_name.Replace("J:", "\\\\alpfile3.al.intel.com\\sdx_ODS");
                    try
                    {
                        // If successful, assign content to file_content
                        file_content = File.ReadAllText(file_name_);
                    }
                    catch
                    {
                        // If unsuccessful, file_content will contain "File Not Found."
                        file_content = "File Not Found.";
                    }
                }
            }

            // Set the fileName attribute
            fileName = file_name;

            // Set the fileContent attribute
            fileContent = file_content;

            // Set textBox text to fileName
            recipeName.Text = fileName;

            // Replace all the links with clikable links
            ClickifyLinks(fileContent);
        }

        // Disable 10 buttons
        private void Disable10Buttons()
        {
            //recipeButton.IsEnabled = false;
            //checksumButton.IsEnabled = false;
            //guidButton.IsEnabled = false;
            allcheckButton.IsEnabled = false;
            ltlButton.IsEnabled = false;
            //updateGUIDButton.IsEnabled = false;
            //updateChkButton.IsEnabled = false;
            updateRecipeButton.IsEnabled = false;
            openInNotepad.IsEnabled = false;
            transferButton.IsEnabled = false;
            //xiuprefixButton.IsEnabled = false;
        }

        // Enable 10 buttons
        private void Enable10Buttons()
        {
            //recipeButton.IsEnabled = true;
            //checksumButton.IsEnabled = true;
            //guidButton.IsEnabled = true;
            allcheckButton.IsEnabled = true;
            ltlButton.IsEnabled = true;
            //updateGUIDButton.IsEnabled = true;
            //updateChkButton.IsEnabled = true;
            updateRecipeButton.IsEnabled = true;
            openInNotepad.IsEnabled = true;
            transferButton.IsEnabled = true;
            //xiuprefixButton.IsEnabled = true;
        }
    }
}
