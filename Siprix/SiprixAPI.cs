using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Siprix
{
    using AccountId = uint;
    using SubscriptionId = uint;
    using PlayerId = uint;
    using CallId = uint;

    using ErrorCode = int;
    using static System.Net.Mime.MediaTypeNames;
    
    [Flags]
    public enum LogLevel : byte
    {
        Stack = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        NoLog = 5
    }

    public enum RegState : byte
    {
        Success = 0, //Registeration success
        Failed,    //Registration failed
        Removed,   //Registration removed
        InProgress
    };

    public enum SubscriptionState : byte
    {
        Created = 0, //Subscription just created and waiting response
        Updated,    //(received NOTIFY)
        Destroyed   //Received error (timeout) on initial SUBSCRIBE or app unsubscribed it
    };

    public enum SecureMedia : byte
    {
        Disabled = 0,        
        SdesSrtp,
        DtlsSrtp,
    };

    public enum SipTransport : byte
    {
        UDP = 0,
        TCP,
        TLS,
    };

    public enum DtmfMethod : byte
    {
        DTMF_RTP = 0,
        DTMF_INFO
    };

    public enum AudioCodec : byte
    {
        Opus = 65,
        ISAC16 = 66,
        ISAC32 = 67,
        G722 = 68,
        ILBC = 69,
        PCMU = 70,
        PCMA = 71,
        DTMF = 72,
        CN = 73
    };

    public enum VideoCodec : byte
    {
        H264 = 80,
        VP8 = 81,
        VP9 = 82,
        AV1 = 83
    };


    public enum HoldState : byte
    {
        None = 0,
        Local = 1,
        Remote = 2,
        LocalAndRemote = 3
    };

    public enum PlayerState : byte
    {
        PlayerStarted = 0,
        PlayerStopped = 1,
        PlayerFailed = 2,
    };

    public enum NetworkState : byte
    {
        NetworkLost = 0,
        NetworkRestored = 1,
        NetworkSwitched = 2
    };

    public enum CallState
    {
        Dialing,      //Outgoing call just initiated
        Proceeding,   //Outgoing call in progress, received 100Trying or 180Ringing

        Ringing,      //Incoming call just received
        Rejecting,    //Incoming call rejecting after invoke 'call.reject'
        Accepting,    //Incoming call aceepting after invoke 'call.accept'

        Connected,    //Call successfully established, RTP is flowing

        Disconnecting,//Call disconnecting after invoke 'call.bye'

        Holding,      //Call holding (renegotiating RTP stream states)
        Held,         //Call held, RTP is NOT flowing

        Transferring, //Call transferring
    }


    public class IniData
    {
        public string?   License;
        public LogLevel? LogLevelFile;
        public LogLevel? LogLevelIde;
        public bool?     ShareUdpTransport;
        public bool?     WriteDmpUnhandledExc;
        public bool?     TlsVerifyServer;
        public bool?     SingleCallMode;
        public ushort?   RtpStartPort;
        public string?   HomeFolder;
        public List<string>? DnsServers;

    }//IniData


    public class AccData
    {
        public AccountId MyAccId = 0;//Assigned by module in 'Account_Add'
        public string  SipServer ="";
        public string  SipExtension = "";        
        public string  SipPassword = "";
         
        public uint    ExpireTime = 300;
        public string? SipAuthId;
        public string? SipProxyServer;

        public string? UserAgent;
        public string? DisplayName;
        public string? InstanceId;
        public string? RingToneFile;

        public SecureMedia?   SecureMediaMode;
        public bool?          UseSipSchemeForTls;
        public bool?          RtcpMuxEnabled;
        public uint?          KeepAliveTime;

        public SipTransport?  TranspProtocol;
        public ushort?        TranspPort;
        public string?        TranspTlsCaCert;
        public string?        TranspBindAddr;
        public bool?          TranspPreferIPv6;
        public bool?          RewriteContactIp;
        public bool?          VerifyIncomingCall;
        
        public List<AudioCodec>? AudioCodecs;
        public List<VideoCodec>? VideoCodecs;
        public Dictionary<String, String>? Xheaders;

    }//AccData


    public class DestData
    {
        public CallId    MyCallId = 0;     //Assigned by module in 'Call_Invite'
        public String    ToExt = "";
        public AccountId FromAccId = 0;
        public bool      WithVideo = false;
        public int?      InviteTimeout;
        public Dictionary<String, String>? Xheaders;

    }//DestData

    public class SubscrData
    {
        public SubscriptionId MySubId = 0;     //Assigned by module in 'Subscription_Add'
        public String    ToExt = "";
        public AccountId FromAccId = 0;
        public string    MimeSubType="";
        public string    EventType="";
        public uint?     ExpireTime;
    }

    public interface IEventDelegate
    {
        void OnTrialModeNotified();
        void OnDevicesAudioChanged();
        
        void OnAccountRegState(AccountId accId, RegState state, string response);
        void OnSubscriptionState(SubscriptionId subId, SubscriptionState state, string response);
        void OnNetworkState(string name, NetworkState state);
        void OnPlayerState(PlayerId playerId, PlayerState state);
        void OnRingerState(bool start);
        
        void OnCallIncoming(CallId callId, AccountId accId, bool withVideo, string hdrFrom, string hdrTo);
        void OnCallConnected(CallId callId, string hdrFrom, string hdrTo, bool withVideo);
        void OnCallTerminated(CallId callId, uint statusCode);
        void OnCallProceeding(CallId callId, string response);
        void OnCallTransferred(CallId callId, uint statusCode);
        void OnCallRedirected(CallId origCallId, CallId relatedCallId, string referTo);
        void OnCallDtmfReceived(CallId callId, ushort tone);
        void OnCallHeld(CallId callId, HoldState state);
        void OnCallSwitched(CallId callId);
    }


    public class Module : IDisposable
    {
        IntPtr modulePtr_;
        IEventDelegate? eventDelegate_;
        const string DllName = "siprix.dll";
        
        public const ErrorCode kNoErr = 0;
        public const uint kInvalidId = 0;

        private readonly OnTrialModeNotified   onTrialModeNotified_;
        private readonly OnDevicesAudioChanged onDevicesAudioChanged_;

        private readonly OnAccountRegState     onAccountRegState_;
        private readonly OnSubscriptionState   onSubscriptionState_;
        private readonly OnNetworkState        onNetworkState_;
        private readonly OnPlayerState         onPlayerState_;
        private readonly OnRingerState         onRingerState_;

        private readonly OnCallIncoming        onCallIncoming_;
        private readonly OnCallConnected       onCallConnected_;
        private readonly OnCallTerminated      onCallTerminated_;
        private readonly OnCallProceeding      onCallProceeding_;
        private readonly OnCallTransferred     onCallTransferred_;
        private readonly OnCallRedirected      onCallRedirected_;
        private readonly OnCallDtmfReceived    onCallDtmfReceived_;
        private readonly OnCallHeld            onCallHeld_;
        private readonly OnCallSwitched        onCallSwitched_;

        public Module()
        {
            onTrialModeNotified_   = new OnTrialModeNotified     (OnTrialModeNotifiedCallback);
            onDevicesAudioChanged_ = new OnDevicesAudioChanged   (OnDevicesAudioChangedCallback);

            onAccountRegState_     = new OnAccountRegState       (OnAccountRegStateCallback);
            onSubscriptionState_   = new OnSubscriptionState     (OnSubscriptionStateCallback);
            onNetworkState_        = new OnNetworkState          (OnNetworkStateCallback);
            onPlayerState_         = new OnPlayerState           (OnPlayerStateCallback);
            onRingerState_         = new OnRingerState           (OnRingerStateCallback);
                                    
            onCallIncoming_        = new OnCallIncoming          (OnCallIncomingCallback);
            onCallConnected_       = new OnCallConnected         (OnCallConnectedCallback);
            onCallTerminated_      = new OnCallTerminated        (OnCallTerminatedCallback);
            onCallProceeding_      = new OnCallProceeding        (OnCallProceedingCallback);
            onCallTransferred_     = new OnCallTransferred       (OnCallTransferredCallback);
            onCallRedirected_      = new OnCallRedirected        (OnCallRedirectedCallback);
            onCallDtmfReceived_    = new OnCallDtmfReceived      (OnCallDtmfReceivedCallback);
            onCallHeld_            = new OnCallHeld              (OnCallHeldCallback);
            onCallSwitched_        = new OnCallSwitched          (OnCallSwitchedCallback);
        }

        ~Module()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {            
            }
                        
            Module_UnInitialize(modulePtr_);
            modulePtr_ = IntPtr.Zero;
        }


        //---------------------------------------------
        //Module

        public ErrorCode Initialize(IEventDelegate eventDelegate, IniData iniData)
        {
            if(modulePtr_ == IntPtr.Zero)
                modulePtr_ = Module_Create();

            ErrorCode err = Module_Initialize(modulePtr_, getNative(iniData));
            if(err != kNoErr) return err;
            
            eventDelegate_ = eventDelegate;

            Callback_SetTrialModeNotified(modulePtr_,    onTrialModeNotified_);
            Callback_SetDevicesAudioChanged(modulePtr_,  onDevicesAudioChanged_);

            Callback_SetAccountRegState(modulePtr_,      onAccountRegState_);
            Callback_SetSubscriptionState(modulePtr_,    onSubscriptionState_);
            Callback_SetNetworkState(modulePtr_,         onNetworkState_);
            Callback_SetPlayerState(modulePtr_,          onPlayerState_);
            Callback_SetRingerState(modulePtr_,          onRingerState_);

            Callback_SetCallProceeding(modulePtr_,       onCallProceeding_);
            Callback_SetCallTerminated(modulePtr_,       onCallTerminated_);
            Callback_SetCallConnected(modulePtr_,        onCallConnected_);
            Callback_SetCallIncoming(modulePtr_,         onCallIncoming_);
            Callback_SetCallDtmfReceived(modulePtr_,     onCallDtmfReceived_);
            Callback_SetCallTransferred(modulePtr_,      onCallTransferred_);
            Callback_SetCallRedirected(modulePtr_,       onCallRedirected_);
            Callback_SetCallSwitched(modulePtr_,         onCallSwitched_);
            Callback_SetCallHeld(modulePtr_,             onCallHeld_);
            return err;
        }

        public ErrorCode UnInitialize()
        {
            return Module_UnInitialize(modulePtr_);
        }

        public bool IsInitialized()
        {
            return (modulePtr_ != IntPtr.Zero) ? Module_IsInitialized(modulePtr_) : false;
        }

        public string HomeFolder()
        {
            IntPtr strPtr = (modulePtr_ != IntPtr.Zero) ? Module_HomeFolder(modulePtr_) : 0;
            string? path = Marshal.PtrToStringUTF8(strPtr);
            return (path == null) ? "" : path;
        }

        public string Version()
        {
            IntPtr strPtr = (modulePtr_ != IntPtr.Zero) ? Module_Version(modulePtr_) : 0;
            string? ver = Marshal.PtrToStringUTF8(strPtr);
            return (ver==null) ? "" : ver;
        }

        public uint VersionCode()
        {
            return (modulePtr_ != IntPtr.Zero) ? Module_VersionCode(modulePtr_) : 0;
        }

        public string ErrorText(ErrorCode code)
        {
            IntPtr strPtr = GetErrorText(code);
            string? ver = Marshal.PtrToStringUTF8(strPtr);
            return (ver == null) ? "" : ver;
        }

        /// [Account] ///////////////////////////////////////////////////////////////////////////////////////////////
        public ErrorCode Account_Add(AccData accData)
        {
            return Account_Add(modulePtr_, getNative(accData), ref accData.MyAccId);
        }

        public ErrorCode Account_Update(AccData accData, AccountId accId)
        {
            return Account_Update(modulePtr_, getNative(accData), accId);
        }

        public ErrorCode Account_GetRegState(AccountId accId, ref RegState state)
        {
            return Account_GetRegState(modulePtr_, accId, ref state);
        }

        public ErrorCode Account_Register(AccountId accId, uint expireTime)
        {
            return Account_Register(modulePtr_, accId, expireTime);
        }

        public ErrorCode Account_Unregister(AccountId accId)
        {
            return Account_Unregister(modulePtr_, accId);
        }

        public ErrorCode Account_Delete(AccountId accId)
        {
            return Account_Delete(modulePtr_, accId);
        }


        /// [Calls] ///////////////////////////////////////////////////////////////////////////////////////////////

        public ErrorCode Call_Invite(DestData dest)
        {
            return Call_Invite(modulePtr_, getNative(dest), ref dest.MyCallId);
        }

        public ErrorCode Call_Reject(CallId callId, uint statusCode=486)
        {
            return Call_Reject(modulePtr_, callId, statusCode);
        }

        public ErrorCode Call_Accept(CallId callId, bool withVideo)
        {
            return Call_Accept(modulePtr_, callId, withVideo);
        }

        public ErrorCode Call_Hold(CallId callId)
        {
            return Call_Hold(modulePtr_, callId);
        }

        public ErrorCode Call_GetHoldState(CallId callId, ref HoldState state)
        {
            return Call_GetHoldState(modulePtr_, callId, ref state);
        }

        public string Call_GetSipHeader(CallId callId, string hdrName)
        {
            uint hdrValLen = 0;
            Call_GetSipHeader(modulePtr_, callId, hdrName, null, ref hdrValLen);
            if (hdrValLen > 0)
            {
                var sb = new StringBuilder((int)(hdrValLen+1));
                Call_GetSipHeader(modulePtr_, callId, hdrName, sb, ref hdrValLen);
                return sb.ToString();
            }
            else return string.Empty;
        }

        public ErrorCode Call_MuteMic(CallId callId, bool mute)
        {
            return Call_MuteMic(modulePtr_, callId, mute);
        }

        public ErrorCode Call_MuteCam(CallId callId, bool mute)
        {
            return Call_MuteCam(modulePtr_, callId, mute);
        }        

        public ErrorCode Call_SendDtmf(CallId callId, string dtmfs, 
            Int16 durationMs, Int16 intertoneGapMs, DtmfMethod method)
        {
            return Call_SendDtmf(modulePtr_, callId, dtmfs, durationMs, intertoneGapMs, method);
        }

        public ErrorCode Call_PlayFile(CallId callId, string pathToMp3File, bool loop, ref PlayerId playerId)
        {
            return Call_PlayFile(modulePtr_, callId, pathToMp3File, loop, ref playerId);
        }

        public ErrorCode Call_StopFile(PlayerId playerId)
        {
            return Call_StopFile(modulePtr_, playerId);
        }

        public ErrorCode Call_StartRecording(CallId callId, string pathToMp3File)
        {
            return Call_StartRecording(modulePtr_, callId, pathToMp3File);
        }

        public ErrorCode Call_StopRecording(CallId callId)
        {
            return Call_StopRecording(modulePtr_, callId);
        }

        public ErrorCode Call_TransferBlind(CallId callId, string toExt)
        {
            return Call_TransferBlind(modulePtr_, callId, toExt);
        }

        public ErrorCode Call_TransferAttended(CallId fromCallId, CallId toCallId)
        {
            return Call_TransferAttended(modulePtr_, fromCallId, toCallId);
        }

        public ErrorCode Call_SetVideoWindow(CallId callId, IntPtr hwnd)
        {
            return Call_SetVideoWindow(modulePtr_, callId, hwnd);
        }

        public ErrorCode Call_Bye(CallId callId)
        {
            return Call_Bye(modulePtr_, callId);
        }


        /// [Mixer] ///////////////////////////////////////////////////////////////////////////////////////////////
        public ErrorCode Mixer_SwitchToCall(CallId callId)
        {
            return Mixer_SwitchToCall(modulePtr_, callId);
        }

        public ErrorCode Mixer_MakeConference()
        {
            return Mixer_MakeConference(modulePtr_);
        }

        /// [Subscriptions] ///////////////////////////////////////////////////////////////////////////////////////////////
        public ErrorCode Subscription_Add(SubscrData subData)
        {
            return Subscription_Create(modulePtr_, getNative(subData), ref subData.MySubId);
        }

        public ErrorCode Subscription_Delete(SubscriptionId subId)
        {
            return Subscription_Destroy(modulePtr_, subId);
        }


        /// [Module] ///////////////////////////////////////////////////////////////////////////////////////////////
        [DllImport(DllName)]
        private static extern IntPtr Module_Create();
        [DllImport(DllName)]
        private static extern ErrorCode Module_Initialize(IntPtr modulePtr, IntPtr iniDataPtr);
        [DllImport(DllName)]
        private static extern ErrorCode Module_UnInitialize(IntPtr modulePtr);
        [DllImport(DllName)]
        private static extern bool Module_IsInitialized(IntPtr modulePtr);
        [DllImport(DllName)]        
        private static extern IntPtr Module_HomeFolder(IntPtr modulePtr);
        [DllImport(DllName)]
        private static extern IntPtr Module_Version(IntPtr modulePtr);
        [DllImport(DllName)]
        private static extern uint Module_VersionCode(IntPtr modulePtr);
        [DllImport(DllName)]
        private static extern IntPtr GetErrorText(ErrorCode code);


        /// [Ini] ///////////////////////////////////////////////////////////////////////////////////////////////
        [DllImport(DllName)]
        private static extern IntPtr Ini_GetDefault();
        [DllImport(DllName)]
        private static extern void Ini_SetLicense(IntPtr ini, [MarshalAs(UnmanagedType.LPUTF8Str)] string license);
        [DllImport(DllName)]
        private static extern void Ini_SetLogLevelFile(IntPtr ini, LogLevel logLevel);
        [DllImport(DllName)]
        private static extern void Ini_SetLogLevelIde(IntPtr ini, LogLevel logLevel);
        [DllImport(DllName)]
        private static extern void Ini_SetShareUdpTransport(IntPtr ini, bool shareUdpTransport);
        [DllImport(DllName)]
        private static extern void Ini_SetAllocStrArg(IntPtr ini, bool callbackAllocStringArgs);
        [DllImport(DllName)]
        private static extern void Ini_SetUseExternalRinger(IntPtr ini, bool useExternalRinger);
        [DllImport(DllName)]
        private static extern void Ini_SetDmpOnUnhandledExc(IntPtr ini, bool writeDmpUnhandledExc);
        [DllImport(DllName)]
        private static extern void Ini_SetTlsVerifyServer(IntPtr ini, bool tlsVerifyServer);
        [DllImport(DllName)]
        private static extern void Ini_SetSingleCallMode(IntPtr ini, bool singleCallMode);
        [DllImport(DllName)]
        private static extern void Ini_SetRtpStartPort(IntPtr ini, ushort rtpStartPort);
        [DllImport(DllName)]
        private static extern void Ini_SetHomeFolder(IntPtr ini, [MarshalAs(UnmanagedType.LPUTF8Str)] string homeFolder);
        [DllImport(DllName)]
        private static extern void Ini_AddDnsServer(IntPtr ini, [MarshalAs(UnmanagedType.LPUTF8Str)]  string dns);

        private IntPtr getNative(IniData iniData)
        {
            IntPtr ptr = Ini_GetDefault();
            if (iniData.License              != null) Ini_SetLicense(ptr,           iniData.License);
            if (iniData.LogLevelFile         != null) Ini_SetLogLevelFile(ptr,      iniData.LogLevelFile.Value);
            if (iniData.LogLevelIde          != null) Ini_SetLogLevelIde(ptr,       iniData.LogLevelIde.Value);
            if (iniData.ShareUdpTransport    != null) Ini_SetShareUdpTransport(ptr, iniData.ShareUdpTransport.Value);
            if (iniData.WriteDmpUnhandledExc != null) Ini_SetDmpOnUnhandledExc(ptr, iniData.WriteDmpUnhandledExc.Value);
            if (iniData.SingleCallMode       != null) Ini_SetSingleCallMode(ptr,    iniData.SingleCallMode.Value);
            if (iniData.RtpStartPort         != null) Ini_SetRtpStartPort(ptr,      iniData.RtpStartPort.Value);
            if (iniData.HomeFolder           != null) Ini_SetHomeFolder(ptr,        iniData.HomeFolder);

            if (iniData.DnsServers != null)
            {
                foreach (var dns in iniData.DnsServers) 
                    Ini_AddDnsServer(ptr, dns);
            }
            return ptr;
        }

        /// [Acc] ///////////////////////////////////////////////////////////////////////////////////////////////
        [DllImport(DllName)]
        private static extern IntPtr Acc_GetDefault();
        [DllImport(DllName)]
        private static extern void Acc_SetSipServer(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string sipServer);
        [DllImport(DllName)]
        private static extern void Acc_SetSipExtension(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string sipExtension);
        [DllImport(DllName)]
        private static extern void Acc_SetSipAuthId(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string sipAuthId);
        [DllImport(DllName)]
        private static extern void Acc_SetSipPassword(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string sipPassword);
        [DllImport(DllName)]
        private static extern void Acc_SetExpireTime(IntPtr acc, uint expireTime);
        [DllImport(DllName)]
        private static extern void Acc_SetSipProxyServer(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string sipProxyServer);
        
        [DllImport(DllName)]        
        private static extern void Acc_SetStunServer(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string stunServer);
        [DllImport(DllName)]
        private static extern void Acc_SetTurnServer(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string turnServer);
        [DllImport(DllName)]
        private static extern void Acc_SetTurnUser(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string turnUser);
        [DllImport(DllName)]
        private static extern void Acc_SetTurnPassword(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string turnPassword);

        [DllImport(DllName)]
        private static extern void Acc_SetUserAgent(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string userAgent);
        [DllImport(DllName)]
        private static extern void Acc_SetDisplayName(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string displayName);
        [DllImport(DllName)]
        private static extern void Acc_SetInstanceId(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string instanceId);
        [DllImport(DllName)]
        private static extern void Acc_SetRingToneFile(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string ringTonePath);        

        [DllImport(DllName)]
        private static extern void Acc_SetSecureMediaMode(IntPtr acc, SecureMedia mode);
        [DllImport(DllName)]
        private static extern void Acc_SetUseSipSchemeForTls(IntPtr acc, bool useSipSchemeForTls);
        [DllImport(DllName)]
        private static extern void Acc_SetRtcpMuxEnabled(IntPtr acc, bool rtcpMuxEnabled);
        [DllImport(DllName)]
        private static extern void Acc_SetIceEnabled(IntPtr acc, bool iceEnabled);

        [DllImport(DllName)]
        private static extern void Acc_SetKeepAliveTime(IntPtr acc, uint keepAliveTimeSec);
        [DllImport(DllName)]
        private static extern void Acc_SetTranspProtocol(IntPtr acc, SipTransport transp);
        [DllImport(DllName)]
        private static extern void Acc_SetTranspPort(IntPtr acc, ushort transpPort);
        [DllImport(DllName)]
        private static extern void Acc_SetTranspTlsCaCert(IntPtr acc, 
                                    [MarshalAs(UnmanagedType.LPUTF8Str)] string pathToCaCertPem);
        [DllImport(DllName)]
        private static extern void Acc_SetTranspBindAddr(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string ipAddr);
        [DllImport(DllName)]
        private static extern void Acc_SetTranspPreferIPv6(IntPtr acc, bool prefer);

        [DllImport(DllName)]
        private static extern void Acc_AddXHeader(IntPtr acc, [MarshalAs(UnmanagedType.LPUTF8Str)] string header,
                                                              [MarshalAs(UnmanagedType.LPUTF8Str)] string value);
        [DllImport(DllName)]
        private static extern void Acc_SetRewriteContactIp(IntPtr acc, bool enabled);

        [DllImport(DllName)]
        private static extern void Acc_SetVerifyIncomingCall(IntPtr acc, bool enabled);


        [DllImport(DllName)]
        private static extern void Acc_AddAudioCodec(IntPtr acc, AudioCodec codec);
        [DllImport(DllName)]
        private static extern void Acc_AddVideoCodec(IntPtr acc, VideoCodec codec);
        [DllImport(DllName)]
        private static extern void Acc_ResetAudioCodecs(IntPtr acc);
        [DllImport(DllName)]
        private static extern void Acc_ResetVideoCodecs(IntPtr acc);
        
        private IntPtr getNative(AccData accData)
        {
            IntPtr ptr = Acc_GetDefault();
            Acc_SetSipServer(ptr,    accData.SipServer);
            Acc_SetSipExtension(ptr, accData.SipExtension);
            Acc_SetSipPassword(ptr,  accData.SipPassword);
            Acc_SetExpireTime(ptr,   accData.ExpireTime);

            if (accData.SipAuthId         != null) Acc_SetSipAuthId(ptr,          accData.SipAuthId);
            if (accData.SipProxyServer    != null) Acc_SetSipProxyServer(ptr,     accData.SipProxyServer);

            if (accData.UserAgent         != null) Acc_SetUserAgent(ptr,          accData.UserAgent);
            if (accData.DisplayName       != null) Acc_SetDisplayName(ptr,        accData.DisplayName);
            if (accData.InstanceId        != null) Acc_SetInstanceId(ptr,         accData.InstanceId);
            if (accData.RingToneFile      != null) Acc_SetRingToneFile(ptr,       accData.RingToneFile);

            if (accData.SecureMediaMode   != null) Acc_SetSecureMediaMode(ptr,    accData.SecureMediaMode.Value);
            if (accData.UseSipSchemeForTls!= null) Acc_SetUseSipSchemeForTls(ptr, accData.UseSipSchemeForTls.Value);
            if (accData.RtcpMuxEnabled    != null) Acc_SetRtcpMuxEnabled(ptr,     accData.RtcpMuxEnabled.Value);
            if (accData.KeepAliveTime     != null) Acc_SetKeepAliveTime(ptr,      accData.KeepAliveTime.Value);

            if (accData.TranspProtocol    != null) Acc_SetTranspProtocol(ptr,     accData.TranspProtocol.Value);
            if (accData.TranspPort        != null) Acc_SetTranspPort(ptr,         accData.TranspPort.Value);
            if (accData.TranspTlsCaCert   != null) Acc_SetTranspTlsCaCert(ptr,    accData.TranspTlsCaCert);
            if (accData.TranspBindAddr    != null) Acc_SetTranspBindAddr(ptr,     accData.TranspBindAddr);
            if (accData.TranspPreferIPv6  != null) Acc_SetTranspPreferIPv6(ptr,   accData.TranspPreferIPv6.Value);
            if (accData.RewriteContactIp  != null) Acc_SetRewriteContactIp(ptr,   accData.RewriteContactIp.Value);
            if (accData.VerifyIncomingCall!= null) Acc_SetVerifyIncomingCall(ptr, accData.VerifyIncomingCall.Value);

            if (accData.AudioCodecs != null)
            {
                Acc_ResetAudioCodecs(ptr);
                foreach (var ac in accData.AudioCodecs)
                    Acc_AddAudioCodec(ptr, ac);
            }

            if (accData.VideoCodecs != null)
            {
                Acc_ResetVideoCodecs(ptr);
                foreach (var vc in accData.VideoCodecs)
                    Acc_AddVideoCodec(ptr, vc);
            }

            if (accData.Xheaders != null)
            {
                foreach (var hdr in accData.Xheaders) 
                    Acc_AddXHeader(ptr, hdr.Key, hdr.Value);
            }

            return ptr;
        }


        /// [Accounts] ///////////////////////////////////////////////////////////////////////////////////////////////
        [DllImport(DllName)]
        private static extern ErrorCode Account_Add(IntPtr module, IntPtr acc, ref AccountId accId);
        [DllImport(DllName)]
        private static extern ErrorCode Account_Update(IntPtr module, IntPtr acc, AccountId accId);
        [DllImport(DllName)]
        private static extern ErrorCode Account_GetRegState(IntPtr module, AccountId accId, ref RegState state);
        [DllImport(DllName)]
        private static extern ErrorCode Account_Register(IntPtr module, AccountId accId, uint expireTime);
        [DllImport(DllName)]
        private static extern ErrorCode Account_Unregister(IntPtr module, AccountId accId);
        [DllImport(DllName)]
        private static extern ErrorCode Account_Delete(IntPtr module, AccountId accId);


        /// [Dest] ///////////////////////////////////////////////////////////////////////////////////////////////
        [DllImport(DllName)]
        private static extern IntPtr Dest_GetDefault();
        [DllImport(DllName)]
        private static extern void Dest_SetExtension(IntPtr dest, [MarshalAs(UnmanagedType.LPUTF8Str)] string extension);
        [DllImport(DllName)]
        private static extern void Dest_SetAccountId(IntPtr dest, AccountId accId);
        [DllImport(DllName)]
        private static extern void Dest_SetVideoCall(IntPtr dest, bool video);
        [DllImport(DllName)]
        private static extern void Dest_SetInviteTimeout(IntPtr dest, int inviteTimeoutSec);
        [DllImport(DllName)]
        private static extern void Dest_AddXHeader(IntPtr dest, [MarshalAs(UnmanagedType.LPUTF8Str)] string header,
                                                                [MarshalAs(UnmanagedType.LPUTF8Str)] string value);
        private IntPtr getNative(DestData destData)
        {
            IntPtr ptr = Dest_GetDefault();
            Dest_SetExtension(ptr, destData.ToExt);
            Dest_SetAccountId(ptr, destData.FromAccId);
            Dest_SetVideoCall(ptr, destData.WithVideo);
            
            if (destData.InviteTimeout != null) 
                Dest_SetInviteTimeout(ptr, destData.InviteTimeout.Value);

            if (destData.Xheaders != null)
            {
                foreach (var hdr in destData.Xheaders)
                    Dest_AddXHeader(ptr, hdr.Key, hdr.Value);
            }

            return ptr;
        }

        /// [Calls] ///////////////////////////////////////////////////////////////////////////////////////////////
        [DllImport(DllName)]
        private static extern ErrorCode Call_Invite(IntPtr module, IntPtr destination, ref CallId callId);
        [DllImport(DllName)]
        private static extern ErrorCode Call_Reject(IntPtr module, CallId callId, uint statusCode);
        [DllImport(DllName)]
        private static extern ErrorCode Call_Accept(IntPtr module, CallId callId, bool withVideo);
        [DllImport(DllName)]
        private static extern ErrorCode Call_Hold(IntPtr module, CallId callId);
        [DllImport(DllName)]
        private static extern ErrorCode Call_GetHoldState(IntPtr module, CallId callId, ref HoldState state);
        [DllImport(DllName)]
        private static extern ErrorCode Call_GetSipHeader(IntPtr module, CallId callId,
                                [MarshalAs(UnmanagedType.LPUTF8Str)] string hdrName,
                                [MarshalAs(UnmanagedType.LPStr)] StringBuilder? hdrVal, 
                                ref uint hdrValLen);
        [DllImport(DllName)]
        private static extern ErrorCode Call_MuteMic(IntPtr module, CallId callId, bool mute);
        [DllImport(DllName)]
        private static extern ErrorCode Call_MuteCam(IntPtr module, CallId callId, bool mute);
        [DllImport(DllName)]
        private static extern ErrorCode Call_SendDtmf(IntPtr module, CallId callId,
                                        [MarshalAs(UnmanagedType.LPUTF8Str)] string dtmfs, 
                                        Int16 durationMs, Int16 intertoneGapMs, DtmfMethod method);
        [DllImport(DllName)]
        private static extern ErrorCode Call_PlayFile(IntPtr module, CallId callId, 
                                        [MarshalAs(UnmanagedType.LPUTF8Str)] string pathToMp3File, bool loop,
                                        ref PlayerId playerId);
        [DllImport(DllName)]
        private static extern ErrorCode Call_StopFile(IntPtr module, PlayerId playerId);
        [DllImport(DllName)]
        private static extern ErrorCode Call_StartRecording(IntPtr module, CallId callId,
                                        [MarshalAs(UnmanagedType.LPUTF8Str)] string pathToMp3File);
        [DllImport(DllName)]
        private static extern ErrorCode Call_StopRecording(IntPtr module, CallId callId);
        [DllImport(DllName)]
        private static extern ErrorCode Call_TransferBlind(IntPtr module, CallId callId,
                                        [MarshalAs(UnmanagedType.LPUTF8Str)] string toExt);
        [DllImport(DllName)]
        private static extern ErrorCode Call_TransferAttended(IntPtr module, CallId fromCallId, 
                                        CallId toCallId);
        [DllImport(DllName)]
        private static extern ErrorCode Call_SetVideoWindow(IntPtr module, CallId callId, IntPtr hwnd);

        [DllImport(DllName)]
        private static extern ErrorCode Call_Bye(IntPtr module, CallId callId);


        /// [Mixer] ///////////////////////////////////////////////////////////////////////////////////////////////
        [DllImport(DllName)]
        private static extern ErrorCode Mixer_SwitchToCall(IntPtr module, CallId callId);
        [DllImport(DllName)]
        private static extern ErrorCode Mixer_MakeConference(IntPtr module);

        /// [Subscriptions] ///////////////////////////////////////////////////////////////////////////////////////////////
        [DllImport(DllName)]
        private static extern ErrorCode Subscription_Create(IntPtr module, IntPtr sub, ref SubscriptionId subId);
        [DllImport(DllName)]
        private static extern ErrorCode Subscription_Destroy(IntPtr module, SubscriptionId subId);


        /// [SubscrData] ///////////////////////////////////////////////////////////////////////////////////////////////
        [DllImport(DllName)]
        private static extern IntPtr Subscr_GetDefault();
        [DllImport(DllName)]
        private static extern void Subscr_SetExtension(IntPtr sub, [MarshalAs(UnmanagedType.LPUTF8Str)] string extension);
        [DllImport(DllName)]
        private static extern void Subscr_SetAccountId(IntPtr sub, SubscriptionId subId);
        [DllImport(DllName)]
        private static extern void Subscr_SetMimeSubtype(IntPtr sub, [MarshalAs(UnmanagedType.LPUTF8Str)] string mimeType);
        [DllImport(DllName)]
        private static extern void Subscr_SetEventType(IntPtr sub, [MarshalAs(UnmanagedType.LPUTF8Str)] string eventType);
        [DllImport(DllName)]
        private static extern void Subscr_SetExpireTime(IntPtr dest, uint expireTimeSec);
        
        private IntPtr getNative(SubscrData subData)
        {
            IntPtr ptr = Subscr_GetDefault();
            Subscr_SetExtension(ptr,   subData.ToExt);
            Subscr_SetAccountId(ptr,   subData.FromAccId);
            Subscr_SetMimeSubtype(ptr, subData.MimeSubType);
            Subscr_SetEventType(ptr,   subData.EventType);

            if (subData.ExpireTime != null)
                Subscr_SetExpireTime(ptr, subData.ExpireTime.Value);

            return ptr;
        }

        /// [Callbacks] ///////////////////////////////////////////////////////////////////////////////////////////////
        private delegate void OnTrialModeNotified();
        private delegate void OnDevicesAudioChanged();        
        private delegate void OnAccountRegState(AccountId accId, RegState state, 
                                            [MarshalAs(UnmanagedType.LPUTF8Str)] string response);
        private delegate void OnSubscriptionState(SubscriptionId subId, SubscriptionState state,
                                            [MarshalAs(UnmanagedType.LPUTF8Str)] string response);
        private delegate void OnNetworkState([MarshalAs(UnmanagedType.LPUTF8Str)] string name,
                                            NetworkState state);
        private delegate void OnPlayerState(PlayerId playerId, PlayerState state);
        private delegate void OnRingerState(bool start);

        private delegate void OnCallIncoming(CallId callId, AccountId accId, bool withVideo, 
                                            [MarshalAs(UnmanagedType.LPUTF8Str)] string hdrFrom,
                                            [MarshalAs(UnmanagedType.LPUTF8Str)] string hdrTo);
        private delegate void OnCallConnected(CallId callId, 
                                            [MarshalAs(UnmanagedType.LPUTF8Str)] string hdrFrom, 
                                            [MarshalAs(UnmanagedType.LPUTF8Str)] string hdrTo,
                                            bool withVideo);
        private delegate void OnCallTerminated(CallId callId, uint statusCode);
        private delegate void OnCallProceeding(CallId callId, 
                                            [MarshalAs(UnmanagedType.LPUTF8Str)] string response);
        private delegate void OnCallTransferred(CallId callId, uint statusCode);
        private delegate void OnCallRedirected(CallId origCallId, CallId relatedCallId, 
                                            [MarshalAs(UnmanagedType.LPUTF8Str)] string referTo);
        private delegate void OnCallDtmfReceived(CallId callId, ushort tone);
        private delegate void OnCallHeld(CallId callId, HoldState state);
        private delegate void OnCallSwitched(CallId callId);


        [DllImport(DllName)]
        private static extern ErrorCode Callback_SetTrialModeNotified(IntPtr module, OnTrialModeNotified callback);
        [DllImport(DllName)]
        private static extern ErrorCode Callback_SetDevicesAudioChanged(IntPtr module, OnDevicesAudioChanged callback);
        [DllImport(DllName)]
        private static extern ErrorCode Callback_SetAccountRegState(IntPtr module, OnAccountRegState callback);
        [DllImport(DllName)]
        private static extern ErrorCode Callback_SetSubscriptionState(IntPtr module, OnSubscriptionState callback);
        [DllImport(DllName)]
        private static extern ErrorCode Callback_SetNetworkState(IntPtr module, OnNetworkState callback);
        [DllImport(DllName)]
        private static extern ErrorCode Callback_SetPlayerState(IntPtr module, OnPlayerState callback);
        [DllImport(DllName)]
        private static extern ErrorCode Callback_SetRingerState(IntPtr module, OnRingerState callback);
        [DllImport(DllName)]
        private static extern ErrorCode Callback_SetCallProceeding(IntPtr module, OnCallProceeding callback);
        [DllImport(DllName)]
        private static extern ErrorCode Callback_SetCallTerminated(IntPtr module, OnCallTerminated callback);
        [DllImport(DllName)]
        private static extern ErrorCode Callback_SetCallConnected(IntPtr module, OnCallConnected callback);
        [DllImport(DllName)]
        private static extern ErrorCode Callback_SetCallIncoming(IntPtr module, OnCallIncoming callback);
        [DllImport(DllName)]
        private static extern ErrorCode Callback_SetCallDtmfReceived(IntPtr module, OnCallDtmfReceived callback);
        [DllImport(DllName)]
        private static extern ErrorCode Callback_SetCallTransferred(IntPtr module, OnCallTransferred callback);
        [DllImport(DllName)]
        private static extern ErrorCode Callback_SetCallRedirected(IntPtr module, OnCallRedirected callback);
        [DllImport(DllName)]
        private static extern ErrorCode Callback_SetCallSwitched(IntPtr module, OnCallSwitched callback);
        [DllImport(DllName)]
        private static extern ErrorCode Callback_SetCallHeld(IntPtr module, OnCallHeld callback);


        void OnTrialModeNotifiedCallback()
        {
            eventDelegate_?.OnTrialModeNotified();
        }

        void OnDevicesAudioChangedCallback()
        {
            eventDelegate_?.OnDevicesAudioChanged();
        }

        void OnAccountRegStateCallback(AccountId accId, RegState state,
                                        [MarshalAs(UnmanagedType.LPUTF8Str)] string response)
        {
            eventDelegate_?.OnAccountRegState(accId, state, response);
        }
                
        void OnSubscriptionStateCallback(SubscriptionId subId, SubscriptionState state,
                                         [MarshalAs(UnmanagedType.LPUTF8Str)] string response)
        {
            eventDelegate_?.OnSubscriptionState(subId, state, response);
        }

        void OnNetworkStateCallback([MarshalAs(UnmanagedType.LPUTF8Str)] string name,
                           NetworkState state)
        {
            eventDelegate_?.OnNetworkState(name, state);
        }

        void OnPlayerStateCallback(PlayerId playerId, PlayerState state)
        {
            eventDelegate_?.OnPlayerState(playerId, state);
        }

        void OnRingerStateCallback(bool start)
        {
            eventDelegate_?.OnRingerState(start);
        }

        void OnCallIncomingCallback(CallId callId, AccountId accId, bool withVideo,
                           [MarshalAs(UnmanagedType.LPUTF8Str)] string hdrFrom,
                           [MarshalAs(UnmanagedType.LPUTF8Str)] string hdrTo)
        {
            eventDelegate_?.OnCallIncoming(callId, accId, withVideo, hdrFrom, hdrFrom);
        }

        void OnCallConnectedCallback(CallId callId,
                           [MarshalAs(UnmanagedType.LPUTF8Str)] string hdrFrom,
                           [MarshalAs(UnmanagedType.LPUTF8Str)] string hdrTo,
                           bool withVideo)
        {
            eventDelegate_?.OnCallConnected(callId, hdrFrom, hdrFrom, withVideo);
        }

        void OnCallTerminatedCallback(CallId callId, uint statusCode)
        {
            eventDelegate_?.OnCallTerminated(callId, statusCode);
        }

        void OnCallProceedingCallback(CallId callId,
                           [MarshalAs(UnmanagedType.LPUTF8Str)] string response)
        {
            eventDelegate_?.OnCallProceeding(callId, response);
        }

        void OnCallTransferredCallback(CallId callId, uint statusCode)
        {
            eventDelegate_?.OnCallTransferred(callId, statusCode);
        }

        void OnCallRedirectedCallback(CallId origCallId, CallId relatedCallId,
                           [MarshalAs(UnmanagedType.LPUTF8Str)] string referTo)
        {
            eventDelegate_?.OnCallRedirected(origCallId, relatedCallId, referTo);
        }

        void OnCallDtmfReceivedCallback(CallId callId, ushort tone)
        {
            eventDelegate_?.OnCallDtmfReceived(callId, tone);
        }


        void OnCallHeldCallback(CallId callId, HoldState state)
        {
            eventDelegate_?.OnCallHeld(callId, state);
        }

        void OnCallSwitchedCallback(CallId callId)
        {
            eventDelegate_?.OnCallSwitched(callId);
        }

    }//Module
}
