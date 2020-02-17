using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.Linq;
using Windows.Devices.Enumeration;
using System.Collections.Generic;
using System;

// founded on https://www.youtube.com/watch?v=RVasdDtgLKY&t=2696s


namespace WpfBlueTooth
{
    public class BluetoothUtil
    {
        public class ServiceDiscoverRet
        {
            public BluetoothLEDevice device { get; set; }
            public List<GattCharacteristic> Characters { get; set; }
            public List<GComErrors> Errors { get; set; }
        }
        public class GComErrors
        {
            public string uuid { get; set; }
            public GattCommunicationStatus Status { get; set; }
        }
        BluetoothLEAdvertisementWatcher bleWatcher;
        Dictionary<ulong, ServiceDiscoverRet> foundDevs = new Dictionary<ulong, ServiceDiscoverRet>();
        protected Action<string, Exception> onError;
        public Action<string> OnInfo;
        protected Action<ServiceDiscoverRet> OnDeviceFound;
        public BluetoothUtil(Action<ServiceDiscoverRet> devFound, Action<string, Exception> onerr = null)
        {
            OnDeviceFound = devFound;
            onError = onerr;
            bleWatcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active,
            };
            bleWatcher.Received += BleWatcher_Received;
        }
        public void Scan()
        {
            bleWatcher.Start();
        }
        public void StopScan()
        {
            bleWatcher.Stop();
        }

        protected void LogInfo(string s)
        {
            OnInfo?.Invoke(s);
        }

        private async void BleWatcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            try
            {
                lock (foundDevs)
                {
                    if (foundDevs.ContainsKey(args.BluetoothAddress))
                    {
                        var dev = foundDevs[args.BluetoothAddress];
                        if (dev == null) return;
                        //Console.Write(dev.Name.Substring(0, 1));
                        return;
                    }
                }

                var device = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress).AsTask();
                if (device == null)
                    return;

                ServiceDiscoverRet devd = null;
                    devd = await CheckDevice(device);
                    if (devd.Errors.Count > 0) LogInfo("Has Errors!!!!!");
                
                OnDeviceFound?.Invoke(devd);
                lock (foundDevs)
                {
                    if (!foundDevs.ContainsKey(args.BluetoothAddress))
                        foundDevs.Add(args.BluetoothAddress, devd);
                }

                
            }
            catch (Exception exc)
            {
                onError?.Invoke("Scan", exc);
            }
        }

        public async Task<ServiceDiscoverRet> CheckDevice(string deviceId)
        {
            var device = await BluetoothLEDevice.FromIdAsync(deviceId).AsTask();
            return await CheckDevice(device);
        }
        protected async Task<ServiceDiscoverRet> CheckDevice(BluetoothLEDevice device)
        {
            if (device == null) return null;
            ServiceDiscoverRet ret = new ServiceDiscoverRet
            {
                device = device,
                Characters = new List<GattCharacteristic>(),
                Errors = new List<GComErrors>(),
            };
            var gatt = await device.GetGattServicesAsync().AsTask();

            // If we have any services...
            if (gatt.Status == GattCommunicationStatus.Success)
            {
                List<GattCharacteristic> chRes = new List<GattCharacteristic>();
                foreach (var service in gatt.Services)
                {
                    LogInfo("Discovered service " + service.Uuid);
                    var chars = await service.GetCharacteristicsAsync().AsTask();
                    if (chars.Status == GattCommunicationStatus.Success)
                    {
                        foreach (var ch in chars.Characteristics)
                        {
                            ret.Characters.Add(ch);
                            LogInfo(" ===>  " + service.Uuid + " " + ch.Uuid);
                        }
                    }
                    else
                    {
                        ret.Errors.Add(new GComErrors
                        {
                            uuid = service.Uuid.ToString(),
                            Status = chars.Status,
                        });
                        LogInfo(" !====>  " + service.Uuid + " " + chars.Status);
                    }
                }
            }
            return ret;
        }

        public async Task<DevicePairingResultStatus> PairToBleDevice(string deviceId)
        {
            var device = await BluetoothLEDevice.FromIdAsync(deviceId).AsTask();

            if (device == null)
            {
                LogInfo($"Pairing: No Device Found {deviceId}");
                return DevicePairingResultStatus.Failed;
            }

            device.DeviceInformation.Pairing.Custom.PairingRequested += (s, args) =>
            {
                LogInfo("Accept pairing");
                args.Accept();
            };

            var result = await device.DeviceInformation.Pairing.Custom.PairAsync(DevicePairingKinds.ConfirmOnly).AsTask();

            if (result.Status == DevicePairingResultStatus.Paired)
            {
                LogInfo("Pairing successful");
            }
            else
            {
                LogInfo($"Pairing failed {result.Status} canPair = {device.DeviceInformation.Pairing.CanPair}");
                var ups = await device.DeviceInformation.Pairing.UnpairAsync().AsTask();
                LogInfo($"Unpair {ups}");

            }
            
            return result.Status;
        }
    }
}
