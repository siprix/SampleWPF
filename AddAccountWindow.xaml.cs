using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Windows;

namespace SampleWpf;

public partial class AddAccountWindow : Window
{
    readonly bool addNew_;
    readonly Siprix.AccData data_;
    readonly Siprix.ObjModel objModel_;

    public AddAccountWindow(Siprix.ObjModel objModel, Siprix.AccData? data = null)
    {
        InitializeComponent();

        objModel_ = objModel;
        addNew_ = (data == null);
        data_ = data ?? new Siprix.AccData();
        Owner = App.Current.MainWindow;

        //Set data to controls
        tbSipServer.Text       = data_.SipServer;
        tbSipExtension.Text    = data_.SipExtension;
        tbSipPassword.Password = data_.SipPassword;
        tbExpireTime.Text      = data_.ExpireTime.ToString();
        tbDisplayName.Text     = data_.DisplayName;

        cbTransport.ItemsSource = Enum.GetValues(typeof(Siprix.SipTransport));
        cbTransport.SelectedItem = data_.TranspProtocol;

        cbSecureMedia.ItemsSource = Enum.GetValues(typeof(Siprix.SecureMedia));
        cbSecureMedia.SelectedItem = data_.SecureMediaMode;

        AddItemsToBindAddressCombobox();

        //Set controls state
        this.Title = addNew_ ? "Add account" : "Edit account";
        tbSipServer.IsEnabled    = addNew_;
        tbSipExtension.IsEnabled = addNew_;
        tbExpireTime.IsEnabled   = addNew_;
        cbTransport.IsEnabled    = addNew_;

        tbSipServer.Focus();
    }

    private void btnCancel_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;
    }

    private void btnOK_Click(object sender, RoutedEventArgs e)
    {
        //Check empty
        if ((tbSipServer.Text.Length == 0) ||
            (tbSipExtension.Text.Length == 0))
        {
            tbErrText.Text = "Fields `Server (PBX)` and `Extension` can't be empty";
            tbErrText.Visibility = Visibility.Visible;
            return;
        }

        //Get data from controls
        data_.SipServer    = tbSipServer.Text;
        data_.SipExtension = tbSipExtension.Text;
        data_.SipPassword  = tbSipPassword.Password;
        data_.ExpireTime   = (tbExpireTime.Text.Length==0) ? 0 : uint.Parse(tbExpireTime.Text);
        data_.DisplayName  = tbDisplayName.Text;

        if (cbTransport.SelectedItem != null)
            data_.TranspProtocol = (Siprix.SipTransport)cbTransport.SelectedItem;

        if (cbSecureMedia.SelectedItem != null)
            data_.SecureMediaMode = (Siprix.SecureMedia)cbSecureMedia.SelectedItem;

        string? selectedBindAddr = cbBindAddr.SelectedValue?.ToString();
        if (!string.IsNullOrEmpty(selectedBindAddr))
            data_.TranspBindAddr = selectedBindAddr;

        //Try to add/update account
        int err = addNew_ ? objModel_.Accounts.Add(data_) 
                          : objModel_.Accounts.Update(data_);
        if(err != Siprix.ErrorCode.kNoErr)
        {
            tbErrText.Text = objModel_.ErrorText(err);
            tbErrText.Visibility = Visibility.Visible;
            return;
        }

        this.DialogResult = true;
    }

    private void ShowPassword_Click(object sender, RoutedEventArgs e)
    {
        if(tbSipPasswordReveal.Visibility != Visibility.Visible)
        {
            tbSipPasswordReveal.Text = tbSipPassword.Password;
            tbSipPasswordReveal.Visibility = Visibility.Visible;
            tbSipPassword.Visibility = Visibility.Hidden;
        }
        else
        {
            tbSipPassword.Password = tbSipPasswordReveal.Text;
            tbSipPasswordReveal.Visibility = Visibility.Hidden;
            tbSipPassword.Visibility = Visibility.Visible;
        }
    }

    public void AddItemsToBindAddressCombobox()
    {
        cbBindAddr.Items.Add("");

        foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
        {
            if ((item.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                (item.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))&& 
                item.OperationalStatus == OperationalStatus.Up)
            {
                foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        cbBindAddr.Items.Add(ip.Address.ToString());
                    }
                }
            }
        }
    }

    private void btnAddLocalAccount_Click(object sender, RoutedEventArgs e)
    {
        string? selectedBindAddr = cbBindAddr.SelectedValue?.ToString();
        if (string.IsNullOrEmpty(selectedBindAddr))
        {
            tbErrText.Text = "Select `Bind address`";
            tbErrText.Visibility = Visibility.Visible;
            return;
        }

        data_.SipServer = selectedBindAddr;
        data_.TranspPort = 5555;//listen incoming SIP requests on this port
        data_.SipExtension = "noreg";
        data_.SipPassword = "---";
        data_.ExpireTime = 0;            

        int err = objModel_.Accounts.Add(data_);
        if (err != Siprix.ErrorCode.kNoErr)
        {
            tbErrText.Text = objModel_.ErrorText(err);
            tbErrText.Visibility = Visibility.Visible;
            return;
        }
        this.DialogResult = true;
    }
}
