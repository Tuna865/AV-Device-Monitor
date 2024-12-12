using System;
using System.Net.Http;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices.JavaScript;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Timers;
using Newtonsoft.Json.Linq;



public class Program
{
    private static readonly HttpClient client = new HttpClient();
    const string psk = "8657";          //would like to get PSK and IP from user input 
    const string ip = "10.0.0.198";     //the Sony display used for testing had its IP set to static
    const string powerMethod = "getPowerStatus";
    const string infoMethod = "getSystemInformation";
    const string timeMethod = "getCurrentTime"; 
    const int powerId = 50;
    const int infoId = 33;
    const int timeId = 51;
    const string url = $"http://{ip}/sony/system";

    public class JsonRequest
    {
        public string? method { get; set; }
        public int? id { get; set; }         
        public object[]? @params { get; set; }
        public string? version { get; set; }
    }

    public static void Main()
    {
        
        try
        {
            Send(infoMethod, infoId);   //collect device info upon startup 
            StartPowerPoll();
            StartTimeCheck();
            Console.ReadLine();     //make the program wait instead of exiting
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error in Main: " + ex.Message);
        }
    }
    
    //checks on the AV Device every 5 minutes 
    private static System.Timers.Timer? statusTimer;
    public static void StartTimeCheck()
    {
        statusTimer = new System.Timers.Timer(10000);
        statusTimer.Elapsed += (sender, e) => Send(timeMethod, timeId);
        statusTimer.AutoReset = true;
        statusTimer.Start();
    }

    //creates a poll to ask the AV Device what its power state is every 3 seconds 
    private static System.Timers.Timer? powerTimer;
    public static void StartPowerPoll()
    {
        powerTimer = new System.Timers.Timer(3000);
        powerTimer.Elapsed += (sender, e) => Send(powerMethod, powerId);
        powerTimer.AutoReset = true;
        powerTimer.Start();
    }

    //sends and receives JSON from Sony's REST API
    public static void Send(string method, int id)
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

            client.DefaultRequestHeaders.Add("X-Auth-PSK", psk);                 //this authentication is required to use Sony's REST API control  
            var response = client.PostAsync(url, jsonContent).Result;
            response.EnsureSuccessStatusCode();

            var jsonResponse = response.Content.ReadAsStringAsync().Result;
            if (id == 33)    //device info
            {
                JObject jsonResult = JObject.Parse(jsonResponse);
                var resultToken = jsonResult.GetValue("result").First;     //the TV returns a lot more than what we need, so we need to select what to display

                var product = $"Device Type: { resultToken.SelectToken("product") }";
                var model = $"Model: { resultToken.SelectToken("model") }";
                var serialNumber = $"Serial Number: { resultToken.SelectToken("serial") }";
                var MACAddress = $"MAC Address: { resultToken.SelectToken("macAddr") }";
                var name = $"Name: { resultToken.SelectToken("name") }";
                
                var avDeviceInfo = $"AV Device Information: \n {product} \n {model} \n {serialNumber} \n {MACAddress} \n {name} \n";
                Console.WriteLine(avDeviceInfo);

            };

            if (id == 50)    //power status
            {
                if (jsonResponse.Contains("active")) { Console.WriteLine("AV Device is active"); }; 
                if (jsonResponse.Contains("standby")) { Console.WriteLine("AV Device is in standby mode"); };
            };

            if (id == 51)      //current time
            {
                JObject jsonResult = JObject.Parse(jsonResponse); 
                var timeToken = jsonResult.GetValue("result").First;       
                Console.WriteLine($"\nAV Device time: { timeToken }"); 
                DateTime dateTime = DateTime.Now;
                var localMachineDateTime = dateTime.ToString();
                var localTime = localMachineDateTime.Substring(11, 5);
                Console.WriteLine($"Local Machine time: { localTime } \n");


                //if (timeToken != localMachineTime | timeToken == null)  //need to convert this to a string to compare it to the local machine's time
                //{
                //    Console.WriteLine("No response from display. Please check connection status.");
                //}
                //else
                //    Console.WriteLine($"Time Check Successful: {timeToken}");        

            }
        }  

        catch (Exception ex)
        {
            Console.WriteLine($"Error: communication with AV Device incomplete.\n {ex.Message}");
        }
    }
}






