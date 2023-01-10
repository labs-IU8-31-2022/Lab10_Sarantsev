using MySql.Data.MySqlClient;
using System;
using System.Globalization;

namespace Lab_10
{
    internal class Program
    {
        static List<Task> AsynchroniusTask = new List<Task>();
        static Mutex mutex = new Mutex();
        static async Task Main(string[] args)
        {
            string connect = "Server=localhost;user=root;database=lab_10;password=adminadmin";
            MySqlConnection connect_gen = new MySqlConnection(connect);
            //await connection.OpenAsync();
            connect_gen.Open();
            using (StreamReader data_string = new StreamReader("ticker.txt"))
            {
                //File.Delete("test.txt");
                while (!data_string.EndOfStream)
                {
                    string data_name = data_string.ReadLineAsync().GetAwaiter().GetResult();


                    using var client = new HttpClient();

                    try
                    {

                        var result = await client.GetStringAsync($"https://query1.finance.yahoo.com/v7/finance/download/{data_name}" +
                            $"?period1={DateTimeOffset.Now.AddDays(-25).ToUnixTimeSeconds()}&period2={DateTimeOffset.Now.AddDays(-10).ToUnixTimeSeconds()}" +
                            "&interval=1d&events=history&includeAdjustedClose=true");
                        ActivityAsync(result, data_name, connect_gen);
                    }

                    catch (HttpRequestException e)
                    {
                        System.Console.WriteLine($"{data_name} : {e.Message}");
                        continue;
                    }
                }
                connect_gen.Close();
            }
            
            while (AsynchroniusTask.Count > 0)
            {
                Task finishedTask = await Task.WhenAny(AsynchroniusTask);
                AsynchroniusTask.Remove(finishedTask);

            }

            static async void ActivityAsync(string response, string data_name, MySqlConnection connection)
            {
                await Task.Run(() => Activity(response, data_name, connection));
            }

            static void Activity(string response, string data_name, MySqlConnection connection)
            {
                mutex.WaitOne();
                string tickersquery = $"INSERT INTO tickers (ticker) VALUES (\"{data_name}\");";
                MySqlCommand command = new MySqlCommand(tickersquery, connection);
                command.ExecuteNonQuery();
                command.Dispose();
                mutex.ReleaseMutex();
                
                string check = "";
                
                try
                {
                    string[] tempdata = response.Split("\n");
                    double temp = Double.Parse(tempdata[tempdata.Length - 1].Split(",")[4], CultureInfo.InvariantCulture) - Double.Parse(tempdata[tempdata.Length - 2].Split(",")[4], CultureInfo.InvariantCulture);
                    if (temp > 0) {check = "Increased";}
                    else if (temp < 0) {check = "Decreased";}
                    else if (temp == 0) {check = "Not changed";}
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine("Wrong data");
                    check = "Not stated.";
                }
                foreach (string line in response.Split("\n"))
                {
                    string[] data = line.Split(",");
                    try {
                        string date = data[0];
                        string avg_price = ((double.Parse(data[2], CultureInfo.InvariantCulture) + double.Parse(data[3], CultureInfo.InvariantCulture)) / 2).ToString().Replace(",",".");
                        
                        string id_of_ticker_query = $"SELECT id FROM tickers WHERE ticker = \"{data_name}\" ";
                        command.CommandText = id_of_ticker_query;
                        
                        mutex.WaitOne();
 
                        string ticker_id = (command.ExecuteScalar().ToString());
                        command.Dispose();
                        mutex.ReleaseMutex();


                        string prices_ = $"INSERT INTO prices (tickerId, price,date) VALUES ({ticker_id} , {avg_price}, \"{date}\" )";
                        command.CommandText = prices_;
                        mutex.WaitOne();
                        command.ExecuteNonQuery();
                        mutex.ReleaseMutex();
                        command.Dispose();

                        
                        string todays_condition_= $"INSERT IGNORE INTO todayscondition (tickerId, state) VALUES ({ticker_id}, \"{check}\")";
                        command.CommandText = todays_condition_;
                        mutex.WaitOne();
                        command.ExecuteNonQuery();
                        mutex.ReleaseMutex();
                        command.Dispose();
                    }
                    catch (Exception ex)  {
                    }
                }
            }
        }
    }
}