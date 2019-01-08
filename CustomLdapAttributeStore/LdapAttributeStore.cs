using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.IdentityServer.ClaimsPolicy.Engine.AttributeStore;
using System.IdentityModel;
using System.DirectoryServices.Protocols;
using System.Diagnostics;
using System.Net;

/*****************************************************************************

MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. 
*****************************************************************************/
/// <summary>
/// Custom Attribute Store that queries LDAP for group membership when passed a username in a custom claims issuance rule. Example usage is below:
/// 
/// c:[Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/email"] => issue(store = "CustomLdapAttributeStore", types = ("userGroups"), query = "ldapGroups", param = c.Value);
/// </summary>
namespace CustomLdapAttributeStore
{
    public class LdapAttributeStore : IAttributeStore
    {

        private string ldapHost;
        private int ldapPort;
        private string ldapUser;
        private string ldapPassword;

        public IAsyncResult BeginExecuteQuery(string query, string[] parameters, AsyncCallback callback, object state)
        {
            if (String.IsNullOrEmpty(query))
            {
                throw new AttributeStoreQueryFormatException("No query string.");
            }

            if (parameters == null)
            {
                throw new AttributeStoreQueryFormatException("No query parameter.");
            }

            if (parameters.Length != 1)
            {
                throw new AttributeStoreQueryFormatException("More than one query parameter.");
            }

            string inputString = parameters[0];

            if (inputString == null)
            {
                throw new AttributeStoreQueryFormatException("Query parameter cannot be null.");
            }

            List<string> groupNames = new List<string>();
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

            switch (query)
            {
                case "ldapGroups":
                    {
                        try
                        {
                            LdapDirectoryIdentifier ldapDirectoryIdentifier = new LdapDirectoryIdentifier(ldapHost, ldapPort);
                            NetworkCredential networkCredential = new NetworkCredential(ldapUser, ldapPassword);
                            LdapConnection ldapConnection = new LdapConnection(ldapDirectoryIdentifier, networkCredential, AuthType.Basic);
                            ldapConnection.SessionOptions.ProtocolVersion = 3;

                            // Look for a user based upon the email address. From this we will get the Distinguished Name
                            SearchRequest userRequest = new SearchRequest(null, "(&(objectClass=dominoperson)(mail=" + inputString + "))", SearchScope.Subtree, new string[] { "cn" });
                            SearchResponse user = ldapConnection.SendRequest(userRequest) as SearchResponse;
                            if (user.Entries[0] != null)
                            {
                                SearchRequest request = new SearchRequest(null, "(member=" + user.Entries[0].DistinguishedName + ")", SearchScope.Subtree, new string[] { "cn", "dominoAccessGroups" });
                                SearchResponse searchResponse = ldapConnection.SendRequest(request) as SearchResponse;

                                foreach (SearchResultEntry entry in searchResponse.Entries)
                                {
                                    if (entry.Attributes["dominoAccessGroups"] != null)
                                    {
                                        groupNames.Add(entry.Attributes["dominoAccessGroups"][0].ToString().Replace("CN=", ""));  
                                    }
                                    List<string> group = new List<string>();
                                    groupNames.Add(entry.Attributes["cn"][0] + "");
                                }
                                groupNames = groupNames.Distinct().ToList();
                                Trace.WriteLine(string.Join(", ", groupNames.ToArray()));
                            }
                            else
                            {
                                // No user could be found from the email address
                                throw new AttributeStoreQueryFormatException("No user could be found for the email address '" + inputString + "'.");
                            }
                        }
                        catch (Exception e)
                        {
                            using (EventLog eventLog = new EventLog("Application"))
                            {
                                eventLog.Source = "CustomLdapAttributeStore";
                                eventLog.WriteEntry("Error whilst retrieving groups for user " + inputString + ", " + e.Message + "%n" + e.StackTrace, EventLogEntryType.Error, 101, 1);
                            }
                            // Throw the error up the stack to ensure ADFS is aware that there's a problem
                            throw e;
                        }

                        break;
                    }
                default:
                    {
                        throw new AttributeStoreQueryFormatException("The query string is not supported.");
                    }
            }

            // Convert the results into the correct format to return from this interface
            List<string[]> claimData = new List<string[]>();
            foreach (string name in groupNames)
            {
                claimData.Add(new string[1] { name });
            }

            TypedAsyncResult<string[][]> asyncResult = new TypedAsyncResult<string[][]>(callback, state);
            asyncResult.Complete(claimData.ToArray(), true);
            return asyncResult;

        }

        public string[][] EndExecuteQuery(IAsyncResult result)
        {
            return TypedAsyncResult<string[][]>.End(result);
        }

        public void Initialize(Dictionary<string, string> config)
        {
            this.ldapHost = config["host"];
            this.ldapPort = Int32.Parse(config["port"]);
            this.ldapUser = config["user"];
            this.ldapPassword = config["password"];
        }
    }
}
