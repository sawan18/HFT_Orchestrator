using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockManager.Log
{
    /// <summary>
    /// Performs formatted Thread safe logging of events.  
    /// </summary>
    public class Logger
    {

        #region properties
        internal string LoggerFilename = "";
        #endregion

        #region constructor

        public Logger()
        {

        }

        /// <summary>
        /// Initialize the logger with a log path
        /// </summary>
        /// <param name="logPath"></param>
        public Logger(string logPath)
        {
            this.LoggerFilename = String.Format("{0}\\Log_{1}.txt", logPath, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));

            //Create the logging filename
            if (!File.Exists(this.LoggerFilename))
            {
                StreamWriter file = File.CreateText(this.LoggerFilename);
                file.Close();
            }

            System.Threading.Thread.Sleep(0);

        }

        #endregion

        #region public methods

        /// <summary>
        /// Logs a message
        /// </summary>
        /// <param name="message">Message to log.</param>
        public void LogMessage(string message)
        {
            using (StreamWriter streamWriter = new StreamWriter(this.LoggerFilename, true))
            {
                streamWriter.WriteLine(String.Format("{0}\t{1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), message));
                Console.WriteLine(message);
            }

            System.Threading.Thread.Sleep(0);
        }

        /// <summary>
        /// Prints a Gap in the log
        /// </summary>
        public void PrintGap()
        {
            using (StreamWriter streamWriter = new StreamWriter(this.LoggerFilename, true))
            {
                streamWriter.WriteLine("");
            }

            System.Threading.Thread.Sleep(0);
        }

        /// <summary>
        /// Logs error
        /// </summary>
        /// <param name="error">Error message</param>
        /// <param name="stackTrace">Detailed stack trace</param>
        public void LogError(string error, string stackTrace = "")
        {
            using (StreamWriter streamWriter = new StreamWriter(this.LoggerFilename, true))
            {
                streamWriter.WriteLine(String.Format("{0}\tERROR: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), error));
                Console.WriteLine(String.Format("ERROR: {0}", error));

                if (stackTrace != null && stackTrace.Trim() != "")
                {
                    streamWriter.WriteLine(String.Format("{0}", stackTrace));
                    Console.WriteLine(String.Format("Stack Trace: {0}", stackTrace));

                    streamWriter.WriteLine("");
                }
            }

            RunSettings.Instance.LastError = error;
            System.Threading.Thread.Sleep(0);
        }

        /// <summary>
        /// Writes the log header
        /// </summary>
        /// <param name="header"></param>
        public void WriteLogHeader(string header)
        {
            PrintGap();

            using (StreamWriter streamWriter = new StreamWriter(this.LoggerFilename, true))
            {
                streamWriter.WriteLine(String.Format("{0}", header));
                Console.WriteLine(header.ToUpper());
                streamWriter.WriteLine("==================================================");
                streamWriter.WriteLine("");
            }

            System.Threading.Thread.Sleep(0);
        }
        #endregion


    }
}
