
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
            /*
            var devId = ReadConnectionStr();
            bu.CheckDevice(devId).ContinueWith(async tret =>
            {
                foundDev = await tret;
                if (foundDev == null)
                {
                    Dsp("Device not found, scan");
                    bu.Scan();
                }else
                {
                    Dsp("Device found, pair");
                    await DoPair();
                }
            });
            */
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
        BluetoothUtil.ServiceDiscoverRet foundDev = null;
        void DevFound(BluetoothUtil.ServiceDiscoverRet dev)
        {
            if (found) return;
            if (dev.device.Name == "MLT-BT05")
            {
                SaveConnectionStr(dev.device.DeviceId);
                lock (bu)
                {
                    if (found) return;
                    foundDev = dev;
                    found = true;
                }
                Dsp("found dev");
                bu.StopScan();
                DoPair();
            }
        }

        bool cfged = false;
        private async Task DoPair()
        {
            if (foundDev == null) return;
            if (foundDev.Errors.Count > 0 || foundDev.Characters.Count == 0)
            {
                foundDev = await bu.CheckDevice(foundDev.device.DeviceId);
            }
            var device = foundDev.device;
            Dsp("connstatus " + device.ConnectionStatus.ToString());
            //if (device.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
            {
                Console.WriteLine("pairing");
                var pairStatus = await bu.PairToBleDevice(device.DeviceId);
            }
            {
                Console.WriteLine("paired to dev " + device.ConnectionStatus);
                foundDev = await bu.CheckDevice(foundDev.device.DeviceId);
                var chars = foundDev.Characters;
                
                var ch = chars.Find(c => c.Uuid.ToString().StartsWith("0000ffe1"));
                if (ch == null)
                {
                    Dsp("no ch");
                    return;
                }
                Dsp("cfg tru");
                try
                {
                    //if (!cfged)
                    {
                        try
                        {
                            Dsp("cfg tru before trye");
                            var cfg = await ch.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify).AsTask();
                            if (cfg == GattCommunicationStatus.Success)
                            {
                                //cfged = true;
                                Dsp("cfg good");
                                ch.ValueChanged += Ch_ValueChanged;
                            }
                            else
                            {
                                Dsp("bad " + cfg);
                            }
                        } catch (Exception exc)
                        {
                            Console.WriteLine(exc.Message + " err cfg");
                        }
                    }

                    if (ch != null)
                    {
                        Dsp("found ch");
                        Dsp("sending pt:180|");
                        await ch.WriteString("pt:180|");
                        //while (true)
                        {
                            var st = await ch.ReadString();
                            //if (st == null) continue;
                            Dsp($"got '{st}'");
                        }
                    }
                } catch (Exception exc)
                {
                    Dsp(exc.Message);
                    Console.WriteLine(exc);
                }
            };
        }

        private void Ch_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
           var r = args.CharacteristicValue.ReadAsString();
            Console.WriteLine("rrrrr !!!! " + r);
            Dsp("from cb"+r);
        }

        private void Test_Click(object sender, RoutedEventArgs e)
        {
            DoPair();
        }

        void Dsp(string s)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                txtInfo.Text = txtInfo.Text+"\n"+s;
            }));
        }
    }
}
