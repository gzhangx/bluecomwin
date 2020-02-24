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
        static bool BluethSdpEnumAttributesCallback(ulong uAttribId, IntPtr pValueStream, ulong cbStreamSize, IntPtr pvParam)
        {
            return true;
        }
        public static void FindLoop()
        {
            byte[] L2CAP_PROTOCOL_UUID = new byte[] { 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x10, 0x00, 0x80, 0x00, 0x00, 0x80, 0x5F, 0x9B, 0x34, 0xFB };
            WSAPROTOCOL_INFO protocolInfo = new WSAPROTOCOL_INFO();
            WSAData data = new WSAData();
            int result = 0;

            if (WSAStartup(2, out data) != 0)
            {
                Console.WriteLine("Wsastartup failed");
                return;
            }

            IntPtr s = socket(ADDRESS_FAMILIES.AF_BTH, SOCKET_TYPE.SOCK_STREAM, PROTOCOL.BTHPROTO_RFCOMM);
            if ((int)s == 0xffff)
            {
                Console.WriteLine("Failed to get bluetooth socket with error code");
                return;
            }
            int protocolInfoSize = Marshal.SizeOf(protocolInfo);
            //SOL_SOCKET = 0xffff SO_PROTOCOL_INFO = 0x2005, 0x2004 SO_PROTOCOL_INFOA 
            int gsr = getsockopt(s, 0xffff, 0x2004, out protocolInfo, ref protocolInfoSize);
            if (gsr != 0)
            {
                Console.WriteLine("getsockopt(SO_PROTOCOL_INFO) failed with error code " + gsr);
                return;
            }
           
            Int32 dwSize = 0;
            Int32 hLookup = 0;
            WSAQUERYSET lpRestrictions = new WSAQUERYSET();
            lpRestrictions.Initialize();
            int flags = LUP_CONTAINERS| LUP_FLUSHCACHE | LUP_RETURN_NAME | LUP_RETURN_TYPE | LUP_RETURN_ADDR | LUP_RETURN_BLOB | LUP_RETURN_COMMENT;
            if (WSALookupServiceBegin(ref lpRestrictions, flags, ref hLookup) != 0)
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
                    CSADDR_INFO addr = Marshal.PtrToStructure<CSADDR_INFO>(qs.lpcsaBuffer);
                    Console.WriteLine(addr);
                    var addrinlocal = Marshal.PtrToStructure<sockaddr>(addr.LocalAddr.lpSockaddr);
                    var addrremote = Marshal.PtrToStructure<sockaddr>(addr.RemoteAddr.lpSockaddr);
                    StringBuilder sb = new StringBuilder();
                    sb.Length = 100;
                    int len = sb.Length;
                    if (WSAAddressToString(addr.RemoteAddr.lpSockaddr, addr.LocalAddr.length, ref protocolInfo, sb, ref len) != 0)
                    {
                        Console.WriteLine("error " + WSAGetLastError());
                    }else
                    {
                        Console.WriteLine(sb);
                        WSAQUERYSET querySet2 = new WSAQUERYSET();
                        querySet2.Initialize();
                        IntPtr unmanagedPointer = Marshal.AllocHGlobal(L2CAP_PROTOCOL_UUID.Length);
                        Marshal.Copy(L2CAP_PROTOCOL_UUID, 0, unmanagedPointer, L2CAP_PROTOCOL_UUID.Length);
                        querySet2.lpServiceClassId = unmanagedPointer;
                        querySet2.dwNameSpace = NS_BTH;
                        querySet2.lpszContext = sb.ToString();
                        Marshal.FreeHGlobal(unmanagedPointer);

                        var blob = Marshal.PtrToStructure<Blob>(qs.Blob);
                        var ddd = BluetoothSdpEnumAttributes(blob.pBlobData, (ulong)blob.cbSize, BluethSdpEnumAttributesCallback, IntPtr.Zero);
                        if (!BluetoothSdpEnumAttributes(blob.pBlobData, (ulong)blob.cbSize, BluethSdpEnumAttributesCallback, IntPtr.Zero))
                        {
                            Console.WriteLine("BluetoothSdpEnumAttributes() failed with error code " );
                        }
                    }
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

        [StructLayout(LayoutKind.Sequential)]

        public struct WSAPROTOCOLCHAIN
        {
            public int ChainLen;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7 /* MAX_PROTOCOL_CHAIN */)]
            public int[] ChainEntries;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct WSAPROTOCOL_INFO
        {

            public int dwServiceFlags1;
            public int dwServiceFlags2;
            public int dwServiceFlags3;
            public int dwServiceFlags4;
            public int dwProviderFlags;
            public Guid ProviderId;
            public int dwCatalogEntryId;
            public WSAPROTOCOLCHAIN ProtocolChain;
            public int iVersion;
            public int iAddressFamily;
            public int iMaxSockAddr;
            public int iMinSockAddr;
            public int iSocketType;
            public int iProtocol;
            public int iProtocolMaxOffset;
            public int iNetworkByteOrder;
            public int iSecurityScheme;
            public int dwMessageSize;
            public int dwProviderReserved;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 255 /* WSAPROTOCOL_LEN */ + 1)]
            public char[] szProtocol;

        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SOCKADDR_BTH
        {
            public ushort addressFamily;
            public ulong btAddr;
            public ulong serviceClassId1;
            public ulong serviceClassId2;
            public ulong serviceClassId3;
            public ulong port;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SOCKET_ADDRESS
        {
            public IntPtr lpSockaddr;
            public Int32 length;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CSADDR_INFO
        {
            public SOCKET_ADDRESS LocalAddr;
            public SOCKET_ADDRESS RemoteAddr;
            public Int32 iSocketType;
            public Int32 iProtocol;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Blob
        {
            public int cbSize;
            public IntPtr pBlobData;
        }


        const Int32 LUP_CONTAINERS = 0x02;
        const Int32 NS_BTH = 16;
        const Int32 LUP_RETURN_NAME = 0x0010;
        const Int32 LUP_RETURN_COMMENT = 0x0080;
        const Int32 LUP_RETURN_ADDR = 0x0100;
        const Int32 LUP_RETURN_ALL = 0x0FF0;
        const Int32 LUP_FLUSHCACHE = 0x2000;
        const Int32 LUP_RETURN_TYPE = 0x0020;
        const Int32 LUP_RETURN_BLOB = 0x0200;

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
        public enum ADDRESS_FAMILIES : short
        {
            /// <summary>
            /// Unspecified [value = 0].
            /// </summary>
            AF_UNSPEC = 0,
            /// <summary>
            /// Local to host (pipes, portals) [value = 1].
            /// </summary>
            AF_UNIX = 1,
            /// <summary>
            /// Internetwork: UDP, TCP, etc [value = 2].
            /// </summary>
            AF_INET = 2,
            /// <summary>
            /// Arpanet imp addresses [value = 3].
            /// </summary>
            AF_IMPLINK = 3,
            /// <summary>
            /// Pup protocols: e.g. BSP [value = 4].
            /// </summary>
            AF_PUP = 4,
            /// <summary>
            /// Mit CHAOS protocols [value = 5].
            /// </summary>
            AF_CHAOS = 5,
            /// <summary>
            /// XEROX NS protocols [value = 6].
            /// </summary>
            AF_NS = 6,
            /// <summary>
            /// IPX protocols: IPX, SPX, etc [value = 6].
            /// </summary>
            AF_IPX = 6,
            /// <summary>
            /// ISO protocols [value = 7].
            /// </summary>
            AF_ISO = 7,
            /// <summary>
            /// OSI is ISO [value = 7].
            /// </summary>
            AF_OSI = 7,
            /// <summary>
            /// european computer manufacturers [value = 8].
            /// </summary>
            AF_ECMA = 8,
            /// <summary>
            /// datakit protocols [value = 9].
            /// </summary>
            AF_DATAKIT = 9,
            /// <summary>
            /// CCITT protocols, X.25 etc [value = 10].
            /// </summary>
            AF_CCITT = 10,
            /// <summary>
            /// IBM SNA [value = 11].
            /// </summary>
            AF_SNA = 11,
            /// <summary>
            /// DECnet [value = 12].
            /// </summary>
            AF_DECnet = 12,
            /// <summary>
            /// Direct data link interface [value = 13].
            /// </summary>
            AF_DLI = 13,
            /// <summary>
            /// LAT [value = 14].
            /// </summary>
            AF_LAT = 14,
            /// <summary>
            /// NSC Hyperchannel [value = 15].
            /// </summary>
            AF_HYLINK = 15,
            /// <summary>
            /// AppleTalk [value = 16].
            /// </summary>
            AF_APPLETALK = 16,
            /// <summary>
            /// NetBios-style addresses [value = 17].
            /// </summary>
            AF_NETBIOS = 17,
            /// <summary>
            /// VoiceView [value = 18].
            /// </summary>
            AF_VOICEVIEW = 18,
            /// <summary>
            /// Protocols from Firefox [value = 19].
            /// </summary>
            AF_FIREFOX = 19,
            /// <summary>
            /// Somebody is using this! [value = 20].
            /// </summary>
            AF_UNKNOWN1 = 20,
            /// <summary>
            /// Banyan [value = 21].
            /// </summary>
            AF_BAN = 21,
            /// <summary>
            /// Native ATM Services [value = 22].
            /// </summary>
            AF_ATM = 22,
            /// <summary>
            /// Internetwork Version 6 [value = 23].
            /// </summary>
            AF_INET6 = 23,
            /// <summary>
            /// Microsoft Wolfpack [value = 24].
            /// </summary>
            AF_CLUSTER = 24,
            /// <summary>
            /// IEEE 1284.4 WG AF [value = 25].
            /// </summary>
            AF_12844 = 25,
            /// <summary>
            /// IrDA [value = 26].
            /// </summary>
            AF_IRDA = 26,
            /// <summary>
            /// Network Designers OSI &amp; gateway enabled protocols [value = 28].
            /// </summary>
            AF_NETDES = 28,
            /// <summary>
            /// [value = 29].
            /// </summary>
            AF_TCNPROCESS = 29,
            /// <summary>
            /// [value = 30].
            /// </summary>
            AF_TCNMESSAGE = 30,
            /// <summary>
            /// [value = 31].
            /// </summary>
            AF_ICLFXBM = 31,
            AF_BTH = 32,
        }
        public enum SOCKET_TYPE : short
        {
            /// <summary>
            /// stream socket
            /// </summary>
            SOCK_STREAM = 1,
            /// <summary>
            /// datagram socket
            /// </summary>
            SOCK_DGRAM = 2,

            /// <summary>
            /// raw-protocol interface
            /// </summary>
            SOCK_RAW = 3,

            /// <summary>
            /// reliably-delivered message
            /// </summary>
            SOCK_RDM = 4,
            /// <summary>
            /// sequenced packet stream
            /// </summary>
            SOCK_SEQPACKET = 5
        }
        public enum PROTOCOL : short
        {//dummy for IP  
            BTHPROTO_RFCOMM = 3,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct sockaddr
        {
            public ushort sa_family;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
            public byte[] sa_data;
        };
        [StructLayout(LayoutKind.Sequential)]
        public struct sockaddr_in
        {
            //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x1e*2 /* WSAPROTOCOL_LEN */ + 1)]
            //public char[] szProtocol;
            public short sin_family;
            public ushort sin_port;
            public ulong sin_addr;
           public ulong sin_zero1;
            public ulong sin_zero2;
        };

        [DllImport("ws2_32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr socket(ADDRESS_FAMILIES af, SOCKET_TYPE socket_type, PROTOCOL protocol);
        [DllImport("Ws2_32.dll")]
        public static extern int getsockopt(IntPtr s, int level, int optname, out WSAPROTOCOL_INFO optval, ref int optlen);
        [DllImport("Ws2_32.dll", CharSet = CharSet.Ansi, EntryPoint = "WSAAddressToStringA")]
        public static extern uint WSAAddressToString(IntPtr lpsaAddress, int dwAddressLength, ref WSAPROTOCOL_INFO lpProtocolInfo,
    StringBuilder lpszAddressString, ref int lpdwAddressStringLength);
        [DllImport("ws2_32.dll", CharSet = CharSet.Auto)]
        static extern Int32 WSAGetLastError();

        public delegate bool BluethSdpEnumAttributesCb(ulong uAttribId, IntPtr pValueStream, ulong cbStreamSize, IntPtr pvParam);
        [DllImport("irprops.cpl", SetLastError = true)] //, CallingConvention = CallingConvention.Cdecl
        public static extern bool BluetoothSdpEnumAttributes(
  IntPtr pSDPStream,
  ulong cbStreamSize,
  [MarshalAs(UnmanagedType.FunctionPtr)] BluethSdpEnumAttributesCb pfnCallback,
  IntPtr IntPtr //IntPtr
);


        //DWORD BluetoothSdpGetElementData(LPBYTE pSdpStream,ulong cbSdpStreamLength,PSDP_ELEMENT_DATA pData);
        /*
        [DllImport("Ws2_32.dll", CharSet = CharSet.Unicode, EntryPoint = "WSAAddressToStringW")]
        public static extern uint WSAAddressToString(ref sockaddr_in lpsaAddress, int dwAddressLength, IntPtr lpProtocolInfo,
    StringBuilder lpszAddressString, ref int lpdwAddressStringLength);

        [DllImport("Ws2_32.dll", CharSet = CharSet.Unicode, EntryPoint = "WSAAddressToStringW")]
        public static extern uint WSAAddressToString(ref sockaddr_in6 lpsaAddress, int dwAddressLength, IntPtr lpProtocolInfo,
            StringBuilder lpszAddressString, ref int lpdwAddressStringLength);
            */
    }
}
