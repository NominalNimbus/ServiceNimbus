/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;

namespace Com.Lmax.Api
{
    /// <summary>
    /// Generalised failure response that is produced whenever a request to the API
    /// is made.  Indicates the failure response and a descriptive message along with
    /// a flag to indicate if this is a system failure (e.g. connection error) or an
    /// application failure (e.g. invalid price).
    /// </summary>
    public class FailureResponse
    {
        private readonly bool _isSystemFailure;
        private readonly string _message;
        private readonly string _description;
        private readonly Exception _exception;

        ///<summary>
        /// Constructs a failure response object with all values.
        ///</summary>
        ///<param name="isSystemFailure">True if the problem is with the system, e.g. a network problem</param>
        ///<param name="message">Short message of the failure</param>
        ///<param name="description">Readable description of the problem</param>
        ///<param name="exception">The captured Exception</param>
        public FailureResponse(bool isSystemFailure, string message, string description, Exception exception)
        {
            _isSystemFailure = isSystemFailure;
            _message = message;
            _description = description;
            _exception = exception;
        }

        ///<summary>
        /// Constructs a failure response object
        ///</summary>
        ///<param name="isSystemFailure">True if the problem is with the system, e.g. a network problem</param>
        ///<param name="message">Short message of the failure</param>
        public FailureResponse(bool isSystemFailure, string message)
            : this(isSystemFailure, message, "", null)
        {
        }

        ///<summary>
        /// Constructs a failure response object 
        ///</summary>
        ///<param name="description">Readable description of the problem</param>
        ///<param name="exception">The captured Exception</param>
        public FailureResponse(Exception exception, string description)
            : this(true, exception.Message, description, exception)
        {
        }
        
        /// <summary>
        /// A readonly property that indicates that this failure was caused 
        /// by some sort of system failure, most likely a connection error.
        /// </summary>
        public bool IsSystemFailure
        {
            get { return _isSystemFailure; }
        }

        /// <summary>
        /// A readonly property that contains the error message that occured.
        /// </summary>
        public string Message
        {
            get { return _message; }
        }

        /// <summary>
        /// A readonly property that a more user friendly description of the error, may be
        /// an empty string.
        /// </summary> 
        public string Description
        {
            get { return _description; }
        }
  
        /// <summary>
        /// A readonly property that holds an exception, if this failiure was caused by
        /// an exception.  Will be null otherwise.
        /// </summary> 
        public Exception Exception
        {
            get { return _exception; }
        }

        public override string ToString()
        {
            return string.Format("IsSystemFailure: {0}, Message: {1}, Description: {2}, Exception: {3}", _isSystemFailure, _message, _description, _exception);
        }
    }
}
