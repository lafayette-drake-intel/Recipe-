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
using System.Windows.Shapes;

namespace Recipe__
{
    /// <summary>
    /// Interaction logic for LotToRecipe.xaml
    /// </summary>
    public partial class LotToRecipe : Window
    {
        public LotToRecipe()
        {
            InitializeComponent();
        }

        private void OnEnterDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Close();
            }
        }

        private void PullRecipe_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, RoutedEventArgs e)
        {
            lot.Text = "";
            Close();
        }

        private void Lot_GotFocus(object sender, RoutedEventArgs e)
        {
            if (lot.Text == "LotID")
            {
                lot.Text = String.Empty;
            }
        }

        private void Lot_LostFocus(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(lot.Text))
            {
                lot.Text = "LotID";
            }
        }

        //private void Oper_GotFocus(object sender, RoutedEventArgs e)
        //{
        //    if (operation.Text == "Operation")
        //    {
        //        operation.Text = String.Empty;
        //    }
        //}
        //private void Oper_LostFocus(object sender, RoutedEventArgs e)
        //{
        //    if (String.IsNullOrEmpty(operation.Text))
        //    {
        //        operation.Text = "Operation";
        //    }
        //}
    }
}
