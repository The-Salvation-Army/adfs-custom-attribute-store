# CustomLdapAttributeSore

This is a custom attribute store for use in ADFS that allows a user's group memberships to be retrieved from an LDAP directory. 

## Installation

Build the project and copy the resulting 'CustomLdapAttributeStore.dll' into the ADFS directory on the server (usually c:\Windows\ADFS). The attribute store can then be added via the ADFS admin UI. The attribute store class name should be set to 'CustomLdapAttributeStore.LdapAttributeStore,CustomLdapAttributeStore'. It requires the following initailisation parameters to be set:

| Name  | Value   |
|---|---|
| host  | The domain name of your LDAP server  |
| port  | The port on which LDAP is available  |
| user  | The LDAP username (bind DN)          |
| password  | The LDAP password          |

## Usage

In order to add LDAP group membership to the outgoing claims, a claims issuance transformation rule needs to be added that matches the following:

```
c:[Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/email"]
 => issue(store = "CustomLdapAttributeStore", types = ("userGroups"), query = "ldapGroups", param = c.Value);
```

This assumes that the `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/email` type was selected when mapping LDAP attributes to ADFS when the LDAP LocalClaimsProviderTrust was created.
