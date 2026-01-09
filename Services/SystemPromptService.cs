using HexaFlow.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HexaFlow.Services
{
    public class SystemPromptService
    {
        private readonly string _databasePath;

        public SystemPromptService()
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

                // 创建系统提示词表
                var createSystemPromptTableCommand = connection.CreateCommand();
                createSystemPromptTableCommand.CommandText =
                @"
                CREATE TABLE IF NOT EXISTS SystemPrompts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    IsActive INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL
                )";
                createSystemPromptTableCommand.ExecuteNonQuery();

                // 检查是否有默认系统提示词，如果没有则添加
                var checkDefaultPromptsCommand = connection.CreateCommand();
                checkDefaultPromptsCommand.CommandText = "SELECT COUNT(*) FROM SystemPrompts";
                int count = Convert.ToInt32(checkDefaultPromptsCommand.ExecuteScalar());

                if (count == 0)
                {
                    // 添加默认系统提示词
                    AddDefaultSystemPrompts(connection);
                }
            }
        }

        private void AddDefaultSystemPrompts(SqliteConnection connection)
        {
            var defaultPrompts = new[]
            {
                new { Name = "默认助手", Content = "你是一个有用的AI助手，能够回答各种问题并提供有用的建议。" },
                new { Name = "编程助手", Content = "你是一个专业的编程助手，精通多种编程语言，能够帮助用户解决编程问题、优化代码和解释技术概念。" },
                new { Name = "创意写作助手", Content = "你是一个创意写作助手，能够帮助用户创作各种类型的文本内容，包括故事、诗歌、文章等。" },
                new { Name = "学术研究助手", Content = "你是一个学术研究助手，能够帮助用户查找学术资料、分析研究问题并提供专业的学术建议。" }
            };

            foreach (var prompt in defaultPrompts)
            {
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                INSERT INTO SystemPrompts (Name, Content, IsActive, CreatedAt, UpdatedAt)
                VALUES ($name, $content, $isActive, $createdAt, $updatedAt);
                ";
                command.Parameters.AddWithValue("$name", prompt.Name);
                command.Parameters.AddWithValue("$content", prompt.Content);
                command.Parameters.AddWithValue("$isActive", prompt.Name == "默认助手" ? 1 : 0);
                command.Parameters.AddWithValue("$createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.ExecuteNonQuery();
            }
        }

        public async Task<List<SystemPrompt>> GetSystemPromptsAsync()
        {
            var prompts = new List<SystemPrompt>();

            using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
            {
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM SystemPrompts ORDER BY CreatedAt DESC";

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        prompts.Add(new SystemPrompt
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Content = reader.GetString(2),
                            IsActive = reader.GetInt32(3) == 1,
                            CreatedAt = DateTime.Parse(reader.GetString(4)),
                            UpdatedAt = DateTime.Parse(reader.GetString(5))
                        });
                    }
                }
            }

            return prompts;
        }

        public async Task<int> AddSystemPromptAsync(string name, string content)
        {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
            {
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText =
                @"
                INSERT INTO SystemPrompts (Name, Content, IsActive, CreatedAt, UpdatedAt)
                VALUES ($name, $content, 0, $createdAt, $updatedAt);
                SELECT last_insert_rowid();
                ";
                command.Parameters.AddWithValue("$name", name);
                command.Parameters.AddWithValue("$content", content);
                command.Parameters.AddWithValue("$createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                command.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
        }

        public async Task<SystemPrompt> GetActiveSystemPromptAsync()
        {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
            {
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM SystemPrompts WHERE IsActive = 1 LIMIT 1";

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new SystemPrompt
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Content = reader.GetString(2),
                            IsActive = reader.GetInt32(3) == 1,
                            CreatedAt = DateTime.Parse(reader.GetString(4)),
                            UpdatedAt = DateTime.Parse(reader.GetString(5))
                        };
                    }
                }
            }

            return null;
        }

        public async Task SetActiveSystemPromptAsync(int id)
        {
            using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
            {
                await connection.OpenAsync();

                // 先将所有系统提示词设置为非活动状态
                var resetCommand = connection.CreateCommand();
                resetCommand.CommandText = "UPDATE SystemPrompts SET IsActive = 0";
                await resetCommand.ExecuteNonQueryAsync();

                // 设置指定ID的系统提示词为活动状态
                var setActiveCommand = connection.CreateCommand();
                setActiveCommand.CommandText = "UPDATE SystemPrompts SET IsActive = 1, UpdatedAt = $updatedAt WHERE Id = $id";
                setActiveCommand.Parameters.AddWithValue("$id", id);
                setActiveCommand.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                await setActiveCommand.ExecuteNonQueryAsync();
            }
        }
    }
}
