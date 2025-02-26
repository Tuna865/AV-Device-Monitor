using System.Text;
using System.Text.Json;
using Newtonsoft.Json.Linq;



public class Program
{
    private static readonly HttpClient client = new();
    const string powerMethod = "getPowerStatus";
    const string infoMethod = "getSystemInformation";
    const string timeMethod = "getCurrentTime"; 
    const int powerId = 50;
    const int infoId = 33;
    const int timeId = 51;

    public class JsonRequest
    {
        public string? method { get; set; }
        public int? id { get; set; }         
        public object[]? @params { get; set; }
        public string? version { get; set; }
    }

    public class ConnectionInfo  
    {
        public static string AskUserForIP()
        {
            Console.Write("Enter Device IP Address using the format xxx.xxx.xxx.xxx  ");
            string ip = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(ip))
            {
                Console.WriteLine("Please enter a valid IP address.");
                return null;
            }
            else return ip;
        }public static string AskUserForPSK()
        {
            Console.Write("Enter 4 Digit Pre-Shared Key:  ");
            string psk = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(psk))
            {
                Console.WriteLine("Please enter a valid 4 digit Pre-Shared Key.");
                return null;
            }
            else return psk;
        }


    }

    public static void Main()
    {
        try
        {
            Console.WriteLine("AV-Device-Monitor start \n");
            string ip = ConnectionInfo.AskUserForIP();
            string psk = ConnectionInfo.AskUserForPSK();
            string baseURL = $"http://{ip}/sony/system";
            Console.WriteLine($"Connection info:\n\t {ip}\n\t {psk}\n\t {baseURL}");

            Send(infoMethod, infoId, baseURL, psk);   //collect and show device info upon startup 
            StartPowerPoll();
            StartTimeCheck();
            Console.ReadLine();     
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in Main: " + ex.Message);
        }
    }

    //checks time on the AV Device every 5 minutes 
    private static System.Timers.Timer? statusTimer;
    public static void StartTimeCheck()
    {
        statusTimer = new System.Timers.Timer(300000);
        statusTimer.Elapsed += (sender, e) => Send(timeMethod, timeId);
        statusTimer.AutoReset = true;
        statusTimer.Start();
    }

    //polls power status 
    private static System.Timers.Timer? powerTimer;
    public static void StartPowerPoll()
    {
        powerTimer = new System.Timers.Timer(3000);
        powerTimer.Elapsed += (sender, e) => Send(powerMethod, powerId);
        powerTimer.AutoReset = true;
        powerTimer.Start();
    }

    //sends and interprets JSON from Sony's REST API
    public static void Send(string method, int id, string url = "", string psk = "")         
    {
        try
        {
            var requestBody = new JsonRequest
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
            client.DefaultRequestHeaders.Add("X-Auth-PSK", psk);                 //this authentication is required to use Sony's REST API control  
            var response = client.PostAsync(url, jsonContent).Result;
            Console.WriteLine($"Response from Sony REST API: {response}");
            response.EnsureSuccessStatusCode();
            
            var jsonResponse = response.Content.ReadAsStringAsync().Result;
            JObject jsonResult = JObject.Parse(jsonResponse);

            switch (id)
            {
                case 33:
                    var resultToken = jsonResult.GetValue("result").First;     //the TV returns a lot more than what we need, so we need to select what to display

                    var product = $"Device Type: {resultToken.SelectToken("product")}";
                    var model = $"Model: {resultToken.SelectToken("model")}";
                    var serialNumber = $"Serial Number: {resultToken.SelectToken("serial")}";
                    var MACAddress = $"MAC Address: {resultToken.SelectToken("macAddr")}";
                    var name = $"Name: {resultToken.SelectToken("name")}";

                    var avDeviceInfo = $"AV Device Information: \n {product} \n {model} \n {serialNumber} \n {MACAddress} \n {name} \n";
                    Console.WriteLine(avDeviceInfo);

                    break;

                case 50:
                    if (jsonResponse.Contains("active")) { Console.WriteLine("AV Device is active"); };
                    if (jsonResponse.Contains("standby")) { Console.WriteLine("AV Device is in standby mode"); };
                    break;

                case 51:
                    var timeToken = jsonResult.GetValue("result").First;
                    var deviceTime = ($"{timeToken}").Substring(11, 4);        //returns the full date & time but we just want HH:MM

                    DateTime dateTime = DateTime.Now;
                    var localMachineDateTime = dateTime.ToString();
                    var localTime = localMachineDateTime.Substring(11, 4);

                    if (deviceTime != localTime | deviceTime == null)         //compare the device being monitored to the machine running the program; if different time zone, minutes should still be the same 
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






