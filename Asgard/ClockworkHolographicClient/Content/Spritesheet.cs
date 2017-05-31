using ClockworkHolographicClient.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace ClockworkHolographicClient.Content
{
    class Spritesheet
    {
        public static float positionScaleFactor = 0.01f;
        public static float textureScaleFactor = 0.001f;

        public class Frame
        {
            public String name, code;
            public float x, y, w, h;
            public int t;
        }

        public class Layer
        {
            public String name;
            public float x, y;
            public int length;
            public Boolean infiniteLength;
            public List<Frame> frames;
        }
        public class State
        {
            public String name;
            public int length;
            public Boolean infiniteLength;
            public List<Layer> layers;
        }
        public class JSONLayer
        {
            public String name;
            public float x, y;
            public int length;
            public List<String> frames;
        }
        public class JSONState
        {
            public String name;
            public int length;
            public List<String> layers;
        }
        public class Sheet
        {
            public String name;
            public string src;
            public List<State> states;
        }
        public class Object
        {
            public Sheet sheet;
            public State state;
            public int t;
            public int id;
            public float x, y, z;
            public Sprite sprite;
            public bool isStatic;

            public Object(int id, Sheet sheet, State state, float x, float y, float z, bool isStatic)
            {
                this.sheet = sheet;
                this.state = state;
                this.x = x;
                this.y = y;
                this.z = z;
                this.isStatic = isStatic;
                t = 0;
                this.id = id;
                sprite = new Sprite(x, y, z, sheet.src);
            }

            public void update(int millisecondsPerUpdate)
            {
                Frame frame;
                if (state.length == 0)
                {
                    Layer layer = state.layers.First();
                    frame = layer.frames[0];
                }
                else
                {
                    t += millisecondsPerUpdate;
                    if (state.infiniteLength == false)
                    {
                        t = t % state.length;
                    }
                    //TODO:Support for multiple layers
                    Layer layer = state.layers.First();
                    int timeAcc = 0, frameN = 0;
                    bool substractFlag = false;
                    while (timeAcc < t)
                    {
                        Frame thisFrame = layer.frames[frameN];
                        substractFlag = true;
                        if (thisFrame.t == 0)
                        {
                            substractFlag = false;
                            break;
                        }
                        frameN++;
                        if(frameN == layer.frames.Count)
                        {
                            break;
                        }
                        timeAcc += thisFrame.t;
                    }
                    if (substractFlag)
                    {
                        frameN--;
                    }
                    frame = layer.frames[frameN];
                }
                sprite.setTextureCoordinates(frame.x, frame.y, frame.w, frame.h);
            }

            public void setState(State state)
            {
                this.state = state;
                this.t = 0;
            }

            public void setPos(float x, float y, float z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                sprite.setPosition(x, y, z);
            }

            public static int maxId = 0;
        }

        internal void deleteObject(int id)
        {
            objects.RemoveAll(x => x.id == id);
        }

        private List<Object> objects;
        List<Sheet> sheets;
        private DispatcherTimer timer;
        private int millisecondsPerUpdate;
        Dictionary<string, string> images;

        Boolean areResourcesLoaded;
        DeviceResources deviceResources;


        public Spritesheet()
        {
            areResourcesLoaded = false;
            objects = new List<Object>();
            images = new Dictionary<string, string>();
            //timer = new DispatcherTimer();
            //timer.Tick += updateObjects;
        }

        private class JSONSpritesheet
        {
            public string name { get; set; }
            public string src { get; set; }
            public Dictionary<string, JSONState> states { get; set; }
            public Dictionary<string, JSONLayer> layers { get; set; }
            public Dictionary<string, Frame> frames { get; set; }
        }

        public void loadJSONSpritesheets(String jsonSpritesheets)
        {
            List<JSONSpritesheet> spritesheets = (List<JSONSpritesheet>)Newtonsoft.Json.JsonConvert.DeserializeObject(jsonSpritesheets, typeof(List<JSONSpritesheet>));
            sheets = spritesheets.Select(spritesheet => loadSpritesheet(spritesheet)).Where(x => x != null).ToList<Sheet>();
        }

        private Sheet loadSpritesheet(JSONSpritesheet spritesheet)
        {
            if (spritesheet.src==null || !images.ContainsKey(spritesheet.src))
            {
                return null;
            }
            //Create the sheet
            var sheet = new Sheet()
            {
                name = spritesheet.name,
                src = images[spritesheet.src]
            };
            //Load all the frames
            var query = from kv in spritesheet.frames
                        select new Frame
                        {
                            name = kv.Key,
                            x = kv.Value.x,
                            y = kv.Value.y,
                            w = kv.Value.w,
                            h = kv.Value.h,
                            t = kv.Value.t,
                            code = kv.Value.code
                        };
            List<Frame> frames = query.ToList<Frame>();
            //Load all the layers, and find the frames that belong to each one
            //TODO: Some layers have dynamic x,y that should be eval()'d with chackra
            var query2 = from kv in spritesheet.layers
                         select new Layer
                         {
                             name = kv.Key,
                             x = kv.Value.x,
                             y = kv.Value.y,
                             frames = kv.Value.frames.Select(frame => frames.Find(x => String.Equals((String)frame, x.name))).ToList<Frame>(),
                             length = kv.Value.frames.Select(frame => frames.Find(x => String.Equals((String)frame, x.name))).Aggregate<Frame, int>(0, (x, y) => x + y.t),
                             infiniteLength = kv.Value.frames.Select(frame => frames.Find(x => String.Equals((String)frame, x.name))).Where(x => x.t == 0).Any()
                         };
            List<Layer> layers = query2.ToList<Layer>();
            //Load all the layers, and find the frames that belong to each one
            var query3 = from kv in spritesheet.states
                         select new State
                         {
                             name = kv.Key,
                             layers = kv.Value.layers.Select(layer => layers.Find(x => String.Equals((String)layer, x.name))).ToList<Layer>(),
                             length = kv.Value.layers.Select(layer => layers.Find(x => String.Equals((String)layer, x.name))).Aggregate<Layer, int>(0, (x, y) => Math.Max(x, y.length)),
                             infiniteLength = kv.Value.layers.Select(layer => layers.Find(x => String.Equals((String)layer, x.name))).Where(x => x.infiniteLength == true).Any()
                         };
            sheet.states = query3.ToList<State>();
            return sheet;
        }

        public void addObject(int id, String sheetName, String stateName, float x, float y, float z, bool isStatic)
        {
            Sheet sheet = sheets.Find(a => String.Equals(a.name, sheetName));
            if (sheet == null)
            {
                return;
            }
            State state;
            if (stateName != null)
            {
                state = sheet.states.Find(a => String.Equals(a.name, stateName));
            }
            else
            {
                state = sheet.states.First();
            }
            Object newObject = new Object(id,sheet, state, x, y, z, isStatic);
            objects.Add(newObject);
            if (areResourcesLoaded)
            {
                newObject.sprite.CreateDeviceDependentResourcesAsync(deviceResources);
            }
        }

        public List<Sprite> getObjects()
        {
            return objects.Select(x => x.sprite).ToList<Sprite>();
        }

        public Object getObject(int id)
        {
            return objects.Where(x => x.id == id).FirstOrDefault();
        }


        public void setUp(int fps)
        {
            millisecondsPerUpdate = 1000 / fps;
            //timer.Interval = new TimeSpan(1, 2, 0, 30, millisecondsPerUpdate);
            //timer.Start();
        }

        public void updateObjects()
        {
            objects.ForEach(x => x.update(millisecondsPerUpdate));
        }

        public void setState(int id, string stateName)
        {
            Object o = objects.Find(x => x.id == id);
            if (o != null)
            {
                State state = o.sheet.states.Find(x => String.Equals(x.name, stateName));
                o.setState(state);
            }
        }

        private void setPosition(int id, float x, float y, float z)
        {
            objects.Find(a => a.id == id).setPos(x, y, z);
        }

        internal void resourcesLoaded(DeviceResources dr)
        {
            deviceResources = dr;
            areResourcesLoaded = true;
        }

        internal async Task registerImage(string src, string data)
        {
            //var ims = new InMemoryRandomAccessStream();
            //var bytes = Convert.FromBase64String(data);
            //var dataWriter = new DataWriter(ims);
            //dataWriter.WriteBytes(bytes);
            //await dataWriter.StoreAsync();
            //ims.Seek(0);
            //var img = new BitmapImage();
            //img.SetSource(ims);
            images.Add(src, data);
        }
    }


}
