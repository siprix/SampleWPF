using Siprix;
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

namespace SampleWpf
{
    /// <summary>
    /// Interaction logic for AddCallWindow.xaml
    /// </summary>
    public partial class AddCallWindow : Window
    {
        readonly DestData data_;
        public AddCallWindow()
        {
            InitializeComponent();
            Owner = App.Current.MainWindow;
            data_ = new Siprix.DestData();

            //Set data to controls
            cbAccounts.DataContext = Siprix.ObjModel.Instance.Accounts;

            //Set controls state
            bool hasAccounts = (Siprix.ObjModel.Instance.Accounts.Collection.Count > 0);
            btnOK.IsEnabled = hasAccounts;
            cbAccounts.IsEnabled = hasAccounts;
            tbErrText.Visibility = hasAccounts ? Visibility.Collapsed : Visibility.Visible;

            txDestExt.Focus();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            //Check empty
            if ((txDestExt.Text.Length == 0)||
                (cbAccounts.SelectedItem==null)) return;

            //Get data from controls
            data_.ToExt = txDestExt.Text;
            data_.FromAccId = ((AccountModel)cbAccounts.SelectedItem).ID;
            data_.WithVideo = (cbWithVideo.IsChecked==null) ? false : (bool)cbWithVideo.IsChecked;

            //Try to make call
            int err = Siprix.ObjModel.Instance.Calls.Invite(data_);
            if (err != Module.kNoErr)
            {
                tbErrText.Text = Siprix.ObjModel.Instance.ErrorText(err);
                return;
            }

            this.DialogResult = true;
        }
    }
}
