// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Reflection;
using MQTTnet.BrokerVANL;

string[] commandLineArgs = Environment.GetCommandLineArgs();

Console.BackgroundColor = ConsoleColor.White;
Console.BackgroundColor = ConsoleColor.Red;
Console.Title = "VANL - MQTTnet";
Console.WriteLine("------------------------------------------------------");
Console.WriteLine("-         Welcome to the VANL MQTTnet broker!        -");
Console.WriteLine("------------------------------------------------------");
Console.ResetColor();
Console.WriteLine();

IPAddress? ipAddress = new IPAddress(new byte[] {10, 10, 4, 163});;

if (commandLineArgs.Length > 1) 
{
    string ipAddressString = commandLineArgs[1];
    if (IPAddress.TryParse(ipAddressString, out ipAddress))
            {
                // Now 'ipAddress' contains the parsed IP address
                Console.WriteLine($"Running Broker on argument IP Address: {ipAddress}");
            }
            else
            {
                Console.WriteLine($"Invalid IP address format: {ipAddressString}");
                Console.WriteLine("Exiting Broker");
                Environment.Exit(1);              
            }
}
else 
{
    Console.WriteLine($"Running Broker on default IP Address: {ipAddress}");
}


Console.WriteLine("Starting Broker...");

try
{
    await Server_ASP_NET_VANL.Start_Server_With_WebSockets_Support(ipAddress);
}
catch (Exception exception)
{
    Console.WriteLine(exception.ToString());
}

Console.WriteLine();
Console.WriteLine("Press Enter to exit.");
Console.ReadLine();