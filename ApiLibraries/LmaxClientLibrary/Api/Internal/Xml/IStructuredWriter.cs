/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;

namespace Com.Lmax.Api.Internal.Xml
{
    public interface IStructuredWriter
    {
        IStructuredWriter StartElement(string name);
        IStructuredWriter EndElement(string name);

        IStructuredWriter WriteEmptyTag(string name);
        
        IStructuredWriter ValueUTF8(string name, string value);
        
        IStructuredWriter ValueOrEmpty(string name, string value);
        IStructuredWriter ValueOrNone(string name, string value);        
    
        IStructuredWriter ValueOrEmpty(string name, long? value);
        IStructuredWriter ValueOrNone(string name, long? value);
    
        IStructuredWriter ValueOrEmpty(string name, decimal? value);
        IStructuredWriter ValueOrNone(string name, decimal? value);
    
        IStructuredWriter ValueOrEmpty(string name, bool value);            
    }
}
