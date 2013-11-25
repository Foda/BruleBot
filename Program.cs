using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BruleBot
{
    class Program
    {
        static void Main(string[] args)
        {
            IRCConfig conf = new IRCConfig();
            conf.Name = "BruleBot";
            conf.Nick = "BruleBot";

            Console.Write("Server:");
            conf.Server = Console.ReadLine();

            Console.Write("Channel:");
            conf.Chan = Console.ReadLine();

            Console.Write("Port:");
            conf.Port = int.Parse(Console.ReadLine());

            Console.Write("Password:");
            conf.Password = Console.ReadLine();

            new IRCBot(conf);
            Console.ReadLine();
        }
    }
}
