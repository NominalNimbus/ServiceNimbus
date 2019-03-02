/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using Com.Lmax.Api.Internal.Xml;

namespace Com.Lmax.Api
{
    /// <summary>
    /// Contains the necessary credential information and product type required
    /// connect to the LMAX Trader platform.
    /// </summary>
    public class LoginRequest : IRequest
    {
        private const string ProtocolVersion = "1.8";
        private const string LoginUri = "/public/security/login";        

        private readonly string _username;
        private readonly string _password;
        private readonly ProductType _productType;
        private readonly bool _checkProtocolVersion;

        /// <summary>
        /// Construct a login request with the appropriate credential, product type and choose whether or not check the protocol version.
        /// </summary>
        /// <param name="username">
        /// A <see cref="System.String"/> contains the username.
        /// </param>
        /// <param name="password">
        /// A <see cref="System.String"/> contains the password.
        /// </param>
        /// <param name="productType">
        /// A <see cref="ProductType"/> either CFD_DEMO for testapi and CFD_LIVE for
        /// production.
        /// </param>
        /// <param name="checkProtocolVersion">
        /// A <see cref="System.Boolean"/> to ensure that the protocol version used by the client and server are the same.
        /// Setting this to <para><b>false</b></para> may cause errors or incorrect behaviour due to protocol changes.
        /// </param>
        public LoginRequest(string username, string password, ProductType productType, bool checkProtocolVersion)
        {
            _username = username;
            _password = password;
            _productType = productType;
            _checkProtocolVersion = checkProtocolVersion;
        }

        /// <summary>
        /// Construct a login request with the appropriate credential and product type. 
        /// </summary>
        /// <param name="username">
        /// A <see cref="System.String"/> contains the username.
        /// </param>
        /// <param name="password">
        /// A <see cref="System.String"/> contains the password.
        /// </param>
        /// <param name="productType">
        /// A <see cref="ProductType"/> either CFD_DEMO for testapi and CFD_LIVE for
        /// production.
        /// </param>
        public LoginRequest(string username, string password, ProductType productType)
            : this(username, password, productType, true)
        {
        }

        /// <summary>
        /// Construct a login request with the appropriate credential.  
        /// Product type will default to CFD_LIVE.
        /// </summary>
        /// <param name="username">
        /// A <see cref="System.String"/> contains the username.
        /// </param>
        /// <param name="password">
        /// A <see cref="System.String"/> contains the password.
        /// </param>
        public LoginRequest(string username, string password)
            : this(username, password, ProductType.CFD_LIVE, true)
        {
        }

  
        /// <summary>
        /// The URI for the login request. 
        /// </summary>
        public string Uri { get { return LoginUri; } }

        /// <summary>
        /// Internal: Output this request.
        /// </summary>
        /// <param name="writer">The destination for the content of this request</param>
        public void WriteTo(IStructuredWriter writer)
        {
            writer.
                StartElement("req").
                StartElement("body").
                ValueUTF8("username", _username).
                ValueUTF8("password", _password);
            if (_checkProtocolVersion)
            {
                writer.ValueOrNone("protocolVersion", ProtocolVersion);
            }
            writer.ValueOrNone("productType", _productType.ToString()).
                    EndElement("body").
                    EndElement("req");
        }
    }
 
    /// <summary>
    /// The product type used to connect to the LMAX Trader platform. 
    /// </summary>
    public enum ProductType
    {
        ///<summary>
        /// Selected if connecting to the production environment
        ///</summary>
        CFD_LIVE,
        ///<summary>
        /// Selected if connecting to the test environment
        ///</summary>
        CFD_DEMO
    }
}
