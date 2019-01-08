using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CustomLdapAttributeStore;
using System.Diagnostics;
using System.Collections.Generic;

namespace AttributeStoreTest
{
    [TestClass]
    public class AttributeStoreTests
    {

        LdapAttributeStore attr = new LdapAttributeStore();

        [TestInitialize]
        public void Initialize()
        {
            Dictionary<string, string> config = new Dictionary<string, string>()
            {
                {"host", "YOUR HOST" },
                {"port", "636" },
                { "user", "YOUR USERNAME"},
                { "password", "YOUR PASSWORD"}
            };
            this.attr.Initialize(config);
        }

        [TestMethod]
        public void TestLdapSearch()
        {
            attr.BeginExecuteQuery("ldapGroups", new string[] { "test@test.com" }, null, null);
        }
    }
}
