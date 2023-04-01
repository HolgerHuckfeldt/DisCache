namespace DisCache
{
    using System;

    public class DisCacheConfiguration
    {
        /// <summary>
        /// Mandatory name of the cache
        /// </summary>
        public string CacheName { get; set; }

        /// <summary>
        /// Gets/Sets the IP for the client to be binded with
        /// </summary>
        public string BaseUri { get; set; }

        /// <summary>
        /// Clients operation timeout specified in seconds.
        /// Clients wait for the response from the server for this time. 
        /// If the response is not received within this time, the operation is not successful.
        /// Based on the network conditions, OperationTimeout value can be adjusted. 
        /// The default value is 90 seconds.
        /// </summary>
        public TimeSpan? ClientRequestTimeOut { get; set; }

        /// <summary>
        /// Client's connection timeout specified in seconds.
        /// </summary>
        public TimeSpan? ConnectionTimeout { get; set; }

        /// <summary>
        /// If different client applications are connected to server and because of any issue which results in connection failure with server, after the client again establishes connection “AppName” is used to identify these different client applications.
        ///Data type is string. Its optional.If value is not set it takes the value of the process id.
        /// </summary>
        public string AppName { get; set; }

        /// <summary>
        /// Time after which a new request forcefully releases the lock which is being held by an older request
        /// </summary>
        public double? RequestTimeout { get; set; }

        /// <summary>
        /// Should any exceptions that occured in the session state module be reported to event log
        /// </summary>
        public bool? WriteExceptionsToEventLog { get; set; }

        /// <summary>
        /// Should DisC logs be enabled
        /// </summary>
        public bool? EnableLogs { get; set; }

        /// <summary>
        /// Should DisC detailed logs be enabled
        /// </summary>
        public bool? EnableDetailLogs { get; set; }

        /// <summary>
        /// Should exceptions be thrown to the application if they occur
        /// </summary>
        public bool? ExceptionsEnabled { get; set; }

    }

}

