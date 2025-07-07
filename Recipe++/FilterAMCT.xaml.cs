using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using TitanLoader;

namespace Recipe__
{
    /// <summary>
    /// Interaction logic for FilterAMCT.xaml
    /// </summary>
    public partial class FilterAMCT : Window
    {
        DataTable fullDataTable;
        DataTable filteredDataTable;
        Dictionary<string, List<string>> filterKeys = new Dictionary<string, List<string>>();
        Dictionary<string, List<string>> filterValuesAll = new Dictionary<string, List<string>>();
        Dictionary<string, StackPanel> filterPanels = new Dictionary<string, StackPanel>();
        Dictionary<string, Func<object, RoutedEventArgs>> filter2Funct = new Dictionary<string, Func<object, RoutedEventArgs>>();
        AMCT parentWindow = Application.Current.Windows.Cast<Window>().FirstOrDefault(window => window is AMCT) as AMCT;

        string tempCsvPath = @"C:\Temp\lotInfo.csv";

        public FilterAMCT(DataTable dataTable)
        {
            InitializeComponent();

            fullDataTable = dataTable.Copy();
            DataView dataView = new DataView(dataTable);
            List<string> uniqueValues;
            double buttonMarginTop = buttonUse.Margin.Top;
            double buttonHeight = buttonUse.Height;
            double maxPanelHeight = 0;
            double panelHeight = 0;
            RadioButton radioButton = new RadioButton();

            filterKeys.Add("ENTITY", new List<string> { "*" });
            filterKeys.Add("OPERATION", new List<string> { "*"});
            filterKeys.Add("PRODUCT", new List<string> { "*" });
            filterKeys.Add("EVENT", new List<string> { "*" });

            filterValuesAll.Add("ENTITY", new List<string>());
            filterValuesAll.Add("OPERATION", new List<string>());
            filterValuesAll.Add("PRODUCT", new List<string>());
            filterValuesAll.Add("EVENT", new List<string>());

            filterPanels.Add("ENTITY", entityBox);
            filterPanels.Add("OPERATION", operationBox);
            filterPanels.Add("PRODUCT", productBox);
            filterPanels.Add("EVENT", eventBox);

            foreach (string filterKey in filterPanels.Keys)
            {
                uniqueValues = getUniqueValues(dataView, filterKey);
                uniqueValues.Sort();

                filterValuesAll[filterKey] = uniqueValues;
                filterKeys[filterKey] = filterValuesAll[filterKey];

                panelHeight = uniqueValues.Count * 15;
                maxPanelHeight = Math.Max(maxPanelHeight, panelHeight);
                filterPanels[filterKey].Height = panelHeight;

                foreach (string buttonName in uniqueValues)
                {
                    radioButton = new RadioButton() { Content = buttonName, GroupName = filterKey };
                    radioButton.Checked += new RoutedEventHandler((s, e) => RadioButton_Checked(s, e, filterKey));
                    filterPanels[filterKey].Children.Add(radioButton);
                }
            }
            
            Application.Current.MainWindow = this;
            //Application.Current.MainWindow.Height = filterPanels["ENTITY"].Margin.Top + maxPanelHeight + 50;
            //Application.Current.MainWindow.ResizeMode = ResizeMode.NoResize;
        }

        // Handles selection of radio buttons
        private void RadioButton_Checked(object sender, RoutedEventArgs e, string filterKey_)
        {
            // Update filter values
            foreach (RadioButton rb in filterPanels[filterKey_].Children)
            {
                if (rb.IsChecked == true)
                {
                    string filterValue = rb.Content.ToString();

                    //First find the matching filters in this filter group
                    List<string> filteredValues;
                    if (filterValue == "")
                    {
                        filteredValues = new List<string> { "" };
                    }
                    else
                    {
                        var myRegex = new Regex(filterValue.Replace("*", ".*"));
                        filteredValues = filterValuesAll[filterKey_].Where(k => myRegex.IsMatch(k)).ToList();
                    }
                    //MessageBox.Show(filterValue + ":\n" + String.Join("\n", filteredValues));
                    // Now instead of using only the filterValue, use the list of filteredValues for the filtering below
                    // This will require filterKeys to be defined (string -> list of strings), instead of dictionary (string, string)
                    
                    filterKeys[filterKey_] = filteredValues;
                    break;
                }
            }

            // Start with full table and apply all four filters, one at a time
            filteredDataTable = fullDataTable.Copy();
            foreach (string filterKey in filterKeys.Keys)
            {
                //var rows = filteredDataTable.AsEnumerable().Where(row => row.Field<String>(filterKey).Contains(filterValue));
                //MessageBox.Show(filterKey + ":\n" + String.Join("\n", filterKeys[filterKey]));
                
                if (filterKeys[filterKey].Count == 1 & filterKeys[filterKey][0] == "")
                {
                    var rows0 = filteredDataTable.AsEnumerable().Where(row => row.Field<String>(filterKey) == "");
                    if (rows0.Any())
                    {
                        filteredDataTable = rows0.CopyToDataTable();

                    }
                    else
                    {
                        filteredDataTable.Clear();
                        break;
                    }
                }
                else
                {
                    var rows = from row in filteredDataTable.AsEnumerable()
                               where filterKeys[filterKey].Any(x => row.Field<string>(filterKey).Contains(x))
                               select row;

                    if (rows.Any())
                    {
                        filteredDataTable = rows.CopyToDataTable();

                    }
                    else
                    {
                        filteredDataTable.Clear();
                        break;
                    }
                }
            }
            parentWindow.amTable.ItemsSource = filteredDataTable.AsDataView();
        }

        // Handle Use click
        private void Click_Use(object sender, RoutedEventArgs e)
        {
            string lotID = textLot.Text.Trim();

            // If not valid lotID, return
            if (lotID == "")
            {
                return;
            }

            string query = @"/*BEGIN SQL*/
                            SELECT 
                                      lot AS lot
                                     ,product AS product
                                     ,attribute_name AS attribute_name
                                     ,Max(attribute_value) AS attribute_value
                            FROM
                            (
                            SELECT  
                                      lrc.lot AS lot
                                     ,lrc.product AS product
                                     ,la.attribute_name AS attribute_name
                                     ,la.attribute_value AS attribute_value
                            FROM 
                                   F_LOT_RUN_CARD lrc
                            INNER JOIN F_LOT l ON l.lot = lrc.lot
                            LEFT JOIN F_LOTATTRIBUTE la ON la.lot = l.lot AND la.facility = l.facility
                            WHERE
                                          lrc.lot In ('" + lotID + @"')
                             AND la.attribute_name In('SortAlternateRecipe' ,'DSAlternateRecipe') 
                            )
                            GROUP BY
                                      lot
                                     ,product
                                     ,attribute_name
                            /*END SQL*/";
            string dataSource = "D1D_PROD_XEUS";
            try
            {
                DataTable loadTable = UBER.GetDataTable(dataSource, query);
                TitanDataAccess.ExportToCSV(loadTable, tempCsvPath, false);
            }
            catch
            {
                MessageBox.Show("Problem with Lot Info pull.", "Lot query error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // Make sure AMCT csv file exists
            if (File.Exists(tempCsvPath) == false)
            {
                return;
            }

            DataTable lotTable = new DataTable("LotTable");

            // Parse csv file and fill up the data table
            using (StreamReader sr = new StreamReader(tempCsvPath))
            {
                string[] headers = sr.ReadLine().Split(',');
                foreach (string header in headers)
                {
                    lotTable.Columns.Add(header.Replace("_", "__"));
                }
                while (!sr.EndOfStream)
                {
                    string[] thisRow = sr.ReadLine().Split(',');
                    DataRow dr = lotTable.NewRow();
                    for (int i = 0; i < headers.Length; i++)
                    {
                        dr[i] = thisRow[i].Trim('"');
                    }
                    lotTable.Rows.Add(dr);
                }
            }

            string productValue = lotTable.Rows[0]["PRODUCT"] as string;
            CheckRadioButton("PRODUCT", productValue);

            foreach (RadioButton rb in filterPanels["OPERATION"].Children)
            {
                if (rb.IsChecked == true & rb.Content.ToString() == "129839")
                {
                    string expression = "LOT = '" + lotID + "' and ATTRIBUTE__NAME = 'SortAlternateRecipe'";
                    DataRow[] foundRows = lotTable.Select(expression);
                    string SortAlternateRecipe = foundRows[0][3] as string;
                    //if (SortAlternateRecipe != "")
                        CheckRadioButton("EVENT", SortAlternateRecipe);
                }
                if (rb.IsChecked == true & rb.Content.ToString() == "119368")
                {
                    string expression = "LOT = '" + lotID + "' and ATTRIBUTE__NAME = 'DSAlternateRecipe'";
                    DataRow [] foundRows = lotTable.Select(expression);
                    string DSAlternateRecipe = foundRows[0][3] as string;
                    //if (DSAlternateRecipe != "")
                        CheckRadioButton("EVENT", DSAlternateRecipe);
                }
            }
        }

        // Programmatically check a radio button
        private void CheckRadioButton(string filterKey, string filterValue)
        {
            foreach (RadioButton rb in filterPanels[filterKey].Children)
            {
                if (rb.Content.ToString() == filterValue)
                {
                    rb.IsChecked = true;
                    RadioButton_Checked(null, null, filterKey);
                }
                else
                {
                    rb.IsChecked = false;
                }
            }
        }

        // Handle Clear Filter click
        private void Click_ClearFilter(object sender, RoutedEventArgs e)
        {
            List<string> keyList = new List<string>(filterKeys.Keys);
            foreach (string filterKey in keyList)
            {
                filterKeys[filterKey] = filterValuesAll[filterKey]; // Remove the filter.
                foreach (RadioButton rb in filterPanels[filterKey].Children)
                {
                    rb.IsChecked = false; // Uncheck the radio button
                }
            }
            parentWindow.amTable.ItemsSource = fullDataTable.AsDataView();
        }

        // Gets unique strings (entity, product, operation) from a table column
        List<string> getUniqueValues(DataView dataView, string colName)
        {
            DataTable myTable = dataView.ToTable(true, new string[] { colName });
            List<string> uniqueValues = new List<string>();
            foreach (DataRow row in myTable.Rows)
            {
                string tempString = row[colName].ToString();
                if (tempString.Contains("|"))
                {
                    foreach (string str in tempString.Split('|'))
                    {
                        uniqueValues.Add(str);
                    }
                }
                else
                {
                    uniqueValues.Add(tempString);
                }
            }
            return uniqueValues.Distinct().ToList();
        }
    }
}
