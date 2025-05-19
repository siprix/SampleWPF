#pragma warning disable IDE0066
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SampleWpf;

/// [MicMute IconConverter] ////////////////////////////////////////////////////////////////

public class MicMuteIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object param, CultureInfo ci)
    {
        bool? micMuted = value as bool?;
        if (micMuted == null) return System.Windows.Data.Binding.DoNothing;
        else                  return micMuted.Value ? Icons.mic_off : Icons.mic;
    }

    public object ConvertBack(object value, Type targetType, object param, CultureInfo ci)
    {
        return false;
    }
}

/// [CamMute IconConverter] ////////////////////////////////////////////////////////////////

public class CamMuteIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object param, CultureInfo ci)
    {
        bool? camMuted = value as bool?;
        if (camMuted == null) return System.Windows.Data.Binding.DoNothing;
        else return camMuted.Value ? Icons.videocam_off : Icons.videocam;
    }

    public object ConvertBack(object value, Type targetType, object param, CultureInfo ci)
    {
        return false;
    }
}

/// [Mute TextConverter] ////////////////////////////////////////////////////////////////

public class MuteTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object param, CultureInfo ci)
    {
        bool? muted = value as bool?;
        if (muted == null) return System.Windows.Data.Binding.DoNothing;
        else return muted.Value ? "UnMute" : "Mute";
    }

    public object ConvertBack(object value, Type targetType, object param, CultureInfo ci)
    {
        return false;
    }
}
    

/// [CallDirection IconConverter] ////////////////////////////////////////////////////////////////

public class CallDirectionIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object param, CultureInfo ci)
    {
        bool? isIncoming = value as bool?;
        if (isIncoming == null) return System.Windows.Data.Binding.DoNothing;
        else return isIncoming.Value ? Icons.call_received : Icons.call_made;
    }

    public object ConvertBack(object value, Type targetType, object param, CultureInfo ci)
    {
        return false;
    }
}


/// [HoldState IconConverter] ////////////////////////////////////////////////////////////////

public class HoldStateIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object param, CultureInfo ci)
    {
        Siprix.HoldState? holdState = value as Siprix.HoldState?;
        if (holdState == null) return System.Windows.Data.Binding.DoNothing;            
        return (holdState == Siprix.HoldState.Local) ||
               (holdState == Siprix.HoldState.LocalAndRemote) ? Icons.play_arrow : Icons.pause;
    }

    public object ConvertBack(object value, Type targetType, object param, CultureInfo ci)
    {
        return false;
    }
}

/// [HoldState TextConverter] ////////////////////////////////////////////////////////////////

public class HoldStateTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object param, CultureInfo ci)
    {
        Siprix.HoldState? holdState = value as Siprix.HoldState?;
        if (holdState == null) return System.Windows.Data.Binding.DoNothing;
        return (holdState == Siprix.HoldState.Local) ||
               (holdState == Siprix.HoldState.LocalAndRemote) ? "Unhold" : "Hold";
    }

    public object ConvertBack(object value, Type targetType, object param, CultureInfo ci)
    {
        return false;
    }
}

/// [RegState IconConverter] ////////////////////////////////////////////////////////////////

public class RegStateIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object param, CultureInfo ci)
    {
        Siprix.RegState? regState = value as Siprix.RegState?;
        if (regState == null) return System.Windows.Data.Binding.DoNothing;

        switch (regState)
        {
            case Siprix.RegState.Success:    return Icons.cloud_done;
            case Siprix.RegState.Failed:     return Icons.cloud_off;
            case Siprix.RegState.InProgress: return Icons.sync;
            default:                         return Icons.done;
        }
    }

    public object ConvertBack(object value, Type targetType, object param, CultureInfo ci)
    {
        return Siprix.RegState.Success;
    }
}


/// [RegState ColorConverter] ////////////////////////////////////////////////////////////////

public class RegStateColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
    {
        Siprix.RegState? regState = value as Siprix.RegState?;
        if (regState == null) return System.Windows.Data.Binding.DoNothing;

        switch (regState)
        {
            case Siprix.RegState.Success:    return new SolidColorBrush(Colors.Green);
            case Siprix.RegState.Failed:     return new SolidColorBrush(Colors.Red);
            case Siprix.RegState.InProgress: return new SolidColorBrush(Colors.Blue);
            default: return new SolidColorBrush(Colors.Gray);
        }
    }

    public object ConvertBack(object value, Type targetType, object param, CultureInfo ci)
    {
        return Siprix.RegState.Success;
    }
}

/// [MessageSentColorConverter ColorConverter] ////////////////////////////////////////////////////////////////

public class MessageSentColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object param, CultureInfo ci)
    {
        bool? sentSuccess = value as bool?;
        return new SolidColorBrush((sentSuccess==true) ? Colors.BlueViolet : Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object param, CultureInfo ci)
    {
        return false;
    }
}    


/// [BLFState ColorConverter] ////////////////////////////////////////////////////////////////

public class BLFStateColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object param, CultureInfo ci)
    {
        Siprix.BLFState? blfState = value as Siprix.BLFState?;
        if (blfState == null) return System.Windows.Data.Binding.DoNothing;

        switch (blfState)
        {
            case Siprix.BLFState.SubscriptionDestroyed: return new SolidColorBrush(Colors.Gray);
            case Siprix.BLFState.Terminated:
            case Siprix.BLFState.Unknown: return new SolidColorBrush(Colors.Green); //Ready to make call
            default: return new SolidColorBrush(Colors.Red); //Call in progress
        }
    }

    public object ConvertBack(object value, Type targetType, object param, CultureInfo ci)
    {
        return Siprix.BLFState.Unknown;
    }
}

/// [CdrState IconConverter] ////////////////////////////////////////////////////////////////

public class CdrStateIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object param, CultureInfo ci)
    {
        if (value is not Siprix.CdrState cdrState) return Binding.DoNothing;

        switch (cdrState)
        {
            case Siprix.CdrState.IncomingConnected: return Icons.call_received;
            case Siprix.CdrState.IncomingMissed: return Icons.call_missed;
            case Siprix.CdrState.OutgoingConnected: return Icons.call_made;
            default: return Icons.call_missed_outgoing;
        }
    }

    public object ConvertBack(object? value, Type? targetType, object param, CultureInfo ci)
    {
        return false;
    }
}

public class CdrStateColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object param, CultureInfo ci)
    {
        if (value is not Siprix.CdrState cdrState) return Binding.DoNothing;

        switch (cdrState)
        {
            case Siprix.CdrState.IncomingConnected: return new SolidColorBrush(Colors.Green);
            case Siprix.CdrState.IncomingMissed:    return new SolidColorBrush(Colors.Red);
            case Siprix.CdrState.OutgoingConnected: return new SolidColorBrush(Colors.LightGreen);
            default:                                return new SolidColorBrush(Colors.Orange);
        }
    }

    public object ConvertBack(object? value, Type? targetType, object param, CultureInfo ci)
    {
        return false;
    }
}

/// [SwitchedCall ColorConverter] ////////////////////////////////////////////////////////////////

public class SwitchedCallColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object param, CultureInfo ci)
    {
        bool? isSwitchedCall = value as bool?;
        if (isSwitchedCall == null) return System.Windows.Data.Binding.DoNothing;
        //Highligh current call
        return isSwitchedCall.Value ? new SolidColorBrush(Colors.BlanchedAlmond) : new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object param, CultureInfo ci)
    {
        return false;
    }
}

/// [InverseBooleanToVisibilityConverter] //////////////////////////////////////////////////////////////// <summary>

/// </summary>
public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object param, CultureInfo ci)
    {
        return (bool)value ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object param, CultureInfo ci)
    {
        return false;
    }
}

