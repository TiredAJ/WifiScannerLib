// Ignore Spelling: IWS Wifi RSSI SSID BSSID NEHHR

#if ANDROID
using Android.Net.Wifi;
#elif IOS
using NetworkExtension;
#endif

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace WifiScannerLib
{
    public interface IWS
    {
        public event EventHandler ScanReturned;

        public Device DType();
        public bool CheckLocation();
        public void CollectData();
    }

    /// <summary>
    /// Holds relevant data about APs
    /// </summary>
    public class WifiInfoItem : IEquatable<WifiInfoItem>
    {
        //Android-specific shenanigans
        #region Android
#if ANDROID
        public WifiInfoItem(ScanResult _SR)
        {
#if ANDROID33_0_OR_GREATER
            SSID = _SR.WifiSsid.ToString();
#elif ANDROID21_0_OR_GREATER
            SSID = _SR.Ssid;
#endif
            BSSID = _SR.Bssid;

            _RSSI = _SR.Level;
            LastUpdated = TimeSpan.FromMicroseconds(_SR.Timestamp);

            if (SSID == "")
	        {SSID = "*Hidden*";}
            else
	        {SSID = SSID.Trim('\"', ' ');}


            PrimaryFrequency = _SR.Frequency;

            _Distance = DistanceCalc(PrimaryFrequency, _RSSI);
        }
#endif
        #endregion

        //IOS-specific shenanigans
        #region IOS
#if IOS
        public WifiInfoItem(NetworkExtension.NEHotspotHelperResult NEHHR)
        {
        }
#endif
        #endregion

        public WifiInfoItem()
        { }

        [JsonInclude]
        public string BSSID { get; set; } = string.Empty;

        [JsonInclude]
        public string SSID { get; set; } = string.Empty;

        [JsonInclude]
        public int _RSSI { get; private set; } = -101;

        [JsonIgnore]
        public string RSSI
        {
            get => _RSSI.ToString("#dbm");
            set => _RSSI = int.Parse(value.Replace("dbm", ""));
        }

        [JsonIgnore]
        public string Capabilities { get; set; } = string.Empty;

        [JsonInclude]
        public TimeSpan LastUpdated { get; set; } = TimeSpan.Zero;
        //https://developer.android.com/reference/android/net/wifi/ScanResult#timestamp

        [JsonInclude]
        public double _Distance { get; private set; } = 0d;

        [JsonIgnore]
        public string Distance
        {
            get => $"{_Distance.ToString("F2")}m";
            set => _Distance = double.Parse(value);
        }

        [JsonInclude]
        public double PrimaryFrequency { get; set; } = 0d;

        public static double DistanceCalc(double _Frequency, float _RSSI)
        {
            //10^((27.55 - 20*log10(f) + |R|)/20)
            return Math.Pow(10, (27.55d - 20 * Math.Log10(_Frequency) + Math.Abs(_RSSI)) / 20d);
        }

        public WifiInfoItem Clone()
        { return this.MemberwiseClone() as WifiInfoItem; }

        public bool Equals(WifiInfoItem A, WifiInfoItem B)
        {
            if (A.BSSID == B.BSSID)
            { return true; }
            else
            { return false; }
        }

        public bool Equals(WifiInfoItem B)
        {
            if (this.BSSID == B.BSSID)
            { return true; }
            else
            { return false; }
        }

        public override string ToString()
        { return $"BSSID: {BSSID}, SSID: {SSID}, RSSI: {RSSI}, Distance: {Distance}"; }

        public SigLevel GetLevel()
        {
            if (_RSSI > -34) return SigLevel.L1;
            else if (_RSSI > -39) return SigLevel.L2;
            else if (_RSSI > -44) return SigLevel.L3;
            else if (_RSSI > -49) return SigLevel.L4;
            else if (_RSSI > -54) return SigLevel.L5;
            else if (_RSSI > -64) return SigLevel.L6;
            else if (_RSSI > -74) return SigLevel.L7;
            else if (_RSSI > -84) return SigLevel.L8;
            else return SigLevel.L9;
        }

        public static SigLevel GetLevel(int _RSSI)
        {
            if (_RSSI > -34) return SigLevel.L1;
            else if (_RSSI > -39) return SigLevel.L2;
            else if (_RSSI > -44) return SigLevel.L3;
            else if (_RSSI > -49) return SigLevel.L4;
            else if (_RSSI > -54) return SigLevel.L5;
            else if (_RSSI > -64) return SigLevel.L6;
            else if (_RSSI > -74) return SigLevel.L7;
            else if (_RSSI > -84) return SigLevel.L8;
            else return SigLevel.L9;
        }
    }

    public class WifiEvent : EventArgs
    {
        public Dictionary<string, WifiInfoItem> Data { get; private set; }
        public int Count { get; private set; } = 0;

        public WifiEvent()
        {
            Data = new Dictionary<string, WifiInfoItem>();
            Count = 0;
        }

        public WifiEvent(IEnumerable<WifiInfoItem> _Wifis)
        {
            Count = _Wifis.Count();

            //you fucking idiot
            Data = _Wifis.ToDictionary(X => X.BSSID, Y => Y);
            //^ this was missing
        }

        public WifiEvent(Dictionary<string, WifiInfoItem> _Data)
        {
            Data = _Data;
            Count = Data.Count;
        }
    }

    public record SnapshotData
    {
        [JsonInclude]
        public int Index { get; private set; } = 0;
        [JsonInclude]
        public TimeSpan LastUpdated { get; private set; }
        [JsonInclude]
        public Dictionary<string, WifiInfoItem> Data { get; set; }

        public SnapshotData()
        {
            LastUpdated = default;
            Data = default;
            Index = 0;
        }

        public SnapshotData(TimeSpan _LastUp, Dictionary<string, WifiInfoItem> _Data, int _Index)
        {
            LastUpdated = _LastUp;
            Data = _Data;
            Index = _Index;
        }

        public SnapshotData(JsonNode _JNode)
        {
            LastUpdated = _JNode["LastUpdated"].Deserialize<TimeSpan>();
            Data = _JNode["Data"].Deserialize<Dictionary<string, WifiInfoItem>>();
            Index = _JNode["Index"].Deserialize<int>();
        }
    }

    public enum Device
    {
        Android,
        IOS,
        Mac,
        Linux,
        Windows
    }

    [Flags]
    public enum SigLevel
    {
        L1 = 1,
        L2 = 2,
        L3 = 4,
        L4 = 8,
        L5 = 16,
        L6 = 32,
        L7 = 64,
        L8 = 128,
        L9 = 256,
    }
}