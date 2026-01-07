using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace HexaFlow.Services
{
    public class ChatHistoryService
    {
        private readonly string _databasePath;

        public ChatHistoryService()
        {
            // 确保应用程序目录存在
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                "HexaFlow");

            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            _databasePath = Path.Combine(appDataPath, "chat_history.db");

            // 初始化数据库
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
            {
                connection.Open();

                // 创建会话表
                var createSessionTableCommand = connection.CreateCommand();
                createSessionTableCommand.CommandText = 
                @"
                CREATE TABLE IF NOT EXISTS Sessions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL
                )";
                createSessionTableCommand.ExecuteNonQuery();

                // 创建消息表
                var createMessageTableCommand = connection.CreateCommand();
                createMessageTableCommand.CommandText = 
                @"
                CREATE TABLE IF NOT EXISTS Messages (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SessionId INTEGER NOT NULL,
                    Content TEXT NOT NULL,
                    IsFromUser INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    FOREIGN KEY (SessionId) REFERENCES Sessions (Id)
                )";
                createMessageTableCommand.ExecuteNonQuery();
            }
        }

        public async Task<int> CreateSessionAsync(string title)
        {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
            {
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = 
                @"
                INSERT INTO Sessions (Title, CreatedAt, UpdatedAt)
                VALUES ($title, $createdAt, $updatedAt);
                SELECT last_insert_rowid();
                ";

                command.Parameters.AddWithValue("$title", title);
                command.Parameters.AddWithValue("$createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                var sessionId = Convert.ToInt32(await command.ExecuteScalarAsync());
                return sessionId;
            }
        }

        public async Task<List<ChatSession>> GetSessionsAsync()
        {
            var sessions = new List<ChatSession>();

            using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
            {
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = 
                @"
                SELECT Id, Title, CreatedAt, UpdatedAt
                FROM Sessions
                ORDER BY UpdatedAt DESC
                ";

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        sessions.Add(new ChatSession
                        {
                            Id = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            CreatedAt = DateTime.Parse(reader.GetString(2)),
                            UpdatedAt = DateTime.Parse(reader.GetString(3))
                        });
                    }
                }
            }

            return sessions;
        }

        public async Task<ChatSession> GetSessionAsync(int sessionId)
        {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
            {
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = 
                @"
                SELECT Id, Title, CreatedAt, UpdatedAt
                FROM Sessions
                WHERE Id = $sessionId
                ";

                command.Parameters.AddWithValue("$sessionId", sessionId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new ChatSession
                        {
                            Id = reader.GetInt32(0),
                            Title = reader.GetString(1),
                            CreatedAt = DateTime.Parse(reader.GetString(2)),
                            UpdatedAt = DateTime.Parse(reader.GetString(3))
                        };
                    }
                }
            }

            return null;
        }

        public async Task<List<ChatMessage>> GetMessagesAsync(int sessionId)
        {
            var messages = new List<ChatMessage>();

            using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
            {
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = 
                @"
                SELECT Id, Content, IsFromUser, CreatedAt
                FROM Messages
                WHERE SessionId = $sessionId
                ORDER BY CreatedAt ASC
                ";

                command.Parameters.AddWithValue("$sessionId", sessionId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        
                        bool isFromUser = reader.GetBoolean(2);
                        messages.Add(new ChatMessage
                        {
                            Content = reader.GetString(1),
                            Alignment = isFromUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                            BubbleBackground = isFromUser ? 
                                new SolidColorBrush(Color.FromRgb(94, 84, 142)) : 
                                new SolidColorBrush(Color.FromRgb(249, 250, 251)),
                            ShadowColor = isFromUser ? 
                                Color.FromRgb(94, 84, 142) : 
                                Color.FromRgb(249, 250, 251)
                        });
                    }
                }
            }

            return messages;
        }

        public async Task<int> SaveMessageAsync(int sessionId, string content, bool isFromUser)
        {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
            {
                await connection.OpenAsync();

                // 保存消息
                var command = connection.CreateCommand();
                command.CommandText = 
                @"
                INSERT INTO Messages (SessionId, Content, IsFromUser, CreatedAt)
                VALUES ($sessionId, $content, $isFromUser, $createdAt);
                SELECT last_insert_rowid();
                ";

                command.Parameters.AddWithValue("$sessionId", sessionId);
                command.Parameters.AddWithValue("$content", content);
                command.Parameters.AddWithValue("$isFromUser", isFromUser ? 1 : 0);
                command.Parameters.AddWithValue("$createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                var messageId = Convert.ToInt32(await command.ExecuteScalarAsync());

                // 更新会话的更新时间
                var updateCommand = connection.CreateCommand();
                updateCommand.CommandText = 
                @"
                UPDATE Sessions
                SET UpdatedAt = $updatedAt
                WHERE Id = $sessionId
                ";

                updateCommand.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                updateCommand.Parameters.AddWithValue("$sessionId", sessionId);

                await updateCommand.ExecuteNonQueryAsync();

                return messageId;
            }
        }

        public async Task DeleteSessionAsync(int sessionId)
        {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
            {
                await connection.OpenAsync();

                // 先删除会话的所有消息
                var deleteMessagesCommand = connection.CreateCommand();
                deleteMessagesCommand.CommandText = 
                @"
                DELETE FROM Messages
                WHERE SessionId = $sessionId
                ";

                deleteMessagesCommand.Parameters.AddWithValue("$sessionId", sessionId);
                await deleteMessagesCommand.ExecuteNonQueryAsync();

                // 然后删除会话
                var deleteSessionCommand = connection.CreateCommand();
                deleteSessionCommand.CommandText = 
                @"
                DELETE FROM Sessions
                WHERE Id = $sessionId
                ";

                deleteSessionCommand.Parameters.AddWithValue("$sessionId", sessionId);
                await deleteSessionCommand.ExecuteNonQueryAsync();
            }
        }
    }

    public class ChatSession
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
