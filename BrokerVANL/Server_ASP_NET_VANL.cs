// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming
// ReSharper disable EmptyConstructor
// ReSharper disable MemberCanBeMadeStatic.Local

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet.AspNetCore;
using MQTTnet.Server;

using System.Text.RegularExpressions;

namespace MQTTnet.BrokerVANL;

public static class Server_ASP_NET_VANL
{

    private static readonly string UserName = "vanlclient";
    private static readonly string Password = "theclown";

    //Production webhook: "https://discord.com/api/webhooks/1192795355998330892/1R3Lic4souDscjofwWA7MrYFVcIYvKh9cutcatb8dagYfwIGKrgI84JFyEaZj90WnsDR";
    private static readonly string discordWebhook = "https://discord.com/api/webhooks/1200433721686372574/BEcLrC_3OeAaFGnamonUyvC0s8RxlZSMRrZDCVutyyBe9qSGirJ-fj7MeL_agVrk7758"; 

    static async void SendDiscordMs(string webhook, string message)
    {
        using (HttpClient client = new HttpClient())
        {
            // Create JSON payload
            string payload = $"{{\"content\": \"{message}\"}}";

            // Send POST request
            HttpResponseMessage response = await client.PostAsync(webhook, new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));

            // Check if the request was successful
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Message sent successfully");
            }
            else
            {
                Console.WriteLine($"Error sending message. Status code: {response.StatusCode}");
            }
        }
    }

    static bool IsDashApp(string ClientId) 
    {
        // python dash app client
        if (Regex.Match(ClientId, @"dash_mqtt_[a-zA-Z0-9]{8}").Success) {
            return true;
        }

        return false;
    }


    static void LogClientConnected(ClientConnectedEventArgs eventArgs) 
    {
        if (!IsDashApp(eventArgs.ClientId)) 
        {
            var message = $"Client '{eventArgs.ClientId}' connected at '{DateTime.Now}'";
            SendDiscordMs(discordWebhook, message);
        }
    }

    static void LogClientDisconnected(ClientDisconnectedEventArgs eventArgs) 
    {
        if (!IsDashApp(eventArgs.ClientId)) 
        {
            var message = $"Client '{eventArgs.ClientId}' disconnected with type '{eventArgs.DisconnectType}' at '{DateTime.Now}'";
            SendDiscordMs(discordWebhook, message);
            
            Console.WriteLine($"Client '{eventArgs.ClientId}' disconnected.");
        }
    }

    public static Task Start_Server_With_WebSockets_Support(System.Net.IPAddress address)
    {
        /*
         * This sample starts a minimal ASP.NET Webserver including a hosted MQTT server.
         */

        var host = Host.CreateDefaultBuilder(Array.Empty<string>())
            .ConfigureWebHostDefaults(
                webBuilder =>
                {
                    webBuilder.UseKestrel(
                        o =>
                        {
                            // This will allow MQTT connections based on TCP port 1883.
                            o.ListenAnyIP(5001, l => l.UseMqtt());

                            o.Listen(address, 5001, l => l.UseMqtt());

                            // This will allow MQTT connections based on HTTP WebSockets with URI "localhost:5000/mqtt"
                            // See code below for URI configuration.
                            o.Listen(address, 9001); // Default HTTP pipeline
                        });

                    webBuilder.UseStartup<Startup>();
                });

        return host.RunConsoleAsync();
    }

    sealed class MqttControllerVANL
    {
        public MqttControllerVANL()
        {
            // Inject other services via constructor.
        }
        
        public Task OnClientConnected(ClientConnectedEventArgs eventArgs)
        {
            LogClientConnected(eventArgs);

            return Task.CompletedTask;
        }


        public Task ValidateConnection(ValidatingConnectionEventArgs eventArgs)
        {

            if (eventArgs.UserName == UserName && eventArgs.Password == Password || IsDashApp(eventArgs.ClientId)) 
            {
                Console.WriteLine($"Client '{eventArgs.ClientId}' wants to connect with valid credentials. Accepting!");
            }
            else 
            {
                eventArgs.ReasonCode = Protocol.MqttConnectReasonCode.BadUserNameOrPassword;
                Console.WriteLine($"Client '{eventArgs.ClientId}' wants to connect with invalid credentials. Denying!");
            }            

            return Task.CompletedTask;
        }

        public Task OnClientDisconnected(ClientDisconnectedEventArgs eventArgs) 
        {
            LogClientDisconnected(eventArgs);

            return Task.CompletedTask;
        }
                
    }

    sealed class Startup
    {
        public void Configure(IApplicationBuilder app, IWebHostEnvironment environment, MqttControllerVANL mqttController)
        {
            app.UseRouting();

            app.UseEndpoints(
                endpoints =>
                {
                    endpoints.MapConnectionHandler<MqttConnectionHandler>(
                        "/mqtt",
                        httpConnectionDispatcherOptions => httpConnectionDispatcherOptions.WebSockets.SubProtocolSelector =
                            protocolList => protocolList.FirstOrDefault() ?? string.Empty);
                });

            app.UseMqttServer(
                server =>
                {
                    /*
                     * Attach event handlers etc. if required.
                     */

                    server.ClientDisconnectedAsync += mqttController.OnClientDisconnected;
                    server.ValidatingConnectionAsync += mqttController.ValidateConnection;
                    server.ClientConnectedAsync += mqttController.OnClientConnected;
                });
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHostedMqttServer(
                optionsBuilder =>
                {
                    optionsBuilder.WithDefaultEndpoint();
                });

            services.AddMqttConnectionHandler();
            services.AddConnections();

            services.AddSingleton<MqttControllerVANL>();
        }
    }
}