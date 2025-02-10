using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NWSeminar5
{
    public enum Command
    {
        Register,
        Message,
        Confirmation,
        Exit
    }
    public class MessageUDP
    {
        public Command Command { get; set; }
        public int? Id { get; set; }

        public string FromName { get; set; }  
        
        public string ToName { get; set; }
        public string Text { get; set; }

        public DateTime STime { get; set; }

        public MessageUDP() { }

        public MessageUDP(string fromname, string text)
        {
            this.FromName = fromname;
            this.Text = text;
            STime = DateTime.Now;
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public static MessageUDP FromJson(string json)
        {
            return JsonSerializer.Deserialize<MessageUDP>(json);
        }

        public override string ToString()
        {
            return $"{STime.ToShortTimeString()}{new string(' ', 2)}{FromName}: {Text}";
        }
    }

}
