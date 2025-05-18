using System.Windows;

namespace SampleWpf;

/// <summary>
/// Interaction logic for AddSubscriptionWindow.xaml
/// </summary>
public partial class AddSubscriptionWindow : Window
{
    readonly Siprix.SubscriptionModel subscr_;
    readonly Siprix.ObjModel objModel_;
    public AddSubscriptionWindow(Siprix.ObjModel objModel)
    {
        InitializeComponent();
        Owner = App.Current.MainWindow;
        subscr_ = Siprix.SubscriptionModel.BLF();
        objModel_ = objModel;

        //Set data to controls
        cbAccounts.DataContext = objModel_.Accounts;

        //Set controls state
        bool hasAccounts = (objModel_.Accounts.Collection.Count > 0);
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
        int err = objModel_.Subscriptions.Add(subscr_);
        if (err != Siprix.ErrorCode.kNoErr)
        {
            tbErrText.Text = objModel_.ErrorText(err);
            return;
        }

        this.DialogResult = true;
    }
}
