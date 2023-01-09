using MySql.Data.MySqlClient;
using System.Threading;

namespace ui_Lab10 {


    public class Program {
        

        public static void Main(string[] args) {
            string connect = "Server=localhost;user=root;database=lab_10;password=adminadmin";

            // Создание подключения
            MySqlConnection  connect_gen = new MySqlConnection(connect);
            //await connection.OpenAsync();
            connect_gen.Open();
            MySqlCommand command = new MySqlCommand("", connect_gen);
            while (true)
            {
                Console.Write("Enter the ticker name, or 'exit' to leave: ");
                string tmp = Console.ReadLine();
                if (tmp == "exit") { break; }
                else
                {
                    try
                    {
                        string ticker_query = $"SELECT id FROM tickers WHERE ticker = \"{tmp}\" ";
                        command.CommandText = ticker_query;
                        
                        string ticker_id = (command.ExecuteScalar().ToString());
                        command.Dispose();

                        command.CommandText = $"SELECT state FROM todayscondition WHERE tickerId = {ticker_id}";
                        Console.WriteLine(command.ExecuteScalar().ToString());

                        command.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error!");
                    }
                }
            }
            connect_gen.Close();
        }
    }
}