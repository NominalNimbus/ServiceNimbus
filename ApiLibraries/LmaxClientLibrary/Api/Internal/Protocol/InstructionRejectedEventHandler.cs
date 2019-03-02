/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using Com.Lmax.Api.Reject;

namespace Com.Lmax.Api.Internal.Protocol
{
    public class InstructionRejectedEventHandler : DefaultHandler
    {
        private const string RootNodeName = "instructionRejected";
        private const string ReasonNodeName = "reason";
        private const string InstrumentIdNodeName = "instrumentId";
        private const string AccountIdNodeName = "accountId";
        private const string InstructionIdNodeName = "instructionId";

        public event OnRejectionEvent RejectionEventListener;

        public InstructionRejectedEventHandler()
            : base(RootNodeName)
        {
            AddHandler(ReasonNodeName);
            AddHandler(InstrumentIdNodeName);
            AddHandler(AccountIdNodeName);
            AddHandler(InstructionIdNodeName);
        }

        public override void EndElement(string endElement)
        {
            if (RootNodeName.Equals(endElement) && RejectionEventListener != null)
            {
                string instructionId;
                long instrumentId;
                long accountId;

                TryGetValue(InstructionIdNodeName, out instructionId);
                TryGetValue(InstrumentIdNodeName, out instrumentId);
                TryGetValue(AccountIdNodeName, out accountId);

                RejectionEventListener(new InstructionRejectedEvent(instructionId, accountId, instrumentId, GetStringValue(ReasonNodeName)));
            }
        }
    }
}
