using System.Text;
using System.Text.Json;
using AV_Device_Monitor;
using Newtonsoft.Json.Linq;

public class Program
{
    private static readonly HttpClient _client = new();

    private static System.Timers.Timer? _powerTimer;
    private static System.Timers.Timer? _statusTimer;


    public class ConnectionInfo  
    {
        public string? IP { get; set; } = string.Empty;
        public string? PSK { get; set;} = string.Empty;

        public string? AskUserForIP()
        {
            Console.Write("Enter Device IP Address using the format xxx.xxx.xxx.xxx  ");
            IP = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(IP))
            {
                Console.WriteLine("Please enter a valid IP address.");
                return null;
            }
            return IP;
        }
        public string? AskUserForPSK()
        {
            Console.Write("Enter 4 Digit Pre-Shared Key:  ");
            PSK = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(PSK))
            {
                Console.WriteLine("Please enter a valid 4 digit Pre-Shared Key.");
                return null;
            }
            return PSK;
        }
    }

    public static void Main()
    {
        try
        {
            ConnectionInfo info = new ConnectionInfo();
            Console.WriteLine("AV-Device-Monitor start \n");
            info.AskUserForIP();
            info.AskUserForPSK();

            string baseURL = $"http://{info.IP}/sony/system";
            Console.WriteLine($"Connection info:\n\t {info.IP}\n\t {info.PSK}\n\t {baseURL}");

            SendRpcRequest(Api.InfoMethod, Api.InfoId, baseURL, info.PSK); 
            StartPowerPoll();
            StartTimeCheck();
            Console.ReadLine();     
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in Main: " + ex.Message);
        }
    }

    
    // time  
    public static void StartTimeCheck()
    {
        _statusTimer = new System.Timers.Timer(300000);
        _statusTimer.Elapsed += (sender, e) => SendRpcRequest(Api.TimeMethod, Api.TimeId);
        _statusTimer.AutoReset = true;
       _statusTimer.Start();
    }

    // power status 
    
    public static void StartPowerPoll()
    {
        _powerTimer = new System.Timers.Timer(3000);
        _powerTimer.Elapsed += (sender, e) => SendRpcRequest(Api.PowerMethod, Api.TimeId);
        _powerTimer.AutoReset = true;
        _powerTimer.Start();
    }

    public class RpcRequest
    {
        public string? method { get; set; }
        public int? id { get; set; }
        public object[]? @params { get; set; }
        public string? version { get; set; }
    }

    public static void SendRpcRequest(string method, int id, string url = "", string psk = "")         
    {
        try
        {
            var requestBody = new RpcRequest
            {
                method = method,
                id = id,          
                @params = [],     
                version = "1.0"
            };
            var jsonSerialized = JsonSerializer.Serialize(requestBody);
            var jsonContent = new StringContent(
                jsonSerialized,
                Encoding.UTF8,
                "application/json"
            );
            Console.WriteLine($"Sending info to TV:\n\t URL: {url}\n\t PSK: {psk}\n" );
            _client.DefaultRequestHeaders.Add("X-Auth-PSK", psk);                 // this authentication is required to use Sony's REST API control  
            var response = _client.PostAsync(url, jsonContent).Result;
            Console.WriteLine($"Response from Sony REST API: {response}");
            response.EnsureSuccessStatusCode();
            
            var jsonResponse = response.Content.ReadAsStringAsync().Result;
            JObject jsonResult = JObject.Parse(jsonResponse);

            switch (id)
            {
                case 33:
                    var resultToken = jsonResult.GetValue("result")?.First;     //the TV returns a lot more than what we need, so we need to select what to display

                    var product = $"Device Type: {resultToken?.SelectToken("product")}";
                    var model = $"Model: {resultToken?.SelectToken("model")}";
                    var serialNumber = $"Serial Number: {resultToken?.SelectToken("serial")}";
                    var MACAddress = $"MAC Address: {resultToken?.SelectToken("macAddr")}";
                    var name = $"Name: {resultToken?.SelectToken("name")}";

                    var avDeviceInfo = $"AV Device Information: \n {product} \n {model} \n {serialNumber} \n {MACAddress} \n {name} \n";
                    Console.WriteLine(avDeviceInfo);

                    break;

                case 50:
                    if (jsonResponse.Contains("active")) { Console.WriteLine("AV Device is active"); };
                    if (jsonResponse.Contains("standby")) { Console.WriteLine("AV Device is in standby mode"); };
                    break;

                case 51:
                    var timeToken = jsonResult.GetValue("result")?.First;
                    var deviceTime = ($"{timeToken}").Substring(11, 4);        // we just want HH:MM

                    DateTime dateTime = DateTime.Now;
                    var localMachineDateTime = dateTime.ToString();
                    var localTime = localMachineDateTime.Substring(11, 4);

                    if (deviceTime != localTime | deviceTime == null)         //
                    {
                        Console.WriteLine("AV Device time is out of sync. Check device network connection and/or settings.");
                        Console.WriteLine($"Device {deviceTime} != Local {localTime}");
                    }
                    if (deviceTime == localTime)
                    {
                        Console.WriteLine($"Time Check Successful: Device {deviceTime} = Local {localTime}");
                    }
                    break;
            }
        }  
        catch (Exception ex)
        {
            Console.WriteLine($"Communication with AV Device error.\n\t {ex.Message}");
        }
    }
}






