using System;
using Datadog.Trace.ClrProfiler;
using Npgsql;

namespace Samples.Npgsql
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"Profiler attached: {Instrumentation.ProfilerAttached}");
            Console.WriteLine($"Platform: {(Environment.Is64BitProcess ? "x64" : "x32")}");

            using (var conn = new NpgsqlConnection("Host=localhost;Username=postgres;Password=postgres;Database=postgres"))
            {
                conn.Open();

                // Insert some data
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "INSERT INTO data (some_field) VALUES (@p)";
                    cmd.Parameters.AddWithValue("p", "Hello world");
                    cmd.ExecuteNonQuery();
                }

                // Retrieve all rows
                using (var cmd = new NpgsqlCommand("SELECT some_field FROM data", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine(reader.GetString(0));
                    }
                }
            }
        }
    }
}
