# Machina

Machina is a library that allows developers to read network data from the windows networking subsystem and reassemble it into usable information.

It supports the following features:
* Simple raw socket for data capture or optional WinPcap driver support
* IP Fragmentation reassembly
* TCP stream reassembly, including retransmits

Because it is accessing network data, it does require running under elevated security privleges on the local machine.  It also requires configuring access through the local firewall, or disabling it completely, in order to read data.

In order to simplify use of this library, the TCPNetworkMonitor class was added to poll the network data for a specific process and raise an event when new data arrives.  Use of this class can be found in the TCPNetworkMonitorTests class, but here is some sample code:


    public static void Main(string[] args)
    {
        TCPNetworkMonitor monitor = new TCPNetworkMonitor();
        monitor.WindowName = "FINAL FANTASY XIV";
        monitor.MonitorType = TCPNetworkMonitor.NetworkMonitorType.RawSocket;
        monitor.DataReceived = (string connection, byte[] data) => DataReceived(connection, data);
        monitor.Start();
        // Run for 10 seconds
        System.Threading.Thread.Sleep(10000);
        monitor.Stop();
    }
    private static void DataReceived(string connection, byte[] data)
    {
        // Process Data
    }

The import elements in the above code are:
1) Configure the monitor class with the correct window name or process ID
2) Hook the monitor up to a data received event
3) Start the monitor - this kicks off a long-running Task
4) Process the data in the DataReceived() event handler
5) Stop the monitor before exiting the process, to prevent unmanaged resources from leaking.  This mostly affects WinPCap.

Prior to the above, be sure to either disable windows firewall, or add a rule for any exceutable using the above code to work through it.  And, the code must be executed as a local administrator.  To debug the above code, you will need to start Visual Studio using the 'Run as Administrator' option in Windows.

# Machina.FFXIV
Machina.FFXIV is an extension to the Machina library that decodes Final Fantasy XIV network data and makes it available to programs.  It uses the Machina library to locate the game traffic and decode the TCP/IP layer, and then decodes / decompresses the game data into individual game messages.  It processes both incoming and outgoing messages.

    public static void Main(string[] args)
    {
        FFXIVNetworkMonitor monitor = new FFXIVNetworkMonitor();
        monitor.MessageReceived = (long epoch, byte[] message) => MessageReceived(connection, message);
        monitor.Start();
        // Run for 10 seconds
        System.Threading.Thread.Sleep(10000);
        monitor.Stop();
    }
    private static void MessageReceived(long epoch, byte[] message)
    {
        // Process Message
    }

An optional Process ID and network monitor type can be specified as properties, to configure per the end-user's machine requirements.