#define WPF_PROJECT
using Siprix;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using System.Diagnostics;
using System.Text.Json;
using System.Linq;
using System.Windows;


namespace Siprix
{
    #if WPF_PROJECT
    using AppDispatcher = System.Windows.Threading.Dispatcher;
    #else
    using AppDispatcher = System.Windows.Forms.Control;
    #endif

    using JsonDict = Dictionary<string, object>;
    
    /////////////////////////////////////////////////////////////////
    /// AccountModel
    public class AccountModel : INotifyPropertyChanged, IEquatable<AccountModel>
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        readonly ObjModel parent_;
        Siprix.AccData accData_;        

        public AccountModel(Siprix.AccData accData, ObjModel parent)
        {
            this.accData_= accData;
            this.parent_ = parent;            

            RegState = (accData.ExpireTime == 0) ? RegState.Removed : RegState.InProgress;
            RegText  = (accData.ExpireTime == 0) ? "Removed"        : "In progress...";
                        
            RegisterCommand   = new RelayCommand(Register,   CanRegister);
            UnRegisterCommand = new RelayCommand(UnRegister, CanUnRegister);
        }
                
        public Siprix.AccData AccData   { get { return accData_; } }
        public string Uri               { get { return accData_.SipExtension + "@" + accData_.SipServer; } }
        public uint ID                  { get { return accData_.MyAccId; } }        
        public bool IsWaiting           { get { return (RegState == RegState.InProgress); } }
        public bool HasSecureMedia      { get { return (accData_.SecureMediaMode != null) && 
                                                       (accData_.SecureMediaMode != SecureMedia.Disabled); } }
        public string          RegText  { get; private set; }
        public Siprix.RegState RegState { get; private set; }
                
        public ICommand RegisterCommand   { get; private set; }
        public ICommand UnRegisterCommand { get; private set; }
        
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool Equals(AccountModel? other) { return (this.ID == other?.ID); }

        public void Update(Siprix.AccData accData)
        {
            this.accData_ = accData;
        }

        bool CanRegister()   { return (RegState != RegState.InProgress); }
        bool CanUnRegister() { return (RegState != RegState.InProgress)&&(RegState != RegState.Removed); }

        void Register()
        {    
            //Send register request (use 300sec as expire time when account not registered)      
            uint? expireSec = accData_.ExpireTime;
            if ((expireSec == null) || (expireSec == 0)) { expireSec = 300; }
            refreshRegistration(expireSec.Value);
        }

        void UnRegister()
        {
            refreshRegistration(0);
        }

        int refreshRegistration(uint expireSec)
        {
            string cmd = (expireSec != 0) ? "Register" : "Unregister";
            int err = (expireSec != 0) ? parent_.Module.Account_Register(ID, expireSec)
                                       : parent_.Module.Account_Unregister(ID);
                                         
            if (err != Siprix.Module.kNoErr)
            {
                parent_.Logs?.Print($"Can't {cmd} AccId:{ID} Err:{err} {parent_.ErrorText(err)}");
                return err;
            }

            //Update UI
            accData_.ExpireTime = expireSec;
            RegState = RegState.InProgress;
            NotifyPropertyChanged("RegState");
            NotifyPropertyChanged("IsWaiting");

            //Save changes
            parent_.postSaveAccountsChanges();
            parent_.Logs?.Print($"{cmd}ing accId:{ID}");            
            return err;
        }
        
        //Event raised by SDK
        public void OnAccountRegState(RegState state, string response)
        {
            RegState = state;
            RegText  = response;
            NotifyPropertyChanged("RegText");
            NotifyPropertyChanged("RegState");
            NotifyPropertyChanged("IsWaiting");
        }

        public JsonDict storeToJson()
        {
            JsonDict dict = new JsonDict();
            
            dict.Add("SipServer",    accData_.SipServer);
            dict.Add("SipExtension", accData_.SipExtension);
            dict.Add("SipPassword",  accData_.SipPassword);
            dict.Add("ExpireTime",   accData_.ExpireTime);

            if (accData_.SipAuthId         != null) dict.Add("SipAuthId",         accData_.SipAuthId);
            if (accData_.SipProxyServer    != null) dict.Add("SipProxyServer",    accData_.SipProxyServer);
                                                                                  
            if (accData_.UserAgent         != null) dict.Add("UserAgent",         accData_.UserAgent);
            if (accData_.DisplayName       != null) dict.Add("DisplayName",       accData_.DisplayName);
            if (accData_.InstanceId        != null) dict.Add("InstanceId",        accData_.InstanceId);
            if (accData_.RingToneFile      != null) dict.Add("RingToneFile",      accData_.RingToneFile);

            if (accData_.SecureMediaMode   != null) dict.Add("SecureMedia",       accData_.SecureMediaMode.Value);
            if (accData_.UseSipSchemeForTls!= null) dict.Add("UseSipSchemeForTls",accData_.UseSipSchemeForTls.Value);
            if (accData_.RtcpMuxEnabled    != null) dict.Add("RtcpMuxEnabled",    accData_.RtcpMuxEnabled.Value);
            if (accData_.KeepAliveTime     != null) dict.Add("KeepAliveTime",     accData_.KeepAliveTime.Value);
                                                                        
            if (accData_.TranspProtocol    != null) dict.Add("TranspProtocol",    accData_.TranspProtocol.Value);
            if (accData_.TranspPort        != null) dict.Add("TranspPort",        accData_.TranspPort.Value);
            if (accData_.TranspTlsCaCert   != null) dict.Add("TranspTlsCaCert",   accData_.TranspTlsCaCert);
            if (accData_.TranspBindAddr    != null) dict.Add("TranspBindAddr",    accData_.TranspBindAddr);
            if (accData_.TranspPreferIPv6  != null) dict.Add("TranspPreferIPv6",  accData_.TranspPreferIPv6.Value);
            if (accData_.RewriteContactIp  != null) dict.Add("RewriteContactIp",  accData_.RewriteContactIp.Value);
                                                                                  
            if (accData_.AudioCodecs       != null) dict.Add("AudioCodecs",       accData_.AudioCodecs);
            if (accData_.VideoCodecs       != null) dict.Add("VideoCodecs",       accData_.VideoCodecs);
            if (accData_.Xheaders          != null) dict.Add("Xheaders",          accData_.Xheaders);
            return dict;            

        }//storeToJson

        public static Siprix.AccData loadFromJson(JsonElement elem)
        {
            Siprix.AccData accData = new Siprix.AccData();

            foreach (JsonProperty prop in elem.EnumerateObject())
            {
                bool isString = (prop.Value.ValueKind == JsonValueKind.String);
                string strVal = isString ? prop.Value.GetString()! : "";

                switch (prop.Name)
                {
                    case "SipServer":          accData.SipServer      = strVal; break;
                    case "SipExtension":       accData.SipExtension   = strVal; break;
                    case "SipPassword":        accData.SipPassword    = strVal; break;
                    case "ExpireTime":         accData.ExpireTime     = prop.Value.GetUInt32(); break;

                    case "SipAuthId":          accData.SipAuthId      = strVal; break;
                    case "SipProxyServer":     accData.SipProxyServer = strVal; break;
                    
                    case "UserAgent":          accData.UserAgent      = strVal; break;
                    case "DisplayName":        accData.DisplayName    = strVal; break;                    
                    case "InstanceId":         accData.InstanceId     = strVal; break;
                    case "RingToneFile":       accData.RingToneFile   = strVal; break;
                    
                    
                    case "SecureMedia":        accData.SecureMediaMode    = (SecureMedia)(prop.Value.GetUInt16()); break;
                    case "UseSipSchemeForTls": accData.UseSipSchemeForTls = prop.Value.GetBoolean(); break;
                    case "RtcpMuxEnabled":     accData.RtcpMuxEnabled     = prop.Value.GetBoolean(); break;
                    case "KeepAliveTime":      accData.KeepAliveTime      = prop.Value.GetUInt32(); break;
                    case "TranspProtocol":     accData.TranspProtocol     = (SipTransport)(prop.Value.GetUInt16()); break;
                    case "TranspPort":         accData.TranspPort         = prop.Value.GetUInt16(); break;
                    case "TranspTlsCaCert":    accData.TranspTlsCaCert    = strVal; break;
                    case "TranspPreferIPv6":   accData.TranspPreferIPv6   = prop.Value.GetBoolean(); break;
                    case "RewriteContactIp":   accData.RewriteContactIp   = prop.Value.GetBoolean(); break;
                    case "AudioCodecs":                         
                    {
                        accData.AudioCodecs = new();
                        foreach (JsonElement cElem in prop.Value.EnumerateArray())
                        {
                            accData.AudioCodecs.Add((AudioCodec)cElem.GetInt32());
                        }
                        break;
                    }

                    case "VideoCodecs": 
                    {
                        accData.VideoCodecs = new();
                        foreach (JsonElement cElem in prop.Value.EnumerateArray())
                        {
                            accData.VideoCodecs.Add((VideoCodec)cElem.GetInt32());
                        }
                        break;
                    }

                    case "Xheaders":                     
                    {
                        accData.Xheaders = new();
                        foreach (JsonProperty xProp in prop.Value.EnumerateObject())
                        {
                            if(xProp.Value.ValueKind == JsonValueKind.String)
                            {
                                accData.Xheaders.Add(prop.Name, xProp.Value.GetString()!);
                            }
                        }
                        break;
                    }
                }//switch
            }//for

            return accData;

        }//loadFromJson

    }//AccountModel


    /////////////////////////////////////////////////////////////////
    /// AccountsListModel

    public class AccountsListModel
    {
        ObservableCollection<AccountModel> collection_;
        AccountModel? selAccount_;        
        readonly ObjModel parent_;

        public AccountsListModel(ObjModel parent)
        {
            collection_ = new ObservableCollection<AccountModel>();            
            parent_ = parent;
        }

        public ObservableCollection<AccountModel> Collection { get { return collection_; } }

        public AccountModel? SelectedAccount { 
            get { return selAccount_;  }
            set { selAccount_ = value; }
        }

        public string getUri(uint accId)
        {
            var accModel = collection_.Where(a => a.ID == accId).FirstOrDefault();
            return (accModel == null) ? "?" : accModel.Uri;
        }

        public bool hasSecureMedia(uint accId)
        {
            var accModel = collection_.Where(a => a.ID == accId).FirstOrDefault();
            return (accModel == null) ? false : accModel.HasSecureMedia;
        }

        public AccData? GetData(uint accId)
        {
            var accModel = collection_.Where(a => a.ID == accId).FirstOrDefault();
            return accModel?.AccData;
        }

        public int Add(Siprix.AccData accData, bool saveChanges=true)
        {   
            parent_.Logs?.Print($"Adding new account: {accData.SipServer}@{accData.SipExtension}");

            int err = parent_.Module.Account_Add(accData);
            if (err != Siprix.Module.kNoErr)
            {
                parent_.Logs?.Print($"Can't add account Err: {err} {parent_.ErrorText(err)}");
                return err;
            }

            AccountModel acc = new AccountModel(accData, parent_);
            collection_.Add(acc);

            if (selAccount_ == null) 
                selAccount_ = acc;

            parent_.Logs?.Print($"Added successfully with id: {acc.ID}");
            if (saveChanges) parent_.postSaveAccountsChanges();
            return err;
        }

        public int Delete(uint accId)
        {
            var accModel = collection_.Where(a => a.ID == accId).FirstOrDefault();
            if (accModel == null) return -1;

            int err = 0;//parent_.Module.Account_Delete(accId);
            if (err != Siprix.Module.kNoErr)
            {
                parent_.Logs?.Print($"Can't delete account Err: {err} {parent_.ErrorText(err)}");
                return err;
            }

            collection_.Remove(accModel);

            if (selAccount_ == accModel)
                selAccount_ = (collection_.Count > 0) ? collection_[0] : null;

            parent_.postSaveAccountsChanges();

            parent_.Logs?.Print($"Deleted account accId:{accId}");
            return err;
        }

        public int Update(Siprix.AccData accData)
        {
            var accModel = collection_.Where(a => a.ID == accData.MyAccId).FirstOrDefault();
            if (accModel == null)
            {
                parent_.Logs?.Print("Account with specified id not found");
                return -1;
            }

            int err = parent_.Module.Account_Update(accData, accData.MyAccId);
            if (err != Siprix.Module.kNoErr)
            {
                parent_.Logs?.Print($"Can't update account: {err} {{parent_.ErrorText(err)}}");
                return err;
            }

            accModel.Update(accData);
            
            parent_.postSaveAccountsChanges();
            parent_.Logs?.Print($"Updated account accId:{accData.MyAccId}");
            return err;
        }

        //Event raised by SDK
        public void OnAccountRegState(uint accId, RegState state, string response)
        {
            var accModel = collection_.Where(a => a.ID == accId).FirstOrDefault();
            accModel?.OnAccountRegState(state, response);
            parent_.Logs?.Print($"OnAccountRegState accId:{accId} state:{state} response:{response}");
        }

        public string storeToJson()
        {
            List<JsonDict> jsonAccList = new();
            foreach(var accModel in collection_)
            {
                jsonAccList.Add(accModel.storeToJson());
            }
            return JsonSerializer.Serialize(jsonAccList);
        }
        public void loadFromJson(string jsonString)
        {
            if (jsonString.Length == 0) return;

            collection_.Clear();

            using (JsonDocument document = JsonDocument.Parse(jsonString))
            {
                foreach (JsonElement element in document.RootElement.EnumerateArray())
                {
                    this.Add(AccountModel.loadFromJson(element), false);
                }
            }
        }

    }//AccountsListModel


    /////////////////////////////////////////////////////////////////
    /// CallModel
    public class CallModel : INotifyPropertyChanged
    {
        readonly uint myCallId_;
        readonly string accUri_;     //Account URI used to accept/make this call
        readonly string remoteExt_;  //Phone number(extension) of remote side  
        readonly bool isIncoming_;
        readonly bool hasSecureMedia_;
                
        CallState callState_;
        HoldState holdState_;
        DateTime startTime_;

        string displName_ = "";      //Contact name
        string receivedDtmf_ = "";
        string duration_ = "";
        string response_ = "";
        bool micMuted_ = false;
        bool camMuted_ = false;
        bool withVideo_;
        uint playerId_ = 0;

        public event PropertyChangedEventHandler? PropertyChanged;
        readonly ObjModel parent_;
        
        public CallModel(uint myCallId, string accUri, string remoteExt, 
                        bool isIncoming, bool hasSecureMedia, bool withVideo,
                        ObjModel parent)
        {
            myCallId_   = myCallId;
            accUri_     = accUri;
            remoteExt_  = remoteExt;
            withVideo_  = withVideo;
            isIncoming_ = isIncoming;
            hasSecureMedia_ = hasSecureMedia;

            callState_ = isIncoming ? CallState.Ringing : CallState.Dialing;

            parent_    = parent;

            AcceptCommand   = new RelayCommand(() => Accept(true));
            RejectCommand   = new RelayCommand(() => Reject());
            SwitchToCommand = new RelayCommand(() => SwitchTo());
            HoldCommand     = new RelayCommand(() => Hold());
            MuteMicCommand  = new RelayCommand(() => MuteMic(!micMuted_));
            MuteCamCommand  = new RelayCommand(() => MuteCam(!camMuted_));
            HangupCommand   = new RelayCommand(() => Bye());
        }
        
        public string NameAndExt { get { 
            return (displName_.Length == 0) ? remoteExt_ : $"{displName_} ({remoteExt_})"; } 
        }
       
        public uint   ID                { get { return myCallId_;       } }                
        public CallState CallState      { get { return callState_;      } }
        public HoldState HoldState      { get { return holdState_;      } }
        public string Duration          { get { return duration_;       } }
        public bool   HasSecureMedia    { get { return hasSecureMedia_; } }
        public bool   IsIncoming        { get { return isIncoming_;     } }
        public bool   IsSwitchedCall    { get { return parent_.Calls.IsSwitchedCall(ID); } }
        public bool   IsWaiting         { get { return (callState_ != CallState.Connected) &&(callState_ != CallState.Held); } }
        public bool   IsConnected       { get { return (callState_ == CallState.Connected); } }
        public bool   IsRinging         { get { return (callState_ == CallState.Ringing);   } }
        public bool   WithVideo         { get { return withVideo_;      } }
        public bool   IsMicMuted        { get { return micMuted_;       } }
        public bool   IsCamMuted        { get { return camMuted_;       } }        
        public string AccUri            { get { return accUri_;         } }
        public string ReceivedDtmf      { get { return receivedDtmf_;   } }

        public ICommand AcceptCommand   { get; private set; }
        public ICommand RejectCommand   { get; private set; }
        public ICommand SwitchToCommand { get; private set; }
        public ICommand HoldCommand     { get; private set; }
        public ICommand MuteMicCommand  { get; private set; }
        public ICommand MuteCamCommand { get; private set; }
        public ICommand HangupCommand   { get; private set; }

        public bool CanAccept           { get { return callState_ == CallState.Ringing; } }
        public bool CanReject           { get { return callState_ == CallState.Ringing; } }        
        public bool CanHold             { get { return (callState_ == CallState.Connected) || (callState_ == CallState.Held); } }
        public bool CanMuteMic          { get { return callState_ == CallState.Connected; } }
        public bool CanMuteCam          { get { return (callState_ == CallState.Connected) && withVideo_ ; } }
        public bool CanHangup           { get { return callState_ != CallState.Ringing; }}
        public bool CanSwitchTo         { get { return !IsSwitchedCall;  } }

        public void setDisplName(string name) { displName_ = name;  NotifyPropertyChanged("NameAndExt"); }
        void setMicMuted(bool muted)          { micMuted_  = muted; NotifyPropertyChanged("IsMicMuted"); }
        void setCamMuted(bool muted)          { camMuted_  = muted; NotifyPropertyChanged("IsCamMuted"); }
        void setWithVideo(bool video)         { withVideo_ = video; NotifyPropertyChanged("WithVideo"); }


        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void setCallState(CallState newState)
        {
            callState_ = newState;
            NotifyPropertyChanged("CallState");
            NotifyPropertyChanged("IsWaiting");
            NotifyPropertyChanged("IsConnected");
            NotifyPropertyChanged("IsRinging");
            NotifyPropertyChanged("CanAccept");
            NotifyPropertyChanged("CanReject");
            NotifyPropertyChanged("CanHold");
            NotifyPropertyChanged("CanMuteMic");
            NotifyPropertyChanged("CanMuteCam");
        }

        private void setHoldState(HoldState holdState)
        {
            holdState_ = holdState;
            NotifyPropertyChanged("HoldState");
        }

        public bool Equals(CallModel? other) { 
            return (this.myCallId_ == other?.myCallId_); 
        }
        
        public void CalcDuration()
        {
            if (callState_ != CallState.Connected) return;

            TimeSpan span = (DateTime.Now - startTime_);
            duration_ = (span.Hours !=0) ? string.Format("{0}:{1:D2}:{2:D2}", span.Hours, span.Minutes, span.Seconds)
                                         : string.Format("{0:D2}:{1:D2}", span.Minutes, span.Seconds);
            NotifyPropertyChanged("Duration");
        }

        public int SwitchTo()
        {
            return parent_.Calls.SwitchTo(myCallId_);
        }

        public int Bye()
        {
            parent_.Logs?.Print($"Ending callId:{myCallId_}");
            int err = parent_.Module.Call_Bye(myCallId_);
            if (err == Siprix.Module.kNoErr) setCallState(CallState.Disconnecting);
            else  parent_.Logs?.Print($"Cant Bye callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
            return err;
        }

        public int Accept(bool withVideo)
        {
            parent_.Logs?.Print($"Accepting callId:{myCallId_}");
            int err = parent_.Module.Call_Accept(myCallId_, withVideo);
            if (err == Siprix.Module.kNoErr) setCallState(CallState.Accepting);
            else parent_.Logs?.Print($"Cant Accept callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
            return err;
        }

        public int Reject()
        {
            parent_.Logs?.Print($"Rejecting callId:{myCallId_}");
            int err = parent_.Module.Call_Reject(myCallId_, 486);//Send '486 Busy now'
            if (err == Siprix.Module.kNoErr) setCallState(CallState.Rejecting);
            else parent_.Logs?.Print($"Cant Reject callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
            return err;
        }

        public int MuteMic(bool mute)
        {
            parent_.Logs?.Print($"Set mic mute={mute} of call {myCallId_}");
            int err = parent_.Module.Call_MuteMic(myCallId_, mute);
            if (err == Siprix.Module.kNoErr) setMicMuted(mute);
            else parent_.Logs?.Print($"Cant MuteMic callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
            return err;
        }

        public int MuteCam(bool mute)
        {
            parent_.Logs?.Print($"Set camera mute={mute} of call {myCallId_}");
            int err = parent_.Module.Call_MuteCam(myCallId_, mute);
            if (err == Siprix.Module.kNoErr) setCamMuted(mute);
            else parent_.Logs?.Print($"Cant MuteCam callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
            return err;
        }

        public int SendDtmf(string tone)
        {
            parent_.Logs?.Print($"Sending dtmf callId:{myCallId_} tone:{tone}");
            int err = parent_.Module.Call_SendDtmf(myCallId_, tone, 200, 50, DtmfMethod.DTMF_RTP);
            if (err != Siprix.Module.kNoErr) 
               parent_.Logs?.Print($"Cant SendDtmf callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
            return err;        
        }

        public int PlayFile(String pathToMp3File, bool loop)
        {
            parent_.Logs?.Print($"Starting play file callId:{myCallId_} {pathToMp3File}");
            int err = parent_.Module.Call_PlayFile(myCallId_, pathToMp3File, loop, ref playerId_);
            if (err != Siprix.Module.kNoErr)
                parent_.Logs?.Print($"Cant PlayFile callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
            return err;        
        }

        public int Hold()
        {
            parent_.Logs?.Print($"Hold callId:{myCallId_}");
            int err = parent_.Module.Call_Hold(myCallId_);
            if (err == Siprix.Module.kNoErr) setCallState(CallState.Holding);
            else parent_.Logs?.Print($"Cant MuteMic callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
            return err;
        }

        public int TransferBlind(String toExt)
        {
            parent_.Logs?.Print($"Transfer blind callId:{myCallId_} to:{toExt}");
            if (toExt.Length==0) return -1;

            int err = parent_.Module.Call_TransferBlind(myCallId_, toExt);
            if (err == Siprix.Module.kNoErr) setCallState(CallState.Transferring);
            else parent_.Logs?.Print($"Cant TransferBlind callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
            return err;
        }

        public int TransferAttended(uint toCallId)
        {
            parent_.Logs?.Print($"Transfer attended callId:{myCallId_} to callId:{toCallId}");
            int err = parent_.Module.Call_TransferAttended(myCallId_, toCallId);
            if (err == Siprix.Module.kNoErr) setCallState(CallState.Transferring);
            else parent_.Logs?.Print($"Cant TransferAttended callId:{myCallId_} Err:{err} {parent_.ErrorText(err)}");
            return err;            
        }
        public int SetVideoWindow(IntPtr hwnd)
        {
            parent_.Logs?.Print($"SetVideoWindow callId:{myCallId_} hwnd:{hwnd}");
            return parent_.Module.Call_SetVideoWindow(myCallId_, hwnd);
        }

        //Events raised by SDK
        public void OnCallProceeding(string response)
        {
            response_ = response;
            setCallState(CallState.Proceeding);
        }

        public void OnCallConnected(string hdrFrom, string hdrTo, bool withVideo)
        {
            startTime_ = DateTime.Now;
            setWithVideo(withVideo);
            setCallState(CallState.Connected);
        }

        public void OnCallTransferred(uint statusCode)
        {
            setCallState(CallState.Connected);
        }

        public void OnCallDtmfReceived(ushort tone)
        {
            if(tone == 10) { receivedDtmf_ += '*'; }else
            if(tone == 11) { receivedDtmf_ += '#'; }
            else           { receivedDtmf_ += tone.ToString(); }
            NotifyPropertyChanged("ReceivedDtmf");
        }

        public void OnCallHeld(HoldState holdState)
        {
            setHoldState(holdState);
            setCallState((holdState_ == HoldState.None) ? CallState.Connected : CallState.Held);            
        }

        public void OnCallSwitched()
        {
            NotifyPropertyChanged("IsSwitchedCall");
            NotifyPropertyChanged("CanSwitchTo");            
        }

    }//CallModel


    /////////////////////////////////////////////////////////////////
    /// CallsListModel

    public class CallsListModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        ObservableCollection<CallModel> collection_;
        readonly ObjModel parent_;

        CallModel? switchedCall_;
        uint lastIncomingCallId_;
        bool confModeStarted_;

        public CallsListModel(ObjModel parent)
        {
            collection_ = new ObservableCollection<CallModel>();
            parent_ = parent;

            switchedCall_ = null;
            lastIncomingCallId_ = Siprix.Module.kInvalidId;
            confModeStarted_ = false;
        }

        public ObservableCollection<CallModel> Collection { get { return collection_; } }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool IsSwitchedCall(uint callId)
        {
            return (switchedCall_ == null) ? false : (switchedCall_.ID == callId);
        }

        public uint LastIncomingCallId() { return lastIncomingCallId_; }

        public CallModel? SwitchedCall
        {
            get { return switchedCall_; }
            private set { switchedCall_ = value; NotifyPropertyChanged(); }
        }

        public bool ConfModeStarted
        {
            get { return confModeStarted_; }
            private set { confModeStarted_ = value; NotifyPropertyChanged(); }
        }

        public void CalcDuration()
        {
            foreach(var c in collection_) c.CalcDuration();
        }

        public int Invite(Siprix.DestData dest)
        {
            parent_.Logs?.Print($"Trying to invite {dest.ToExt} from account:{dest.FromAccId}");
            //CallModel c = new CallModel(1, "111@172.30.30.50", "222", true, true, false, module_, logsModel_);
            //collection_.Add(c);

            int err = parent_.Module.Call_Invite(dest);
            if (err != Siprix.Module.kNoErr)
            {
                parent_.Logs?.Print($"Can't invite Err: {err} {parent_.ErrorText(err)}");
                return err;
            }

            String accUri       = parent_.Accounts.getUri(dest.FromAccId);
            bool hasSecureMedia = parent_.Accounts.hasSecureMedia(dest.FromAccId);

            CallModel newCall = new CallModel(dest.MyCallId, accUri, dest.ToExt, false, hasSecureMedia, dest.WithVideo, parent_);
            collection_.Add(newCall);
            //_cdrs?.add(newCall);             //TODO add CDR
            
            parent_.postResolveContactName_(newCall);
            return err;
        }

        public int SwitchTo(uint callId)
        {
            parent_.Logs?.Print($"Switching mixer to callId:{callId}");

            int err = parent_.Module.Mixer_SwitchToCall(callId);
            if (err == Siprix.Module.kNoErr) ConfModeStarted = false;
            else parent_.Logs?.Print($"Cant switch to callId:{callId} Err:{err} {parent_.ErrorText(err)}");
            //Value '_switchedCallId' will set in the callback 'onSwitched'
            return err;
        }

        public int MakeConference()
        {
            //TODO
            if(confModeStarted_) {
                uint callId = (switchedCall_!=null) ? switchedCall_.ID : 0;
                parent_.Logs?.Print($"Ending conference, switch mixer to callId: {callId}");
                int err = parent_.Module.Mixer_SwitchToCall(callId);
                ConfModeStarted = false;
                return err;
            }
            else {
                parent_.Logs?.Print("Joining all calls to conference");
                int err = parent_.Module.Mixer_MakeConference();
                if (err == Siprix.Module.kNoErr) ConfModeStarted = true;
                else parent_.Logs?.Print($"Cant make conference Err:{err} {parent_.ErrorText(err)}");
                return err;
            }
        }

        public int SetPreviowVideoWindow(IntPtr hwnd)
        {
            parent_.Logs?.Print($"SetPreviowVideoWindow hwnd:{hwnd}");
            return parent_.Module.Call_SetVideoWindow(Siprix.Module.kInvalidId, hwnd);
        }

        bool hasConnectedFewCalls()
        {
            int counter = 0;
            foreach(var m in collection_)
                counter += m.IsConnected ? 1 : 0;

            return counter > 1;
        }

        void setLastIncomingCallId(uint id) { lastIncomingCallId_ = id; NotifyPropertyChanged("LastIncomingCallId"); }


        //Events raised by SDK
        public void OnCallIncoming(uint callId, uint accId, bool withVideo, string hdrFrom, string hdrTo)
        {
            parent_.Logs?.Print($"onIncoming callId:{callId} accId:{accId} from:{hdrFrom} to:{hdrTo}");

            var callModel = collection_.Where(a => a.ID == callId).FirstOrDefault();
            if (callModel != null) return;//Call already exists, skip

            String accUri       = parent_.Accounts.getUri(accId);
            bool hasSecureMedia = parent_.Accounts.hasSecureMedia(accId);

            CallModel newCall = new CallModel(callId, accUri, parseExt(hdrFrom), true, hasSecureMedia, withVideo, parent_);
            newCall.setDisplName(parseDisplayName(hdrFrom));
            collection_.Add(newCall);

            setLastIncomingCallId(callId);

            if (SwitchedCall == null)
                SwitchedCall = newCall;

            //_cdrs?.add(newCall);
            //_postResolveContactName(newCall); //TODO add '_postResolveContactName'
        }

        public void OnCallConnected(uint callId, string hdrFrom, string hdrTo, bool withVideo)
        {
            parent_.Logs?.Print($"onConnected callId:{callId} from:{hdrFrom} to:{hdrTo}");
            //_cdrs?.setConnected(callId, hdrFrom, hdrTo);

            var callModel = collection_.Where(a => a.ID == callId).FirstOrDefault();
            callModel?.OnCallConnected(hdrFrom, hdrTo, withVideo);
        }

        public void OnCallTerminated(uint callId, uint statusCode)
        {
            parent_.Logs?.Print($"onTerminated callId:{callId} statusCode:{statusCode}");

            var callModel = collection_.Where(a => a.ID == callId).FirstOrDefault();
            //_cdrs?.setTerminated(callId, statusCode, callModel?.displName);

            if (callModel != null)
            {
                collection_.Remove(callModel);

                if (ConfModeStarted && !hasConnectedFewCalls())
                    ConfModeStarted = false;
            }
        }

        public void OnCallProceeding(uint callId, string response)
        {
            parent_.Logs?.Print($"onProceeding callId:{callId} response:{response}");
            var callModel = collection_.Where(a => a.ID == callId).FirstOrDefault();
            callModel?.OnCallProceeding(response);
        }

        public void OnCallTransferred(uint callId, uint statusCode)
        {
            parent_.Logs?.Print($"onTransferred callId:{callId} statusCode:{statusCode}");
            var callModel = collection_.Where(a => a.ID == callId).FirstOrDefault();
            callModel?.OnCallTransferred(statusCode);
        }

        public void OnCallRedirected(uint origCallId, uint relatedCallId, string referTo)
        {
           parent_.Logs?.Print($"onRedirected origCallId:{origCallId} relatedCallId:{relatedCallId} to:{referTo}");

           //Find 'origCallId'
           var origCall = collection_.Where(a => a.ID == origCallId).FirstOrDefault();
           if (origCall == null) return;

           //Clone 'origCallId' and add to collection of calls as related one           
           CallModel relatedCall = new CallModel(relatedCallId, origCall.AccUri, parseExt(referTo), false, 
                                                origCall.HasSecureMedia, origCall.WithVideo, 
                                                parent_);
           collection_.Add(relatedCall);
        }

        public void OnCallDtmfReceived(uint callId, ushort tone)
        {
            parent_.Logs?.Print($"onDtmfReceived callId:{callId} tone:{tone}");

            var callModel = collection_.Where(a => a.ID == callId).FirstOrDefault();
            callModel?.OnCallDtmfReceived(tone);
        }

        public void OnCallHeld(uint callId, HoldState state)
        {
            parent_.Logs?.Print($"onHeld callId:{callId} {state}");

            var callModel = collection_.Where(a => a.ID == callId).FirstOrDefault();
            callModel?.OnCallHeld(state);
        }

        public void OnCallSwitched(uint callId)
        {
            parent_.Logs?.Print($"onSwitched callId:{callId}");

            var callModel = collection_.Where(a => a.ID == callId).FirstOrDefault();            
            SwitchedCall = callModel;

            foreach (var c in collection_) c.OnCallSwitched();
        }

        static String parseExt(String uri)
        {
            //URI format: "displName" <sip:EXT@domain:port>
            int startIndex = uri.IndexOf(':');
            if (startIndex == -1) return "";

            int endIndex = uri.IndexOf('@', startIndex + 1);
            return (endIndex == -1) ? "" : uri.Substring(startIndex + 1, endIndex - startIndex - 1);
        }

        static String parseDisplayName(String uri)
        {
            //URI format: "DisplName" <sip:ext@domain:port>
            int startIndex = uri.IndexOf('"');
            if (startIndex == -1) return "";

            int endIndex = uri.IndexOf('"', startIndex + 1);
            return (endIndex == -1) ? "" : uri.Substring(startIndex + 1, endIndex - startIndex - 1);
        }

    }//CallsListModel


    /////////////////////////////////////////////////////////////////
    /// NetworkModel

    public class NetworkModel : INotifyPropertyChanged
    {
        readonly ObjModel parent_;
        public event PropertyChangedEventHandler? PropertyChanged;

        public NetworkModel(ObjModel parent)   { parent_ = parent; }

        public bool NetworkLost { get; private set; }

        //Event raised by SDK
        public void OnNetworkStateChanged(String name, NetworkState state)
        {
            parent_.Logs?.Print($"onNetworkStateChanged name:{name} {state}");
            NetworkLost = (state == NetworkState.NetworkLost);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("NetworkLost"));
        }

    }//NetworkModel


    /////////////////////////////////////////////////////////////////
    /// LogsModel
    public class LogsModel : INotifyPropertyChanged
    {
        string logText_ = "";        
        public event PropertyChangedEventHandler? PropertyChanged;

        public string LogText { get { return logText_; } }

        public void Print(string text)
        {
            this.logText_ += DateTime.Now.ToString("HH:mm:ss ");
            this.logText_ += text;            
            this.logText_ += System.Environment.NewLine;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LogText"));
        }

        public void OnTrialModeNotified()
        {
            Print("--- SIPRIX SDK is working in TRIAL mode ---");
        }

    }//LogsModel



    /////////////////////////////////////////////////////////////////
    /// ObjModel

    public class ObjModel : Siprix.IEventDelegate
    {
        AccountsListModel accountsListModel_;
        CallsListModel callsListModel_;
        NetworkModel networkModel_;
        LogsModel? logsModel_;

        Siprix.Module module_;

        AppDispatcher? dispatcher_;

        static ObjModel? instance = null;

        ObjModel()
        {
            //Create module
            module_ = new Siprix.Module();

            //Create models
            logsModel_ = new LogsModel();
            accountsListModel_ = new AccountsListModel(this);
            callsListModel_ = new CallsListModel(this);
            networkModel_ = new NetworkModel(this);
        }

        public AccountsListModel Accounts { get { return accountsListModel_; } }
        public CallsListModel Calls    { get { return callsListModel_; } } 
        public NetworkModel Networks { get { return networkModel_; } }
        public LogsModel? Logs    { get { return logsModel_; } }
        public Siprix.Module Module { get { return module_; } }
        public string ErrorText(int err) { return module_.ErrorText(err);  }

        public static ObjModel Instance 
        {
            get 
            {
                if (instance == null)
                {
                    instance = new ObjModel();
                }
                return instance;
            }
        }

        public void Initialize(AppDispatcher dispatcher)
        {
            if (module_.IsInitialized())
                return;
            
            dispatcher_ = dispatcher;

            Siprix.IniData iniData = new Siprix.IniData();
            iniData.License = "...license-credentials...";
            iniData.SingleCallMode = false;
            iniData.LogLevelIde = Siprix.LogLevel.Debug;
            iniData.LogLevelFile = Siprix.LogLevel.Debug;
            iniData.WriteDmpUnhandledExc = true;
    
            int err = module_.Initialize(this, iniData);

            if(err == Siprix.Module.kNoErr){                
                Logs?.Print("Siprix module initialized successfully");
                Logs?.Print($"Version: {module_.Version()}");

                loadSavedAccounts();
            }
            else{
                Logs?.Print($"Can't initialize Siprix module Err: {err} {ErrorText(err)}");
            }
        }

        public void UnInitialize()
        {
            int err = module_.UnInitialize();
            if (err == Siprix.Module.kNoErr){
                Logs?.Print("Siprix module uninitialized");                
            }
            else{
                Logs?.Print($"Can't uninitialize Siprix module Err: {err} {ErrorText(err)}");
            }
        }

        internal void loadSavedAccounts()
        {
            try
            {
                Logs?.Print("Loading accounts...");

                accountsListModel_.loadFromJson(SampleWpf.Properties.Settings.Default.accounts);

                Logs?.Print($"Loaded {accountsListModel_.Collection.Count} accounts");
            }
            catch (Exception e) {
                Logs?.Print(e.Message);
            }
        }

        internal void postSaveAccountsChanges()
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                string jsonStr = accountsListModel_.storeToJson();

                SampleWpf.Properties.Settings.Default.accounts = jsonStr;
                SampleWpf.Properties.Settings.Default.Save();
            }));
        }

        internal void postResolveContactName_(CallModel newCall)
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                //string str = newCall.NameAndExt;
                //TODO add 'ResolveContactName'    
                //newCall.setDisplName                
            }));
        }


        //Event raised by SDK
        public void OnTrialModeNotified()
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                logsModel_?.OnTrialModeNotified();
            }));            
        }

        public void OnDevicesAudioChanged()
        {
            //TODO add
        }

        public void OnAccountRegState(uint accId, RegState state, string response)
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                accountsListModel_.OnAccountRegState(accId, state, response);
            }));
        }

        public void OnNetworkState(string name, NetworkState state)
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                networkModel_.OnNetworkStateChanged(name, state);
            }));
        }

        public void OnPlayerState(uint playerId, PlayerState state)
        {
            //TODO add
        }

        public void OnRingerState(bool start)
        {
        }

        public void OnCallIncoming(uint callId, uint accId, bool withVideo, string hdrFrom, string hdrTo)
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                callsListModel_.OnCallIncoming(callId, accId, withVideo, hdrFrom, hdrTo);
            }));
        }

        public void OnCallConnected(uint callId, string hdrFrom, string hdrTo, bool withVideo)
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                callsListModel_.OnCallConnected(callId, hdrFrom, hdrTo, withVideo);
            }));
        }

        public void OnCallTerminated(uint callId, uint statusCode)
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                callsListModel_.OnCallTerminated(callId, statusCode);
            }));
        }

        public void OnCallProceeding(uint callId, string response)
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                callsListModel_.OnCallProceeding(callId, response);
            }));
        }

        public void OnCallTransferred(uint callId, uint statusCode)
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                callsListModel_.OnCallTransferred(callId, statusCode);
            }));
        }

        public void OnCallRedirected(uint origCallId, uint relatedCallId, string referTo)
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                callsListModel_.OnCallRedirected(origCallId, relatedCallId, referTo);
            }));
        }

        public void OnCallDtmfReceived(uint callId, ushort tone)
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                callsListModel_.OnCallDtmfReceived(callId, tone);
            }));
        }

        public void OnCallHeld(uint callId, HoldState state)
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                callsListModel_.OnCallHeld(callId, state);
            }));
        }

        public void OnCallSwitched(uint callId)
        {
            dispatcher_?.BeginInvoke(new Action(() => {
                callsListModel_.OnCallSwitched(callId);
            }));
        }       

    }//ObjModel



    /// A command whose sole purpose is to relay its functionality to other
    /// objects by invoking delegates.
    public class RelayCommand : ICommand
    {   
        readonly Func<bool>? canExecute_;
        readonly Action      execute_;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            execute_    = execute;
            canExecute_ = canExecute;
        }

        [DebuggerStepThrough]
        public bool CanExecute(object? parameter)
        {
            return canExecute_ == null ? true : canExecute_();
        }

        public event EventHandler? CanExecuteChanged
        {
            add{
                if (canExecute_ != null)
                    CommandManager.RequerySuggested += value;
            }
            remove { 
                if (canExecute_ != null)
                    CommandManager.RequerySuggested -= value;
            }
        }

        public void Execute(object? parameter)
        {
            execute_();
        }

    }//RelayCommand

}//namespace SampleWpf
