/*
    Copyright(c) 2014-2018 FOG Project

    The MIT License

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files(the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions :
    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
*/

using System.Diagnostics;
using Zazzles.Core.Device.Power;
using Zazzles.Core.Device.Power.DataContract;

namespace Zazzles.Mac.Device.Power
{
    public class MacPower : IPower
    {
        public void LogOffUser()
        {
            Process.Start("logout");
        }

        public void Hibernate()
        {
            Process.Start("pmset -a hibernatemode 25");
        }

        public void LockWorkStation()
        {
            Process.Start(@"/System/Library/CoreServices/Menu\ Extras/User.menu/Contents/Resources/CGSession -suspend");
        }

        public void InvokeEvent(PowerEvent powerEvent)
        {
            var switches = "";
            switch (powerEvent.Action)
            {
                case PowerAction.Shutdown:
                    switches = "-h";
                    break;
                case PowerAction.Reboot:
                    switches = "-r";
                    break;
                default:
                    return;
            }
            var parameters = $"{switches} + 0 \"{powerEvent.Comment}\"";
            Process.Start("shutdown", parameters);
        }
    }
}