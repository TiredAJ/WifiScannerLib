using Android;
using Android.App;
using Android.Content;
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

        public AndroidWS(Context _CTX)
        {
            CTX = _CTX;

            //SDD.WriteLine("**** Constructor called ****");

            Initialise();
        }

        public AndroidWS()
        { }

        public override void OnCreate()
        {
            //SDD.WriteLine("**** On Create Called ****");

            //Context temp = Android.App.Application.Context;

            Initialise();
        }

        private void Initialise()
        {
            //SDD.WriteLine("**** Initialise called ****");

            if (CTX == null)
            { throw new NullReferenceException("CTX was null!?"); }


#if ANDROID23_0_OR_GREATER
            SDD.Write($"access wifi state: {CTX.CheckSelfPermission(Manifest.Permission.AccessWifiState)}");
            SDD.Write($"access fine location: {CTX.CheckSelfPermission(Manifest.Permission.AccessFineLocation)}");
            SDD.Write($"change wifi state: {CTX.CheckSelfPermission(Manifest.Permission.ChangeWifiState)}");
#endif

            WM = (WifiManager)CTX.GetSystemService(WifiService);
            CM = (ConnectivityManager)CTX.GetSystemService(ConnectivityService);

            if (WM == null)
            { throw new Exception("WM was null!"); }

            NetCB = new NC();

            NetCB.Initialise(CB);
        }

        //is called to collect data
        public IEnumerable<WifiInfoItem> GetData()
        {
            //SDD.WriteLine("**** Get Data called ****");

            if (CM == null)
            {throw new Exception("CM was null!");}

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

            //SDD.WriteLine($"***** Scan results {ScanData.Count} *****");

            return ScanData;
        }

        //collects the data
        public bool CB()
        {
            ScanData.Clear();

            if (WM.ScanResults.Count > 0)
            {
                foreach (var N in WM.ScanResults)
                { ScanData.Add(new WifiInfoItem(N)); }

                return true;
            }
            else
            { return false; }
        }

        //only here because Service inheritance mandates it
        public override IBinder? OnBind(Intent? _Int)
        {
#if ANDROID29_0_OR_GREATER
            return new Binder(_Int.Action);
#endif
            throw new Exception("idfk what this function's supposed to do");
        }
    }

    public class NC : ConnectivityManager.NetworkCallback
    {
        public Func<bool> CallBack { get; set; }

        public void Initialise(Func<bool> _CallBack)
        { CallBack = _CallBack; }

        public NC()
        { }

        public override void OnAvailable(Network network)
        { CallBack(); }
    }
}