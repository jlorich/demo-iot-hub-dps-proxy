using System;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;

namespace MicrosoftDemos.IoT.DeviceProvisioningProxy
{
    public class Program
    {

        // Strongly typed class to load configuration into
        private class DeviceProvisioningProxySettings {
            public string ProxyUri { get; set; }
            public string GlobalDeviceEndpoint { get; set; }
            public string IdScope { get; set; }
            public string RegistrationId { get; set; }
            public string PrimaryKey { get; set; }
            public string SecondaryKey { get; set; }
        }

        // Main program method
        private static void Main(string[] args)
        {
            // Set up configuration to load local.settings.json file
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("local.settings.json");

            var config = configurationBuilder.Build();
            var demoSettings = config.Get<DeviceProvisioningProxySettings>();

            ConnectAndSend(demoSettings).GetAwaiter().GetResult();
        }

        // Connect to IoT Hub/Central and send a test message
        private static async Task ConnectAndSend(DeviceProvisioningProxySettings settings) {
            // Configure DPS Transport Proxy
            var transport = new ProvisioningTransportHandlerHttp();
            transport.Proxy = new WebProxy(settings.ProxyUri);

            // Create DPS Client
            var security = new SecurityProviderSymmetricKey(settings.RegistrationId, settings.PrimaryKey, settings.SecondaryKey);
            var provisioningClient = ProvisioningDeviceClient.Create(settings.GlobalDeviceEndpoint, settings.IdScope, security, transport);

            // Register Device
            var registrationResult = await provisioningClient.RegisterAsync().ConfigureAwait(false);

            // Configure Device Authentication from result
            var auth = new DeviceAuthenticationWithRegistrySymmetricKey(registrationResult.DeviceId, security.GetPrimaryKey());

            // Configure IoT Hub/Central Proxy
            Http1TransportSettings transportSettings = new Http1TransportSettings();
            transportSettings.Proxy = new WebProxy(settings.ProxyUri);
            ITransportSettings[] transportSettingsArray = new ITransportSettings[] { transportSettings };

            // Connect to IoT Hub/Central and send a message
            var messageText = "{ \"test\": \"hello\" }";
            var message = new Message(Encoding.UTF8.GetBytes(messageText));

            using (var deviceClient = DeviceClient.Create(registrationResult.AssignedHub, auth, transportSettingsArray)) {
                try {
                    await deviceClient.SendEventAsync(message);
                    Console.WriteLine("Message sent");
                } finally {
                    Console.WriteLine("Done");
                }
                
            }
        }
    }
}