@Echo off
echo Running Windows Build Script
del .\.nuget\NuGet.targets

msbuild .\Zazzles.sln /p:PlatformTarget=x86 /p:Configuration=Release
.\.nuget\NuGet.exe install NUnit.Runners -Version 2.6.4 -OutputDirectory .\testrunner
.\testrunner\NUnit.Runners.2.6.4\tools\nunit-console-x86.exe .\bin\Zazzles.Tests.dll /xml=nunit-result.xml
