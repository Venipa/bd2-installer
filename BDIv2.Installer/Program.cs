using System;
using System.Threading.Tasks;
using BDIv2.App;

namespace BDIv2
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(() =>
            {
                Installer.Create();
            });
            Task.Delay(-1).Wait();
        }
    }
}
