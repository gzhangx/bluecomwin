
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
using System.IO;

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
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var devId = ReadConnectionStr();
            bu.IsDeviceHere(devId).ContinueWith(async tret =>
            {
                bool foundDev = await tret;
                if (!foundDev)
                {
                    Dsp("Device not found, scan");
                    bu.Scan();
                }
                else
                {
                    Dsp("Device found, pair");
                    await DoPair(devId);
                }
            });
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


        void SaveConnectionStr(string id)
        {
            File.WriteAllText(@"c:\temp\conn.txt", id);
        }

        string ReadConnectionStr()
        {
            try
            {
                return File.ReadAllText(@"c:\temp\conn.txt");
            } catch
            {
                return "";
            }
        }

        bool found = false;
        async void DevFound(BluetoothUtil.ServiceDiscoverRet dev)
        {
            if (found) return;
            if (dev.device.Name == "MLT-BT05")
            {
                SaveConnectionStr(dev.device.DeviceId);
                lock (bu)
                {
                    if (found) return;
                    found = true;
                }
                Dsp("found dev");
                bu.StopScan();
                await DoPair(dev.device.DeviceId);
            }
        }

        BluetoothUtil.BleChannel bleChannel;
        private async Task DoPair(string deviceId)
        {
            Dsp("pairing dev");
            DspAct(() => {
                btnSend.IsEnabled = false;
            });
            if (bleChannel != null)
            {
                bleChannel.Dispose();
            }
            try
            {
                var newChannel = new BluetoothUtil.BleChannel(deviceId, "0000ffe1",str=>Dsp(str));
                await bu.GetBleChannel(newChannel);
                bleChannel = newChannel;
                if (bleChannel.service == null && bleChannel.ErrorMsg == null)
                {
                    bleChannel.ErrorMsg = "Device not found";
                }
            } catch (Exception err)
            {
                Dsp(err.Message);
                Console.WriteLine(err);
            }
            if (bleChannel.ErrorMsg != null)
            {
                Dsp(bleChannel.ErrorMsg);
                bleChannel.Dispose();
                bleChannel = null;
                btnSend.IsEnabled = false;
                return;
            }
            DspAct(() => {
                btnSend.IsEnabled = true;
            });

            bleChannel.OnReceive = str => Dsp(str);
            Dsp("Sending test");
            bleChannel.Send("started");
        }

        private void Ch_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
           var r = args.CharacteristicValue.ReadAsString();
            Console.WriteLine("rrrrr !!!! " + r);
            Dsp("from cb"+r);
        }

        private async void Pair_ClickAsync(object sender, RoutedEventArgs e)
        {
            await DoPair(ReadConnectionStr());
        }

        void DspAct(Action act)
        {
            Dispatcher.BeginInvoke(new Action(act));
        }
        void Dsp(string s)
        {
            DspAct(() =>
            {
                txtInfo.Text = txtInfo.Text+"\n"+s;
            });
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            if (bleChannel != null)
            {
                bleChannel.Send("test1");
            }
        }

        
    }
}
