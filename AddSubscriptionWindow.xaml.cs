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
    /// Interaction logic for AddSubscriptionWindow.xaml
    /// </summary>
    public partial class AddSubscriptionWindow : Window
    {
        readonly SubscriptionModel subscr_;
        public AddSubscriptionWindow()
        {
            InitializeComponent();
            Owner = App.Current.MainWindow;
            subscr_ = SubscriptionModel.BLF();

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
            if ((txDestExt.Text.Length == 0) ||
                (tbLabel.Text.Length == 0) ||
                (cbAccounts.SelectedItem == null)) return;

            //Get data from controls
            subscr_.ToExt = txDestExt.Text;
            subscr_.AccId = ((AccountModel)cbAccounts.SelectedItem).ID;
            subscr_.Label = tbLabel.Text;

            //Try to make call
            int err = Siprix.ObjModel.Instance.Subscriptions.Add(subscr_);
            if (err != Module.kNoErr)
            {
                tbErrText.Text = Siprix.ObjModel.Instance.ErrorText(err);
                return;
            }

            this.DialogResult = true;
        }
    }
}
