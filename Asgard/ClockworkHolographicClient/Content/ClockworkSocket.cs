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
using Windows.Perception.Spatial.Surfaces;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Perception.Spatial;

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
            messageQueue.ForEach(sendMessage);
            startServerRead();
        }

        static List<string> messageQueue = new List<string>();
        static List<string> log = new List<string>();

        public static async void sendMessage(string message)
        {
            if (writer != null)
            {
                log.Add(message);
                writer.WriteInt32((int)writer.MeasureString(message));
                writer.WriteString(message);
                await writer.StoreAsync();
            }
            else
            {
                messageQueue.Add(message);
            }
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
            int id;
            float value;
            Spritesheet.Object currentObject;
            switch (command[0])
            {
                case "addObject":
                    id = int.Parse(command[1]);
                    string spritesheet = command[2];
                    string state = command[3];
                    float x = float.Parse(command[4]);
                    float y = float.Parse(command[5]);
                    float z = float.Parse(command[6]);
                    animationLibrary.addObject(id, spritesheet, state, x, y, z, false);
                    break;
                case "deleteObject":
                    id = int.Parse(command[1]);
                    animationLibrary.deleteObject(id);
                    break;
                case "registerImage":
                    string src = command[1];
                    string data = command[2];
                    await animationLibrary.registerImage(src, data);
                    break;
                case "loadSpritesheetJSONObject":
                    animationLibrary.loadJSONSpritesheets(command[1]);
                    break;
                case "setState":
                    id = int.Parse(command[1]);
                    animationLibrary.setState(id, command[2]);
                    break;
                case "setX":
                    id = int.Parse(command[1]);
                    value = float.Parse(command[2]);
                    currentObject = animationLibrary.getObject(id);
                    if (currentObject != null)
                    {
                        currentObject.setPos(value, currentObject.y, currentObject.z);
                    }
                    break;
                case "setY":
                    id = int.Parse(command[1]);
                    value = float.Parse(command[2]);
                    currentObject = animationLibrary.getObject(id);
                    if (currentObject != null)
                    {
                        currentObject.setPos(currentObject.x, value, currentObject.z);
                    }
                    break;
                case "setZ":
                    id = int.Parse(command[1]);
                    value = float.Parse(command[2]);
                    currentObject = animationLibrary.getObject(id);
                    if (currentObject != null)
                    {
                        currentObject.setPos(currentObject.x, currentObject.y, value);
                    }
                    break;
            }
        }


        public static void processInput(string input, SpatialPointerPose pointerPose)
        {
            if (null != pointerPose)
            {
                System.Numerics.Vector3 headPosition = pointerPose.Head.Position / Spritesheet.positionScaleFactor;
                System.Numerics.Vector3 headDirection = pointerPose.Head.ForwardDirection;
                List<Object> message = new List<object>() { input, headPosition, headDirection };
                sendMessage(JsonConvert.SerializeObject(message));
            }
        }

        static Dictionary<Guid, SpatialSurfaceInfo> surfaces = new Dictionary<Guid, SpatialSurfaceInfo>();

        public static SpatialCoordinateSystem currentCoordinateSystem { get; private set; }

        internal static void SurfacesChanged(SpatialSurfaceObserver sender, object args)
        {
            List<Guid> toRemove = new List<Guid>();
            var surfaceCollection = sender.GetObservedSurfaces();
            foreach (var pair in surfaces)
            {
                if (!surfaceCollection.ContainsKey(pair.Key))
                {
                    toRemove.Add(pair.Key);
                }
            }
            foreach (var id in toRemove)
            {
                surfaceRemoved(id);
                surfaces.Remove(id);
            }
            foreach (var pair in surfaceCollection)
            {
                if (surfaces.ContainsKey(pair.Key))
                {
                    surfaceModified(pair.Key, pair.Value);
                    surfaces[pair.Key] = pair.Value;
                }
                else
                {
                    surfaceAdded(pair.Key, pair.Value);
                    surfaces.Add(pair.Key, pair.Value);
                }
            }
        }

        public static async void surfaceAdded(Guid id, SpatialSurfaceInfo info)
        {
            SpatialSurfaceMesh mesh = await info.TryComputeLatestMeshAsync(2);
            if (mesh != null)
            {
                Vector3 scale = mesh.VertexPositionScale;
                Matrix4x4 coordinateSystem = (Matrix4x4)mesh.CoordinateSystem.TryGetTransformTo(currentCoordinateSystem);
                byte[] vertexPositions = mesh.VertexPositions.Data.ToArray();
                byte[] triangleIndices = mesh.TriangleIndices.Data.ToArray();
                var triangleIndicesList = new List<int>();
                var vertexPositionsList = new List<Vector3>();
                for (int i = 0; i < triangleIndices.Length; i += (int)mesh.TriangleIndices.Stride)
                {
                    int j = (int)System.BitConverter.ToUInt16(triangleIndices, i);
                    triangleIndicesList.Add(j);
                }
                for (int i = 0; i < vertexPositions.Length;)
                {
                    float x = System.BitConverter.ToInt16(vertexPositions, i);
                    i += 2;
                    float y = System.BitConverter.ToInt16(vertexPositions, i);
                    i += 2;
                    float z = System.BitConverter.ToInt16(vertexPositions, i);
                    i += 2;
                    float w = System.BitConverter.ToInt16(vertexPositions, i);
                    i += 2;
                    Vector3 position = Vector3.Transform(new Vector3(x / w, y / w, z / w) * scale, coordinateSystem);
                    vertexPositionsList.Add(position / Spritesheet.positionScaleFactor);
                }
                List<Object> message = new List<object>() { "surfaceAdded", id, vertexPositionsList, triangleIndicesList };
                sendMessage(JsonConvert.SerializeObject(message));
            }
        }
        public static async void surfaceModified(Guid id, SpatialSurfaceInfo info)
        {
            SpatialSurfaceMesh mesh = await info.TryComputeLatestMeshAsync(2);
            if (mesh != null)
            {
                Vector3 scale = mesh.VertexPositionScale;
                Matrix4x4 coordinateSystem = (Matrix4x4)mesh.CoordinateSystem.TryGetTransformTo(currentCoordinateSystem);
                byte[] vertexPositions = mesh.VertexPositions.Data.ToArray();
                byte[] triangleIndices = mesh.TriangleIndices.Data.ToArray();
                var triangleIndicesList = new List<int>();
                var vertexPositionsList = new List<Vector3>();
                for (int i = 0; i < triangleIndices.Length; i += (int)mesh.TriangleIndices.Stride)
                {
                    int j = (int)System.BitConverter.ToUInt16(triangleIndices, i);
                    triangleIndicesList.Add(j);
                }
                for (int i = 0; i < vertexPositions.Length;)
                {
                    float x = (float)System.BitConverter.ToInt16(vertexPositions, i);
                    i += 2;
                    float y = (float)System.BitConverter.ToInt16(vertexPositions, i);
                    i += 2;
                    float z = (float)System.BitConverter.ToInt16(vertexPositions, i);
                    i += 2;
                    float w = (float)System.BitConverter.ToInt16(vertexPositions, i);
                    i += 2;

                    Vector3 position = Vector3.Transform(new Vector3(x / w, y / w, z / w) * scale, coordinateSystem);
                    vertexPositionsList.Add(position / Spritesheet.positionScaleFactor);
                }
                List<Object> message = new List<object>() { "surfaceModified", id, vertexPositionsList, triangleIndicesList };
                sendMessage(JsonConvert.SerializeObject(message));
            }
        }

        internal static void setCoordinateSystem(SpatialCoordinateSystem ccs)
        {
            currentCoordinateSystem = ccs;
        }

        public static void surfaceRemoved(Guid id)
        {
            List<Object> message = new List<object>() { "surfaceRemoved", id };
            sendMessage(JsonConvert.SerializeObject(message));
        }
    }
}

