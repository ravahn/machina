// Machina ~ FirewallWrapper.cs
// 
// Copyright © 2007 - 2017 Ryan Wilson - All Rights Reserved
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using NetFwTypeLib;
using NLog;

namespace Machina
{
    public class FirewallWrapper
    {
        #region Logger

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion

        public bool IsFirewallDisabled()
        {
            var netFwMgr = Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwMgr")) as INetFwMgr;
            return netFwMgr != null && !netFwMgr.LocalPolicy.CurrentProfile.FirewallEnabled;
        }

        public bool IsFirewallApplicationConfigured(string applicationName)
        {
            var flag = false;
            var netFwMgr = Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwMgr")) as INetFwMgr;
            var enumerator = netFwMgr?.LocalPolicy.CurrentProfile.AuthorizedApplications.GetEnumerator();
            if (enumerator == null)
            {
                return false;
            }
            while (enumerator.MoveNext() && !flag)
            {
                var authorizedApplication = enumerator.Current as INetFwAuthorizedApplication;
                if (authorizedApplication != null && authorizedApplication.Name == applicationName && authorizedApplication.Enabled)
                {
                    flag = true;
                }
            }
            return flag;
        }

        public bool IsFirewallRuleConfigured(string applicationName)
        {
            var flag = false;
            var netFwPolicy2 = Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2")) as INetFwPolicy2;
            var enumerator = netFwPolicy2?.Rules.GetEnumerator();
            if (enumerator == null)
            {
                return false;
            }
            while (enumerator.MoveNext() && !flag)
            {
                var netFwRule2 = enumerator.Current as INetFwRule2;
                if (netFwRule2 != null && netFwRule2.Name == applicationName && netFwRule2.Enabled && netFwRule2.Protocol == 6)
                {
                    flag = true;
                }
            }
            return flag;
        }

        public void AddFirewallApplicationEntry(string applicationName, string executablePath)
        {
            try
            {
                var netFwMgr = Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwMgr")) as INetFwMgr;
                if (netFwMgr == null)
                {
                    throw new ApplicationException("Unable To Connect To Firewall.");
                }
                var app = Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwAuthorizedApplication")) as INetFwAuthorizedApplication;
                if (app == null)
                {
                    throw new ApplicationException("Unable To Create New Firewall Application Reference");
                }
                app.Enabled = true;
                app.IpVersion = NET_FW_IP_VERSION_.NET_FW_IP_VERSION_ANY;
                app.Name = applicationName;
                app.ProcessImageFileName = executablePath;
                app.Scope = NET_FW_SCOPE_.NET_FW_SCOPE_ALL;

                netFwMgr.LocalPolicy.CurrentProfile.AuthorizedApplications.Add(app);
            }
            catch (Exception ex)
            {
                NetworkHandler.Instance.RaiseException(Logger, ex);
            }
        }
    }
}
