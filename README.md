# Argentini.NameCheap

This .NET project builds a command line interface (CLI) application that can add and remove text records using the NameCheap API.

It was originally built to allow for creating wildcard TLS certificates using *win-acme* (Let's Encrypt) on an IIS server. Creating wildcard certificates with *win-acme* requires DNS host validation. This application can be used with the *win-acme* script feature to allow it to communicate with the NameCheap API and create/delete TXT records that will validate domain ownership.

**This tool does not support the complete NameCheap API.** But it does handle the challenging task of adding and removing text records. Why is this challenging? The NameCheap API does not have functions to add or remove individual records, so the entire set of records must be downloaded, modified, and sent back.

## How to Install

Download the project and publish it from the root project folder as below.

```
dotnet publish Argentini.NameCheap/Argentini.NameCheap.csproj -o publish -p:PublishSingleFile=true -c Release -r win-x64 --self-contained
```

In the publish folder, edit the `appsettings.json` file and supply your own values.

```json
{
    "NameCheap": {

        "ApiKey": "{your namecheap API key}",
        "UserName": "{your namecheap username}",
        "ApiUserName": "{your namecheap API username}",
        "ClientIP": "{a whitelisted IPv4 address}"
    }
}
```

### Note:

* You can enable the NameCheap API and get a key on their [website](https://www.namecheap.com/support/api/intro/).
* *UserName* and *ApiUserName* are usually the same value, and it is usually the user name you use to sign in to NameCheap.
* *ClientIP* is a whitelisted IP address allowed to connect to the API. These whitelisted addresses can be added to NameCheap when/where you enable the API on their website. **Note:** API calls will check your current WAN IP with the one you provide in the settings. So they need to match.

Once the `appsettings.json` file is modified, put the contents of the publish folder on your server and you should be able to use the executable with *win-acme* or any other tool by calling it with a fully qualified path.

## Usage

The command line is in the format below:

```bash
Argentini.NameCheap.exe [create|delete] [hostname] [name] [value]
```

Some examples include:

```bash
Argentini.NameCheap.exe create example.com * testrecord=yaddayadda
Argentini.NameCheap.exe create example.com my.api mykey=yaddayadda
Argentini.NameCheap.exe create example.com my.txt "val1=yadda; val2=yadda"
```

So in `win-acme` you would set your create script arguments to this:

```
create {ZoneName} {NodeName} {Token}
```

Likewise, your delete script arguments would be:

```
delete {ZoneName} {NodeName} {Token}
```

### macOS and Linux

The tool can be used on Linux or macOS as well. If the published executable doesn't run on macOS you may need to manually sign the published application using something like this:

```bash
cd publish
codesign -s - Argentini.NameCheap
```
