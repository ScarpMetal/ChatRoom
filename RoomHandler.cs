using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using CSCI251Project3.Models;

public class Socket
{
    public WebSocket socket { get; set; }
    public int user_id { get; set; }

    public Socket(WebSocket s, int user_id)
    {
        this.socket = s;
        this.user_id = user_id;
    }
}

public class Room
{
    public List<Socket> sockets;
    public int roomNumber { get; set; }
    
    public Room(int roomNumber)
    {
        this.sockets = new List<Socket>();
        this.roomNumber = roomNumber;
    }

    public void Add(WebSocket s, int user_id)
    {
        this.sockets.Add(new Socket(s, user_id));
    }

    public void Remove(WebSocket s)
    {
        this.sockets.Remove(GetSocketObject(s));
    }

    public Socket GetSocketObject(WebSocket s)
    {
        foreach(Socket socketObj in sockets)
        {
            if(socketObj.socket.GetHashCode() == s.GetHashCode())
            {
                return socketObj;
            }
        }
        return null;
    }

    public static string RandomColor()
    {
        var random = new Random();
        var color = String.Format("#{0:X6}", random.Next(0x1000000));
        return color;
    }
}

public class RoomHandler
{
    public List<Room> rooms;

    public RoomHandler()
    {
        this.rooms = DBHandler.GetRoomsList();
    }

    public bool AddSocket(WebSocket s, int user_id, int roomNumber)
    {
        if (RoomExists(roomNumber))
        {
            Room room = FindRoom(roomNumber);
            room.Add(s, user_id);
            return true;
        }
        Console.WriteLine("Room {0} does not exist. Could not AddSocket.", roomNumber);
        return false;
    }

    public bool RemoveSocket(WebSocket s)
    {
        //Console.WriteLine("removing socket");
        foreach(Room room in rooms)
        {
            foreach(Socket socketObj in room.sockets)
            {
                if (socketObj.socket.GetHashCode() == s.GetHashCode())
                {
                    room.Remove(s);
                    return true;
                }
            }
        }
        Console.WriteLine("Socket {0} does not belong to a room. Could not RemoveSocket.", s.GetHashCode());
        return false;
    }

    public bool AddRoom(string roomName)
    {
        if(DBHandler.RoomNameAvailable(roomName))
        {
            DBHandler.InsertRoom(roomName);
            var roomNumber = DBHandler.GetRoomId(roomName);
            this.rooms.Add(new Room(roomNumber));
            return true;
        }
        Console.WriteLine("Room with name \"{0}\" already exists. Could not AddRoom.", roomName);
        return false;
    }

    public bool ChangeRoom(WebSocket s, int user_id ,int roomNumber)
    {
        if(RemoveSocket(s) && AddSocket(s, user_id, roomNumber)){
            return true;
        }
        Console.WriteLine("Could not ChangeRoom.");
        return false;
    }

    public async Task SendDataToAll(Data data)
    {
        foreach (Room room in rooms) {
            foreach (Socket socketObj in room.sockets)
            {
                var outBuf = System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(data));
                var outgoing = new ArraySegment<byte>(outBuf, 0, outBuf.Length);
                await socketObj.socket.SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
        //Console.WriteLine("Sent Message To All");
    }

    public async Task SendDataToRoom(Data data, int roomNumber)
    {
        if (RoomExists(roomNumber))
        {
            //Console.WriteLine("Room Exists {0}",roomNumber);
            Room room = FindRoom(roomNumber);
            var outBuf = System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(data));
            var outgoing = new ArraySegment<byte>(outBuf, 0, outBuf.Length);
            foreach (Socket socketObj in room.sockets)
            {
                await socketObj.socket.SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
        else
        {
            Console.WriteLine("Room {0} does not exist. Could not SendDataToRoom.", roomNumber);
        }
        //Console.WriteLine("Sent Message To Room {0}", roomNumber);
    }

    public async Task SendMessageToRoom(DataToSendMessage data, int roomNumber)
    {
        if (RoomExists(roomNumber))
        {
            Room room = FindRoom(roomNumber);
            
            foreach (Socket socketObj in room.sockets)
            {
                data.highlight = false;
                Regex regex = new Regex("@([a-zA-Z0-9]*)");
                var match = regex.Match(data.message);
                var matchStr = match.Groups[1].Value;
                if (matchStr != "")
                {
                    var thisUsersId = DBHandler.GetUserId(matchStr);
                    if (DBHandler.UserExists(matchStr) && socketObj.user_id == thisUsersId)
                    {
                        //var notifiedUserId = DBHandler.GetUserId(matchStr);
                        //var sender_id = DBHandler.GetUserId(data.username);
                        //var notificationId = DBHandler.InsertNotification(notifiedUserId, sender_id, roomNumber, data.timestamp);
                        data.highlight = true;
                        //await SendNotification(notifiedUserId, data.username, roomNumber, data.timestamp, notificationId);
                    }
                }

                var outBuf = System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(data));
                var outgoing = new ArraySegment<byte>(outBuf, 0, outBuf.Length);
                await socketObj.socket.SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
        else
        {
            Console.WriteLine("Room {0} does not exist. Could not SendDataToRoom.", roomNumber);
        }
        await SendNotificationToAll(data, roomNumber);
    }

     public async Task SendNotificationToAll(DataToSendMessage data, int roomNumber)
    {
        foreach (Room room in rooms)
        {
            foreach (Socket socketObj in room.sockets)
            {
                Regex regex = new Regex("@([a-zA-Z0-9]*)");
                var match = regex.Match(data.message);
                var matchStr = match.Groups[1].Value;
                if (matchStr != "")
                {
                    var thisUsersId = DBHandler.GetUserId(matchStr);
                    if (DBHandler.UserExists(matchStr) && socketObj.user_id == thisUsersId)
                    {
                        var notifiedUserId = DBHandler.GetUserId(matchStr);
                        var sender_id = DBHandler.GetUserId(data.username);
                        var notificationId = DBHandler.InsertNotification(notifiedUserId, sender_id, roomNumber, data.timestamp);
                        await SendNotification(notifiedUserId, data.username, roomNumber, data.timestamp, notificationId);
                    }
                }
            }
        }
    }

    public async Task SendDataToSocket(Data data, WebSocket s)
    {
        var outBuf = System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(data));
        var outgoing = new ArraySegment<byte>(outBuf, 0, outBuf.Length);
        await s.SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async Task SendDataToUser(Data data, int user_id)
    {
        Socket socketObj = GetSocketObject(user_id);
        Console.WriteLine("socketObj.user_id={0}", socketObj.user_id);
        var outBuf = System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(data));
        var outgoing = new ArraySegment<byte>(outBuf, 0, outBuf.Length);
        await socketObj.socket.SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    async Task SendNotification(int notifiedUser_id, string senderName, int room_id, string timestamp, int notification_id)
    {
        var dataToSend = new DataToSendNotification();
        dataToSend.type = "notification";
        dataToSend.senderName = senderName;
        dataToSend.roomName = DBHandler.GetRoomName(room_id);
        dataToSend.timestamp = timestamp;
        dataToSend.notificationId = notification_id;

        await SendDataToUser(dataToSend, notifiedUser_id);
    }

    public Socket GetSocketObject(int user_id)
    {
        foreach(Room room in rooms)
        {
            foreach (Socket socketObj in room.sockets)
            {
                //Console.WriteLine("socketObj.user_id={0} user_id={1}", socketObj.user_id, user_id);
                if(socketObj.user_id == user_id)
                {
                    return socketObj;
                }
            }
        }
        return null;
    }

    private Room FindRoom(int roomNumber)
    {
        foreach (Room room in rooms)
        {
            if(room.roomNumber == roomNumber)
            {
                return room;
            }
        }
        return null;
    }

    private bool RoomExists(int roomNumber)
    {
        foreach(Room room in rooms)
        {
            if(room.roomNumber == roomNumber)
            {
                return true;
            }
        }
        return false;
    }
}
