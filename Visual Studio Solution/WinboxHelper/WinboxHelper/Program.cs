using KeePassLib.Interfaces;
using KeePassLib.Keys;
using KeePassLib.Serialization;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinboxHelper
{
    //Mysterious and necessary class used for KeePass magic
    public sealed class CoutLogger : IStatusLogger
    {
        public void StartLogging(string strOperation, bool bWriteOperationToLog)
        {
            System.Console.WriteLine(strOperation);
        }

        public void EndLogging() { }

        public bool SetProgress(uint uPercent) { return true; }
        public bool SetText(string strNewText, LogStatusType lsType)
        {
            System.Console.WriteLine(strNewText);
            return true;
        }
        public bool ContinueWork() { return true; }
    }

    //Main program
    class Program
    {
        static void Main(string[] args)
        {
            /*Variables to track (in respective order) IP address,
             location of the password db, location of Winbox, 
             and registry value to lookup*/
            String address;
            string kpLocation;
            string wbLocation;
            string valueName = "KeePass Location";
            var masterpw = "";
            String username = "";
            String password = "";

            //Grab the file paths for password db and winbox
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("Software\\WinboxHelper");
            kpLocation = (string)rk.GetValue(valueName);
            valueName = "Winbox Location";
            wbLocation = (string)rk.GetValue(valueName);

            //If no args passed, program will exit
            if (args.Length < 1)
            {
                address = "";
                Console.WriteLine("Args less than one.");
                Console.Read();
                Environment.Exit(0);
            }
            else
            {
                //IP address must be the first argument passed.  Everything else is ignored.
                address = args[0];
            }
            
            //String manipulation for using web links
            if (address.ToLower().Contains("winboxhelper")) {
                address = address.Substring(13);
            }

            //Make new KeePass pwdb object and point it to the specified db
            var db = new KeePassLib.PwDatabase();
            var dbpath = @kpLocation;

            //Retrieve master password from user and mask input
            while (true)
            {
                Console.Write("Enter Master PW: ");
                ConsoleKeyInfo key;

                do
                {
                    key = Console.ReadKey(true);

                    if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                    {
                        masterpw += key.KeyChar;
                        Console.Write("*");
                    }
                    else
                    {
                        if (key.Key == ConsoleKey.Backspace && masterpw.Length > 0)
                        {
                            masterpw = masterpw.Substring(0, (masterpw.Length - 1));
                            Console.Write("\b \b");
                        }
                    }
                }
                // Stops Receving Keys Once Enter is Pressed
                while (key.Key != ConsoleKey.Enter);
                Console.WriteLine();

                //Connect to pwdb
                var ioConnInfo = new IOConnectionInfo { Path = dbpath };
                var compKey = new CompositeKey();
                compKey.AddUserKey(new KcpPassword(masterpw));
                Console.WriteLine();

                //IP address or gtfo
                if (address.Equals("") || address.Equals(null)) { 
                    Console.WriteLine("Must specify an IP address.  Terminating.");
                    Console.Read();
                    Environment.Exit(0);
                }
                //Pass composite key to db and try to open.  If not, gently tell user they have the wrong password or have probably been fired.
                try
                {
                    db.Open(ioConnInfo, compKey, new CoutLogger());
                    break;
                }
                catch (Exception)
                {
                    Console.WriteLine("Invalid password or could not load the database.  Please try again.");
                }
            }

            //Retrieve the KeePass entries.
            var kpdata = from entry in db.RootGroup.GetEntries(true)
                         select new
                         {
                             //Grab all the KeePass entries
                             Group = entry.ParentGroup.Name,
                             Title = entry.Strings.ReadSafe("Title"),
                             Username = entry.Strings.ReadSafe("UserName"),
                             Password = entry.Strings.ReadSafe("Password"),
                             URL = entry.Strings.ReadSafe("URL"),
                             Notes = entry.Strings.ReadSafe("Notes")
                         };


            //Search the KeePass entries for the IP address
            foreach (Object anon in kpdata)
            {
                String[] s;
                s = anon.ToString().Split(new Char[] { ',' });
                for (int i = 0; i < s.Length; i++)
                {
                    if (s[i].Contains(address))
                    {
                        /*Username is the 3rd element of the KeePass entry returned
                        and has some leading text that we're not interested in.*/
                        username = s[2].Substring(12);
                        /*Password is the 4th element of the KeePass entry returned
                        and also has some leading text that we're not interested in.*/
                        password = s[3].Substring(12);
                    }
                }
            }

            //If no matching entry was found, exit the program.
            if (password.Equals("") || password.Equals(null)) {
                Console.WriteLine("No matching record found. Terminating.");
                Console.ReadLine();
                db.Close();
                Environment.Exit(0);
            }

            //Otherwise, open Winbox with the discovered parameters
            ProcessStartInfo start = new ProcessStartInfo();
            String winbox;
            //Enter in the command line arguments
            winbox = address + " " + username + " " + password;
            start.Arguments = winbox;
            //Enter the executable to run
            start.FileName = wbLocation;
            start.WindowStyle = ProcessWindowStyle.Hidden;
            start.CreateNoWindow = true;

            //Run the external process & wait for it to finish
            using (Process proc = Process.Start(start))
            {
                /*Insert hide window here if you don't want to stare
                 at a command prompt while you're working in winbox.*/
                proc.WaitForExit();
            }
            // Make sure to release the file
            db.Close();
        }
    }
}
