using System;
using System.Collections.Generic;

namespace HexaFlow.Models
{
    public class Conversation
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<Message> Messages { get; set; } = new List<Message>();
    }
}