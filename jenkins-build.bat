@Echo off
Echo(
Echo(
echo ============================
echo Running Windows Build Script
echo ============================

del .\.nuget\NuGet.targets
copy .\.nuget\NuGet.targets.windows .\.nuget\NuGet.targets

.\.nuget\NuGet.exe restore .\
msbuild .\Zazzles.sln /p:Platform="Any CPU" /p:Configuration=Release
.\.nuget\NuGet.exe install NUnit.Runners -Version 2.6.4 -OutputDirectory .\testrunner
.\testrunner\NUnit.Runners.2.6.4\tools\nunit-console-x86.exe .\bin\Zazzles.Tests.dll /xml=nunit-result.xml
