using iRacingReplayDirector.Phases.Capturing;
using iRacingSDK.Support;
using MediaFoundation.Net;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;

//TODO: Add plugin management form to main app
//TODO: Discover overlay plugins, and show list in form
//TODO: Allow user to 'select' a plugin, and use that plugin during transcoding
//TODO: Show the plugin's settings & allow user to change.
//TODO: Move all leaderboard graphiccs into plugin
//TODO: Create plugin test app - to draw on a single bitmap
//TODO: Creating publishing model - allow app to install different versions of plugin.
//TODO: Ensure good error feedback if plugin missing stuff (eg: Graphics field)
//TODO: Move support files from overlayplugin to plugin base
//TODO: Add lots more style helpers
//TODO: Additional meta data for plugin - name, description, author, etc. - use assembly attributes
//TODO: Extract timing logic into seperate methods

namespace iRacingReplayDirector
{
    public class PluginProxy
    {
        static readonly string PluginPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),  "plugins\\");
        const string StandardOverlayDirectory = "iRacingDirector.Plugin.StandardOverlays";
        const string StandardOverlaysName = "StandardOverlays";

        dynamic plugin;
        Type pluginType;
        OverlayData data;
        long timestamp;
        readonly Func<Type, object> CreateInstance = t => Activator.CreateInstance(t);
        readonly Type pluginSettingsType;

        public static string[] Names
        {
            get
            {
                return System.IO.Directory.EnumerateDirectories(PluginPath)
                    .Select(t => t.Substring(PluginPath.Length))
                    .Select(t => t == StandardOverlayDirectory ? StandardOverlaysName : t)
                    .ToArray();
            }
        }

        public long Duration
        {
            get
            {
                return data.LeaderBoards.Last().StartTime.FromSecondsToNano();
            }
        }

        public PluginProxy(string pluginName)
        {
            if(!pluginName.ToLower().EndsWith(".dll"))
                pluginName = Path.Combine(PluginPath, pluginName, pluginName + ".dll");
            /* previous code handling StandardOverlay different than other plugins - likely not necessary - currently in testing
            if (pluginName == StandardOverlaysName)
                pluginName = Path.Combine(PluginPath, StandardOverlayDirectory, StandardOverlayDirectory + ".dll");
            else
                pluginName = Path.Combine(PluginPath, pluginName, pluginName + ".dll");*/
            TraceInfo.WriteLine("Trying to load overlay plugin:{0}", pluginName);
            var an = AssemblyName.GetAssemblyName(pluginName);
            var assembly = Assembly.Load(an);

            pluginType = assembly.GetTypes()
                .Where(t => !t.IsInterface)
                .Where(t => !t.IsAbstract)
                .FirstOrDefault(t => t.FullName.EndsWith(".MyPlugin"));

            plugin = Activator.CreateInstance(pluginType);

            var x = assembly.GetTypes();

            pluginSettingsType = assembly.GetTypes()
                .Where(t => !t.IsInterface)
                .FirstOrDefault(t => t.FullName.EndsWith(".Settings"));
        }

        public void DrawIntroFlashCard(long duration)
        {
            plugin.IntroFlashCard(duration, this.timestamp);
        }

        public void DrawOutroFlashCard(long duration, long period)
        {
            plugin.OutroFlashCard(duration, period);
        }

        public void SetGraphics(Graphics graphics)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            plugin.Graphics = graphics;
        }

        public void SetReplayConfig(string filePath)
        {
            this.data = OverlayData.FromFile(filePath);
        }

        public PluginProxySettings[] GetSettingsList()
        {
            if (pluginSettingsType == null)
                return null;

            var fields = pluginSettingsType.GetFields(BindingFlags.GetField | BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty);
            return fields.Select(f =>
            {
                var descAttr = (dynamic)f.GetCustomAttributes().FirstOrDefault(d => d.GetType().Name.EndsWith("DescriptionAttribute"));
                return new PluginProxySettings
                {
                    Name = "StandardOverlay.{0}".F(f.Name),
                    Value = f.GetValue(null),
                    Type = f.FieldType,
                    Description = descAttr == null ? "" : descAttr.Description
                };
            }).ToArray();
        }

        public void SetReplayConfig(OverlayData data)
        {
            this.data = data;
        }

        public void InjectFields(long timestamp, string[] perferredDriverNames)
        {
            this.timestamp = timestamp;

            SetEventData();
            SetLeaderboard();
            SetCamDriver();
            SetMessageSet();
            SetFastLap();
            SetPreferredDriver(perferredDriverNames);
        }

        private void SetPreferredDriver(string[] perferredDriverNames)
        {
            var drivers = perferredDriverNames
                .Select(n => n.ToLower())
                .Select(n => data.SessionData.DriverInfo.Drivers.FirstOrDefault(d => d.UserName.ToLower() == n))
                .Where(d => d != null)
                .Select(d => new OverlayData.Driver { UserName = d.UserName, CarIdx = (int)d.CarIdx, CarNumber = d.CarNumber })
                .ToArray();
            
            CreateAndAssignInstance("PreferredDriverNames", false, type =>
            {
                var instance = Activator.CreateInstance(type, drivers.Length);
                var x = instance.GetType().GetMethod("Set");

                for (var i = 0; i < drivers.Length; i++)
                {
                    var driverInstance = CreateDriverInstance(instance.GetType().GetElementType(), drivers[i]);

                    x.Invoke(instance, new object[] { i, driverInstance });

                }
                return instance;
            });
        }
        

        void SetEventData()
        {
            var eventDataField = pluginType.GetField("EventData");
            var eventDataType = eventDataField.FieldType;
            var instance = Activator.CreateInstance(eventDataType, data.SessionData);

            eventDataField.SetValue(plugin, instance);
        }
        
        object CreateAndAssignInstance(string fieldName, bool setToNull = false, Func<Type, object> createInstance = null)
        {
            var field = pluginType.GetField(fieldName);

            if (field == null)
                return null;

            if (setToNull)
            {
                field.SetValue(plugin, null);
                return null;
            }

            var instance = createInstance == null ? Activator.CreateInstance(field.FieldType) : createInstance(field.FieldType);
            field.SetValue(plugin, instance);
            return instance;
        }

        object CreateAndAssignInstance(string fieldName, bool setToNull = false, Action<object> createInstance = null)
        {
            return CreateAndAssignInstance(fieldName, setToNull, t =>
            {
                var instance = Activator.CreateInstance(t);
                createInstance(instance);
                return instance;
            });
        }
        
        void SetLeaderboard()
        {
            var lb = data.LeaderBoards.LastOrDefault(t => t.StartTime <= timestamp.FromNanoToSeconds());
            if (lb == null)
                lb = data.LeaderBoards.First();

            var leaderboardInstance = CreateAndAssignInstance("LeaderBoard", lb == null, CreateInstance);

            if (leaderboardInstance == null)
                return;

            SetField(leaderboardInstance, "RacePosition", lb.RacePosition);
            SetField(leaderboardInstance, "LapCounter", lb.LapCounter);

            var driversDataField = leaderboardInstance.GetType().GetField("Drivers");
            var driversDataArrayType = driversDataField.FieldType;
            var driversDataType = driversDataArrayType.GetElementType();

            var driversInstance = Activator.CreateInstance(driversDataArrayType, lb.Drivers.Length);
            var x = driversInstance.GetType().GetMethod("Set");
            SetField(leaderboardInstance, "Drivers", driversInstance);

            for (var i = 0; i < lb.Drivers.Length; i++)
            {
                var driverInstance = CreateDriverInstance(driversDataType, lb.Drivers[i]);

                x.Invoke(driversInstance, new object[] { i, driverInstance });

            }
        }

        object CreateDriverInstance(Type driversDataType, OverlayData.Driver driver)
        {
            var driverInstance = Activator.CreateInstance(driversDataType);

            AssignDriverInstance(driverInstance, driver);

            return driverInstance;
        }

        void AssignDriverInstance(object driverInstance, OverlayData.Driver driver)
        {
            SetField(driverInstance, "CarNumber", driver.CarNumber);
            SetField(driverInstance, "UserName", driver.UserName);
            SetField(driverInstance, "PitStopCount", driver.PitStopCount);
            SetField(driverInstance, "ShortName", driver.ShortName);
            SetField(driverInstance, "Position", driver.Position);
            SetField(driverInstance, "DriverDetails", data.SessionData.DriverInfo.CompetingDrivers.FirstOrDefault(d => d.CarIdx == driver.CarIdx));
        }

        public void SetCamDriver()
        {
            var cd = data.CamDrivers.LastOrDefault(t => t.StartTime <= timestamp.FromNanoToSeconds());
            if (cd == null)
                cd = data.CamDrivers.First();

            CreateAndAssignInstance("CamDriver", cd == null, instance => AssignDriverInstance(instance, cd.CurrentDriver));
        }

        void SetMessageSet()
        {
            var cd = data.MessageStates.LastOrDefault(t => t.Time <= timestamp.FromNanoToSeconds());

            CreateAndAssignInstance("MessageSet", cd == null, instance =>
            {
                SetField(instance, "Time", cd.Time);
                SetField(instance, "Messages", cd.Messages);
            });
        }

        void SetFastLap()
        {
            var timeInSeconds = timestamp.FromNanoToSeconds();
            var cd = data.FastestLaps.LastOrDefault(t => t.StartTime <= timeInSeconds && t.StartTime + 15 > timeInSeconds);

            CreateAndAssignInstance("FastLap", cd == null, instance =>
            {
                SetField(instance, "Time", cd.Time);

                var driverType = instance.GetType().GetField("Driver").FieldType;
                SetField(instance, "Driver", CreateDriverInstance(driverType, cd.Driver));
            });
        }

        void SetField(object instance, string name, object value)
        {
            instance
                .GetType()
                .GetField(name)
                .SetValue(instance, value);
        }
        
        public void RaceOverlay()
        {
            plugin.RaceOverlay(timestamp);
        }
    }
}
