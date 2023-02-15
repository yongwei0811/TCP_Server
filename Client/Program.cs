using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            TcpClient client = new TcpClient("127.0.0.1", 8080);
            NetworkStream stream = client.GetStream();

            while (true)
            {
                Console.Write("Enter a request to send to the server (enter 'exit' to exit):");
                string request = Console.ReadLine();
                if (request == "exit")
                {
                    break;
                }

                byte[] buffer = Encoding.ASCII.GetBytes(request);
                stream.Write(buffer, 0, buffer.Length);

                buffer = new byte[1024];
                int bytesRead;
                string response;
                StringBuilder responseBuilder = new StringBuilder();

                do
                {
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    responseBuilder.Append(response);
                } while (stream.DataAvailable);

                response = responseBuilder.ToString();
                Console.WriteLine("Response from server: " + response);
            }

            client.Close();
        }
    }
}
