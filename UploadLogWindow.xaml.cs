using System.Windows;
using System.Windows.Threading;

namespace SampleWpf;

public partial class UploadLogWindow : Window
{
    readonly Siprix.ObjModel objModel_;
    System.Windows.Threading.DispatcherTimer? submitTimer_;
    public UploadLogWindow(Siprix.ObjModel objModel)
    {
        InitializeComponent();
        Owner = App.Current.MainWindow;
        objModel_ = objModel;

        objModel_.LogUploadState += OnLogUploadState;
        tbExplanation.TextChanged += TbExplanation_TextChanged;

        btnOK.IsEnabled = false;
        tbStatusText.Visibility = Visibility.Collapsed;
        progressBar.Visibility = Visibility.Collapsed;

        tbExplanation.Focus();
    }

    private void TbExplanation_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        btnOK.IsEnabled = (tbExplanation.Text.Length >= 40);
    }

    private void btnCancel_Click(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;
    }
    private void btnOK_Click(object sender, RoutedEventArgs e)
    {
        int err = objModel_.UploadLogFile(tbExplanation.Text);
        if (err != Siprix.ErrorCode.kNoErr)
        {
            tbStatusText.Text = objModel_.ErrorText(err);
            tbStatusText.Visibility = Visibility.Visible;
        }
        else
        {
            btnOK.IsEnabled = false;
            btnCancel.IsEnabled = false;
            progressBar.Visibility = Visibility.Visible;
            tbExplanation.IsEnabled = false;

            submitTimer_ = new DispatcherTimer();
            submitTimer_.Tick += OnSubmitTimer;
            submitTimer_.Interval = new TimeSpan(0, 0, 3);
            submitTimer_.Start();
        }
    }

    void OnSubmitTimer(object? sender, EventArgs e)
    {
        btnCancel.IsEnabled = true;
        submitTimer_?.Stop();

        tbStatusText.Text = "It takes longer than expected...";
        tbStatusText.Visibility = Visibility.Visible;
    }

    void OnLogUploadState(bool success, string response)
    {
        if (success)
            tbStatusText.Text = "Log file successfully uploaded.\nAssigned id: ";
        else
            tbStatusText.Text = "Upload failed:\n";
        tbStatusText.Text += response;
        tbStatusText.Visibility = Visibility.Visible;

        progressBar.Visibility = Visibility.Collapsed;
        btnCancel.IsEnabled = true;
    }
}
