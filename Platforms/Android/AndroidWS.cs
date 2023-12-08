using Android.App;
using Android.Content;
using Android.Locations;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using SDD = System.Diagnostics.Debug;

namespace WifiScannerLib
{
    // All the code in this file is only included on Android.
    [Service(Exported = true)]
    public class AndroidWS : Service, IWS
    {
        private WifiManager? WM;
        private ConnectivityManager? CM;
        private NC NetCB { get; set; }

        private bool IsRegistered = false;

        private List<WifiInfoItem> ScanData = new List<WifiInfoItem>();

        public Context CTX;

        public event EventHandler ScanReturned;

        /// <summary>
        /// Parametrised constructor to assign context to the scanner object
        /// </summary>
        /// <param name="_CTX">Context</param>
        public AndroidWS(Context _CTX)
        {
            CTX = _CTX;

            //SDD.WriteLine("**** Constructor called ****");

            Initialise();
        }

        public AndroidWS()
        { }

        /// <summary>
        /// Needed by Service inheritance. Think it's called when service is created
        /// </summary>
        public override void OnCreate()
        { Initialise(); }

        /// <summary>
        /// Initialises the scanner
        /// </summary>
        /// <exception cref="NullReferenceException">Thrown when context is missing</exception>
        /// <exception cref="Exception">Thrown when WM (WifiManager) is null</exception>
        private void Initialise()
        {
            //SDD.WriteLine("**** Initialise called ****");

            if (CTX == null)
            { throw new NullReferenceException("CTX was null!?"); }


#if ANDROID23_0_OR_GREATER
            SDD.Write($"access wifi state: {CTX.CheckSelfPermission(Android.Manifest.Permission.AccessWifiState)}");
            SDD.Write($"access fine location: {CTX.CheckSelfPermission(Android.Manifest.Permission.AccessFineLocation)}");
            SDD.Write($"change wifi state: {CTX.CheckSelfPermission(Android.Manifest.Permission.ChangeWifiState)}");
#endif

            WM = (WifiManager)CTX.GetSystemService(WifiService);
            CM = (ConnectivityManager)CTX.GetSystemService(ConnectivityService);

            if (WM == null)
            { throw new Exception("WM was null!"); }

            NetCB = new NC();

            NetCB.Initialise(CB);
        }

        /// <summary>
        /// Triggers a scan. Subscribe to <c>ScanReturned</c> for data
        /// </summary>
        public void TriggerScan()
        {
            SDD.WriteLine("Android called!");

            if (CM == null)
            { throw new Exception("CM was null!"); }

            if (IsRegistered)
            {
                CM.UnregisterNetworkCallback(NetCB);
                IsRegistered = false;
            }

            if (!IsRegistered)
            {
                CM.RegisterNetworkCallback
                (
                    new NetworkRequest.Builder().AddTransportType(TransportType.Wifi).Build(),
                    NetCB
                );
                IsRegistered = true;
            }
        }

        /// <summary>
        /// Callback for Connectivity Manager
        /// </summary>
        private void CB()
        {
            ScanData.Clear();

            if (WM.ScanResults.Count > 0)
            {
                foreach (var N in WM.ScanResults)
                { ScanData.Add(new WifiInfoItem(N)); }

                OnScanResult(new WifiEvent(ScanData));
            }
            else
            { OnScanResult(new WifiEvent()); }
        }

        /// <summary>
        /// Wrapper for ScanReturn event
        /// </summary>
        /// <param name="We">WifiEvent args</param>
        private void OnScanResult(WifiEvent We)
        { ScanReturned?.Invoke(this, We); }

        /// <summary>
        /// Mandated by Service inheritance
        /// </summary>
        public override IBinder? OnBind(Intent? _Int)
        {
#if ANDROID29_0_OR_GREATER
            return new Binder(_Int.Action);
#endif
            throw new Exception("idfk what this function's supposed to do");
        }

        public Device DType()
        { return Device.Android; }

        public bool CheckLocation()
        {
            using (LocationManager LM = (LocationManager)CTX.GetSystemService(LocationService))
            {
#if ANDROID28_0_OR_GREATER
                return LM.IsLocationEnabled;
#endif
            }
        }

        #region Old
        //is called to collect data
        /*public IEnumerable<WifiInfoItem> GetData()
        {
            //SDD.WriteLine("**** Get Data called ****");

            if (CM == null)
            { throw new Exception("CM was null!"); }

            if (IsRegistered)
            {
                CM.UnregisterNetworkCallback(NetCB);
                IsRegistered = false;
            }

            if (!IsRegistered)
            {
                CM.RegisterNetworkCallback
                (
                    new NetworkRequest.Builder().AddTransportType(TransportType.Wifi).Build(),
                    NetCB
                );
                IsRegistered = true;
            }

            //SDD.WriteLine($"***** TriggerScan results {ScanData.Count} *****");

            return ScanData;
        } */


        #endregion
    }

    public class NC : ConnectivityManager.NetworkCallback
    {
        public Action CallBack { get; set; }

        public void Initialise(Action _CallBack)
        { CallBack = _CallBack; }

        public NC()
        { }

        public override void OnAvailable(Network network)
        { CallBack(); }
    }
}