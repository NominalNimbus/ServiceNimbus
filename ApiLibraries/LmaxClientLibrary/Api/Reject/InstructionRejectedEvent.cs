/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;

namespace Com.Lmax.Api.Reject
{
    /// <summary>
    /// An event that contains the detail of the rejection of an instruction.
    /// E.g. INSUFFICIENT_LIQUIDITY.
    /// </summary>
    public class InstructionRejectedEvent : IEquatable<InstructionRejectedEvent>
    {
        private readonly string _instructionId;
        private readonly long _accountId;
        private readonly long _instrumentId;
        private readonly String _reason;

        ///<summary>
        /// Constructs an event containing the details of why an instruction was rejected
        ///</summary>
        ///<param name="instructionId">The ID of the instruction that was rejected</param>
        ///<param name="accountId">The ID of the account that sent the instruction</param>
        ///<param name="instrumentId">The ID of the instrument the instruction related to</param>
        ///<param name="reason">The description of why the instruction was rejected</param>
        public InstructionRejectedEvent(string instructionId, long accountId, long instrumentId, string reason)
        {
            _instructionId = instructionId;
            _reason = reason;
            _instrumentId = instrumentId;
            _accountId = accountId;
        }

        /// <summary>
        /// Get the id of the instruction that was rejected.
        /// </summary>
        public string InstructionId
        {
            get { return _instructionId; }
        }
        
        /// <summary>
        /// Get the id of the account that placed the instruction.
        /// </summary>
        public long AccountId
        {
            get { return _accountId; }
        }
        
        /// <summary>
        /// Get the id of the instrument that the instruction was placed on.
        /// </summary>
        public long InstrumentId
        {
            get { return _instrumentId; }
        }
        
        /// <summary>
        /// Get the reason for the rejection, e.g. INSUFFICIENT_LIQUIDITY
        /// </summary>
        public string Reason
        {
            get { return _reason; }
        }

        public override string ToString()
        {
            return string.Format("InstructionRejectedEvent{{InstructionId: {0}, AccountId: {1}, InstrumentId: {2}, Reason: {3}}}", _instructionId, _accountId, _instrumentId, _reason);
        }

        public bool Equals(InstructionRejectedEvent other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other._instructionId == _instructionId && other._accountId == _accountId && other._instrumentId == _instrumentId && Equals(other._reason, _reason);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (InstructionRejectedEvent)) return false;
            return Equals((InstructionRejectedEvent) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = _instructionId.GetHashCode();
                result = (result*397) ^ _accountId.GetHashCode();
                result = (result*397) ^ _instrumentId.GetHashCode();
                result = (result*397) ^ (_reason != null ? _reason.GetHashCode() : 0);
                return result;
            }
        }

        public static bool operator ==(InstructionRejectedEvent left, InstructionRejectedEvent right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(InstructionRejectedEvent left, InstructionRejectedEvent right)
        {
            return !Equals(left, right);
        }
    }
}
