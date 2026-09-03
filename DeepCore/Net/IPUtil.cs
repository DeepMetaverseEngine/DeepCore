using DeepCore.Concurrent;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace DeepCore.Net
{
    public static class IPUtil
    {
        private static bool s_IsOnlyIPv6 = false;
        private static AtomicReference<bool> s_CheckIP = new AtomicReference<bool>(false);
        private static AtomicReference<bool> s_IgnoreCheckIPv6 = new AtomicReference<bool>(false);
        public static bool PortInUse(int port)
        {
            bool inUse = false;
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();
            foreach (IPEndPoint endPoint in ipEndPoints)
            {
                if (endPoint.Port == port)
                {
                    inUse = true;
                    break;
                }
            }
            return inUse;
        }

        public static bool IgnoreCheckIPv6 { get { return s_IgnoreCheckIPv6.Value; } set { s_IgnoreCheckIPv6.Value = value; } }

        public static IPAddress[] GetIPAddress(string host, int port, out AddressFamily family, out IPHostEntry dns_entries)
        {
            IPAddress[] addrs = new IPAddress[1];
            if (IPAddress.TryParse(host, out addrs[0]))
            {
                //物理地址//
                dns_entries = null;
                family = addrs[0].AddressFamily;
                return addrs;
            }
            else
            {
                IPHostEntry ips = Dns.GetHostEntry(host);
                addrs = ips.AddressList;
                dns_entries = ips;
                if (IsOnlyIPv6(ips.AddressList))
                {
                    //全部V6//
                    family = AddressFamily.InterNetworkV6;
                    return addrs;
                }
                else if (IsOnlyIPv4(ips.AddressList))
                {
                    //全部V4//
                    family = AddressFamily.InterNetwork;
                    return addrs;
                }
                else
                {
                    //V4和V6并存//
                    var ipv4 = ips.AddressList.First(addr => { return addr.AddressFamily == AddressFamily.InterNetwork; });
                    var ipv6 = ips.AddressList.First(addr => { return addr.AddressFamily == AddressFamily.InterNetworkV6; });
                    if (IgnoreCheckIPv6)
                    {
                        family = AddressFamily.InterNetwork;
                        return new IPAddress[] { ipv4 };
                    }
                    lock (s_CheckIP)
                    {
                        if (s_IsOnlyIPv6)
                        {
                            //系统强制V6//
                            family = AddressFamily.InterNetworkV6;
                            return new IPAddress[] { ipv6 };
                        }
                        else
                        {
                            if (s_CheckIP.Value)
                            {
                                family = AddressFamily.InterNetwork;
                                return new IPAddress[] { ipv4 };
                            }
                            else
                            {
                                //检测什么鬼//
                                var ipv4ok = Ping(ipv4, port);
                                if (ipv4ok)
                                {
                                    s_CheckIP.Value = true;
                                    family = AddressFamily.InterNetwork;
                                    return new IPAddress[] { ipv4 };
                                }
                                var ipv6ok = Ping(ipv6, port);
                                if (ipv6ok)
                                {
                                    s_IsOnlyIPv6 = true;
                                    s_CheckIP.Value = true;
                                    family = AddressFamily.InterNetworkV6;
                                    return new IPAddress[] { ipv6 };
                                }
                                throw new Exception("Host Not Resolved : " + host);
                            }
                        }
                    }
                }
            }
        }

        public static IPEndPoint[] GetIPEndPoints(string host, int port, out AddressFamily family, out IPHostEntry dns_entries)
        {
            var addrs = GetIPAddress(host, port, out family, out dns_entries);
            if (addrs != null)
            {
                return Array.ConvertAll(addrs, addr => new IPEndPoint(addr, port));
            }
            return null;
        }

        public static bool TryGetIPAddress(string host, int port, out IPAddress address, out AddressFamily family, out IPHostEntry dns_entries)
        {
            var addrs = GetIPAddress(host, port, out family, out dns_entries);
            if (addrs != null && addrs.Length > 0)
            {
                address = addrs[0];
                return true;
            }
            address = null;
            return false;
        }
        public static bool TryGetIPEndPoint(string host, int port, out IPEndPoint endpoint, out AddressFamily family, out IPHostEntry dns_entries)
        {
            var addrs = GetIPAddress(host, port, out family, out dns_entries);
            if (addrs != null && addrs.Length > 0)
            {
                endpoint = new IPEndPoint(addrs[0], port);
                return true;
            }
            endpoint = null;
            return false;
        }
        public static IPEndPoint ToIPEndPoint(string host, int port)
        {
            var addrs = GetIPAddress(host, port, out var family, out var dns_entries);
            if (addrs != null && addrs.Length > 0)
            {
                return new IPEndPoint(addrs[0], port);
            }
            return null;
        }
        public static IPEndPoint ToIPEndPoint(string address)
        {
            if (TryParseHostPort(address, out var host, out var port))
            {
                var addrs = GetIPAddress(host, port, out var family, out var dns_entries);
                if (addrs != null && addrs.Length > 0)
                {
                    return new IPEndPoint(addrs[0], port);
                }
            }
            return null;
        }

        private static bool Ping(IPAddress ip, int port)
        {
            Console.WriteLine("Check Connect : " + ip);
            var s = new TcpClient(ip.AddressFamily);
            try
            {
                s.NoDelay = true;
                s.SendTimeout = 3000;
                s.ReceiveTimeout = 3000;
                s.Client.Blocking = true;
                s.Connect(new IPAddress[] { ip }, port);
                var ret = s.Connected;
                return ret;
            }
            catch (Exception err)
            {
                err.PrintStackTrace("Check Connect Error : " + ip + " : ");
                return false;
            }
            finally
            {
                try { s.Client.Shutdown(SocketShutdown.Both); } catch { }
                try { s.Close(); } catch { }
            }
        }

        public static bool IsOnlyIPv6(IPAddress[] adddrs)
        {
            if (adddrs.Length == 0)
            {
                return false;
            }
            foreach (var ip in adddrs)
            {
                if (ip.AddressFamily != AddressFamily.InterNetworkV6)
                {
                    return false;
                }
            }
            return true;
        }
        public static bool IsOnlyIPv4(IPAddress[] adddrs)
        {
            if (adddrs.Length == 0)
            {
                return false;
            }
            foreach (var ip in adddrs)
            {
                if (ip.AddressFamily != AddressFamily.InterNetwork)
                {
                    return false;
                }
            }
            return true;
        }

        public static IPAddress MapToIPv6(this IPAddress addr)
        {
            if (addr.AddressFamily != AddressFamily.InterNetwork)
                throw new ArgumentException("Must pass an IPv4 address to MapToIPv6");

            string ipv4str = addr.ToString();

            return IPAddress.Parse("::ffff:" + ipv4str);
        }
        public static IPAddress MapToIPv6(this string ipv4str)
        {
            return IPAddress.Parse("::ffff:" + ipv4str);
        }

        public static bool IsIPv4MappedToIPv6(this IPAddress addr)
        {
            bool pass1 = addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6, pass2;

            try
            {
                pass2 = (addr.ToString().StartsWith("0000:0000:0000:0000:0000:ffff:") ||
                        addr.ToString().StartsWith("0:0:0:0:0:ffff:") ||
                        addr.ToString().StartsWith("::ffff:")) &&
                        IPAddress.Parse(addr.ToString().Substring(addr.ToString().LastIndexOf(":") + 1)).AddressFamily == AddressFamily.InterNetwork;
            }
            catch (Exception err)
            {
                err.PrintStackTrace();
                return false;
            }

            return pass1 && pass2;
        }

        public static bool TryParseHostPort(string addr, out string host, out int port)
        {
            if (!string.IsNullOrEmpty(addr))
            {
                var kv = addr.Split(':');
                if (kv.Length == 2)
                {
                    host = kv[0];
                    return Parser.TryParseInt(kv[1], out port);
                }
            }
            host = null;
            port = 0;
            return false;
        }



        public static string GetDefaultHost(AddressFamily family = AddressFamily.InterNetwork)
        {
            try
            {
                string name = Dns.GetHostName();
                IPAddress[] ipadrlist = Dns.GetHostAddresses(name);
                foreach (IPAddress ipa in ipadrlist)
                {
                    if (ipa.AddressFamily == family)
                    {
                        return ipa.ToString();
                    }
                }
            }
            catch { }
            return null;
        }

    }
}
