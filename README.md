# Machina

Machina is a library that allows developers to read network data from the windows networking subsystem and reassemble it into usable information.

Community discussion is available on the [ACT_FFXIV_Plugin Discord server](https://discord.gg/H48N2vZ) in the #machina-discussion channel.

It supports the following features:
* Simple raw socket for data capture or optional WinPcap driver support
* IP Fragmentation reassembly
* TCP stream reassembly, including retransmits

Because it is accessing network data, it does require running under elevated security privleges on the local machine.  It also requires configuring access through the local firewall, or disabling it completely, in order to read data.

In order to simplify use of this library, the TCPNetworkMonitor class polls the network data for a specific process and raises an event when new data arrives.  Use of this class can be found in the TCPNetworkMonitorTests class, but here is some sample code:


    public static void Main(string[] args)
    {
        TCPNetworkMonitor monitor = new TCPNetworkMonitor();
        monitor.Config.WindowName = "FINAL FANTASY XIV";
        monitor.Config.MonitorType = NetworkMonitorType.RawSocket;
        monitor.DataReceivedEventHandler += (TCPConnection connection, byte[] data) => DataReceived(connection, data);
        monitor.Start();
        // Run for 10 seconds
        System.Threading.Thread.Sleep(10000);
        monitor.Stop();
    }
    private static void DataReceived(TCPConnection connection, byte[] data)
    {
        // Process Data
    }

The important elements in the above code are:
1) Configure the monitor class with the correct window name or process ID
2) Hook the monitor up to a data received event
3) Start the monitor - this kicks off a long-running Task
4) Process the data in the DataReceived() event handler
5) Stop the monitor before exiting the process, to prevent unmanaged resources from leaking.

Prior to the above, be sure to either disable windows defender firewall, or add a rule for any executable using the above code to work through it.  Raw sockets require running as a local administrator to capture data, but the PCap driver does not.  To debug the above code, you will need to start Visual Studio using the 'Run as Administrator' option in Windows.

The public property UseRemoteIpFilter, when set to true, will apply socket and winpcap filters on both source and target IP Addresses for the connections being monitored.  This means that each connection to a new remote IP must be detected and listener started before data will be received.  It is likely that some network data will be lost between when the process initiates the connection, and when the Machina library begins to listen.  It should only be used if the initial data sent on the connection is not critical.  However, it has the benefit of significantly reducing the potential for data loss when there is excessive local network traffic, for example when streaming or when doing bulk file transfers over the network.

# Machina.FFXIV
Machina.FFXIV is an extension to the Machina library that decodes Final Fantasy XIV network data and makes it available to programs.  It uses the Machina library to locate the game traffic and decode the TCP/IP layer, and then decodes / decompresses the game data into individual game messages.  It processes both incoming and outgoing messages.

    public static void Main(string[] args)
    {
        FFXIVNetworkMonitor monitor = new FFXIVNetworkMonitor();
        monitor.MessageReceivedEventHandler = (TCPConnection connection, long epoch, byte[] message) => MessageReceived(connection, epoch, message);
        monitor.Start();
        // Run for 10 seconds
        System.Threading.Thread.Sleep(10000);
        monitor.Stop();
    }
    private static void MessageReceived(TCPConnection connection, long epoch, byte[] message)
    {
        // Process Message
    }

An optional Process ID and network monitor type can be specified as properties, to configure per the end-user's machine requirements.

An optional property UseRemoteIpFilter can be set, which is passed through to the TCPNetworkMonitor's property with the same name.  This is generally fine for FFXIV, since the remote server IP does not frequently change.
