#!/bin/bash
xbuild ./Zazzles.sln /p:PlatformTarget=x86 /p:Configuration=Release
mono ./.nuget/NuGet.exe install NUnit.Runners -Version 2.6.4 -OutputDirectory ./testrunner
mono ./testrunner/NUnit.Runners.2.6.4/tools/nunit-console-x86.exe ./bin/Zazzles.Tests.dll /xml=nunit-result.xml