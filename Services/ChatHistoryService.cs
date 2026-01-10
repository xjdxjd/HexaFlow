using HexaFlow.Data;
using HexaFlow.Models;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HexaFlow.Services
{
    public class ChatHistoryService
    {
        private readonly ChatDbContext _dbContext;

        public ChatHistoryService()
        {
            _dbContext = new ChatDbContext();
        }

        /// <summary>
        /// 保存对话
        /// </summary>
        public async Task<int> SaveConversationAsync(Conversation conversation)
        {
            return await Task.Run(() =>
            {
                using var connection = _dbContext.GetConnection();
                connection.Open();

                // 如果是新对话，插入对话记录
                if (conversation.Id == 0)
                {
                    using var insertConvCmd = new SqliteCommand(@"
                        INSERT INTO Conversations (Title, CreatedAt, UpdatedAt) 
                        VALUES (@title, @createdAt, @updatedAt);
                        SELECT last_insert_rowid();", connection);

                    insertConvCmd.Parameters.AddWithValue("@title", conversation.Title);
                    insertConvCmd.Parameters.AddWithValue("@createdAt", conversation.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                    insertConvCmd.Parameters.AddWithValue("@updatedAt", conversation.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

                    conversation.Id = Convert.ToInt32(insertConvCmd.ExecuteScalar());
                }
                else
                {
                    // 如果是已有对话，更新更新时间
                    using var updateConvCmd = new SqliteCommand(@"
                        UPDATE Conversations 
                        SET Title = @title, UpdatedAt = @updatedAt 
                        WHERE Id = @id", connection);

                    updateConvCmd.Parameters.AddWithValue("@title", conversation.Title);
                    updateConvCmd.Parameters.AddWithValue("@updatedAt", conversation.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                    updateConvCmd.Parameters.AddWithValue("@id", conversation.Id);
                    updateConvCmd.ExecuteNonQuery();
                }

                // 删除该对话的现有消息
                using var deleteMsgsCmd = new SqliteCommand("DELETE FROM Messages WHERE ConversationId = @convId", connection);
                deleteMsgsCmd.Parameters.AddWithValue("@convId", conversation.Id);
                deleteMsgsCmd.ExecuteNonQuery();

                // 插入新消息
                foreach (var message in conversation.Messages)
                {
                    using var insertMsgCmd = new SqliteCommand(@"
                        INSERT INTO Messages (ConversationId, Role, Content, Timestamp) 
                        VALUES (@convId, @role, @content, @timestamp)", connection);

                    insertMsgCmd.Parameters.AddWithValue("@convId", conversation.Id);
                    insertMsgCmd.Parameters.AddWithValue("@role", message.Role);
                    insertMsgCmd.Parameters.AddWithValue("@content", message.Content);
                    insertMsgCmd.Parameters.AddWithValue("@timestamp", message.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));

                    insertMsgCmd.ExecuteNonQuery();
                }

                return conversation.Id;
            });
        }

        /// <summary>
        /// 获取所有对话
        /// </summary>
        public async Task<List<Conversation>> GetAllConversationsAsync()
        {
            return await Task.Run(() =>
            {
                var conversations = new List<Conversation>();

                using var connection = _dbContext.GetConnection();
                connection.Open();

                using var command = new SqliteCommand(@"
                    SELECT Id, Title, CreatedAt, UpdatedAt 
                    FROM Conversations 
                    ORDER BY UpdatedAt DESC", connection);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var conv = new Conversation
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Title = reader["Title"].ToString(),
                        CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString()),
                        UpdatedAt = DateTime.Parse(reader["UpdatedAt"].ToString())
                    };
                    conversations.Add(conv);
                }

                return conversations;
            });
        }

        /// <summary>
        /// 根据ID获取对话
        /// </summary>
        public async Task<Conversation> GetConversationByIdAsync(int id)
        {
            return await Task.Run(() =>
            {
                Conversation conversation = null;

                using var connection = _dbContext.GetConnection();
                connection.Open();

                // 获取对话基本信息
                using var convCommand = new SqliteCommand(@"
                    SELECT Id, Title, CreatedAt, UpdatedAt 
                    FROM Conversations 
                    WHERE Id = @id", connection);
                convCommand.Parameters.AddWithValue("@id", id);

                using var convReader = convCommand.ExecuteReader();
                if (convReader.Read())
                {
                    conversation = new Conversation
                    {
                        Id = Convert.ToInt32(convReader["Id"]),
                        Title = convReader["Title"].ToString(),
                        CreatedAt = DateTime.Parse(convReader["CreatedAt"].ToString()),
                        UpdatedAt = DateTime.Parse(convReader["UpdatedAt"].ToString())
                    };
                }
                convReader.Close();

                if (conversation != null)
                {
                    // 获取对话的消息
                    using var msgCommand = new SqliteCommand(@"
                        SELECT Role, Content, Timestamp 
                        FROM Messages 
                        WHERE ConversationId = @id 
                        ORDER BY Timestamp ASC", connection);
                    msgCommand.Parameters.AddWithValue("@id", id);

                    using var msgReader = msgCommand.ExecuteReader();
                    while (msgReader.Read())
                    {
                        var message = new Message(
                            msgReader["Role"].ToString(),
                            msgReader["Content"].ToString()
                        )
                        {
                            Timestamp = DateTime.Parse(msgReader["Timestamp"].ToString())
                        };
                        conversation.Messages.Add(message);
                    }
                }

                return conversation;
            });
        }

        /// <summary>
        /// 删除对话
        /// </summary>
        public async Task DeleteConversationAsync(int id)
        {
            await Task.Run(() =>
            {
                using var connection = _dbContext.GetConnection();
                connection.Open();

                using var command = new SqliteCommand("DELETE FROM Conversations WHERE Id = @id", connection);
                command.Parameters.AddWithValue("@id", id);
                command.ExecuteNonQuery();
            });
        }

        /// <summary>
        /// 自动生成对话标题
        /// </summary>
        public string GenerateTitleFromFirstMessage(string firstMessage)
        {
            if (string.IsNullOrWhiteSpace(firstMessage))
                return "新对话";

            // 取前20个字符作为标题，如果超过20个字符则添加省略号
            var title = firstMessage.Length > 20 
                ? firstMessage.Substring(0, 20) + "..." 
                : firstMessage;
            
            // 替换可能引起问题的字符
            title = title.Replace("\n", " ").Replace("\r", " ");
            
            return title;
        }
    }
}