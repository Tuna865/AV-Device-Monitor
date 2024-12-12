## AV-Device Monitor   

This console program communicates with a Sony display using their REST API documentation found [here](https://pro-bravia.sony.net/develop/integrate/rest-api/spec/index.html).


#### Features:

- Connects to an AV device via ethernet    
- Displays relevant information collected from the device, such as:   
    - Name
    - Product type
    - Model
    - Serial Number 
    - MAC Address 
- Continuously monitors the device's power status 
    -reports either "active" or "standby" to the console 
- Performs a check every 5 minutes comparing the device's time to the local machine's 
    - reports an error if the two times get out of sync, indicating there may be an issue with the AV Device's ethernet/network connection


