using System;
using System.Runtime.Serialization;

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// 
    /// </summary>
    public class SchedulingStrategyException : Exception
    {
        /// <summary>
        /// 
        /// </summary>
        public SchedulingStrategyException()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public SchedulingStrategyException(string message) : base(message)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public SchedulingStrategyException(string message, Exception innerException) : base(message, innerException)
        {
        }

    }
}