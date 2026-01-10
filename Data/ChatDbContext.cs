using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace HexaFlow.Data
{
    public class ChatDbContext
    {
        private readonly string _connectionString;

        public ChatDbContext()
        {
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HexaFlow");
            Directory.CreateDirectory(dbPath);
            var dbFilePath = Path.Combine(dbPath, "chat_history.db");
            _connectionString = $"Data Source={dbFilePath}";
            
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // 创建对话表
            var createConversationsTable = @"
                CREATE TABLE IF NOT EXISTS Conversations (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL
                )";

            using var command1 = new SqliteCommand(createConversationsTable, connection);
            command1.ExecuteNonQuery();

            // 创建消息表
            var createMessagesTable = @"
                CREATE TABLE IF NOT EXISTS Messages (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ConversationId INTEGER NOT NULL,
                    Role TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    Timestamp TEXT NOT NULL,
                    FOREIGN KEY (ConversationId) REFERENCES Conversations(Id) ON DELETE CASCADE
                )";

            using var command2 = new SqliteCommand(createMessagesTable, connection);
            command2.ExecuteNonQuery();
        }

        public SqliteConnection GetConnection()
        {
            return new SqliteConnection(_connectionString);
        }
    }
}