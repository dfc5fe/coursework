using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Runtime;
using System;
using System.Threading.Tasks;

namespace TelegramBot.Services
{
    public class DynamoDbService
    {
        private readonly DynamoDBContext _context;

        public DynamoDbService(string accessKey, string secretKey, RegionEndpoint region)
        {
            var credentials = new BasicAWSCredentials(accessKey, secretKey);
            var client = new AmazonDynamoDBClient(credentials, region);
            _context = new DynamoDBContext(client);
        }

        public async Task SaveChatLog(ChatLog log)
        {
            await _context.SaveAsync(log);
        }
    }

    [DynamoDBTable("ChatData")]
    public class ChatLog
    {
        [DynamoDBHashKey]
        public string TextMessages { get; set; }

        [DynamoDBProperty]
        public long ChatId { get; set; }

        [DynamoDBProperty]
        public string Username { get; set; }

        [DynamoDBProperty]
        public string Message { get; set; }

        [DynamoDBProperty]
        public DateTime Time { get; set; }
    }
}