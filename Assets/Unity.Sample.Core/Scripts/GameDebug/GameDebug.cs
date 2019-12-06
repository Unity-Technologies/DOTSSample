using System;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Unity.Sample.Core
{
    public struct WorldId
    {
        public int Value;

        public static WorldId Undefined => new WorldId();

        public static implicit operator WorldId(World world) => new WorldId { Value = world != null ? (int)world.SequenceNumber : 0 };
        public static implicit operator int(WorldId worldId) => worldId.Value;

        public static World FindWorld(WorldId worldId)
        {
            for (int i = 0; i < World.AllWorlds.Count; i++)
            {
                if (worldId == (WorldId) World.AllWorlds[i])
                    return World.AllWorlds[i];
            }

            return null;
        }


    }

    //
    // Logging of messages
    //
    // There are three different types of messages:
    //
    // Debug.Log/Warn/Error coming from unity (or code, e.g. packages, not using GameDebug)
    //    These get caught here and sent onto the console and into our log file
    // GameDebug.Log/Warn/Error coming from game
    //    These gets sent onto the console and into our log file
    //    *IF* we are in editor, they are also sent to Debug.* so they show up in editor Console window
    // Console.Write
    //    Only used for things that should not be logged. Typically reponses to user commands. Only shown on Console.
    //

    public static class GameDebug
    {
        static System.IO.StreamWriter logFile = null;

        private static int s_frameCount;

        static bool forwardToDebug = true;
        public static void Init(string logfilePath, string logBaseName, bool forceForwardToDebug)
        {
            forwardToDebug = Application.isEditor || Application.isMobilePlatform || forceForwardToDebug;
            Application.logMessageReceived += LogCallback;

            // Try creating logName; attempt a number of suffixxes
            string name = "";
            for (var i = 0; i < 10; i++)
            {
                name = logBaseName + (i == 0 ? "" : "_" + i) + ".log";
                try
                {
                    logFile = System.IO.File.CreateText(logfilePath + "/" + name);
                    logFile.AutoFlush = true;
                    break;
                }
                catch
                {
                    name = "<none>";
                }
            }
            GameDebug.Log("GameDebug initialized. Logging to " + logfilePath + "/" + name);
        }

        public static void SetFrameCount(int frameCount)
        {
            s_frameCount = frameCount;
        }
        
        public static void Shutdown()
        {
            Application.logMessageReceived -= LogCallback;
            if (logFile != null)
                logFile.Close();
            logFile = null;
        }

        static void LogCallback(string message, string stack, LogType logtype)
        {
            switch (logtype)
            {
                default:
                case LogType.Log:
                    GameDebug._Log(message);
                    break;
                case LogType.Warning:
                    GameDebug._LogWarning(message);
                    break;
                case LogType.Error:
                    GameDebug._LogError(message);
                    break;
            }
        }

        [BurstDiscard]
        public static void Log(string message)
        {
            if (forwardToDebug)
                Debug.Log(s_frameCount + ": " + message);
            else
                _Log(message);
        }

        [BurstDiscard]
        public static void Log<T>(string format, T arg1)
        {
            Log(string.Format(format.ToString(), arg1));
        }

        [BurstDiscard]
        public static void Log<T1,T2>(string format, T1 arg1, T2 arg2)
        {
            Log(string.Format(format.ToString(), arg1, arg2));
        }

        [BurstDiscard]
        public static void Log<T1,T2,T3>(string format, T1 arg1, T2 arg2, T3 arg3)
        {
            Log(string.Format(format.ToString(), arg1, arg2, arg3));
        }

        [BurstDiscard]
        public static void Log<T1,T2,T3,T4>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            Log(string.Format(format.ToString(), arg1, arg2, arg3, arg4));
        }

        [BurstDiscard]
        public static void Log<T1,T2,T3,T4,T5>( string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            Log(string.Format(format.ToString(), arg1, arg2, arg3, arg4, arg5));
        }

        [BurstDiscard]
        public static void Log<T1,T2,T3,T4,T5,T6>( string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            Log(string.Format(format.ToString(), arg1, arg2, arg3, arg4, arg5, arg6));
        }

        [BurstDiscard]
        public static void Log<T1,T2,T3,T4,T5,T6,T7>( string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            Log(string.Format(format.ToString(), arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }


        // TODO (mogensh) use NativeString64 for all strings (so burst can compile)

        [BurstDiscard]
        public static void Log(ConfigVar configVar, string message)
        {
            if (configVar.IntValue == 0)
                return;

            Log( message.ToString());
        }

        [BurstDiscard]
        public static void Log<T>(ConfigVar configVar, string format, T arg1)
        {
            if (configVar != null && configVar.IntValue == 0)
                return;

            Log(string.Format(format.ToString(), arg1));
        }

        [BurstDiscard]
        public static void Log<T1,T2>(ConfigVar configVar, string format, T1 arg1, T2 arg2)
        {
            if (configVar != null && configVar.IntValue == 0)
                return;

            Log(string.Format(format.ToString(), arg1, arg2));
        }

        [BurstDiscard]
        public static void Log<T1,T2,T3>(ConfigVar configVar, string format, T1 arg1, T2 arg2, T3 arg3)
        {
            if (configVar != null && configVar.IntValue == 0)
                return;

            Log(string.Format(format.ToString(), arg1, arg2, arg3));
        }

        [BurstDiscard]
        public static void Log<T1,T2,T3,T4>(ConfigVar configVar, string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (configVar != null && configVar.IntValue == 0)
                return;

            Log(string.Format(format.ToString(), arg1, arg2, arg3, arg4));
        }

        [BurstDiscard]
        public static void Log<T1,T2,T3,T4,T5>(ConfigVar configVar, string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (configVar != null && configVar.IntValue == 0)
                return;

            Log(string.Format(format.ToString(), arg1, arg2, arg3, arg4, arg5));
        }

        [BurstDiscard]
        public static void Log<T1,T2,T3,T4,T5,T6>(ConfigVar configVar, string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            if (configVar != null && configVar.IntValue == 0)
                return;

            Log(string.Format(format.ToString(), arg1, arg2, arg3, arg4, arg5, arg6));
        }

        [BurstDiscard]
        public static void Log<T1,T2,T3,T4,T5,T6,T7>(ConfigVar configVar, string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            if (configVar != null && configVar.IntValue == 0)
                return;

            Log(string.Format(format.ToString(), arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }


        static string GetColorStringFromHash(uint hash)
        {
            var rnd = new Unity.Mathematics.Random();
            rnd.InitState(hash);

            var color = Color.HSVToRGB(rnd.NextFloat(0.1f,0.9f),rnd.NextFloat(0.5f,1),rnd.NextFloat(0.5f,1));
            var colorStr = ColorUtility.ToHtmlStringRGB(color);
            return colorStr;
        }


        [BurstDiscard]
        static void InternalLog( WorldId worldId, ConfigVar configVar, string message)
        {
            var msg = message;

            if (configVar != null)
            {
                var configColor = GetColorStringFromHash((uint)configVar.name.GetHashCode());
                msg = "<color=#" + configColor + "><" + configVar.name + "></color>" + msg;
            }


            if (worldId != WorldId.Undefined)
            {
                var world = WorldId.FindWorld(worldId);
                var hash = MathHelper.hash(worldId);
                var worldColor = GetColorStringFromHash(hash);
                msg = "<color=#" + worldColor + "><" + world?.Name + "></color>" + msg;
            }
            else
            {
                msg = "<NOWORLD>" + msg;
            }

            if (forwardToDebug)
                Debug.Log(s_frameCount + ": " + msg);
            else
                _Log(msg);
        }



        [BurstDiscard]
        public static void Log(WorldId worldId,ConfigVar configVar, string message)
        {
            if (configVar != null && configVar.IntValue == 0)
                return;

            InternalLog(worldId, configVar, message.ToString());
        }

        [BurstDiscard]
        public static void Log<T>(WorldId worldId,ConfigVar configVar, string format, T arg1)
        {
            if (configVar != null && configVar.IntValue == 0)
                return;

            InternalLog(worldId,configVar, string.Format(format.ToString(), arg1));
        }

        [BurstDiscard]
        public static void Log<T1,T2>(WorldId worldId,ConfigVar configVar, string format, T1 arg1, T2 arg2)
        {
            if (configVar != null && configVar.IntValue == 0)
                return;

            InternalLog(worldId,configVar,string.Format(format.ToString(), arg1, arg2));
        }

        [BurstDiscard]
        public static void Log<T1,T2,T3>(WorldId worldId,ConfigVar configVar, string format, T1 arg1, T2 arg2, T3 arg3)
        {
            if (configVar != null && configVar.IntValue == 0)
                return;

            InternalLog(worldId,configVar,string.Format(format.ToString(), arg1, arg2, arg3));
        }

        [BurstDiscard]
        public static void Log<T1,T2,T3,T4>(WorldId worldId,ConfigVar configVar, string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (configVar != null && configVar.IntValue == 0)
                return;

            InternalLog(worldId,configVar,string.Format(format.ToString(), arg1, arg2, arg3, arg4));
        }

        [BurstDiscard]
        public static void Log<T1,T2,T3,T4,T5>(WorldId worldId,ConfigVar configVar, string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (configVar != null && configVar.IntValue == 0)
                return;

            var f = format.ToString();
            InternalLog(worldId,configVar,string.Format(format.ToString(), arg1, arg2, arg3, arg4, arg5));
        }

        [BurstDiscard]
        public static void Log<T1,T2,T3,T4,T5,T6>(WorldId worldId,ConfigVar configVar, string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            if (configVar != null && configVar.IntValue == 0)
                return;

            InternalLog(worldId,configVar,string.Format(format.ToString(), arg1, arg2, arg3, arg4, arg5, arg6));
        }

        [BurstDiscard]
        public static void Log<T1,T2,T3,T4,T5,T6,T7>(WorldId worldId,ConfigVar configVar, string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            if (configVar != null && configVar.IntValue == 0)
                return;

            InternalLog(worldId,configVar,string.Format(format.ToString(), arg1, arg2, arg3, arg4, arg5, arg6, arg7));
        }

        static void _Log(string message)
        {
            Console.Write(s_frameCount + ": " + message);
            if (logFile != null)
                logFile.WriteLine(s_frameCount + ": " + message + "\n");
        }

        [BurstDiscard]
        public static void LogError(string message)
        {
            if (forwardToDebug)
                Debug.LogError(message);
            else
                _LogError(message);
        }

        [BurstDiscard]
        public static void LogError(World world, string message)
        {
            message = "<" + world + ">" + message;
            if (forwardToDebug)
                Debug.LogError(message);
            else
                _LogError(message);
        }


        [BurstDiscard]
        public static void LogWarning(string message)
        {
            if (forwardToDebug)
                Debug.LogWarning(message);
            else
                _LogWarning(message);
        }

        [BurstDiscard]
        public static void LogWarning(World world, string message)
        {
            message = "<" + world + ">" + message;
            if (forwardToDebug)
                Debug.LogWarning(message);
            else
                _LogWarning(message);
        }

        static void _LogError(string message)
        {
            Console.Write(s_frameCount + ": [ERR] " + message);
            if (logFile != null)
                logFile.WriteLine("[ERR] " + message + "\n");
        }

        static void _LogWarning(string message)
        {
            Console.Write(s_frameCount + ": [WARN] " + message);
            if (logFile != null)
                logFile.WriteLine("[WARN] " + message + "\n");
        }

        public static void Assert(bool condition)
        {
            if (!condition)
                throw new ApplicationException("GAME ASSERT FAILED");
        }

        public static void Assert(bool condition, string msg)
        {
            if (!condition)
                throw new ApplicationException("GAME ASSERT FAILED : " + msg);
        }

        public static void Assert<T>(bool condition, string format, T arg1)
        {
            if (!condition)
                throw new ApplicationException("GAME ASSERT FAILED : " + string.Format(format, arg1));
        }

        public static void Assert<T1, T2>(bool condition, string format, T1 arg1, T2 arg2)
        {
            if (!condition)
                throw new ApplicationException("GAME ASSERT FAILED : " + string.Format(format, arg1, arg2));
        }

        public static void Assert<T1, T2, T3>(bool condition, string format, T1 arg1, T2 arg2, T3 arg3)
        {
            if (!condition)
                throw new ApplicationException("GAME ASSERT FAILED : " + string.Format(format, arg1, arg2, arg3));
        }

        public static void Assert<T1, T2, T3, T4>(bool condition, string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (!condition)
                throw new ApplicationException("GAME ASSERT FAILED : " + string.Format(format, arg1, arg2, arg3, arg4));
        }

        public static void Assert<T1, T2, T3, T4, T5>(bool condition, string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            if (!condition)
                throw new ApplicationException("GAME ASSERT FAILED : " + string.Format(format, arg1, arg2, arg3, arg4, arg5));
        }



    }
}


