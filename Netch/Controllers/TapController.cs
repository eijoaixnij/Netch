using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Netch.Enums;
using Netch.Interfaces;
using Netch.Interops;
using Netch.Models;
using Netch.Servers.Socks5;
using Netch.Utils;
using static Netch.Interops.tap2socks;

namespace Netch.Controllers
{
    public class TapController : IModeController
    {
        private const string DummyDns = "6.6.6.6";
        private readonly DNSController _dnsController = new();
        private NetRoute _outbound;
        private IPAddress? _serverRemoteAddress;
        private NetRoute _tap;

        public string Name => "tap2socks";

        public void Start(in Mode mode)
        {
            _outbound = NetRoute.GetBestRouteTemplate(out var address);

            var s = MainController.Server!;
            _serverRemoteAddress = DnsUtils.Lookup(s.Hostname);
            if (_serverRemoteAddress != null && IPAddress.IsLoopback(_serverRemoteAddress))
                _serverRemoteAddress = null;

            Dial(NameList.TYPE_BYPBIND, address.ToString());

            if (mode.Type is ModeType.BypassRuleIPs && mode.GetRules().Any())
            {
                // Bypass Rule IPs
                File.WriteAllLines(Constants.TempRouteFile, mode.GetRules());
                Dial(NameList.TYPE_BYPLIST, Constants.TempRouteFile);
            }
            else
            {
                Dial(NameList.TYPE_BYPLIST, "disabled");
            }

            Dial(NameList.TYPE_DNSADDR, Global.Settings.TUNTAP.UseCustomDNS ? Global.Settings.TUNTAP.HijackDNS : "127.0.0.1");
            Dial(NameList.TYPE_TCPREST, "");
            Dial(NameList.TYPE_UDPREST, "");

            switch (s)
            {
                case Socks5 node:
                    Dial(NameList.TYPE_TCPTYPE, "Socks");
                    Dial(NameList.TYPE_UDPTYPE, "Socks");
                    Dial(NameList.TYPE_TCPHOST, $"{node.AutoResolveHostname()}:{node.Port}");
                    Dial(NameList.TYPE_UDPHOST, $"{node.AutoResolveHostname()}:{node.Port}");

                    if (node.Auth())
                    {
                        Dial(NameList.TYPE_TCPUSER, node.Username!);
                        Dial(NameList.TYPE_UDPUSER, node.Username!);

                        Dial(NameList.TYPE_TCPPASS, node.Password!);
                        Dial(NameList.TYPE_UDPPASS, node.Password!);
                    }

                    break;
                default:
                    Dial(NameList.TYPE_TCPTYPE, "Socks");
                    Dial(NameList.TYPE_TCPHOST, $"127.0.0.1:{Global.Settings.Socks5LocalPort}");
                    Dial(NameList.TYPE_UDPTYPE, "Socks");
                    Dial(NameList.TYPE_UDPHOST, $"127.0.0.1:{Global.Settings.Socks5LocalPort}");
                    break;
            }

            _dnsController.Start();

            if (!tap_init())
                throw new MessageException("tap2socks start failed");

            if (!AssignInterface())
                throw new MessageException("Assign Interface Address failed");

            CreateServerRoute(s);

            // Proxy Rule IPs
            CreateHandleRoute(mode);
        }

        public void Stop()
        {
            Task.WaitAll(Task.Run(_dnsController.Stop), Task.Run(tap_free), Task.Run(RemoveBypassRoute));
        }

        private bool AssignInterface()
        {
            var tap = NetworkInterfaceUtils.Get(tap_name());
            _tap = NetRoute.TemplateBuilder(IPAddress.Parse(Global.Settings.TUNTAP.Gateway), tap.GetIndex());

            tap.SetDns(DummyDns);

            return RouteHelper.CreateUnicastIP(AddressFamily.InterNetwork,
                Global.Settings.TUNTAP.Address,
                (byte)Misc.SubnetToCidr(Global.Settings.TUNTAP.Netmask),
                (ulong)_tap.InterfaceIndex);
        }

        private void CreateHandleRoute(in Mode mode)
        {
            if (mode.Type is ModeType.ProxyRuleIPs)
                RouteUtils.CreateRouteFill(_tap, mode.GetRules());
        }

        private void CreateServerRoute(Server server)
        {
            if (_serverRemoteAddress != null)
                RouteUtils.CreateRoute(_outbound.FillTemplate(_serverRemoteAddress.ToString(), 32));
        }

        private void RemoveBypassRoute()
        {
            if (_serverRemoteAddress != null)
                RouteUtils.DeleteRoute(_outbound.FillTemplate(_serverRemoteAddress.ToString(), 32));
        }
    }
}