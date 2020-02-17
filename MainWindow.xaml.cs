
using System;
using System.Windows;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.Linq;
using Windows.Devices.Enumeration;
using System.Collections.Generic;
using Windows.Storage.Streams;

namespace WpfBlueTooth
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BluetoothUtil bu;
        public MainWindow()
        {

            InitializeComponent();
            bu = new BluetoothUtil(DevFound, OnError);
            bu.OnInfo = OnInfo;
            bu.Scan();
        }

        void OnInfo(string s)
        {
            Console.WriteLine("INFO: " + s);
        }
        void OnError(string err, Exception exc)
        {
            Console.WriteLine(err);
            Console.WriteLine(exc);
        }

        bool found = false;
        void DevFound(BluetoothLEDevice dev)
        {
            if (found) return;
            if (dev.Name == "MLT-BT05")
            {
                lock(bu)
                {
                    if (found) return;
                    found = true;
                }
                Console.WriteLine("found dev");
                bu.StopScan();
                bu.PairToBleDevice(dev.DeviceId).ContinueWith(async charsTask =>
                {
                    Console.WriteLine("paired to dev");
                    var chars = await charsTask;
                    chars.ForEach(c =>
                    {
                        Console.WriteLine($"DONE {c.Uuid} {c.UserDescription} {c.Service.Uuid}");
                    });
                    var ch = chars.Find(c => c.Uuid.ToString().StartsWith("0000ffe1"));
                    if (ch != null)
                    {
                        Console.WriteLine("sending 1234");
                        await ch.WriteString("pt:180|");
                        while (true)
                        {
                            var st = await ch.ReadString();
                            if (st == null) continue;
                            Console.WriteLine($"got '{st}'");
                        }
                    }
                });
            }
        }


        Dictionary<ulong, BluetoothLEDevice> foundDevs = new Dictionary<ulong, BluetoothLEDevice>();

        private async Task<BluetoothLEDevice> GetBluetoothLEDeviceAsync(ulong address, DateTimeOffset broadcastTime, short rssi)
        {
            // Get bluetooth device info
            var device = await BluetoothLEDevice.FromBluetoothAddressAsync(address).AsTask();

            // Null guard
            if (device == null)
                return null;

            // NOTE: This can throw a System.Exception for failures
            // Get GATT services that are available
            var gatt = await device.GetGattServicesAsync().AsTask();

            // If we have any services...
            if (gatt.Status == GattCommunicationStatus.Success)
            {
                // Loop each GATT service
                foreach (var service in gatt.Services)
                {
                    // This ID contains the GATT Profile Assigned number we want!
                    // TODO: Get more info and connect
                    var gattProfileId = service.Uuid;
                }
            }
            return device;
        }
        private async void W_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
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
                var device = await GetBluetoothLEDeviceAsync(
                   args.BluetoothAddress,
                   args.Timestamp,
                   args.RawSignalStrengthInDBm);
                lock (foundDevs)
                {
                    if (!foundDevs.ContainsKey(args.BluetoothAddress))
                        foundDevs.Add(args.BluetoothAddress, device);
                }
                if (device == null) return;

                if (device.Name == "MLT-BT05")
                {
                    var sers = await device.GetGattServicesAsync().AsTask();
                    await PairToBleDevice(device.DeviceId);
                }

                Console.WriteLine(device.Name + " <<<name_addr= " + args.BluetoothAddress);
            } catch(Exception exc)
            {
                Console.WriteLine(exc);
            }
        }

        bool inParing = false;
        public async Task PairToBleDevice(string deviceId)
        {
            if (inParing) return;
            inParing = true;
            var device = await BluetoothLEDevice.FromIdAsync(deviceId).AsTask();

            if (device == null)
                return;

            //if (device.DeviceInformation.Pairing.IsPaired)
            //    return;

            // Listen out for pairing request
            device.DeviceInformation.Pairing.Custom.PairingRequested += (sender, args) =>
            {
                Console.WriteLine("===> pairing request");
                args.Accept(); // <-- pin if any
            };

            var result = await device.DeviceInformation.Pairing.Custom.PairAsync(
                DevicePairingKinds.ConfirmOnly
                ).AsTask();

            // Log the result
            if (result.Status == DevicePairingResultStatus.Paired)
            {
                Console.WriteLine("Pairing successful");
            }
            else
                Console.WriteLine($"Pairing failed: {result.Status}");
            var gatt = await device.GetGattServicesAsync().AsTask();

            // If we have any services...
            if (gatt.Status == GattCommunicationStatus.Success)
            {
                foreach (var service in gatt.Services)
                {
                    var gattProfileId = service.Uuid;
                    var chars = await service.GetCharacteristicsAsync().AsTask();
                    if (chars.Status == GattCommunicationStatus.Success)
                    {
                        foreach(var ch in chars.Characteristics)
                        {
                            Console.WriteLine(ch);
                            Console.WriteLine(ch.UserDescription + " <<desc uuid=" + ch.Uuid);
                            Console.WriteLine(ch.CharacteristicProperties);
                        }
                    }
                }
            }
        }
    }
}
