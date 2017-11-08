using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using System.Text;
using System.Security.Cryptography;


namespace CSCI251Project3.Models
{
    public static class DBHandler
    {
        public static SqliteConnection conn { get; set; }

        public static void ExecuteNonQuery(string commandStr)
        {
            var command = new SqliteCommand(commandStr, conn);
            command.ExecuteNonQuery();
        }

        public static async Task ExecuteNonQueryAsync(string commandStr)
        {
            var command = new SqliteCommand(commandStr, conn);
            await command.ExecuteNonQueryAsync();
        }

        public static SqliteDataReader ExecuteReader(string commandStr)
        {
            var command = new SqliteCommand(commandStr, conn);
            return command.ExecuteReader();
        }

        public static async Task<SqliteDataReader> ExecuteReaderAsync(string commandStr)
        {
            var command = new SqliteCommand(commandStr, conn);
            return await command.ExecuteReaderAsync();
        }

        public static Object ExecuteScalar(string commandStr)
        {
            var command = new SqliteCommand(commandStr, conn);
            return command.ExecuteScalar();
        }

        public static async Task<object> ExecuteScalarAsync(string commandStr)
        {
            var command = new SqliteCommand(commandStr, conn);
            return await command.ExecuteScalarAsync();
        }

        public static int VerifyAuthKey(object authKey)
        {
            var command = new SqliteCommand("SELECT USER_ID FROM AuthKeys WHERE AUTHKEY=@authkey", conn);
            command.Parameters.Add(new SqliteParameter("@authkey", authKey));
            var result = command.ExecuteScalar();
            if (result != null)
            {
                return Convert.ToInt32(result);
            }
            return -1;
        }

        public static bool ValidLogin(object username, object password)
        {
            var command = new SqliteCommand("SELECT count(ID) FROM Users WHERE USERNAME=@username AND PASSWORD=@password", conn);
            command.Parameters.Add(new SqliteParameter("@username", username));
            command.Parameters.Add(new SqliteParameter("@password", password));
            var rows = Convert.ToInt32(command.ExecuteScalar());
            if(rows >= 1)
            {
                return true;
            }
            return false;
        }

        /* HERE IS THE CODE I WOULD USE TO ENCRYPT THE PASSWORDS BEFORE I PUT THEM IN THE DATABASE.
         * UNFORTUNATELY I COULD NOT GET
         * 
         *  System.Security.Cryptography.SHA256Managed 
         * 
         * TO SHOW UP AFTER HOURS OF TRYING :(
         * 
        public static string Encode(string str)
        {
            System.Security.Cryptography.SHA256Managed crypt = new SHA256Managed();
            StringBuilder hash = new StringBuilder();
            byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(str), 0, Encoding.UTF8.GetByteCount(str));
            foreach (byte theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }
            return hash.ToString();
        }
        */


        /*
         *  Inserts 
         */

        public static void InsertMessage(DataToSendMessage data, int roomNumber)
        {
            //.WriteLine("user_id={0}", GetUserId(data.username));
            var command = new SqliteCommand("INSERT INTO Messages (MESSAGE, USER_ID, ROOM_ID, TIMESTAMP) VALUES (@message, @user_id, @room_id, @timestamp)", conn);
            command.Parameters.Add(new SqliteParameter("@message", data.message));
            command.Parameters.Add(new SqliteParameter("@user_id", GetUserId(data.username)));
            command.Parameters.Add(new SqliteParameter("@room_id", roomNumber));
            command.Parameters.Add(new SqliteParameter("@timestamp", data.timestamp));
            command.ExecuteNonQuery();
        }

        public static void InsertRoom(string roomName)
        {
            var command = new SqliteCommand("INSERT INTO Rooms (NAME, COLOR) VALUES (@roomName, @color)", conn);
            command.Parameters.Add(new SqliteParameter("@roomName", roomName));
            command.Parameters.Add(new SqliteParameter("@color", Room.RandomColor()));
            command.ExecuteNonQuery();
        }

        public static void InsertAuthKey(string authKey, int user_id)
        {
            var command = new SqliteCommand("INSERT INTO AuthKeys (AUTHKEY, USER_ID) VALUES (@authkey, @user_id)", conn);
            command.Parameters.Add(new SqliteParameter("@authkey", authKey));
            command.Parameters.Add(new SqliteParameter("@user_id", user_id));
            command.ExecuteNonQuery();
        }

        public static void InsertUser(string username, string password)
        {
            var command = new SqliteCommand("INSERT INTO Users (USERNAME, PASSWORD, COLOR) VALUES (@username, @password, @color)", conn);
            command.Parameters.Add(new SqliteParameter("@username", username));
            command.Parameters.Add(new SqliteParameter("@password", password));
            command.Parameters.Add(new SqliteParameter("@color", Room.RandomColor()));
            command.ExecuteNonQuery();
        }

        public static int InsertNotification(object user_id, object sender_id, object room_id, object timestamp)
        {
            var command = new SqliteCommand("INSERT INTO Notifications (USER_ID, SENDER_ID, ROOM_ID, TIMESTAMP) VALUES (@user_id, @sender_id, @room_id, @timestamp); SELECT last_insert_rowid();", conn);
            command.Parameters.Add(new SqliteParameter("@user_id", user_id));
            command.Parameters.Add(new SqliteParameter("@sender_id", sender_id));
            command.Parameters.Add(new SqliteParameter("@room_id", room_id));
            command.Parameters.Add(new SqliteParameter("@timestamp", timestamp));
            return Convert.ToInt32(command.ExecuteScalar());
        }

        /*
         *  Other Functions 
         */

        public static DataToSendNotification[] GetNotificationData(int user_id)
        {
            var numRows = GetNumNotifications(user_id);
            var data = new DataToSendNotification[numRows];

            var command = new SqliteCommand("SELECT * FROM Notifications WHERE USER_ID=@user_id AND SEEN=0", conn);
            command.Parameters.Add(new SqliteParameter("@user_id", user_id));
            var reader = command.ExecuteReader();

            var i = 0;
            while (reader.Read())
            {
                //Console.WriteLine(reader["MESSAGE"]);
                data[i] = new DataToSendNotification();
                data[i].type = "notification";
                data[i].senderName = GetUsername(reader["SENDER_ID"].ToString());
                data[i].roomName = GetRoomName(reader["ROOM_ID"].ToString());
                data[i].timestamp = reader["TIMESTAMP"].ToString();
                data[i].notificationId = Convert.ToInt32(reader["ID"].ToString());
                i++;
            }
            return data;
        }

        public static DataToSendPopulate GetPopulateData(int user_id, int roomNumber)
        {
            var dataToSend = new DataToSendPopulate();
            dataToSend.type = "populate";
            var numRows = GetNumMessages(roomNumber);
            dataToSend.data = new DataToSendMessage[numRows];

            var command = new SqliteCommand("SELECT * FROM Messages WHERE ROOM_ID=@roomNumber", conn);
            command.Parameters.Add(new SqliteParameter("@roomNumber", roomNumber));
            var reader = command.ExecuteReader();

            var i = 0;
            while (reader.Read())
            {
                //Console.WriteLine(reader["MESSAGE"]);
                dataToSend.data[i] = new DataToSendMessage();
                dataToSend.data[i].username = GetUsername(reader["USER_ID"].ToString());
                dataToSend.data[i].message = reader["MESSAGE"].ToString();
                dataToSend.data[i].timestamp = reader["TIMESTAMP"].ToString();
                dataToSend.data[i].color = GetUserColor(reader["USER_ID"].ToString());
                dataToSend.data[i].highlight = false;

                Regex regex = new Regex("@([a-zA-Z0-9]*)");
                var match = regex.Match(dataToSend.data[i].message);
                var matchStr = match.Groups[1].Value;
                if (matchStr != "")
                {
                    var thisUsersId = DBHandler.GetUserId(matchStr);
                    if (DBHandler.UserExists(matchStr) && user_id == thisUsersId)
                    {
                        dataToSend.data[i].highlight = true;
                    }
                }

                i++;
            }
            return dataToSend;
        }

        public static DataToSendRooms GetRoomsData()
        {
            var dataToSend = new DataToSendRooms();
            dataToSend.type = "rooms";
            var numRows = GetNumRooms();
            dataToSend.data = new DataToSendRoom[numRows];

            var command = new SqliteCommand("SELECT * FROM Rooms", conn);
            var reader = command.ExecuteReader();

            var i = 0;
            while (reader.Read())
            {
                //Console.WriteLine(reader["MESSAGE"]);
                dataToSend.data[i] = new DataToSendRoom();
                dataToSend.data[i].name = reader["NAME"].ToString();
                dataToSend.data[i].color = reader["COLOR"].ToString();
                dataToSend.data[i].roomNumber = Convert.ToInt32(reader["ID"]);
                i++;
            }
            return dataToSend;
        }

        public static List<Room> GetRoomsList()
        {
            List<Room> rooms = new List<Room>();
            var command = new SqliteCommand("SELECT * FROM Rooms", conn);
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var roomNumber = Convert.ToInt32(reader["ID"]);
                rooms.Add(new Room(roomNumber));
            }

            return rooms;
        }

        /* 
         *  Helper Functions 
         */

        public static int GetNumRooms()
        {
            var command = new SqliteCommand("SELECT count(ID) FROM Rooms", conn);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        public static int GetNumMessages(object room_id)
        {
            var command = new SqliteCommand("SELECT count(ID) FROM Messages WHERE ROOM_ID=@room_id", conn);
            command.Parameters.Add(new SqliteParameter("@room_id", room_id));
            return Convert.ToInt32(command.ExecuteScalar());
        }

        public static int GetNumNotifications(object user_id)
        {
            var command = new SqliteCommand("SELECT count(ID) FROM Notifications WHERE USER_ID=@user_id AND SEEN=0", conn);
            command.Parameters.Add(new SqliteParameter("@user_id", user_id));
            return Convert.ToInt32(command.ExecuteScalar());
        }
        
        public static bool UsernameAvailable(object username)
        {
            var command = new SqliteCommand("SELECT count(ID) FROM Users WHERE USERNAME=@username", conn);
            command.Parameters.Add(new SqliteParameter("@username", username));
            var rows = Convert.ToInt32(command.ExecuteScalar());
            if(rows <= 0)
            {
                return true;
            }
            return false;
        }

        public static bool UserExists(object username)
        {
            return !UsernameAvailable(username);
        }

        public static bool RoomNameAvailable(object roomName)
        {
            var command = new SqliteCommand("SELECT count(ID) FROM Rooms WHERE NAME=@roomName", conn);
            command.Parameters.Add(new SqliteParameter("@roomName", roomName));
            var rows = Convert.ToInt32(command.ExecuteScalar());
            if (rows <= 0)
            {
                return true;
            }
            return false;
        }

        public static string GetUsername(object user_id)
        {
            var command = new SqliteCommand("SELECT USERNAME FROM Users WHERE ID=@user_id", conn);
            command.Parameters.Add(new SqliteParameter("@user_id", user_id));
            return command.ExecuteScalar().ToString();
        }

        public static int GetUserId(object username)
        {
            var command = new SqliteCommand("SELECT ID FROM Users WHERE USERNAME=@username", conn);
            command.Parameters.Add(new SqliteParameter("@username", username));
            return Convert.ToInt32(command.ExecuteScalar());
        }

        public static string GetUserColor(object user_id)
        {
            var command = new SqliteCommand("SELECT COLOR FROM Users WHERE ID=@user_id", conn);
            command.Parameters.Add(new SqliteParameter("@user_id", user_id));
            return command.ExecuteScalar().ToString();
        }

        public static string GetRoomName(object room_id)
        {
            var command = new SqliteCommand("SELECT NAME FROM Rooms WHERE ID=@room_id", conn);
            command.Parameters.Add(new SqliteParameter("@room_id", room_id));
            return command.ExecuteScalar().ToString();
        }

        public static int GetRoomId(object roomName)
        {
            var command = new SqliteCommand("SELECT ID FROM Rooms WHERE NAME=@roomName", conn);
            command.Parameters.Add(new SqliteParameter("@roomName", roomName));
            return Convert.ToInt32(command.ExecuteScalar());
        }

        public static string GetRoomColor(object room_id)
        {
            var command = new SqliteCommand("SELECT COLOR FROM Rooms WHERE ID=@room_id", conn);
            command.Parameters.Add(new SqliteParameter("@room_id", room_id));
            return command.ExecuteScalar().ToString();
        }

    }
}
