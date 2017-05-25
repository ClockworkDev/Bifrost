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
        private class Frame
        {
            public String name, code;
            public float x, y, w, h;
            public int t;
        }

        private class Layer
        {
            public String name;
            public float x, y;
            public int length;
            public List<Frame> frames;
        }
        private class State
        {
            public String name;
            public int length;
            public List<Layer> layers;
        }
        private class JSONLayer
        {
            public String name;
            public float x, y;
            public int length;
            public List<String> frames;
        }
        private class JSONState
        {
            public String name;
            public int length;
            public List<String> layers;
        }
        private class Sheet
        {
            public String name;
            public string src;
            public List<State> states;
        }
        private class Object
        {
            public Sheet sheet;
            public State state;
            public int t;
            public int id;
            public float x, y, z;
            public List<State> states;
            public Sprite sprite;
            public bool isStatic;

            public Object(Sheet sheet, State state, float x, float y, float z, bool isStatic)
            {
                this.sheet = sheet;
                this.state = state;
                this.x = x;
                this.y = y;
                this.z = z;
                this.isStatic = isStatic;
                t = 0;
                id = maxId++;
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
                    t = t % state.length;
                    //TODO:Support for multiple layers
                    Layer layer = state.layers.First();
                    int timeAcc = 0, frameN = 0;
                    while (timeAcc < t)
                    {
                        timeAcc += layer.frames[frameN].t;
                        frameN++;
                        if (layer.frames[frameN - 1].t == 0)
                        {
                            break;
                        }
                    }
                    if (frameN == 0)
                    {
                        frameN++;
                    }
                    frame = layer.frames[frameN - 1];
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
            sheets = spritesheets.Select(spritesheet => loadSpritesheet(spritesheet)).ToList<Sheet>();
        }

        private Sheet loadSpritesheet(JSONSpritesheet spritesheet)
        {
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
                             length = kv.Value.frames.Select(frame => frames.Find(x => String.Equals((String)frame, x.name))).Aggregate<Frame, int>(0, (x, y) => x + y.t)
                         };
            List<Layer> layers = query2.ToList<Layer>();
            //Load all the layers, and find the frames that belong to each one
            var query3 = from kv in spritesheet.states
                         select new State
                         {
                             name = kv.Key,
                             layers = kv.Value.layers.Select(layer => layers.Find(x => String.Equals((String)layer, x.name))).ToList<Layer>(),
                             length = kv.Value.layers.Select(layer => layers.Find(x => String.Equals((String)layer, x.name))).Aggregate<Layer, int>(0, (x, y) => Math.Max(x, y.length))
                         };
            sheet.states = query3.ToList<State>();
            return sheet;
        }

        public int addObject(String sheetName, String stateName, float x, float y, float z, bool isStatic)
        {
            Sheet sheet = sheets.Find(a => String.Equals(a.name, sheetName));
            State state;
            if (stateName != null)
            {
                state = sheet.states.Find(a => String.Equals(a.name, stateName));
            }
            else
            {
                state = sheet.states.First();
            }
            Object newObject = new Object(sheet, state, x, y, z, isStatic);
            objects.Add(newObject);
            if (areResourcesLoaded)
            {
                newObject.sprite.CreateDeviceDependentResourcesAsync(deviceResources);
            }
            return newObject.id;
        }

        public List<Sprite> getObjects()
        {
            return objects.Select(x => x.sprite).ToList<Sprite>();
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

        private void setState(int id, string stateName)
        {
            Object o = objects.Find(x => x.id == id);
            State state = o.states.Find(x => String.Equals(x.name, stateName));
            o.setState(state);
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
