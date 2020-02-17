
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
        BluetoothUtil.ServiceDiscoverRet foundDev = null;
        void DevFound(BluetoothUtil.ServiceDiscoverRet dev)
        {
            if (found) return;
            if (dev.device.Name == "MLT-BT05")
            {
                lock(bu)
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

        bool cfged = true;
        private async Task DoPair()
        {
            if (foundDev == null) return;
            if (foundDev.Errors.Count > 0 || foundDev.Characters.Count == 0)
            {
                foundDev = await bu.CheckDevice(foundDev.device.DeviceId);
            }
            var device = foundDev.device;
            await bu.PairToBleDevice(device.DeviceId).ContinueWith(async status =>
            {
                Console.WriteLine("paired to dev " + device.ConnectionStatus);
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
                    if (!cfged)
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
                        while (true)
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
            });
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
