using System.Windows;
using System.Windows.Controls;

namespace SampleWpf;

/// <summary>
/// Interaction logic for AddCallWindow.xaml
/// </summary>
public partial class AddCallWindow : Window
{
    //readonly Siprix.DestData data_;
    //readonly Siprix.ObjModel objModel_;
    readonly CallRecentListControl callRecentListCtrl_;

    public AddCallWindow(Siprix.ObjModel objModel)
    {
        InitializeComponent();
        Owner = App.Current.MainWindow;
        this.Width = App.Current.MainWindow.Width * 0.8;
        this.Height = App.Current.MainWindow.Height * 0.8;

        callRecentListCtrl_ = new CallRecentListControl(objModel, true);
        callRecentListCtrl_.CancelClick += () => this.DialogResult = false;
        MainGrid.Children.Add(callRecentListCtrl_);        

        /*
        data_ = new Siprix.DestData();
        objModel_ = objModel;

        //Set data to controls
        cbAccounts.DataContext = objModel_.Accounts;

        //Set controls state
        bool hasAccounts = (objModel_.Accounts.Collection.Count > 0);
        btnOK.IsEnabled = hasAccounts;
        cbAccounts.IsEnabled = hasAccounts;
        tbErrText.Visibility = hasAccounts ? Visibility.Collapsed : Visibility.Visible;

        txDestExt.Focus();*/
    }
    /*
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
        data_.FromAccId = ((Siprix.AccountModel)cbAccounts.SelectedItem).ID;
        data_.WithVideo = cbWithVideo.IsChecked != null && (bool)cbWithVideo.IsChecked;

        //Try to make call
        int err = objModel_.Calls.Invite(data_);
        if (err != Siprix.ErrorCode.kNoErr)
        {
            tbErrText.Text = objModel_.ErrorText(err);
            return;
        }

        this.DialogResult = true;
    }*/
}
