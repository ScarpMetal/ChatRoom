using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public abstract class Data
{
    string type { get; set; }
}

// Data To Send

public class DataToSendMessage : Data
{
    public string type { get; set; }
    public string username { get; set; }
    public string message { get; set; }
    public string timestamp { get; set; }
    public string color { get; set; }
    public bool highlight { get; set; }
}

public class DataToSendPopulate : Data
{
    public string type { get; set; }
    public DataToSendMessage[] data { get; set; }
}

public class DataToSendRooms : Data
{
    public string type { get; set; }
    public DataToSendRoom[] data { get; set; }
}

public class DataToSendRoom
{
    public string name { get; set; }
    public string color { get; set; }
    public int roomNumber { get; set; }
}

public class DataToSendChangeRoom : Data
{
    public string type { get; set; }
    public string roomName { get; set; }
    public string roomColor { get; set; }
}

public class DataToSendBot : Data
{
    public string type { get; set; }
    public string message { get; set; }
}

public class DataToSendAuthKey : Data
{
    public string type { get; set; }
    public string authKey { get; set; }
}

public class DataToSendNotification : Data
{
    public string type { get; set; }
    public string senderName { get; set; }
    public string roomName { get; set; }
    public string timestamp { get; set; }
    public int notificationId { get; set; }
}

// Data To Receive
public class DataToReceiveConnected : Data
{
    public string type { get; set; }
    public string authKey { get; set; }
}

public class DataToReceiveMessage : Data
{
    public string type { get; set; }
    public string authKey { get; set; }
    public string message { get; set; }
}

public class DataToReceiveChangeRoom : Data
{
    public string type { get; set; }
    public string authKey { get; set; }
    public int roomNumber { get; set; }
}

public class DataToReceiveCreateRoom : Data
{
    public string type { get; set; }
    public string authKey { get; set; }
    public string roomName { get; set; }
}

public class DataToReceiveLoginOrCreate: Data
{
    public string type { get; set; }
    public string username { get; set; }
    public string password { get; set; }
}
