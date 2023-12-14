// Ignore Spelling: IOSWS Wifi

namespace WifiScannerLib
{
    // All the code in this file is only included on iOS.
    public class IOSWS : IWS
    {
        public event EventHandler ScanReturned;

        public bool CheckLocation()
        {
            throw new NotImplementedException();
        }

        public Device DType() => Device.IOS;

        public void CollectData()
        { }

        private void OnScanResult(WifiEvent We)
        { ScanReturned?.Invoke(this, We); }
    }
}