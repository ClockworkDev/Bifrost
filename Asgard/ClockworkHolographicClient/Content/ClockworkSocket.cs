using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Newtonsoft.Json;
using Windows.UI.Input.Spatial;

namespace ClockworkHolographicClient.Content
{
    class ClockworkSocket
    {
        static StreamSocket socket;
        static DataReader reader;
        static DataWriter writer;
        static Spritesheet animationLibrary;
        public static async void setup(Spritesheet sp)
        {
            animationLibrary = sp;
            socket = new Windows.Networking.Sockets.StreamSocket();
            HostName hostname = new Windows.Networking.HostName("192.168.137.1");
            await socket.ConnectAsync(hostname, "87776");
            reader = new Windows.Storage.Streams.DataReader(socket.InputStream);
            writer = new Windows.Storage.Streams.DataWriter(socket.OutputStream);
            startServerRead();
        }

        public static async void sendMessage(string message)
        {
            writer.WriteInt32((int)writer.MeasureString(message));
            writer.WriteString(message);
            await writer.StoreAsync();
        }

        public static async void startServerRead()
        {
            uint stringBytesRead = await reader.LoadAsync(4);
            if (stringBytesRead != 4)
            {
                //Connection lost
                return;
            }
            uint count = (uint)reader.ReadInt32();
            stringBytesRead = await reader.LoadAsync(count);
            if (stringBytesRead != count)
            {
                //Connection lost
                return;
            }
            string message = reader.ReadString(count);
            List<String> deserializedMessage = (List<String>)Newtonsoft.Json.JsonConvert.DeserializeObject(message, typeof(List<String>));
            await processCommand(deserializedMessage);
            startServerRead();
        }

        public static async Task processCommand(List<String> command)
        {
            switch (command[0])
            {
                case "addObject":
                    float x = float.Parse(command[3]);
                    float y = float.Parse(command[4]);
                    float z = float.Parse(command[5]);
                    animationLibrary.addObject("dog", "IdleL", x, y, z, false);
                    break;
                case "registerImage":
                    string src = command[1];
                    string data = command[2];
                    await animationLibrary.registerImage(src, data);
                    break;
                case "loadSpritesheetJSONObject":
                    animationLibrary.loadJSONSpritesheets(command[1]);
                    break;
            }
        }


        public static void processInput(string input,SpatialPointerPose pointerPose)
        {
            if (null != pointerPose)
            {
                System.Numerics.Vector3 headPosition = pointerPose.Head.Position;
                System.Numerics.Vector3 headDirection = pointerPose.Head.ForwardDirection;
                List<Object> message = new List<object>() { input, headPosition, headDirection };
                sendMessage(JsonConvert.SerializeObject(message));
            }
        }
    }
}
