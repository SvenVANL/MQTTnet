using System.Net;
using System.Text;
using MQTTnet.Diagnostics;
using MQTTnet.Internal;
using MQTTnet.Server;

namespace MQTTnet;
public static class VANLBroker {
    
    readonly static string discordWebhook = "https://discord.com/api/webhooks/1192795355998330892/1R3Lic4souDscjofwWA7MrYFVcIYvKh9cutcatb8dagYfwIGKrgI84JFyEaZj90WnsDR";

    static async void SendDiscordMs(string webhook, string message)
    {
        using (HttpClient client = new HttpClient())
        {
            // Create JSON payload
            string payload = $"{{\"content\": \"{message}\"}}";

            // Send POST request
            HttpResponseMessage response = await client.PostAsync(webhook, new StringContent(payload, Encoding.UTF8, "application/json"));

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
    public static async Task Run_Server()
    {
        /*
         * This sample starts a simple MQTT server and prints the logs to the output.
         *
         * IMPORTANT! Do not enable logging in live environment. It will decrease performance.
         *
         * See sample "Run_Minimal_Server" for more details.
         */

        var mqttFactory = new MqttFactory(new ConsoleLogger());

        var mqttServerOptions = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()            
            .WithDefaultEndpointBoundIPAddress(new IPAddress(new byte[] {10, 10, 4, 163}))
            .WithDefaultEndpointPort(5001).Build();

        using (var mqttServer = mqttFactory.CreateMqttServer(mqttServerOptions))
        {
            mqttServer.ClientDisconnectedAsync += e => 
            {
                var message = $"Client '{e.ClientId}' disconnected with type '{e.DisconnectType}' at '{DateTime.Now}'";
                Console.WriteLine(message);
                SendDiscordMs(discordWebhook, message);
              
                return CompletedTask.Instance;
            };

            await mqttServer.StartAsync();

            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();

            // Stop and dispose the MQTT server if it is no longer needed!
            await mqttServer.StopAsync();
        }
    }

    class ConsoleLogger : IMqttNetLogger
    {
        readonly object _consoleSyncRoot = new();

        public bool IsEnabled => true;

        public void Publish(MqttNetLogLevel logLevel, string source, string message, object[]? parameters, Exception? exception)
        {
            var foregroundColor = ConsoleColor.White;
            switch (logLevel)
            {
                case MqttNetLogLevel.Verbose:
                    foregroundColor = ConsoleColor.White;
                    break;

                case MqttNetLogLevel.Info:
                    foregroundColor = ConsoleColor.Green;
                    break;

                case MqttNetLogLevel.Warning:
                    foregroundColor = ConsoleColor.DarkYellow;
                    break;

                case MqttNetLogLevel.Error:
                    foregroundColor = ConsoleColor.Red;
                    break;
            }

            if (parameters?.Length > 0)
            {
                message = string.Format(message, parameters);
            }

            lock (_consoleSyncRoot)
            {
                Console.ForegroundColor = foregroundColor;
                Console.WriteLine(message);

                if (exception != null)
                {
                    Console.WriteLine(exception);
                }
            }
        }
    }

}

