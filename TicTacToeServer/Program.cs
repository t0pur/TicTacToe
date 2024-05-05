using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TicTacToeServer
{
    class Program
    {
        private static List<TcpClient> clients = new List<TcpClient>();
        private static TcpListener listener;
        private static bool player1Turn = true;
        private static char[,] board = new char[3, 3];
        private static int port = 8000;

        static void Main(string[] args)
        {
            InitializeBoard();
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine($"Server started on port {port}.");

            try
            {
                while (true)
                {
                    Console.WriteLine("Waiting for client connections...");
                    TcpClient client = listener.AcceptTcpClient();
                    clients.Add(client);
                    Console.WriteLine("Client connected.");
                    Thread clientThread = new Thread(() => HandleClient(client));
                    clientThread.Start();
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        private static void InitializeBoard()
        {
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    board[row, col] = ' ';
                }
            }
        }

        private static void HandleClient(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int bytesRead;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Received: {message}");
                    if (UpdateBoard(message, player1Turn ? 'X' : 'O'))
                    {
                        if (CheckWinner())
                        {
                            message = $"Player {(player1Turn ? "X" : "O")} wins!";
                            BroadcastMessage(message, null);
                            InitializeBoard();
                        }
                        else if (IsBoardFull())
                        {
                            BroadcastMessage("Draw. Game over.", null);
                            InitializeBoard();
                        }
                        player1Turn = !player1Turn;
                    }
                    BroadcastMessage(message, client);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                clients.Remove(client);
                client.Close();
                Console.WriteLine("Client disconnected.");
            }
        }

        private static bool UpdateBoard(string move, char playerSymbol)
        {
            // Assume move format: "Move made on: ButtonX"
            string[] parts = move.Split(' ');
            if (parts.Length < 4) return false;
            string buttonId = parts[3];
            int index = int.Parse(buttonId.Substring(6));
            int row = index / 3;
            int col = index % 3;
            if (board[row, col] == ' ')
            {
                board[row, col] = playerSymbol;
                return true;
            }
            return false;
        }

        private static bool CheckWinner()
        {
            for (int i = 0; i < 3; i++)
            {
                if (board[i, 0] != ' ' && board[i, 0] == board[i, 1] && board[i, 1] == board[i, 2])
                    return true;
                if (board[0, i] != ' ' && board[0, i] == board[1, i] && board[1, i] == board[2, i])
                    return true;
            }
            if (board[0, 0] != ' ' && board[0, 0] == board[1, 1] && board[1, 1] == board[2, 2])
                return true;
            if (board[0, 2] != ' ' && board[0, 2] == board[1, 1] && board[1, 1] == board[2, 0])
                return true;

            return false;
        }

        private static bool IsBoardFull()
        {
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    if (board[row, col] == ' ')
                        return false;
                }
            }
            return true;
        }

        private static void BroadcastMessage(string message, TcpClient senderClient)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(message);
            foreach (var client in clients)
            {
                if (client != senderClient) // Send message to other players
                {
                    NetworkStream stream = client.GetStream();
                    stream.Write(buffer, 0, buffer.Length);
                }
            }
        }
    }
}
