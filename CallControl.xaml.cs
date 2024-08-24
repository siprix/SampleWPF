using Siprix;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;


namespace SampleWpf
{
    ///Provides controls for manipulating current/switched call    
    public partial class CallControl : System.Windows.Controls.UserControl
    {
        enum UiMode { eUndef, eMain, eDtmf, eTransferBlind, eTranferAtt };

        CallModel? callModel_;
        Dictionary<CallState, UiMode> uiModes_ = new Dictionary<CallState, UiMode>();
        System.Windows.Threading.DispatcherTimer callDurationTimer_;
        VideoControlHost receivedVideoHost_, previewVideoHost_;

        public CallControl()
        {
            InitializeComponent();

            Siprix.ObjModel.Instance.Calls.Collection.CollectionChanged += onCalls_CollectionChanged;

            Siprix.ObjModel.Instance.Calls.PropertyChanged += onCalls_PropertyChanged;

            callDurationTimer_ = new System.Windows.Threading.DispatcherTimer();
            callDurationTimer_.Tick += onCallDurationTimer_Tick;
            callDurationTimer_.Interval = TimeSpan.FromSeconds(1);

            receivedVideoHost_ = new VideoControlHost();
            previewVideoHost_ = new VideoControlHost();
            receivedVideo.Child = receivedVideoHost_;
            previewVideo.Child = previewVideoHost_;
        }

        private void onCalls_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            callDurationTimer_.IsEnabled = (Siprix.ObjModel.Instance.Calls.Collection.Count > 0);
        }

        private void onCalls_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(CallsListModel.SwitchedCall))
            {
                if (callModel_ != null) {
                    callModel_.PropertyChanged -= onCall_PropertyChanged;
                    callModel_.SetVideoWindow(IntPtr.Zero);
                }

                callModel_  = Siprix.ObjModel.Instance.Calls.SwitchedCall;                
                DataContext = callModel_;

                resetUiModes();
                updateVisibility();

                if (callModel_ != null) {
                    callModel_.PropertyChanged += onCall_PropertyChanged;
                    callModel_.SetVideoWindow(receivedVideoHost_.Hwnd);
                }

                Siprix.ObjModel.Instance.Calls.SetPreviowVideoWindow(previewVideoHost_.Hwnd);
            }
        }

        private void onCall_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CallModel.CallState)||
               (e.PropertyName == nameof(CallModel.HoldState)))
            {
                updateVisibility();
            }
        }

        void resetUiModes()
        {
            uiModes_[CallState.Ringing]  = UiMode.eUndef;
            uiModes_[CallState.Connected]= UiMode.eMain;
            uiModes_[CallState.Held]     = UiMode.eMain;
        }

        UiMode getUiMode(CallState? state)
        {
            UiMode mode = UiMode.eUndef;
            if (state == null) return mode;

            uiModes_.TryGetValue(state.Value, out mode);
            return mode;
        }

        void setUiMode(CallState? state, UiMode mode)
        {
            if (state == null) return;
            
            if ((state == CallState.Ringing) && (mode == UiMode.eMain))
                mode = UiMode.eUndef;

            uiModes_[state.Value] = mode;
        }

        void updateVisibility()
        {
            tbNameAndExt.Visibility = (callModel_ == null) ? Visibility.Collapsed : Visibility.Visible;
            spDetails.Visibility    = (callModel_ == null) ? Visibility.Collapsed : Visibility.Visible;

            //Ringing
            bool isConnected = (callModel_ == null) ? false : callModel_.IsConnected;
            bool isRinging   = (callModel_ == null) ? false : callModel_.IsRinging;
            bool isVideo     = (callModel_ == null) ? false : callModel_.WithVideo;
            spAcceptReject.Visibility = isRinging   ? Visibility.Visible : Visibility.Collapsed;
            bnDtmfMode.Visibility     = isConnected ? Visibility.Visible : Visibility.Collapsed;

            //Video
            gridVideo.Visibility = isVideo ? Visibility.Visible : Visibility.Collapsed;            

            //Connected display depending on input mode
            UiMode uiMode = getUiMode(callModel_?.CallState);
            bnRedirect.Visibility   = (uiMode == UiMode.eUndef)&&isRinging ? Visibility.Visible : Visibility.Collapsed;
            gridDtmf.Visibility     = (uiMode == UiMode.eDtmf)          ? Visibility.Visible : Visibility.Collapsed;
            gridMain.Visibility     = (uiMode == UiMode.eMain)          ? Visibility.Visible : Visibility.Collapsed;
            gridTransfer.Visibility = (uiMode == UiMode.eTransferBlind) ? Visibility.Visible : Visibility.Collapsed;
                        
            bnMakeCall.Visibility = (callModel_ == null) ? Visibility.Visible : Visibility.Collapsed;
            bnHangup.Visibility   = (callModel_ == null)|| isRinging ? Visibility.Collapsed : Visibility.Visible;

            if (callModel_ != null)
                bnTransfer.Content = callModel_.IsRinging ? "Redirect" : "Transfer";
        }

        private void onCallDurationTimer_Tick(object? sender, EventArgs e)
        {
            Siprix.ObjModel.Instance.Calls.CalcDuration();
        }

        private void DtmfMode_Click(object sender, RoutedEventArgs e)
        {
            UiMode uiMode = getUiMode(callModel_?.CallState);
            if (uiMode == UiMode.eDtmf)
            {
                //Hide DTMF keys if displayed                
                setUiMode(callModel_?.CallState, UiMode.eMain);
            }
            else
            {
                //Show DTMF keys
                tbSentDtmf.Text = "";
                setUiMode(callModel_?.CallState, UiMode.eDtmf);                
            }
            updateVisibility();
        }

        private void DtmfSend_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button btnSender = (System.Windows.Controls.Button)sender;
            string tone = (string)btnSender.Tag;
            tbSentDtmf.Text += tone;
            callModel_?.SendDtmf(tone);
        }

        private void TransferBlindMode_Click(object sender, RoutedEventArgs e)
        {
            UiMode uiMode = getUiMode(callModel_?.CallState);
            if (uiMode == UiMode.eTransferBlind)
            {
                //Hide transfer/redirect edit if displayed
                tbTransferToExt.Text = "";
                setUiMode(callModel_?.CallState, UiMode.eMain);
            }
            else
            {
                //Show transfer/redirect edit if displayed
                setUiMode(callModel_?.CallState, UiMode.eTransferBlind);
            }
            updateVisibility();
        }

        private void TransferBlind_Click(object sender, RoutedEventArgs e)
        {
            callModel_?.TransferBlind(tbTransferToExt.Text);
        }
        
       
        private void ButtonMenu_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button? btn = sender as System.Windows.Controls.Button;
            if (btn == null) return;

            ContextMenu contextMenu = btn.ContextMenu;
            if (contextMenu == null) return;

            contextMenu.PlacementTarget = btn;
            contextMenu.Placement = PlacementMode.Left;
            contextMenu.HorizontalOffset = btn.ActualWidth;
            contextMenu.IsOpen = true;
            e.Handled = true;
        }

        private void AddCall_Click(object sender, RoutedEventArgs e)
        {
            AddCallWindow wnd = new AddCallWindow();
            wnd.Owner = App.Current.MainWindow;
            wnd.ShowDialog();
        }
    }

    public class VideoControlHost : HwndHost
    {
        internal const int
            WsChild = 0x40000000,
            WsVisible = 0x10000000,
            HostId = 0x00000002;
        
        private IntPtr _hwndHost;

        public IntPtr Hwnd { get { return _hwndHost; } }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            _hwndHost = CreateWindowEx(0, "static", "",
                WsChild | WsVisible, 0, 0, 100, 100,
                hwndParent.Handle, (IntPtr)HostId, IntPtr.Zero, IntPtr.Zero);

            return new HandleRef(this, _hwndHost);
        }

        protected override IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = false;
            return IntPtr.Zero;
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            DestroyWindow(hwnd.Handle);
        }

        //PInvoke declarations
        [DllImport("user32.dll", EntryPoint = "CreateWindowEx", CharSet = CharSet.Unicode)]
        internal static extern IntPtr CreateWindowEx(int dwExStyle,
            string lpszClassName, string lpszWindowName, int style,
            int x, int y, int width, int height,
            IntPtr hwndParent, IntPtr hMenu, IntPtr hInst, IntPtr pvParam);

        [DllImport("user32.dll", EntryPoint = "DestroyWindow", CharSet = CharSet.Unicode)]
        internal static extern bool DestroyWindow(IntPtr hwnd);
    }//VideoControlHost
}
