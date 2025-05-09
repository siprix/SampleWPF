﻿using System.Windows;

namespace SampleWpf
{
    public partial class AddAccountWindow : Window
    {
        readonly bool addNew_;
        readonly Siprix.AccData data_;

        public AddAccountWindow(Siprix.AccData? data = null)
        {
            InitializeComponent();

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
                (tbSipExtension.Text.Length == 0)) return;

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

            //Try to add/update account
            int err = addNew_ ? Siprix.ObjModel.Instance.Accounts.Add(data_) 
                              : Siprix.ObjModel.Instance.Accounts.Update(data_);
            if(err != Siprix.Module.kNoErr)
            {
                tbErrText.Text = Siprix.ObjModel.Instance.ErrorText(err);
                return;
            }

            this.DialogResult = true;
        }
    }
}
