using System;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;

namespace TicTacToeClient
{
    public partial class MainWindow : Window
    {
        private TcpClient client;
        private NetworkStream stream;
        private string playerSymbol = "X"; // This would ideally be set by the server

        public MainWindow()
        {
            InitializeComponent();
            ConnectToServer();
            Task.Run(() => ListenForMessages());
        }

        private void ConnectToServer()
        {
            client = new TcpClient("127.0.0.1", 8000);
            stream = client.GetStream();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button.Content == null)
            {
                button.Content = playerSymbol;
                string message = $"Move made on: {button.Name}";
                byte[] data = Encoding.ASCII.GetBytes(message);
                stream.Write(data, 0, data.Length);
            }
        }

        private void ListenForMessages()
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Dispatcher.Invoke(() =>
                    {
                        ProcessServerMessage(message);
                    });
                }
            }
        }

        private void ProcessServerMessage(string message)
        {
            if (message.Contains("wins"))
            {
                MessageBox.Show(message);
                ResetBoard();
            }
            else if (message.StartsWith("Draw"))
            {
                MessageBox.Show(message);
                ResetBoard();
            }
            else if (message.StartsWith("Move made on:"))
            {
                string buttonId = message.Split(':')[1].Trim();
                Button button = FindName(buttonId) as Button;
                if (button != null)
                {
                    button.Content = playerSymbol == "X" ? "O" : "X"; // Assuming the server handles who is X or O
                }
            }
        }

        private void ResetBoard()
        {
            foreach (var control in MainGrid.Children)
            {
                if (control is Button button)
                {
                    button.Content = null;
                }
            }
        }
    }
}
