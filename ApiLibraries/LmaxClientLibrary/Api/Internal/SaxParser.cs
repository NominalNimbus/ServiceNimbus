/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System.IO;
using System.Xml;

namespace Com.Lmax.Api.Internal
{
    public class SaxParser : IXmlParser
    {
        public void Parse(TextReader reader, ISaxContentHandler saxContentHandler)
        {
            var settings = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment };
            var xmlReader = XmlReader.Create(reader, settings);

            while (xmlReader.Read())
            {
                if (xmlReader.HasValue)
                {
                    saxContentHandler.Content(xmlReader.Value);
                }
                else
                {
                    if (xmlReader.IsEmptyElement)
                    {
                        saxContentHandler.StartElement(xmlReader.Name);
                        saxContentHandler.EndElement(xmlReader.Name);
                    }
                    else if (xmlReader.IsStartElement())
                    {
                        saxContentHandler.StartElement(xmlReader.Name);
                    }
                    else
                    {
                        saxContentHandler.EndElement(xmlReader.Name);
                    }
                }
            }
        }
    }
}
