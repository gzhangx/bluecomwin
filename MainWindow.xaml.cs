
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
using System.Collections.ObjectModel;

namespace WpfBlueTooth
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BluetoothUtil bu;
        DispatcherTimer tmr = new DispatcherTimer();

        int oldL = 0, oldR = 0;

        ObservableCollection<IdName> deviceList = new ObservableCollection<IdName>();

        public MainWindow()
        {

            InitializeComponent();
            cmbDevices.ItemsSource = deviceList;
            bu = new BluetoothUtil(DevFound, OnError);
            bu.OnInfo = OnInfo;
            sliderSetPoint.Value = 180;
            sliderKp.Value = 200;
            sliderKi.Value = 0.15;
            sliderKd.Value = 0.01;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            /*
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
            */
            
            tmr.Interval = new TimeSpan(0, 0, 0, 0, 300);
            tmr.Tick += Tmr_Tick;
            tmr.Start();
            bu.Scan();
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

        string GetSaveFileName()
        {
            var dir = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            return Path.Combine(dir, "conn.txt");
        }
        void SaveConnectionStr(string id)
        {
            try
            {
                File.WriteAllText(GetSaveFileName(), id);
            }
            catch { }
        }

        string ReadConnectionStr()
        {
            try
            {
                return File.ReadAllText(GetSaveFileName());
            } catch
            {
                return "";
            }
        }

        Dictionary<string, string> Items = new Dictionary<string, string>();
        bool found = false;
        async void DevFound(BluetoothUtil.ServiceDiscoverRet dev)
        {
            DspAct(() =>
            {
                var deviceId = dev.device.DeviceId;
                lock (Items)
                {
                    if (!Items.ContainsKey(deviceId))
                    {
                        Items.Add(deviceId, deviceId);
                        deviceList.Add(new IdName
                        {
                            Id = dev.device.DeviceId,
                            Name = dev.device.Name,
                        });
                    }
                }
            });


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
                        BlueDsp(str);
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
            if (bleChannel != null && bleChannel.ErrorMsg != null)
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

        private async void Pair_ClickAsync(object sender, RoutedEventArgs e)
        {
            await DoPair(ReadConnectionStr());
        }

        void DspAct(Action act)
        {
            Dispatcher.BeginInvoke(new Action(act));
        }

        void BlueDsp(string s)
        {
            DspAct(() =>
            {
                txtAdr.Text = s;
            });
            Dsp(s);
        }
        void Dsp(string s)
        {
            DspAct(() =>
            {
                txtInfo.Text = txtInfo.Text+"\n"+s;
                txtInfo.Focus();
                txtInfo.CaretIndex = txtInfo.Text.Length;
                txtInfo.ScrollToEnd();
            });
        }

        async Task<GattCommunicationStatus> SendBle(string s)
        {
            if (bleChannel != null)
            {
                return await bleChannel.Send(s);
            }
            return GattCommunicationStatus.Unreachable;
        }
        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            var res = await SendBle("testtest");
            Dsp(res.ToString());
        }

        Task<GattCommunicationStatus> SendCmd(string cmd, string val)
        {
            return SendBle($"{cmd}:{val}|");
        }

        async void DataSender()
        {
            if (bleChannel != null && sliderSetPoint != null)
            {
                {
                    int val = ((int)sliderSetPoint.Value);
                    if (oldSetpoint != val)
                    {
                        oldSetpoint = val;
                        var res = await SendCmd("sp", val.ToString());
                        Dsp(res.ToString());
                    }
                }
                {
                    int val = ((int)sliderKp.Value);
                    if (oldKp != val)
                    {
                        oldKp = val;
                        var res = await SendCmd("kp", val.ToString());
                        Dsp(res.ToString());
                    }
                }

                {
                    var val = sliderKi.Value / 100;
                    if (oldKi != val)
                    {
                        oldKi = val;
                        var res = await SendCmd("ki", val.ToString());
                        Dsp(res.ToString());
                    }
                }

                {
                    var val = sliderKd.Value / 100;
                    if (oldKd != val)
                    {
                        oldKd = val;
                        var res = await SendCmd("kd", val.ToString());
                        Dsp(res.ToString());
                    }
                }

                {
                    var val = (int)sliderL.Value;
                    if (oldL != val)
                    {
                        oldL = val;
                        txtL.Text = val.ToString();
                        await SendCmd("l", val.ToString());
                    }
                }

                {
                    var val = (int)sliderR.Value;
                    if (oldR != val)
                    {
                        oldL = val;
                        txtR.Text = val.ToString();
                        await SendCmd("r", val.ToString());
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
            }
        }

        double oldKd = -1;
        private void SliderKd_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var val = sliderKd.Value / 100;
            if (txtKd != null)
            {
                txtKd.Text = val.ToString("0.0000");
            }
        }

        private async void cmbDevices_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selected = (IdName)cmbDevices.SelectedItem;
            await DoPair(selected.Id);
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            tmr.Stop();
        }
                
    }

    public class IdName
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
