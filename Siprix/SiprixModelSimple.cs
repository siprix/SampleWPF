#define WPF_PROJECT
using Siprix;
using System;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Siprix
{
    #if WPF_PROJECT
    using AppDispatcher = System.Windows.Threading.Dispatcher;
    #else
    using AppDispatcher = System.Windows.Forms.Control;
    #endif

    public class SimpleModel
    {
        readonly Siprix.Module module_ = new();
        SiprixEventsHandler? eventHandler_;

        bool camMuted_ = false;
        bool micMuted_ = false;

        public void Initialize(AppDispatcher dispatcher)
        {
            if (module_.IsInitialized())
                return;

            eventHandler_ = new SiprixEventsHandler(this, dispatcher);

            Siprix.IniData iniData = new();            
            iniData.SingleCallMode = false;
            iniData.LogLevelIde = Siprix.LogLevel.Debug;
            iniData.LogLevelFile = Siprix.LogLevel.Debug;
            
            int err = module_.Initialize(eventHandler_, iniData);

            if (err == Siprix.Module.kNoErr) {
                Debug.WriteLine("Siprix module initialized successfully");
            }
            else{
                Debug.WriteLine($"Can't initialize Siprix module Err: {err} {ErrorText(err)}");
            }
        }

        public void UnInitialize()
        {
            module_.UnInitialize();
        }

        public uint AddAccount(AccData data)
        {
            int err = module_.Account_Add(data);
            if (err != Siprix.Module.kNoErr)
            {
                Debug.WriteLine($"Can't add account Err: {err} {ErrorText(err)}");                
                return Siprix.Module.kInvalidId;
            }
            else
            {
                Debug.WriteLine($"Account added successfully id:{data.MyAccId}");
                return data.MyAccId;
            }
        }

        public void Accept(uint callId)
        {
            int err = module_.Call_Accept(callId, true);
            if (err != Siprix.Module.kNoErr)
            {
                Debug.WriteLine($"Can't accept call Err: {err} {ErrorText(err)}");
            }
        }

        public void Reject(uint callId)
        {
            int err = module_.Call_Reject(callId);
            if (err != Siprix.Module.kNoErr)
            {
                Debug.WriteLine($"Can't reject call Err: {err} {ErrorText(err)}");
            }
        }

        public void EndCall(uint callId)
        {
            int err = module_.Call_Bye(callId);
            if (err != Siprix.Module.kNoErr)
            {
                Debug.WriteLine($"Can't reject call Err: {err} {ErrorText(err)}");
            }
        }

        public uint StartCall(string addr, uint accId)
        {
            DestData dest = new();
            dest.ToExt = addr;
            dest.FromAccId = accId;
            dest.WithVideo = true;

            int err = module_.Call_Invite(dest);
            if (err != Siprix.Module.kNoErr)
            {
                Debug.WriteLine($"Can't start call Err: {err} {ErrorText(err)}");
                return Siprix.Module.kInvalidId;
            }

            return dest.MyCallId;
        }

        public void SetVideoWindow(uint callId, IntPtr hwnd)
        {
            module_.Call_SetVideoWindow(callId, hwnd);
        }

        public void MuteCam(uint callId)
        {
            camMuted_ = !camMuted_;
            module_.Call_MuteCam(callId, camMuted_);
        }

        public void MuteMic(uint callId)
        {
            micMuted_ = !micMuted_;
            module_.Call_MuteMic(callId, micMuted_);
        }

        public delegate void OnCallIncomingHandler(uint callId, bool withVideo, string hdrFrom, string hdrTo);
        public delegate void OnCallConnectedHandler(uint callId, bool withVideo);
        public delegate void OnCallTerminatedHandler(uint callId, uint statusCode);

        public event OnCallIncomingHandler? OnCallIncoming_;
        public event OnCallConnectedHandler? OnCallConnected_;
        public event OnCallTerminatedHandler? OnCallTerminated_;

        private string ErrorText(int err) { return module_.ErrorText(err); }

        class SiprixEventsHandler(SimpleModel parent, AppDispatcher dispatcher) : Siprix.IEventDelegate
        {
            readonly AppDispatcher dispatcher_ = dispatcher;
            readonly SimpleModel parent_ = parent;

            //Event raised by SDK
            public void OnTrialModeNotified()
            {
                dispatcher_?.BeginInvoke(new Action(() => {
                    Debug.WriteLine("OnTrialModeNotified");
                }));
            }

            public void OnDevicesAudioChanged() { }

            public void OnSubscriptionState(uint subId, SubscriptionState state, string response)
            {
                dispatcher_?.BeginInvoke(new Action(() => {
                    Debug.WriteLine($"OnSubscriptionState: subId:{subId} state:{state}");
                }));
            }

            public void OnAccountRegState(uint accId, RegState state, string response)
            {
                dispatcher_?.BeginInvoke(new Action(() => {
                    Debug.WriteLine($"OnAccountRegState: accId:{accId} state:{state}");
                }));
            }
            public void OnNetworkState(string name, NetworkState state)
            {
                dispatcher_?.BeginInvoke(new Action(() => {
                    Debug.WriteLine($"OnNetworkStateChanged: state:{state}");
                }));
            }
            public void OnPlayerState(uint playerId, PlayerState state) { }
            public void OnRingerState(bool start) { }

            public void OnCallIncoming(uint callId, uint accId, bool withVideo, string hdrFrom, string hdrTo)
            {
                dispatcher_?.BeginInvoke(new Action(() => {
                    parent_.OnCallIncoming_?.Invoke(callId, withVideo, hdrFrom, hdrTo);
                }));
            }

            public void OnCallConnected(uint callId, string hdrFrom, string hdrTo, bool withVideo)
            {
                dispatcher_?.BeginInvoke(new Action(() => {
                    parent_.OnCallConnected_?.Invoke(callId, withVideo);
                }));
            }

            public void OnCallTerminated(uint callId, uint statusCode)
            {
                dispatcher_?.BeginInvoke(new Action(() => {
                    parent_.OnCallTerminated_?.Invoke(callId, statusCode);
                }));
            }

            public void OnCallProceeding(uint callId, string response) { }
            public void OnCallTransferred(uint callId, uint statusCode) { }
            public void OnCallRedirected(uint origCallId, uint relatedCallId, string referTo) {}
            public void OnCallDtmfReceived(uint callId, ushort tone) { }
            public void OnCallHeld(uint callId, HoldState state) { }
            public void OnCallSwitched(uint callId) { }

            public void OnMessageSentState(uint messageId, bool success, string response) { }
            public void OnMessageIncoming(uint accId, string hdrFrom, string body) { }
        }

    }//DirectCallModel
}
