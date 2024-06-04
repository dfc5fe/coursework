using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.GettingUpdates;
using System.Collections.Generic;
using TelegramBot.Services;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

namespace TelegramBot
{
    class Program
    {
        private static Dictionary<long, string> userStates = new Dictionary<long, string>();
        private static Dictionary<long, string> pendingWords = new Dictionary<long, string>();
        private static AmazonDynamoDBClient dynamoDbClient;

        static async Task Main(string[] args)
        {
            var client = new TelegramBotClient("");
            var dictionaryService = new DictionaryService();
            int offset = 0;

            var credentials =
                new BasicAWSCredentials("", "");
            dynamoDbClient = new AmazonDynamoDBClient(credentials, Amazon.RegionEndpoint.EUNorth1);

            while (true)
            {
                try
                {
                    var updates = await client.GetUpdatesAsync(offset);
                    if (updates.Any())
                    {
                        foreach (var update in updates)
                        {
                            var chatId = update.Message.Chat.Id;
                            var username = update.Message.Chat.Username;
                            var messageText = update.Message.Text;
                            long unixTimestamp = update.Message.Date;
                            DateTime dateTime = DateTimeOffset
                                .FromUnixTimeSeconds(unixTimestamp)
                                .DateTime
                                .AddHours(3); // Adjust for your timezone if necessary

                            Console.WriteLine($"Chat Id: {chatId}");
                            Console.WriteLine($"Username: {username}");
                            Console.WriteLine($"Message: {messageText}");
                            Console.WriteLine($"Time: {dateTime}");

                            await SaveMessageToDynamoDB(chatId, username, messageText, dateTime);

                            if (!string.IsNullOrEmpty(messageText))
                            {
                                if (messageText.StartsWith("/"))
                                {
                                    HandleCommand(client, chatId, messageText);
                                }
                                else
                                {
                                    await HandleUserInput(client, chatId, messageText, dictionaryService, dateTime);
                                }

                                offset = updates.Last().UpdateId + 1;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }


        private static void HandleCommand(TelegramBotClient client, long chatId, string command)
        {
            switch (command)
            {
                case "/definition":
                    userStates[chatId] = "definition";
                    SendMessageAndLog(client, chatId, "Enter a word:");
                    break;
                case "/synonyms":
                    userStates[chatId] = "synonyms";
                    SendMessageAndLog(client, chatId, "Enter a word:");
                    break;
                case "/antonyms":
                    userStates[chatId] = "antonyms";
                    SendMessageAndLog(client, chatId, "Enter a word:");
                    break;
                case "/phonetics":
                    userStates[chatId] = "phonetics";
                    SendMessageAndLog(client, chatId, "Enter a word:");
                    break;
                case "/addword":
                    userStates[chatId] = "addword";
                    SendMessageAndLog(client, chatId, "Enter the word you want to add:");
                    break;
                case "/update":
                    userStates[chatId] = "update";
                    SendMessageAndLog(client, chatId, "Enter the word you want to update:");
                    break;
                case "/delete":
                    userStates[chatId] = "delete";
                    SendMessageAndLog(client, chatId, "Enter the word you want to delete:");
                    break;
                default:
                    SendMessageAndLog(client, chatId, "Unknown command");
                    break;
            }
        }

        private static async Task HandleUserInput(TelegramBotClient client, long chatId, string userInput,
            DictionaryService dictionaryService, DateTime dateTime)
        {
            if (userStates.TryGetValue(chatId, out var state))
            {
                userInput = userInput.ToLower();
                switch (state)
                {
                    case "definition":
                        await SendWordDefinitions(client, chatId, dictionaryService, userInput, dateTime);
                        break;
                    case "synonyms":
                        await SendWordSynonyms(client, chatId, dictionaryService, userInput, dateTime);
                        break;
                    case "antonyms":
                        await SendWordAntonyms(client, chatId, dictionaryService, userInput, dateTime);
                        break;
                    case "phonetics":
                        await SendWordPhonetics(client, chatId, dictionaryService, userInput, dateTime);
                        break;
                    case "addword":
                        pendingWords[chatId] = userInput;
                        userStates[chatId] = "addword_definition";
                        await SendMessageAndLog(client, chatId, $"Enter the definition for the word '{userInput}':");
                        break;
                    case "addword_definition":
                        await AddWordToDynamoDB(chatId, pendingWords[chatId], userInput);
                        await SendMessageAndLog(client, chatId,
                            $"Word '{pendingWords[chatId]}' with definition '{userInput}' added.");
                        pendingWords.Remove(chatId);
                        userStates.Remove(chatId);
                        break;
                    case "update":
                        pendingWords[chatId] = userInput;
                        userStates[chatId] = "update_definition";
                        await SendMessageAndLog(client, chatId,
                            $"Enter the new definition for the word '{userInput}':");
                        break;
                    case "update_definition":
                        await UpdateWordInDynamoDB(chatId, pendingWords[chatId], userInput);
                        await SendMessageAndLog(client, chatId,
                            $"Word '{pendingWords[chatId]}' updated with new definition '{userInput}'.");
                        pendingWords.Remove(chatId);
                        userStates.Remove(chatId);

                        break;
                    case "delete":
                        await DeleteWordFromDynamoDB(chatId, userInput);
                        await SendMessageAndLog(client, chatId, $"Word '{userInput}' deleted.");
                        userStates.Remove(chatId);
                        break;
                }
            }
            else
            {
                await SendMessageAndLog(client, chatId, "Please enter a command first.");
            }
        }

        private static async Task SendWordDefinitions(TelegramBotClient client, long chatId,
            DictionaryService dictionaryService, string word, DateTime dateTime)
        {
            try
            {
                // Check if the word exists in the UserWords table
                var getItemRequest = new GetItemRequest
                {
                    TableName = "UserWords",
                    Key = new Dictionary<string, AttributeValue>
                    {
                        { "Word", new AttributeValue { S = word } }
                    }
                };

                var getItemResponse = await dynamoDbClient.GetItemAsync(getItemRequest);
                if (getItemResponse.Item != null && getItemResponse.Item.ContainsKey("Definition"))
                {
                    var definition = getItemResponse.Item["Definition"].S;
                    await SendMessageAndLog(client, chatId, $"Definition from user database: {definition}");
                    await SaveBotAnswerToDynamoDB(dateTime, chatId, "definition", word, definition);
                }
                else
                {
                    // If word is not in UserWords table, call the API to get the definition
                    var definitions = await dictionaryService.GetDefinitions(word);
                    var combinedResponse = string.Join("\n", definitions);
                    foreach (var definition in definitions)
                    {
                        await SendMessageAndLog(client, chatId, definition);
                    }

                    await SaveBotAnswerToDynamoDB(dateTime, chatId, "definition", word, combinedResponse);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error during API request: {ex.Message}";
                await SendMessageAndLog(client, chatId, errorMessage);
                await SaveBotAnswerToDynamoDB(dateTime, chatId, "definition", word, errorMessage);
            }
        }

        private static async Task SendWordSynonyms(TelegramBotClient client, long chatId,
            DictionaryService dictionaryService, string word, DateTime dateTime)
        {
            try
            {
                var synonyms = await dictionaryService.GetSynonyms(word);
                var combinedResponse = $"Synonyms for '{word}': {string.Join(", ", synonyms)}";
                await SendMessageAndLog(client, chatId, combinedResponse);
                await SaveBotAnswerToDynamoDB(dateTime, chatId, "synonyms", word, combinedResponse);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error during API request: {ex.Message}";
                await SendMessageAndLog(client, chatId, errorMessage);
                await SaveBotAnswerToDynamoDB(dateTime, chatId, "synonyms", word, errorMessage);
            }
        }


        private static async Task SendWordAntonyms(TelegramBotClient client, long chatId,
            DictionaryService dictionaryService, string word, DateTime dateTime)
        {
            try
            {
                var antonyms = await dictionaryService.GetAntonyms(word);
                var combinedResponse = $"Antonyms for '{word}': {string.Join(", ", antonyms)}";
                await SendMessageAndLog(client, chatId, combinedResponse);
                await SaveBotAnswerToDynamoDB(dateTime, chatId, "antonyms", word, combinedResponse);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error during API request: {ex.Message}";
                await SendMessageAndLog(client, chatId, errorMessage);
                await SaveBotAnswerToDynamoDB(dateTime, chatId, "antonyms", word, errorMessage);
            }
        }

        private static async Task SendWordPhonetics(TelegramBotClient client, long chatId,
            DictionaryService dictionaryService, string word, DateTime dateTime)
        {
            try
            {
                var phonetics = await dictionaryService.GetPhonetics(word);
                var combinedResponse = $"Phonetics for '{word}': {string.Join(", ", phonetics)}";
                await SendMessageAndLog(client, chatId, combinedResponse);
                await SaveBotAnswerToDynamoDB(dateTime, chatId, "phonetics", word, combinedResponse);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error during API request: {ex.Message}";
                await SendMessageAndLog(client, chatId, errorMessage);
                await SaveBotAnswerToDynamoDB(dateTime, chatId, "phonetics", word, errorMessage);
            }
        }

        private static async Task SaveMessageToDynamoDB(long chatId, string username, string messageText,
            DateTime dateTime)
        {
            var putItemRequest = new PutItemRequest
            {
                TableName = "ChatData",
                Item = new Dictionary<string, AttributeValue>
                {
                    { "DateTime", new AttributeValue { S = dateTime.ToString("o") } },
                    { "ChatID", new AttributeValue { N = chatId.ToString() } },
                    { "MessageText", new AttributeValue { S = messageText } },
                    { "Username", new AttributeValue { S = username } }
                }
            };

            try
            {
                await dynamoDbClient.PutItemAsync(putItemRequest);
                Console.WriteLine("Message saved to DynamoDB");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving message to DynamoDB: {ex.Message}");
            }
        }

        private static async Task SaveBotAnswerToDynamoDB(DateTime dateTime, long chatId, string requestType,
            string word, string response)
        {
            var putItemRequest = new PutItemRequest
            {
                TableName = "BotAnswers",
                Item = new Dictionary<string, AttributeValue>
                {
                    { "DateTime", new AttributeValue { S = dateTime.ToString("o") } },
                    { "ChatID", new AttributeValue { N = chatId.ToString() } },
                    { "RequestType", new AttributeValue { S = requestType } },
                    { "Word", new AttributeValue { S = word } },
                    { "Response", new AttributeValue { S = response } }
                }
            };

            try
            {
                await dynamoDbClient.PutItemAsync(putItemRequest);
                Console.WriteLine("Bot answer saved to DynamoDB");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving bot answer to DynamoDB: {ex.Message}");
            }
        }


        private static async Task AddWordToDynamoDB(long chatId, string word, string definition)
        {
            word = word.ToLower();
            var putItemRequest = new PutItemRequest
            {
                TableName = "UserWords",
                Item = new Dictionary<string, AttributeValue>
                {
                    { "Word", new AttributeValue { S = word } },
                    { "Definition", new AttributeValue { S = definition } }
                }
            };

            try
            {
                await dynamoDbClient.PutItemAsync(putItemRequest);
                Console.WriteLine("Word added to UserWords table");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding word to UserWords table: {ex.Message}");
            }
        }

        private static async Task UpdateWordInDynamoDB(long chatId, string word, string definition)
        {
            word = word.ToLower();
            var updateItemRequest = new UpdateItemRequest
            {
                TableName = "UserWords",
                Key = new Dictionary<string, AttributeValue>
                {
                    { "Word", new AttributeValue { S = word } }
                },
                AttributeUpdates = new Dictionary<string, AttributeValueUpdate>
                {
                    {
                        "Definition",
                        new AttributeValueUpdate
                            { Action = AttributeAction.PUT, Value = new AttributeValue { S = definition } }
                    }
                }
            };

            try
            {
                await dynamoDbClient.UpdateItemAsync(updateItemRequest);
                Console.WriteLine("Word updated in UserWords table");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating word in UserWords table: {ex.Message}");
            }
        }

        private static async Task DeleteWordFromDynamoDB(long chatId, string word)
        {
            word = word.ToLower();
            var deleteItemRequest = new DeleteItemRequest
            {
                TableName = "UserWords",
                Key = new Dictionary<string, AttributeValue>
                {
                    { "Word", new AttributeValue { S = word } }
                }
            };

            try
            {
                await dynamoDbClient.DeleteItemAsync(deleteItemRequest);
                Console.WriteLine("Word deleted from UserWords table");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting word from UserWords table: {ex.Message}");
            }
        }

        private static async Task SendMessageAndLog(TelegramBotClient client, long chatId, string message)
        {
            await client.SendMessageAsync(chatId, message);
            Console.WriteLine($"Sent to {chatId}: {message}");
        }
    }
}
