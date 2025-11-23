using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Application.Utils
{
    public class NetworkDrive
    {
        public enum ResourceScope
        {
            RESOURCE_CONNECTED = 1,
            RESOURCE_GLOBALNET,
            RESOURCE_REMEMBERED,
            RESOURCE_RECENT,
            RESOURCE_CONTEXT
        }

        public enum ResourceType
        {
            RESOURCETYPE_ANY,
            RESOURCETYPE_DISK,
            RESOURCETYPE_PRINT,
            RESOURCETYPE_RESERVED
        }

        public enum ResourceUsage
        {
            RESOURCEUSAGE_CONNECTABLE = 0x00000001,
            RESOURCEUSAGE_CONTAINER = 0x00000002,
            RESOURCEUSAGE_NOLOCALDEVICE = 0x00000004,
            RESOURCEUSAGE_SIBLING = 0x00000008,
            RESOURCEUSAGE_ATTACHED = 0x00000010,
            RESOURCEUSAGE_ALL = (RESOURCEUSAGE_CONNECTABLE | RESOURCEUSAGE_CONTAINER | RESOURCEUSAGE_ATTACHED),
        }

        public enum ResourceDisplayType
        {
            RESOURCEDISPLAYTYPE_GENERIC,
            RESOURCEDISPLAYTYPE_DOMAIN,
            RESOURCEDISPLAYTYPE_SERVER,
            RESOURCEDISPLAYTYPE_SHARE,
            RESOURCEDISPLAYTYPE_FILE,
            RESOURCEDISPLAYTYPE_GROUP,
            RESOURCEDISPLAYTYPE_NETWORK,
            RESOURCEDISPLAYTYPE_ROOT,
            RESOURCEDISPLAYTYPE_SHAREADMIN,
            RESOURCEDISPLAYTYPE_DIRECTORY,
            RESOURCEDISPLAYTYPE_TREE,
            RESOURCEDISPLAYTYPE_NDSCONTAINER
        }

        [StructLayout(LayoutKind.Sequential)]
        private sealed class NETRESOURCE
        {
            public ResourceScope dwScope = 0;
            public ResourceType dwType = 0;
            public ResourceDisplayType dwDisplayType = 0;
            public ResourceUsage dwUsage = 0;
            public string lpLocalName = "";
            public string lpRemoteName = "";
            public string lpComment = "";
            public string lpProvider = "";
        }

        [DllImport("mpr.dll")]
        private static extern int WNetAddConnection2(NETRESOURCE lpNetResource, string lpPassword, string lpUsername, int dwFlags);

        [DllImport("mpr.dll")]
        private static extern int WNetCancelConnection2(string name, int flags, bool force);

        public string MapNetworkDrive(string unc, string drive, string user, string password)
        {
            try
            {
                try
                {
                    var res = UnmapNetworkDrive(drive);
                    res = UnmapNetworkDrive(unc);
                }
                catch { }
                if (unc.IndexOf("\\\\") != 0) return "";
                NETRESOURCE myNetResource = new NETRESOURCE();
                myNetResource.lpLocalName = drive;
                myNetResource.lpRemoteName = unc;
                myNetResource.lpProvider = null;
                int result = WNetAddConnection2(myNetResource, password, user, 0);
                if (result == 0) return "";
                if (result == 53) return "Network path not found  Check UNC path and firewall";
                if (result == 66) return "The specified username is invalid";
                if (result == 67) return "Network name cannot be found Wrong share name";
                if (result == 85) return "Local device name is already in use";
                if (result == 86) return "ERROR_INVALID_PASSWORD";
                if (result == 1202) return "The local device name has a remembered connection to another network resource";
                if (result == 1219) return "Multiple connections    You must unmap existing connection first";
                if (result == 1326) return "Login failure   Wrong username or password";
                return result + "";
            }
            catch
            {
                return "";
            }
        }

        public static int UnmapNetworkDrive(string localDrive)
        {
            try
            {
                return WNetCancelConnection2(localDrive, 0, true);
            }
            catch
            {
                return 0;
            }
            /*
ERROR_NOT_CONNECTED (2250)
"This network connection does not exist."
             */
        }
    }
}
