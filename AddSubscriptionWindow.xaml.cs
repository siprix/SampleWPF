using System.Windows;

namespace SampleWpf
{
    /// <summary>
    /// Interaction logic for AddSubscriptionWindow.xaml
    /// </summary>
    public partial class AddSubscriptionWindow : Window
    {
        readonly Siprix.SubscriptionModel subscr_;
        public AddSubscriptionWindow()
        {
            InitializeComponent();
            Owner = App.Current.MainWindow;
            subscr_ = Siprix.SubscriptionModel.BLF();

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
            subscr_.AccId = ((Siprix.AccountModel)cbAccounts.SelectedItem).ID;
            subscr_.Label = tbLabel.Text;

            //Try to make call
            int err = Siprix.ObjModel.Instance.Subscriptions.Add(subscr_);
            if (err != Siprix.Module.kNoErr)
            {
                tbErrText.Text = Siprix.ObjModel.Instance.ErrorText(err);
                return;
            }

            this.DialogResult = true;
        }
    }
}
