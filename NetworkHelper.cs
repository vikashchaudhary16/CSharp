using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ExceptionDemo
{
    class NetworkHelper
    {
        public ManagementObjectCollection GetRemoteSystemDriveInfo(string remoteSytemName,string userName,string password)
        {
            ConnectionOptions options = new ConnectionOptions();
            options.Username = userName;
            options.Password = password;
            options.Impersonation = System.Management.ImpersonationLevel.Impersonate;
            ManagementScope scope = null;
            scope = new ManagementScope(@"\\" + remoteSytemName + @"\root\cimv2", options);
            scope.Connect();

            //Query system for Operating System information
            ObjectQuery query = new ObjectQuery("select FreeSpace,Size,Name from Win32_LogicalDisk where DriveType=3");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
            ManagementObjectCollection queryCollection = searcher.Get();

            return queryCollection;
        }

        public ManagementObject GetRemoteSystemDriveInfo(string remoteSytemName, string userName, string password,string driveName)
        {
            ConnectionOptions options = new ConnectionOptions();
            options.Username = userName;
            options.Password = password;
            options.Impersonation = System.Management.ImpersonationLevel.Impersonate;
            ManagementScope scope = null;
            scope = new ManagementScope(@"\\" + remoteSytemName + @"\root\cimv2", options);
            scope.Connect();

            //Query system for Operating System information
            SelectQuery query = new SelectQuery("select FreeSpace,Size,Name from Win32_LogicalDisk where DriveType=3", driveName + ":");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
            ManagementObjectCollection queryCollection = searcher.Get();

            ManagementObject[] result = null;
            queryCollection.CopyTo(result, 0);

            return result[0];
        }
    }

    public class ConnectToSharedFolder : IDisposable
    {
        readonly string _networkName;

        public ConnectToSharedFolder(string networkName, NetworkCredential credentials)
        {
            _networkName = networkName;

            var netResource = new NetResource
            {
                Scope = ResourceScope.GlobalNetwork,
                ResourceType = ResourceType.Disk,
                DisplayType = ResourceDisplaytype.Share,
                RemoteName = networkName
            };

            var userName = string.IsNullOrEmpty(credentials.Domain) ? credentials.UserName : string.Format(@"{0}\{1}", credentials.Domain, credentials.UserName);

            var result = WNetAddConnection2(netResource,credentials.Password,userName,0);

            if (result != 0)
            {
                throw new Win32Exception(result, "Error connecting to remote share");
            }
        }

        ~ConnectToSharedFolder()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            WNetCancelConnection2(_networkName, 0, true);
        }

        [DllImport("mpr.dll")]
        private static extern int WNetAddConnection2(NetResource netResource, string password, string username, int flags);

        [DllImport("mpr.dll")]
        private static extern int WNetCancelConnection2(string name, int flags, bool force);

        [StructLayout(LayoutKind.Sequential)]
        public class NetResource
        {
            public ResourceScope Scope;
            public ResourceType ResourceType;
            public ResourceDisplaytype DisplayType;
            public int Usage;
            public string LocalName;
            public string RemoteName;
            public string Comment;
            public string Provider;
        }

        public enum ResourceScope : int
        {
            Connected = 1,
            GlobalNetwork,
            Remembered,
            Recent,
            Context
        };

        public enum ResourceType : int
        {
            Any = 0,
            Disk = 1,
            Print = 2,
            Reserved = 8,
        }

        public enum ResourceDisplaytype : int
        {
            Generic = 0x0,
            Domain = 0x01,
            Server = 0x02,
            Share = 0x03,
            File = 0x04,
            Group = 0x05,
            Network = 0x06,
            Root = 0x07,
            Shareadmin = 0x08,
            Directory = 0x09,
            Tree = 0x0a,
            Ndscontainer = 0x0b
        }

    }
}
