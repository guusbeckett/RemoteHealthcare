using System;
using Server.Control;

namespace Server
{
    class Program
    {
        ///<summary>
        ///Starts the application.
        ///</summary>
        static void Main(string[] args)
        {
            Console.Title = "Healthcare Over Internet";
            new ServerControl();
        }
    }
}
