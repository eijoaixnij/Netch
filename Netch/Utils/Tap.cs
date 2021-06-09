using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Win32;
using Netch.Models;
using Serilog;

namespace Netch.Utils
{
    public static class TAP
    {
        public const string TUNTAP_COMPONENT_ID_0901 = "tap0901";
        public const string TUNTAP_COMPONENT_ID_0801 = "tap0801";
        public const string NETWORK_KEY = @"SYSTEM\CurrentControlSet\Control\Network\{4D36E972-E325-11CE-BFC1-08002BE10318}";
        public const string ADAPTER_KEY = @"SYSTEM\CurrentControlSet\Control\Class\{4D36E972-E325-11CE-BFC1-08002BE10318}";

        public static string? GetComponentID()
        {
            try
            {
                var adaptersRegistry = Registry.LocalMachine.OpenSubKey(ADAPTER_KEY)!;

                foreach (var keyName in adaptersRegistry.GetSubKeyNames().Where(s => s is not ("Configuration" or "Properties")))
                {
                    var adapterRegistry = adaptersRegistry.OpenSubKey(keyName)!;
                    var componentId = adapterRegistry.GetValue("ComponentId")?.ToString();
                    if (componentId == null)
                        continue;

                    if (componentId == TUNTAP_COMPONENT_ID_0901 || componentId == TUNTAP_COMPONENT_ID_0801)
                        return (string)(adapterRegistry.GetValue("NetCfgInstanceId") ??
                                        throw new Exception("Tap adapter have no NetCfgInstanceId key"));
                }
            }
            catch (Exception e)
            {
                Log.Warning(e, "获取 TAP ComponentID 异常");
            }

            return null;
        }

        public static void deltapall()
        {
            Log.Information("卸载 TAP 适配器");
            using var process = new Process
            {
                StartInfo =
                {
                    FileName = Path.Combine("bin/tap-driver", "deltapall.bat"),
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            process.Start();
            process.WaitForExit();
            process.Close();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="MessageException"></exception>
        public static void addtap()
        {
            Log.Information("安装 TAP 适配器");
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine("bin/tap-driver", "addtap.bat"),
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            process.Start();
            process.WaitForExit();

            Thread.Sleep(1000);
            if (GetComponentID() == null)
                throw new MessageException("TAP 驱动安装失败，找不到 ComponentID 注册表项");
        }
    }
}