// Machina ~ FirewallWrapper.cs
// 
// Copyright © 2017 Ravahn - All Rights Reserved
// 
//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.If not, see<http://www.gnu.org/licenses/>.

using System;
using NetFwTypeLib;
using System.Diagnostics;

namespace Machina
{
    /// <summary>
    /// Helper functions to assist with checking and configuring Windows Firewall 
    /// </summary>
    public class FirewallWrapper
    {

        /// <summary>
        /// Determines whether the windows firewall is enabled.
        /// </summary>
        /// <returns>boolean indicating whether firewall is enabled</returns>
        public bool IsFirewallDisabled()
        {
            Type typeFWMgr = Type.GetTypeFromProgID("HNetCfg.FwMgr");
            NetFwTypeLib.INetFwMgr manager = Activator.CreateInstance(typeFWMgr) as NetFwTypeLib.INetFwMgr;
            if (manager == null)
                return false;

            // check if Firewall is enabled
            if (manager.LocalPolicy.CurrentProfile.FirewallEnabled == false)
                return true;

            return false;
        }

        /// <summary>
        /// Determines whether the windows firewall is has configuration elements for the specified application name.
        /// </summary>
        /// <param name="appName">Name of the application to test for firewall configuration</param>
        /// <returns>boolean indicating whether firewall is configured for the specified application</returns>
        public bool IsFirewallApplicationConfigured(string appName)
        {
            bool bFound = false;

            Type typeFWMgr = Type.GetTypeFromProgID("HNetCfg.FwMgr");
            NetFwTypeLib.INetFwMgr manager = Activator.CreateInstance(typeFWMgr) as NetFwTypeLib.INetFwMgr;
            if (manager == null)
                return false;

            // check applications list
            System.Collections.IEnumerator appEnumerate = manager.LocalPolicy.CurrentProfile.AuthorizedApplications.GetEnumerator();
            if (appEnumerate == null)
                return false;
            while (appEnumerate.MoveNext() && bFound == false)
            {
                NetFwTypeLib.INetFwAuthorizedApplication app = appEnumerate.Current as NetFwTypeLib.INetFwAuthorizedApplication;
                if (app != null && app.Name == appName && app.Enabled == true)
                    bFound = true;
            }

            return bFound;
        }

        /// <summary>
        /// Determines whether the specified application has TCP firewall rules configured
        /// </summary>
        /// <param name="appName">name of the application to test for TCP rules</param>
        /// <returns>boolean indicating whether the firewall is configured with TCP rules for the specified application</returns>
        public bool IsFirewallRuleConfigured(string appName)
        {
            bool bFound = false;

            // check firewall rules - need TCP open for the application
            Type typePolicy = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            NetFwTypeLib.INetFwPolicy2 policy = Activator.CreateInstance(typePolicy) as NetFwTypeLib.INetFwPolicy2;
            if (policy == null)
                return false;
            System.Collections.IEnumerator appEnumerate = policy.Rules.GetEnumerator();
            if (appEnumerate == null)
                return false;
            while (appEnumerate.MoveNext() && bFound == false)
            {
                NetFwTypeLib.INetFwRule2 rule = appEnumerate.Current as NetFwTypeLib.INetFwRule2;
                if (rule != null && rule.Name == appName && rule.Enabled == true)// && rule.Protocol == 6) // tcp
                    bFound = true;
            }

            return bFound;
        }

        /// <summary>
        /// Adds the specified application name, located at the specific path, to the firewall rules 
        /// </summary>
        /// <param name="appName">name of the application to add</param>
        /// <param name="ExecutablePath">file path of the application to add</param>
        public void AddFirewallApplicationEntry(string appName, string ExecutablePath)
        {
            try
            {
                Type typeFWMgr = Type.GetTypeFromProgID("HNetCfg.FwMgr");
                NetFwTypeLib.INetFwMgr manager = Activator.CreateInstance(typeFWMgr) as NetFwTypeLib.INetFwMgr;
                if (manager == null)
                    throw new ApplicationException("Unable to connect to fireall.");

                Type typeApp = Type.GetTypeFromProgID("HNetCfg.FwAuthorizedApplication");
                NetFwTypeLib.INetFwAuthorizedApplication app = Activator.CreateInstance(typeApp) as NetFwTypeLib.INetFwAuthorizedApplication;
                if (app == null)
                    throw new ApplicationException("Unable to create new Firewall Application reference.");

                app.Enabled = true;
                //app.IpVersion = NET_FW_IP_VERSION_.NET_FW_IP_VERSION_V4;
                app.Name = appName;
                app.ProcessImageFileName = ExecutablePath;// System.Windows.Forms.Application.ExecutablePath;
                app.Scope = NET_FW_SCOPE_.NET_FW_SCOPE_ALL;

                manager.LocalPolicy.CurrentProfile.AuthorizedApplications.Add(app);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("FirewallWrapper: Exception " + ex.ToString(), "FIREWALL");
            }
        }
    }
}
