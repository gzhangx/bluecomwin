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
            WSAData data = new WSAData();
            int result = 0;

            if (WSAStartup(2, out data) != 0)
            {
                Console.WriteLine("Wsastartup failed");
                return;
            }
            Int32 dwSize = 0;
            Int32 hLookup = 0;
            WSAQUERYSET lpRestrictions = new WSAQUERYSET();
            lpRestrictions.Initialize();
            if (WSALookupServiceBegin(ref lpRestrictions, LUP_CONTAINERS, ref hLookup) != 0)
            {
                Console.WriteLine("WSALookupServiceBegin failed");
            }

            int nextresult = 0;

            while (nextresult != WSAENOMORE && nextresult != WSA_E_NO_MORE)
            {
                int dwLength = 5000;
                WSAQUERYSET qs = new WSAQUERYSET();
                IntPtr pqs = Marshal.AllocHGlobal(dwLength);
                nextresult = WSALookupServiceNext(hLookup, 4080, ref dwLength, pqs);
                if (nextresult == 0)
                {
                    qs = Marshal.PtrToStructure<WSAQUERYSET>(pqs);
                    Console.WriteLine(qs.szServiceInstanceName);
                }
                else
                {
                    nextresult = Marshal.GetLastWin32Error();
                }
                Marshal.FreeHGlobal(pqs);
            }

            WSALookupServiceEnd(hLookup);
            WSACleanup();
        }


        public static void FindLoopNOWSA()
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
                        Guid[] guidServices = new Guid[100];
                        uint services = (uint)(16*guidServices.Length);
                        if (BluetoothEnumerateInstalledServices(m_radio, ref m_device_info, ref services, guidServices) != 0)
                        {
                            Console.WriteLine("failed to get services");
                        }
                        m_device_id++;

                        Console.WriteLine("fouond device " + m_device_info.szName);
                        // Well, the found device information can be used for further socket
                        // operation such as creating a socket, bind, listen, connect, send, receive etc..
                        // If no more device, exit the loop

                        if (!BluetoothFindNextDevice(m_bt_dev, ref m_device_info))

                            break;
                    } while (BluetoothFindNextDevice(m_bt_dev, ref m_device_info));
                    if (!BluetoothFindDeviceClose(m_bt_dev))
                        Console.WriteLine("BluetoothFindDeviceClose(m_bt_dev) failed with");
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
        [DllImport("irprops.cpl", SetLastError = true)]
        static extern bool BluetoothFindDeviceClose(IntPtr hFind);
        [DllImport("irprops.cpl", SetLastError = true)]
        private static extern uint BluetoothEnumerateInstalledServices(IntPtr hRadio, ref BLUETOOTH_DEVICE_INFO pbtdi, ref uint pcServices, Guid[] pGuidServices);






        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WSAQUERYSET
        {
            public Int32 dwSize;
            public String szServiceInstanceName;
            public IntPtr lpServiceClassId;
            public IntPtr lpVersion;
            public String lpszComment;
            public Int32 dwNameSpace;
            public IntPtr lpNSProviderId;
            public String lpszContext;
            public Int32 dwNumberOfProtocols;
            public IntPtr lpafpProtocols;
            public String lpszQueryString;
            public Int32 dwNumberOfCsAddrs;
            public IntPtr lpcsaBuffer;
            public Int32 dwOutputFlags;
            public IntPtr Blob;

            public void Initialize()
            {
                dwSize = Marshal.SizeOf(typeof(WSAQUERYSET));
                szServiceInstanceName = null;
                dwNameSpace = NS_BTH;
                lpcsaBuffer = IntPtr.Zero;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct WSAData
        {
            public Int16 version;
            public Int16 highVersion;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 257)]
            public String description;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 129)]
            public String systemStatus;

            public Int16 maxSockets;
            public Int16 maxUdpDg;
            public IntPtr vendorInfo;
        }


        const Int32 LUP_CONTAINERS = 0x02;
        const Int32 NS_BTH = 16;
        const Int32 LUP_RETURN_NAME = 0x0010;
        const Int32 LUP_RETURN_COMMENT = 0x0080;
        const Int32 LUP_RETURN_ADDR = 0x0100;
        const Int32 LUP_RETURN_ALL = 0x0FF0;

        const Int32 WSAENOMORE =10102;
        const Int32 WSA_E_NO_MORE = 10110;

        [DllImport("ws2_32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern Int32 WSAStartup(Int16 wVersionRequested, out WSAData wsaData);
        [DllImport("ws2_32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern Int32 WSALookupServiceBegin(ref WSAQUERYSET lpqsRestrictions, Int32 dwControlFlags, ref Int32 lphLookup);
        [DllImport("ws2_32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern Int32 WSALookupServiceNext(Int32 hLookup, Int32 dwControlFlags, ref Int32 lpdwBufferLength, IntPtr pqsResults);
        [DllImport("ws2_32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern Int32 WSALookupServiceEnd(Int32 hLookup);
        [DllImport("ws2_32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern Int32 WSACleanup();
    }
}
