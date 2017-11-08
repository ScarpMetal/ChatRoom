using System;
using System.Globalization;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using CSCI251Project3.Models;


public class SocketHandler
{
    public static RoomHandler roomHandler;
    private static Random random;

    public const int BufferSize = 4096;

    WebSocket socket;
    int roomNumber;
    int user_id;

    SocketHandler(WebSocket socket, int roomNumber)
    {
        this.socket = socket;
        this.roomNumber = roomNumber;
    }

    async Task PopulateNotifications()
    {
        var data = DBHandler.GetNotificationData(user_id);

        foreach(DataToSendNotification dataToSend in data)
        {
            var outBuf = System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(dataToSend));
            var outgoing = new ArraySegment<byte>(outBuf, 0, outBuf.Length);
            await this.socket.SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    async Task PopulateRoomWithMessages()
    {
        var dataToSend = DBHandler.GetPopulateData(user_id, roomNumber);

        var outBuf = System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(dataToSend));
        var outgoing = new ArraySegment<byte>(outBuf, 0, outBuf.Length);
        await this.socket.SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    async Task PopulateSidebarWithRooms()
    {
        var dataToSend = DBHandler.GetRoomsData();

        var outBuf = System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(dataToSend));
        var outgoing = new ArraySegment<byte>(outBuf, 0, outBuf.Length);
        await this.socket.SendAsync(outgoing, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    async Task ChangeRoom(int room_id)
    {
        roomNumber = room_id;
        roomHandler.ChangeRoom(socket, user_id, room_id);

        var dataToSendChangeRoom = new DataToSendChangeRoom();
        dataToSendChangeRoom.type = "changeroom";
        dataToSendChangeRoom.roomName = DBHandler.GetRoomName(room_id);
        dataToSendChangeRoom.roomColor = DBHandler.GetRoomColor(room_id);
        await roomHandler.SendDataToSocket(dataToSendChangeRoom, socket);
        await PopulateRoomWithMessages();
        var dataToSendBot = new DataToSendBot();
        dataToSendBot.type = "bot";
        dataToSendBot.message = DBHandler.GetUsername(user_id) + " connected!";
        await roomHandler.SendDataToRoom(dataToSendBot, roomNumber);
    }

    async Task SocketSender()
    {
        //Console.WriteLine("Begin MessageSender");

        var buffer = new byte[BufferSize];
        var seg = new ArraySegment<byte>(buffer);
        
        for (;;)
        {
            //Console.WriteLine("In Loop");
            if (this.socket.State != WebSocketState.Open) break;
            var inBuf = await this.socket.ReceiveAsync(seg, CancellationToken.None);
            if (this.socket.State != WebSocketState.Open) break;
            var jsonStr = System.Text.Encoding.ASCII.GetString(seg.Array.Take(inBuf.Count).ToArray());

            if (jsonStr.IndexOf("message") == 9)
            {
                //Console.WriteLine("roomHandler.rooms[0].sockets.Count={0}", roomHandler.rooms[0].sockets.Count);
                //Console.WriteLine("Server received a Message");
                var json = JsonConvert.DeserializeObject<DataToReceiveMessage>(jsonStr);
                var dataToSend = new DataToSendMessage();
                dataToSend.type = "message";
                dataToSend.username = DBHandler.GetUsername(user_id);
                dataToSend.message = json.message;
                dataToSend.timestamp = DateTime.Now.ToString("h:mm tt");
                dataToSend.color = DBHandler.GetUserColor(user_id);

                DBHandler.InsertMessage(dataToSend, roomNumber);
                await roomHandler.SendMessageToRoom(dataToSend, roomNumber);
            }
            else if (jsonStr.IndexOf("connected") == 9)
            {
                var json = JsonConvert.DeserializeObject<DataToReceiveConnected>(jsonStr);
                var userResult = DBHandler.VerifyAuthKey(json.authKey);
                if (userResult >= 0)
                {
                    user_id = userResult;
                    await PopulateRoomWithMessages();
                    await PopulateSidebarWithRooms();

                    roomHandler.AddSocket(socket, user_id, roomNumber);
                    var dataToSend = new DataToSendBot();
                    dataToSend.type = "bot";
                    dataToSend.message = DBHandler.GetUsername(user_id) + " connected!";
                    await roomHandler.SendDataToRoom(dataToSend, roomNumber);
                }
                else
                {
                    var dataToSend = new DataToSendAuthKey();
                    dataToSend.type = "authkey";
                    dataToSend.authKey = "";
                    await roomHandler.SendDataToSocket(dataToSend, socket);
                }
            }
            else if (jsonStr.IndexOf("changeroom") == 9)
            {
                var json = JsonConvert.DeserializeObject<DataToReceiveChangeRoom>(jsonStr);
                await ChangeRoom(json.roomNumber);
            }
            else if (jsonStr.IndexOf("createroom") == 9)
            {
                var json = JsonConvert.DeserializeObject<DataToReceiveCreateRoom>(jsonStr);
                if (DBHandler.RoomNameAvailable(json.roomName))
                {
                    roomHandler.AddRoom(json.roomName);
                    await ChangeRoom(DBHandler.GetRoomId(json.roomName));
                    var dataToSend = DBHandler.GetRoomsData();
                    await roomHandler.SendDataToAll(dataToSend);
                }
            }
            else if (jsonStr.IndexOf("login") == 9)
            {
                var json = JsonConvert.DeserializeObject<DataToReceiveLoginOrCreate>(jsonStr);
                var dataToSend = new DataToSendAuthKey();
                dataToSend.type = "authkey";
                if (DBHandler.ValidLogin(json.username, json.password))
                {
                    var authKey = SocketHandler.GenerateAuthKey(16);
                    user_id = DBHandler.GetUserId(json.username);
                    DBHandler.InsertAuthKey(authKey, user_id);
                    dataToSend.authKey = authKey;
                }
                else
                {
                    dataToSend.authKey = "invalid login";
                }

                await roomHandler.SendDataToSocket(dataToSend, socket);
            }
            else if (jsonStr.IndexOf("createuser") == 9)
            {
                var json = JsonConvert.DeserializeObject<DataToReceiveLoginOrCreate>(jsonStr);
                var dataToSend = new DataToSendAuthKey();
                dataToSend.type = "authkey";
                if (DBHandler.UsernameAvailable(json.username))
                {
                    var authKey = SocketHandler.GenerateAuthKey(16);
                    DBHandler.InsertUser(json.username, json.password);
                    user_id = DBHandler.GetUserId(json.username);
                    DBHandler.InsertAuthKey(authKey, user_id);
                    dataToSend.authKey = authKey;
                }
                else
                {
                    dataToSend.authKey = "taken username";
                }

                await roomHandler.SendDataToSocket(dataToSend, socket);
            }
            else
            {
                Console.WriteLine("Invalid Data Type");
            }
        }

        roomHandler.RemoveSocket(socket);
    }

    static async Task Acceptor(HttpContext hc, Func<Task> n)
    {
        //Console.WriteLine("Begin Acceptor");
        int roomNum;
        if (!Int32.TryParse(hc.Request.Path.Value, out roomNum))
        {
            roomNum = 1;
        }

        if (!hc.WebSockets.IsWebSocketRequest)
        {
            await hc.Response.WriteAsync(File.ReadAllText("Views/Room/Index.html"));
            return;
        }

        var socket = await hc.WebSockets.AcceptWebSocketAsync();
        var handler = new SocketHandler(socket, roomNum);
        await handler.SocketSender();
    }

    public static void Map(IApplicationBuilder app)
    {
        roomHandler = new RoomHandler();
        random = new Random();
        
        app.UseWebSockets();
        app.Use(SocketHandler.Acceptor);
    }

    public static string GenerateAuthKey(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
          .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
