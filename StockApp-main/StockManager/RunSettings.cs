using StockManager.Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockManager
{
    /// <summary>
    /// Run settings for the application
    /// </summary>
    public sealed class RunSettings
    {
        private static volatile RunSettings instance;
        private static object syncRoot = new Object();
        private Logger logger = new Logger();

        private RunSettings()
        {
            StartTime = DateTime.Now;
            Server = "localhost";
            Database = "StockManager";
        }

        private string metaDataFolder = @"c:\StockApp";

        /// <summary>
        /// Gets and sets the application meta data folder
        /// </summary>
        public string MetaDataFolder
        {
            get { return metaDataFolder; }
            set { metaDataFolder = value; }
        }

        /// <summary>
        /// Gets the start time for the current processing run
        /// </summary>
        public DateTime StartTime
        {
            private set;
            get;
        }

        /// <summary>
        /// Gets and sets the last error.
        /// </summary>
        public String LastError
        {
            set;
            get;
        }

        public String Server
        {
            get;
            set;
        }

        public String Database
        {
            get;
            set;
        }

        public bool ProcessTrade
        {
            get;
            set;
        }

        public double StopLossPercentage
        {
            get;
            set;
        }

        /// <summary>
        /// Returns only one instance for all classes
        /// </summary>
        public static RunSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new RunSettings();
                    }
                }

                return instance;
            }
        }

        /// <summary>
        /// Gets the logger 
        /// </summary>
        public Logger Log
        {
            get
            {
                if (logger.LoggerFilename == "")
                {
                    string path = String.Format("{0}\\Logs", this.metaDataFolder);

                    //If this directory doesn't exist, create it
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);

                    this.logger = new Logger(path);
                }

                return logger;
            }
        }
    }

    public static class DateTimeExtensions
    {
        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }
    }
}
