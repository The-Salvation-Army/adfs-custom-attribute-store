using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CustomLdapAttributeStore;
using System.Diagnostics;

namespace AttributeStoreTest
{
    [TestClass]
    public class AttributeStoreTests
    {

        LdapAttributeStore attr = new LdapAttributeStore();

        [TestInitialize]
        public void Initialize()
        {
           
            this.attr.Initialize(null);
           

        }

        [TestMethod]
        public void TestLdapSearch()
        {
            attr.BeginExecuteQuery("ldapGroups", new string[] { "test@test.com" }, null, null);
                 
                

        }
    }
}
