using System;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;

namespace CasinoRoyale.Utils
{
    public static class Logger
    {
        private static readonly string LogDirectory = "Logs";
        private static readonly string LogFileName = "debug.log";
        private static readonly object LockObject = new object();
        private static bool IsInitialized = false;

        public enum LogLevel
        {
            DEBUG,
            INFO,
            WARNING,
            ERROR
        }

        public static void Initialize()
        {
            lock (LockObject)
            {
                if (!IsInitialized)
                {
                    // Create logs directory if it doesn't exist
                    if (!Directory.Exists(LogDirectory))
                    {
                        Directory.CreateDirectory(LogDirectory);
                    }

                    // Clear the log file on each session start
                    string logPath = Path.Combine(LogDirectory, LogFileName);
                    if (File.Exists(logPath))
                    {
                        File.Delete(logPath);
                    }

                    IsInitialized = true;
                    Log(LogLevel.INFO, "Logger initialized - new session started");
                }
            }
        }

        public static void Log(LogLevel level, string message)
        {
            if (!IsInitialized)
            {
                Initialize();
            }

            lock (LockObject)
            {
                try
                {
                    string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
                    
                    // Write to file
                    string logPath = Path.Combine(LogDirectory, LogFileName);
                    File.AppendAllText(logPath, logEntry + Environment.NewLine, Encoding.UTF8);

                    // Also write to console for important messages
                    if (level == LogLevel.ERROR || level == LogLevel.WARNING)
                    {
                        Console.WriteLine(logEntry);
                    }
                }
                catch (Exception ex)
                {
                    // Fallback to console if file logging fails
                    Console.WriteLine($"[LOGGER ERROR] Failed to write to log file: {ex.Message}");
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}");
                }
            }
        }

        public static void Debug(string message)
        {
            Log(LogLevel.DEBUG, message);
        }

        public static void Info(string message)
        {
            Log(LogLevel.INFO, message);
        }

        public static void Warning(string message)
        {
            Log(LogLevel.WARNING, message);
        }

        public static void Error(string message)
        {
            Log(LogLevel.ERROR, message);
        }

        public static void LogMovement(uint playerId, string action, Vector2 oldCoords, Vector2 newCoords, string additionalInfo = "")
        {
            string message = $"Player {playerId} {action}: {oldCoords} -> {newCoords}";
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                message += $" ({additionalInfo})";
            }
            Debug(message);
        }

        public static void LogHitbox(uint playerId, Rectangle hitbox, string context = "")
        {
            string message = $"Player {playerId} hitbox: {hitbox}";
            if (!string.IsNullOrEmpty(context))
            {
                message += $" ({context})";
            }
            Debug(message);
        }

        public static void LogCollision(uint playerId, string collisionType, string details)
        {
            Debug($"Player {playerId} collision - {collisionType}: {details}");
        }

        public static void LogNetwork(string component, string message)
        {
            Info($"[{component}] {message}");
        }

        public static void LogPhysics(uint playerId, string physicsEvent, string details)
        {
            Debug($"Player {playerId} physics - {physicsEvent}: {details}");
        }
    }
}
