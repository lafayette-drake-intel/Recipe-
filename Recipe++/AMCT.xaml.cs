using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using TitanLoader;

namespace Recipe__
{
    /// <summary>
    /// Interaction logic for AMCT.xaml
    /// </summary>
    public partial class AMCT : Window
    {
        DataTable dataTable;
        FilterAMCT filterWindow;
        string amctCsvPath = @"C:\Temp\AmctTemp.csv";
        MainWindow mainWindow = Application.Current.Windows.Cast<Window>().FirstOrDefault(window => window is MainWindow) as MainWindow;
        string selectedRadioButton = "SDX";

        public AMCT()
        {
            InitializeComponent();

            radioButtonSDX.IsChecked = true;
            DisplayTable();
            clearFilter.IsEnabled = false;

        }

        // Display AMCT
        private void DisplayTable()
        {
            // Make sure AMCT csv file exists
            if (File.Exists(amctCsvPath) == false)
            {
                return;
            }

            dataTable = new DataTable("AMCT");

            // Parse csv file and fill up the data table
            using (StreamReader sr = new StreamReader(amctCsvPath))
            {
                // Read column headers
                string[] headers = sr.ReadLine().Split(',');
                foreach (string header in headers)
                {
                    dataTable.Columns.Add(header.Replace("_", "__"));
                }

                // Read values
                while (!sr.EndOfStream)
                {
                    string[] rows = sr.ReadLine().Split(',');
                    DataRow dr = dataTable.NewRow();
                    for (int i = 0; i < headers.Length; i++)
                    {
                        dr[i] = rows[i].Trim('"');
                    }
                    dataTable.Rows.Add(dr);
                }

                // Use SDX/HOP/All filter
                //var rows0 = dataTable.AsEnumerable().Where(row => row.Field<String>("PATH").Contains(selectedRadioButton));
                var rows0 = dataTable.AsEnumerable().Where(row => row.Field<String>("MODELNAME").StartsWith(selectedRadioButton));
                if (rows0.Any())
                {
                    dataTable = rows0.CopyToDataTable();

                }
                else
                {
                    dataTable.Clear();
                    return;
                }

                // Remove some not-so-important columns
                dataTable.Columns.Remove("PATH");
                dataTable.Columns.Remove("PROCESS");
                dataTable.Columns.Remove("MODELNAME");
                dataTable.Columns.Remove("OBJECTNAME");
            }

            // Show the table
            amTable.ItemsSource = dataTable.AsDataView();

            // Get the modification time and print it
            DateTime dt = File.GetLastWriteTime(amctCsvPath);
            string csvWriteTime = dt.ToShortDateString() + " " + dt.ToLongTimeString();
            timeLabel.Content = "AMCT pull time: " + csvWriteTime;
        }

        private void Recipe_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;
            while ((dep != null) && !(dep is DataGridCell))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep == null)
                return;

            if (dep is DataGridCell)
            {
                DataGridCell cell = dep as DataGridCell;
                DataGridBoundColumn col = cell.Column as DataGridBoundColumn;
                Binding binding = col.Binding as Binding;
                string columnName = binding.Path.Path;
                string columnValue = cell.ToString();
                if (columnName.ToUpper() == "RECIPE" && columnValue.Contains(":"))
                {
                    columnValue = cell.ToString().Split(':')[1];
                    columnValue = columnValue.Trim();
                    string recipeName = @"I:\recipe\" + columnValue;
                    if (!recipeName.ToLower().EndsWith(".xml"))
                    {
                        recipeName += ".xml";
                    }
                    //MessageBox.Show(recipeName);
                    mainWindow.recipeName.Text = recipeName;

                    var key = Key.Enter;
                    var target = mainWindow.recipeName;
                    var routedEvent = Keyboard.KeyDownEvent;

                    target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(target), 0, key) { RoutedEvent = routedEvent });
                    mainWindow.allcheckButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    mainWindow.Activate(); // project does not compile in new laptop
                }
            }

        }

        // Handle Pull button click
        private void Click_PullAmct(object sender, RoutedEventArgs e)
        {
            // Disable Filter button
            buttonFilter.IsEnabled = false;

            // SQL query to pull manifest given a lot ID
            string query = @"/*BEGIN SQL*/
                            SELECT 
                                      am5.am_ldr_path AS path
                                     ,am5.am_ldr_process AS process
                                     ,am5.am_ldr_modelname AS modelname
                                     ,am5.am_ldr_objectname AS objectname
                                     ,am5.row_id AS row_id
                                     ,am5.row_order AS row_order
                                     ,am5.operation AS operation
                                     ,am5.entity AS entity
                                     ,am5.product AS product
                                     ,am5.route AS route
                                     /*,am5.chamber AS chamber*/
                                     /*,am5.has_sif AS has_sif*/
                                     /*,am5.recipe_root AS recipe_root*/
                                     /*,am5.recipe_name AS recipe_name*/
                                     ,REGEXP_SUBSTR( am5.parameter_list , 'EVENT=([^;]*)', 1, 1,'i',1) AS event
                                     ,am5.recipe AS recipe
                                     /*,am5.testname AS testname*/
                                     /*,am5.ct_file AS ct_file*/
                                     ,REGEXP_SUBSTR( am5.parameter_list , 'SET_SIU=([^;]*)', 1, 1,'i',1) AS set_siu
                                     ,REGEXP_SUBSTR( am5.parameter_list , 'CTFILE=([^;]*)', 1, 1,'i',1) AS ctfile
                                     /*,am5.parameter_list AS parameter_list*/
                                     ,Replace(Replace(Replace(Replace(Replace(Replace(am5.comments,',',';'),chr(9),' '),chr(10),' '),chr(13),' '),chr(34),''''),chr(7),' ') AS comments
                            FROM 
                                   F_AM_F3 am5
                            WHERE
                                   am5.am_ldr_path LIKE '%DieSort%'
                               AND am5.am_ldr_process != '1272'
                            /*END SQL*/";
            string dataSource = "D1D_PROD_XEUS";
            try
            {
                DataTable loadTable = UBER.GetDataTable(dataSource, query);
                TitanDataAccess.ExportToCSV(loadTable, amctCsvPath, false);
            }
            catch
            {
                MessageBox.Show("Problem with AMCT pull.", "AMCT pull error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            DisplayTable();

            // Reenable Filter button
            buttonFilter.IsEnabled = true;
        }

        // Handle Filter button click
        private void Click_Filter(object sender, RoutedEventArgs e)
        {
            filterWindow = new FilterAMCT(dataTable);
            filterWindow.Owner = this;
            filterWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            buttonFilter.IsEnabled = false;
            filterWindow.Closed += new EventHandler(FilterWindow_Closed);
            filterWindow.Show();

            clearFilter.IsEnabled = true;
            radioButtonSDX.IsEnabled = false;
            radioButtonHOP.IsEnabled = false;
            radioButtonAll.IsEnabled = false;
        }

        // Enable Filter button on Filter window close
        private void FilterWindow_Closed(object sender, EventArgs e)
        {
            buttonFilter.IsEnabled = true;
            radioButtonSDX.IsEnabled = true;
            radioButtonHOP.IsEnabled = true;
            radioButtonAll.IsEnabled = true;
        }

        // Handle Clear Filter button click
        private void Click_ClearFilter(object sender, RoutedEventArgs e)
        {
            DisplayTable();
            filterWindow.Close();
            buttonFilter.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            // Update filter values
            RadioButton rb = sender as RadioButton;
            if (rb.IsChecked == true)
            {
                selectedRadioButton = rb.Content.ToString().Split(' ')[0];
                if (selectedRadioButton == "All")
                {
                    selectedRadioButton = "DieSort";
                }
                DisplayTable();
            }
        }
    }
}
