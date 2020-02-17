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
        BluetoothLEAdvertisementWatcher bleWatcher;
        Dictionary<ulong, BluetoothLEDevice> foundDevs = new Dictionary<ulong, BluetoothLEDevice>();
        protected Action<string, Exception> onError;
        public Action<string> OnInfo;
        protected Action<BluetoothLEDevice> OnDeviceFound;
        public BluetoothUtil(Action<BluetoothLEDevice> devFound, Action<string, Exception> onerr = null)
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

        private async Task<BluetoothLEDevice> GetBluetoothLEDeviceAsync(ulong address)
        {
            // Get bluetooth device info
            var device = await BluetoothLEDevice.FromBluetoothAddressAsync(address).AsTask();

            // Null guard
            if (device == null)
                return null;

            OnDeviceFound?.Invoke(device);
            return device;
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
                var device = await GetBluetoothLEDeviceAsync(args.BluetoothAddress);
                lock (foundDevs)
                {
                    if (!foundDevs.ContainsKey(args.BluetoothAddress))
                        foundDevs.Add(args.BluetoothAddress, device);
                }
            }
            catch (Exception exc)
            {
                onError?.Invoke("Scan", exc);
            }
        }


        public async Task<List<GattCharacteristic>> PairToBleDevice(string deviceId)
        {
            var device = await BluetoothLEDevice.FromIdAsync(deviceId).AsTask();

            if (device == null)
            {
                LogInfo($"Pairing: No Device Found {deviceId}");
                return null;
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
                LogInfo($"Pairing failed {result.Status}");

            var gatt = await device.GetGattServicesAsync().AsTask();

            // If we have any services...
            if (gatt.Status == GattCommunicationStatus.Success)
            {
                List<GattCharacteristic> chRes = new List<GattCharacteristic>();
                foreach (var service in gatt.Services)
                {
                    Console.WriteLine("Discovered service " + service.Uuid);
                    var chars = await service.GetCharacteristicsAsync().AsTask();
                    if (chars.Status == GattCommunicationStatus.Success)
                    {
                        foreach (var ch in chars.Characteristics)
                        {
                            chRes.Add(ch);
                            Console.WriteLine(" ===>  " + service.Uuid + " " + ch.Uuid);
                        }
                    }else
                    {
                        Console.WriteLine(" !====>  " + service.Uuid + " " + chars.Status);
                    }
                }
                return chRes;
            }
            return null;
        }
    }
}
