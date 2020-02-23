using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WpfBlueTooth
{
    public static class Win32
    {
        public static void FindLoop()
        {
            BLUETOOTH_FIND_RADIO_PARAM m_bt_find_radio = new BLUETOOTH_FIND_RADIO_PARAM();
            m_bt_find_radio.init();

            BluetoothRadioInfo m_bt_info = new BluetoothRadioInfo();
            m_bt_info.Initialize();
            IntPtr m_radio = IntPtr.Zero;
            IntPtr m_bt_dev = IntPtr.Zero;
            int m_radio_id = 0;
            UInt32 mbtinfo_ret;
            int m_device_id;

            BLUETOOTH_DEVICE_SEARCH_PARAMS m_search_params = new BLUETOOTH_DEVICE_SEARCH_PARAMS();
            m_search_params.Initialize();

            BLUETOOTH_DEVICE_INFO m_device_info = new BLUETOOTH_DEVICE_INFO();
            m_device_info.Initialize();


            while (true)
            {
                IntPtr m_bt = BluetoothFindFirstRadio(ref m_bt_find_radio, out m_radio);

                if (m_bt == IntPtr.Zero)
                {
                    Console.WriteLine("BluetoothFindFirstRadio failed");
                }
                do
                {

                    // Then get the radio device info....

                    mbtinfo_ret = BluetoothGetRadioInfo(m_radio, ref m_bt_info);
                    if (mbtinfo_ret != 0)
                    {
                        Console.WriteLine("BluetoothGetRadioInfo failed");
                    }else
                    {
                        Console.WriteLine("radio name " + m_bt_info.szName);
                    }

                    m_search_params.hRadio = m_radio;
                    m_bt_dev = BluetoothFindFirstDevice(ref m_search_params, ref m_device_info);
                    if (m_bt_dev == IntPtr.Zero)
                    {
                        Console.WriteLine("BluetoothFindFirstDevice failed");
                        break;
                    }
                    m_radio_id++;

                    m_device_id = 0;

                    do
                    {
                        m_device_id++;

                        Console.WriteLine("fouond device " + m_device_info.szName);

                        // Well, the found device information can be used for further socket

                        // operation such as creating a socket, bind, listen, connect, send, receive etc..

                        // If no more device, exit the loop

                        if (!BluetoothFindNextDevice(m_bt_dev, ref m_device_info))

                            break;
                    } while (BluetoothFindNextDevice(m_bt_dev, ref m_device_info));
                } while (!BluetoothFindNextRadio(ref m_bt_find_radio, out m_radio));
                    //work with bluetooth
                    CloseHandle(m_radio);
                BluetoothFindRadioClose(m_bt);

                if (!BluetoothFindNextRadio(ref m_bt_find_radio, out m_radio))
                    break;
            }
        }
        /// <summary>
        /// The BLUETOOTH_FIND_RADIO_PARAMS structure facilitates enumerating installed Bluetooth radios.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct BLUETOOTH_FIND_RADIO_PARAM
        {
            internal UInt32 dwSize;
            public void init()
            {
                this.dwSize = (UInt32)Marshal.SizeOf(typeof(BLUETOOTH_FIND_RADIO_PARAM));
            }
        }

        private const int BLUETOOTH_MAX_NAME_SIZE = 248;
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct BluetoothRadioInfo
        {
            internal UInt32 dwSize;
            internal UInt64 address;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = BLUETOOTH_MAX_NAME_SIZE)]
            internal string szName;
            internal UInt32 ulClassOfDevice;
            internal UInt16 lmpSubversion;
            internal UInt16 manufacturer;

            internal void Initialize()
            {
                this.dwSize = (UInt32)Marshal.SizeOf(typeof(BluetoothRadioInfo));
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BLUETOOTH_DEVICE_SEARCH_PARAMS
        {
            internal UInt32 dwSize;
            internal bool fReturnAuthenticated;
            internal bool fReturnRemembered;
            internal bool fReturnUnknown;
            internal bool fReturnConnected;
            internal bool fIssueInquiry;
            internal byte cTimeoutMultiplier;
            internal IntPtr hRadio;

            internal void Initialize()
            {
                this.dwSize = (uint)Marshal.SizeOf(typeof(BLUETOOTH_DEVICE_SEARCH_PARAMS));
                cTimeoutMultiplier = 15;
                fIssueInquiry = true;
                fReturnAuthenticated = true;
                fReturnConnected = true;
                fReturnRemembered = false;
                fReturnUnknown = true;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct BLUETOOTH_DEVICE_INFO
        {
            public UInt32 dwSize;
            public UInt64 Address;
            public uint ulClassofDevice;
            public bool fConnected;
            public bool fRemembered;
            public bool fAuthenticated;
            public SYSTEMTIME stLastSeen;
            public SYSTEMTIME stLastUsed;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 248)]
            public string szName;

            public void Initialize()
            {
                this.dwSize = (uint)Marshal.SizeOf(typeof(BLUETOOTH_DEVICE_INFO));
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEMTIME
        {
            [MarshalAs(UnmanagedType.U2)] public short Year;
            [MarshalAs(UnmanagedType.U2)] public short Month;
            [MarshalAs(UnmanagedType.U2)] public short DayOfWeek;
            [MarshalAs(UnmanagedType.U2)] public short Day;
            [MarshalAs(UnmanagedType.U2)] public short Hour;
            [MarshalAs(UnmanagedType.U2)] public short Minute;
            [MarshalAs(UnmanagedType.U2)] public short Second;
            [MarshalAs(UnmanagedType.U2)] public short Milliseconds;

            public SYSTEMTIME(DateTime dt)
            {
                dt = dt.ToUniversalTime();  // SetSystemTime expects the SYSTEMTIME in UTC
                Year = (short)dt.Year;
                Month = (short)dt.Month;
                DayOfWeek = (short)dt.DayOfWeek;
                Day = (short)dt.Day;
                Hour = (short)dt.Hour;
                Minute = (short)dt.Minute;
                Second = (short)dt.Second;
                Milliseconds = (short)dt.Millisecond;
            }
        }

        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <param name="handle">[In] A valid handle to an open object.</param>
        /// <returns>If the function succeeds, the return value is nonzero. If the function fails, the return value is zero. To get extended error information, call GetLastError.</returns>
        [DllImport("Kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);
        [DllImport("irprops.cpl", SetLastError = true)]
        static extern IntPtr BluetoothFindFirstRadio(ref BLUETOOTH_FIND_RADIO_PARAM pbtfrp, out IntPtr phRadio);
        [DllImport("irprops.cpl", SetLastError = true)]
        private static extern bool BluetoothFindRadioClose(IntPtr hFind);
        [DllImport("irprops.cpl", SetLastError = true)]
        static extern bool BluetoothFindNextRadio(ref BLUETOOTH_FIND_RADIO_PARAM ptr, out IntPtr phRadio);
        [DllImport("irprops.cpl", SetLastError = true)]
        private static extern UInt32 BluetoothGetRadioInfo(IntPtr hRadio, ref BluetoothRadioInfo pRadioInfo);
        [DllImport("irprops.cpl", SetLastError = true)]
        static extern IntPtr BluetoothFindFirstDevice(ref BLUETOOTH_DEVICE_SEARCH_PARAMS searchParams, ref BLUETOOTH_DEVICE_INFO deviceInfo);
        [DllImport("Irprops.cpl", SetLastError = true)]
        static extern bool BluetoothFindNextDevice(IntPtr hFind, ref BLUETOOTH_DEVICE_INFO pbtdi);
    }
}
