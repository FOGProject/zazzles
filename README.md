# Zazzles

## Concept
Zazzles is a cross platform server<->client framework designed for the [FOG Client](https://github.com/FOGProject/fog-client).

## Development
[![Stories in progress](https://badge.waffle.io/FOGProject/zazzles.svg?label=In%20Progress&title=Issues%20In%20Progress)](http://waffle.io/FOGProject/zazzles)

Windows      | Linux       
-------------|-------------
![Windows](https://dev.fogproject.org/buildStatus/icon?job=zazzles/OS=windows) | ![Linux](https://dev.fogproject.org/buildStatus/icon?job=zazzles/OS=linux)

## Building

#### Environment

To build Zazzles, any OS can be used, as long as it is capable of `msbuild` or `xbuild` targeted at .NET 4.5

#### Build Command
```
[msbuild/xbuild] Zazzles.sln
```

The binaries will be in `bin`


## Modules
The framework's functionality derives from modules. Each module has 1 specific goal, and is isolated from every other module. Each module is executed in a sandbox-like environment, preventing bad code from crashing the service. Since each module is isolated, the framework's server can choose which modules to enable or disable.

## Noteworthy API

#### Bus
The Bus is a glorified IPC [publisher/subscriber](https://en.wikipedia.org/wiki/Publish%E2%80%93subscribe_pattern) system. The currently implementation uses a local websocket server bound to the 127.0.0.1 loopback address. The Bus provides methods for both IPC and non-IPC events. It should be assumed that this websocket can be comprimised at any time, thus treat all messages that derived from an IPC source, with the possible exception of root, with caution. 

### Log
`Log.Debug` calls will only output if Zazzles was build in `Debug` mode. This is because Log methods are built with preprocessor directives to prevent the possiblility of sensitive data being logged in Release builds.

### Debugger
The Debugger will provides an interface for building tools capable of allowing a strings to call individual methods / modules with parameters. The tool will offer little benifit unless Zazzles is built in Debug mode, due to the preprocessor directives in `Log.Debug`

### User
`User.GetInactivityTime()` will only work on Linux if `xprintidle` is installed. This also means inactivity checks cannot occur on any non xsession (such as ssh).


