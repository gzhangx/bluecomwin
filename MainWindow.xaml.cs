
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
using System.Windows.Threading;

namespace WpfBlueTooth
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BluetoothUtil bu;
        DispatcherTimer tmr = new DispatcherTimer();
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

            
            tmr.Interval = new TimeSpan(0, 0, 0, 0, 300);
            tmr.Tick += Tmr_Tick;
            tmr.Start();
        }

        private void Tmr_Tick(object sender, EventArgs e)
        {
            DataSender();
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

        void EnableDisable(bool enable)
        {
            DspAct(() =>
            {
                //sliderSetPoint.IsEnabled = enable;
                //sliderKp.IsEnabled = enable;
                //sliderKi.IsEnabled = enable;
                //sliderKd.IsEnabled = enable;
                btnSend.IsEnabled = enable;
            });
        }
        BluetoothUtil.BleChannel bleChannel;
        private async Task DoPair(string deviceId)
        {
            Dsp("pairing dev");
            EnableDisable(false);
            if (bleChannel != null)
            {
                bleChannel.Dispose();
            }
            try
            {
                var newChannel = new BluetoothUtil.BleChannel(deviceId, "0000ffe1",str=>
                {
                    if (str == null)
                    {
                        Dsp("connection closed");
                        try
                        {
                            bleChannel.Dispose();
                        } catch
                        {
                            Console.WriteLine("err in dispose");
                        }
                        bleChannel = null;
                        EnableDisable(false);
                    }
                    else
                    {
                        Dsp(str);
                    }
                });
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
            EnableDisable(true);

            Dsp("Sending test");
            //bleChannel.Send("started");
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

        void SendBle(string s)
        {
            if (bleChannel != null)
            {
                bleChannel.Send(s);
            }
        }
        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            SendBle("testtest");
        }

        void SendCmd(string cmd, string val)
        {
            SendBle($"{cmd}:{val}|");
        }

        void DataSender()
        {
            if (bleChannel != null && sliderSetPoint != null)
            {
                {
                    int val = ((int)sliderSetPoint.Value);
                    if (oldSetpoint != val)
                    {
                        oldSetpoint = val;
                        SendCmd("sp", val.ToString());
                    }
                }
                {
                    int val = ((int)sliderKp.Value);
                    if (oldKp != val)
                    {
                        oldKp = val;
                        SendCmd("kp", val.ToString());
                    }
                }

                {
                    var val = sliderKi.Value / 100;
                    if (oldKi != val)
                    {
                        oldKi = val;
                    }
                }

                {
                    var val = sliderKd.Value / 100;
                    if (oldKd != val)
                    {
                        oldKd = val;
                    }
                }
            }
        }
        int oldSetpoint = -1;
        private void SliderSetPoint_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int val = ((int)sliderSetPoint.Value);
            if (txtSetPoint != null)
            {
                txtSetPoint.Text = val.ToString();
            }
        }

        int oldKp = -1;
        private void SliderKp_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (txtKp != null)
            {
                int val = ((int)sliderKp.Value);
                txtKp.Text = val.ToString();
                
            }
        }

        double oldKi = -1;
        private void SliderKi_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var val = sliderKi.Value/100;
            if (txtKi != null)
            {
                txtKi.Text = val.ToString("0.0000");
                SendCmd("ki", val.ToString());
            }
        }

        double oldKd = -1;
        private void SliderKd_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var val = sliderKd.Value / 100;
            if (txtKd != null)
            {
                txtKd.Text = val.ToString("0.0000");
                SendCmd("kd", val.ToString());
            }
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            tmr.Stop();
        }
    }
}
