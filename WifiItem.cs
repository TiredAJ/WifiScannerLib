// Ignore Spelling: IWS Wifi

#if ANDROID
using Android.Net.Wifi;
#elif IOS
using NetworkExtension;
#endif

using System.Text.Json.Serialization;

namespace WifiScannerLib
{
    public interface IWS
    {
        public event EventHandler ScanReturned;

        public Device DType();
        public bool CheckLocation();
        public void TriggerScan();
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
        public float _RSSI { get; private set; } = -101;

        [JsonIgnore]
        public string RSSI
        {
            get => _RSSI.ToString("#dbm");
            set => _RSSI = float.Parse(value.Replace("dbm", ""));
        }

        [JsonIgnore]
        public string Capabilities { get; set; } = string.Empty;

        [JsonIgnore]
        public TimeSpan LastUpdated { get; set; } = TimeSpan.Zero;

        [JsonInclude]
        public double _Distance { get; private set; } = 0d;

        [JsonIgnore]
        public string Distance
        {
            get => $"{_Distance.ToString("F2")}m";
            set => _Distance = double.Parse(value);
        }

        [JsonIgnore]
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

    public enum Device
    {
        Android,
        IOS,
        Mac,
        Linux,
        Windows
    }
}