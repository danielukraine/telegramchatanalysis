using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;


class TextEntity
{
  public string? type {get;set;}
  public string? text {get;set;}
}
class Chat
{
  public string? name {get; set;}
  public List<Message>? messages {get; set;}
}
class Message
{
  public int id {get;set;}
  public string? from {get;set;}
  //Text related
  public List<TextEntity>? text_entities {get;set;}
  public string? messageText => extractText(text_entities);
 
  public int messageLengthBeforeReponse {get;set;}
  
  //Message type related
  public string? media_type {get;set;}
  public string? type {get;set;}
  public string? photo {get;set;}
  public string? action {get;set;}
  public string? message_type => setMessageType(media_type, action, type, photo);
   public int messageLength => message_type == "text"? messageText.Length: duration_seconds;
  public int duration_seconds {get;set;}
  public string? actor {get;set;}

  public string? name => string.IsNullOrEmpty(actor)? from: actor;

  public int date_unixtime {get;set;}
  //Date Related
  public DateTime date {get;set;}
  public int weekDay => (int)date.DayOfWeek;
  public int hour => date.Hour;
  public double sinWeekDay => Math.Sin(2 * Math.PI * (int)date.DayOfWeek / 7);
  public double cosWeekDay => Math.Cos(2 * Math.PI * (int)date.DayOfWeek / 7);
  public double sinHour => Math.Sin(2 * Math.PI * date.Hour / 24);
  public double cosHour => Math.Cos(2 * Math.PI * date.Hour / 24);
  public double sinMinute => Math.Sin(2 * Math.PI * date.Minute / 60);
  public double cosMinute => Math.Cos(2 * Math.PI * date.Minute /60);
  
  public string? messageTypeBeforeResponse {get;set;}

  //Response time related

  //Hot much time it took to answer the message
  public int ownResponseTime {get;set;}

  //How much time it took to get answer to the message
  public int otherResponseTime {get;set;}
  //Sequance of message of the person itself
  public int ownMessagesSentBeforeAnswer {get;set;}
  //Sequance of messages of another person
  public int otherMessagesSentBeforeAnswer {get;set;}
  public int answerToOwnSequance {get;set;}
  public int answerToOtherSequance {get;set;}
  public int isConversation {get;set;}
  public int messagesInConversationBeforeMessage {get;set;}
  public double hourWeekdayRate {get;set;}
  
  public static string setMessageType(string media_type, string action, string type, string photo)
  {
    string message_type;

    if (!string.IsNullOrEmpty(media_type))
    {
      message_type = media_type;
    }else{
      if (type == "service")
      {
        message_type = action;
      }else if (string.IsNullOrEmpty(photo)){
        message_type = "text";
      }else{
        message_type = "photo";
      }
    }
  return message_type;
  }

public static string extractText(List<TextEntity>? textEntity)
{
    if (textEntity == null || !textEntity.Any())  // Check for null or empty list
    {
        return "";
    }
    
    return textEntity[0].text;
}

public static void WriteToFile(List<Message> messages, string filePath)
{
    using (StreamWriter writer = new StreamWriter(filePath, append: false))
    {
        // Write the header row
        writer.WriteLine("id,from,date,weekDay,hour,type,length,text");

        foreach (Message message in messages)
        {
            // Format the CSV line with proper escaping
            string csvLine = $"{EscapeCsv(message.id.ToString())}," +
                             $"{EscapeCsv(message.name)}," +
                             $"{message.date:yyyy-MM-dd}," +
                             $"{message.weekDay}," +
                             $"{message.hour}," +
                             $"{EscapeCsv(message.message_type)}," +
                             $"{message.messageLength}," +
                             $"{EscapeCsv(message.messageText)}";

            writer.WriteLine(csvLine);
        }
    }
}

// ✅ Fix: Escape Quotes and Commas in Text Fields
private static string EscapeCsv(string input)
{
    if (string.IsNullOrEmpty(input)) return "";
    if (input.Contains("\""))
    {
        input = input.Replace("\"", "\"\""); // Escape double quotes by doubling them
    }
    if (input.Contains(",") || input.Contains("\n") || input.Contains("\r"))
    {
        return $"\"{input}\"";  // Wrap text in quotes if it contains commas, newlines, or double quotes
    }
    return input;
}

}

//Class for extracting messages from the JSON file and convert them into a List object
class ExtractMessages
{
  public static List<Message> extractMessages(string filePath)
  {
      string jsonData = File.ReadAllText(filePath);
      Chat? chat = JsonConvert.DeserializeObject<Chat>(jsonData);
      return chat.messages;
  }
}

class GetDataForMl
{
  // Function to label messages with conversation groups and response times
  // Parameters:
  //   messages -> list of Message objects (ordered by time)
  //   hourWeekStat -> list of dictionaries mapping [weekday][hour] to message activity rate
  // Returns: void (updates message objects in-place)
  public static void setConversationAndResponseTimes(List<Message> messages, List<Dictionary<int, double>> hourWeekStat)
  {
    int maxConvTime =600; // Max gap (in seconds) to consider messages as part of a conversation
    int conversationMessagesCounter = 0;
    int messagesInLast15Minutes = 0;
    string lastSenderMessage = messages[0].from;
    int ownMessages = 0;
    int firstMessageTime = messages[0].date_unixtime;
    int startingIndex = 0;
    int previousMessageTime = 0;
    int conversationStartIndex = 0;

    for (int i = 0; i < messages.Count(); i++)
    {
      Message message = messages[i];

      // Set previous message info for ML features
      if (i != 0)
      {
        message.messageLengthBeforeReponse = messages[i-1].messageLength;
        message.messageTypeBeforeResponse = messages[i-1].message_type;
      }else{
        message.messageLengthBeforeReponse = 10;
        message.messageTypeBeforeResponse = "text";
      }

      // Set hour-activity rate
      message.hourWeekdayRate = hourWeekStat[message.weekDay][message.hour];

      int lastMessageTime = message.date_unixtime;

      // Check if current message is part of an ongoing conversation
      if (lastMessageTime - previousMessageTime < maxConvTime)
      {
        if (conversationMessagesCounter == 0)
        {
          conversationStartIndex = i;
        }
        conversationMessagesCounter++;
      }else{
        // Tag all messages in previous conversation
        for (int k = conversationStartIndex; k < i; k++)
        {
          if (conversationMessagesCounter > 10){
            messages[k].isConversation = 1;
            messages[k].messagesInConversationBeforeMessage = k - conversationStartIndex + 1;
          }else{
            messages[k].isConversation = 0;
            messages[k].messagesInConversationBeforeMessage = 0;
          }
        }

        conversationMessagesCounter = 0;
        conversationStartIndex = i;
      }

      previousMessageTime = lastMessageTime;

      // If sender is same as before, continue sequence
      if (message.from == lastSenderMessage)
      {
        ownMessages++;
        message.otherMessagesSentBeforeAnswer = 0;
      } 
      else // Sender switched, treat as a response
      {
        for (int j = startingIndex; j < i; j++)
        {
          messages[j].ownMessagesSentBeforeAnswer = ownMessages;
          messages[j].otherResponseTime = message.date_unixtime - messages[j].date_unixtime; 
        }

        message.answerToOtherSequance = message.date_unixtime - firstMessageTime;
        message.otherResponseTime = message.date_unixtime - messages[i-1].date_unixtime;
        message.ownResponseTime = messages[i-1].otherResponseTime;

        firstMessageTime = message.date_unixtime;

        message.otherMessagesSentBeforeAnswer = ownMessages;
        ownMessages = 1;
        startingIndex = i;
        lastSenderMessage = message.from;
      }
    }

    // Fill in final sequence for messages at the end
    for (int j = startingIndex; j < messages.Count; j++)
    {
        messages[j].ownMessagesSentBeforeAnswer = ownMessages;
    }
  }

  // Function to calculate hourly activity rate for each weekday
  // Parameters:
  //   messages -> list of all messages
  //   yourName, personName -> optional filters to compute stats per person
  // Returns: List of 7 dictionaries (one per weekday), mapping hour (0-23) -> normalized message count (0-1)
  public static List<Dictionary<int, double>> hourAndWeekDayRate(List<Message> messages, string yourName = "", string personName = "")
  {
    List<Dictionary<int, double>> hourAndDayStat = new List<Dictionary<int, double>>();
    int messageCount = 0;

    for (int i = 0; i < 7; i++)
    {
      hourAndDayStat.Add(Enumerable.Range(0, 24)
      .ToDictionary(hour => hour, _ => 0.0));
    }

    foreach(Message message in messages)
    {
      int hour = message.date.Hour;
      int weekDay = (int)message.date.DayOfWeek;

      if (!string.IsNullOrEmpty(yourName) && message.from == yourName)
      {
        hourAndDayStat[weekDay][hour]++;
        messageCount++;
      }else if (!string.IsNullOrEmpty(personName) && message.from == personName)
      {
        hourAndDayStat[weekDay][hour]++;
        messageCount++;
      }else if (string.IsNullOrEmpty(yourName) && string.IsNullOrEmpty(personName))
      {
        hourAndDayStat[weekDay][hour]++;
        messageCount++;
      }
    }

    foreach(Dictionary<int, double> dict in hourAndDayStat)
    {
      int summ = (int)dict.Values.Sum();
      foreach(var key in dict.Keys)
      {
        double value = (double)dict[key] / summ;
        dict[key] = value ;
      }
    }
    Console.WriteLine($"{messageCount}");
    return hourAndDayStat;
  }

  // Function to export processed message data to CSV for ML
  // Parameters:
  //   messages -> list of Message objects (should be preprocessed)
  //   dataSetSize -> max number of rows to export
  //   filePath -> destination path for CSV file
  // Returns: void (writes file to disk)
  public static void convertToCSV(List<Message> messages, int dataSetSize, string filePath)
  {
    using (StreamWriter writer = new StreamWriter(filePath, append: false))
    {
        writer.WriteLine("id;from;messageLengthBeforeResponse;messageLength;messageType;messageTypeBeforeResponse;date;weekDaySin;weekDayCos;hourSin;hourCos;minuteSin;minuteCos;ownResponseTime;otherResponseTime;ActivityRate;ownSequenceBeforeAnswer;otherSequance;isConversation;messagesInconversation");

        int count = 0;
        foreach (var message in messages)
        {
           if (count >= dataSetSize) break;
            writer.WriteLine($"{message.id};{message.from};{message.messageLength};{message.messageLengthBeforeReponse};{message.message_type};{message.messageTypeBeforeResponse};{DateOnly.FromDateTime(message.date)};{message.sinWeekDay};{message.cosWeekDay};{message.sinHour}; {message.cosHour}; {message.sinMinute};{message.cosMinute};{message.ownResponseTime};{message.otherResponseTime};{message.hourWeekdayRate};{message.ownMessagesSentBeforeAnswer};{message.otherMessagesSentBeforeAnswer};{message.isConversation};{message.messagesInConversationBeforeMessage}");
          count++;
        }
    } 
  }
}

class GetStats
{
    // Function to find index of a message by a specific date
    // Parameters: messages -> list of all messages, dateToFind -> date to search for
    // Returns: index of the first message on that date, or -1 if not found
    public static int getSpecificDate(List<Message> messages, DateOnly dateToFind)
    {
        int dateIndexHigh = messages.Count() - 1;
        int dateIndexLow = 0;
        int dateIndexMid;

        while (dateIndexLow <= dateIndexHigh)
        {
            dateIndexMid = (dateIndexHigh + dateIndexLow) / 2;
            DateOnly date = DateOnly.FromDateTime(messages[dateIndexMid].date);

            if (date == dateToFind)
            {
                while (dateIndexMid > 0 && DateOnly.FromDateTime(messages[dateIndexMid - 1].date) == dateToFind)
                {
                    dateIndexMid--;
                }
                return dateIndexMid;
            }
            else
            {
                if (date > dateToFind)
                {
                    dateIndexHigh = dateIndexMid - 1;
                }
                else
                {
                    dateIndexLow = dateIndexMid + 1;
                }
            }
        }

        return -1;
    }

    // Function to generate list of all dates from first to last message
    // Parameters: messages -> list of all messages
    // Returns: List of dates covered by the chat
    public static List<DateOnly> getDateRange(List<Message> messages)
    {
        List<DateOnly> dateRange = new List<DateOnly>();
        DateOnly startDate = DateOnly.FromDateTime(messages[0].date);
        DateOnly endDate = DateOnly.FromDateTime(messages[^1].date);

        for (DateOnly date = startDate; date <= endDate; date = date.AddDays(1))
        {
            dateRange.Add(date);
        }

        return dateRange;
    }

    // Function to count messages each day by person
    // Parameters: allMessages -> full chat history
    //             firstPerson -> name of person A
    //             dateRange -> full range of chat days
    // Returns: Dictionary with each date mapped to [personA_count, personB_count, total_count]
    public static Dictionary<DateOnly, List<int>> countMessages(List<Message> allMessages,
                                                                 string firstPerson,
                                                                 List<DateOnly> dateRange)
    {
        Dictionary<DateOnly, List<int>> messagesEveryDay = new Dictionary<DateOnly, List<int>>();

        foreach (DateOnly date in dateRange)
        {
            messagesEveryDay[date] = new List<int> { 0, 0, 0 };
        }

        foreach (Message message in allMessages)
        {
            DateOnly messageDate = DateOnly.FromDateTime(message.date);
            if (message.from == firstPerson)
            {
                messagesEveryDay[messageDate][0]++;
                messagesEveryDay[messageDate][2]++;
            }
            else
            {
                messagesEveryDay[messageDate][1]++;
                messagesEveryDay[messageDate][2]++;
            }
        }
        return messagesEveryDay;
    }

    // Function to count how many times certain words were used
    // Parameters: messages -> full message list
    //             wordsToFind -> list of words to search for
    //             dateRange -> range of dates to scan
    //             fromSpecificPerson -> (optional) filter by sender name
    // Returns: Dictionary mapping date to array of word counts for each searched word
    public static Dictionary<DateOnly, int[]> wordUsageStats(List<Message> messages, string[] wordsToFind,
                                                             List<DateOnly> dateRange, string fromSpecificPerson = "")
    {
        int wordsAmount = wordsToFind.Count();
        Dictionary<DateOnly, int[]> wordsStats = new Dictionary<DateOnly, int[]>();

        foreach (DateOnly date in dateRange)
        {
            wordsStats[date] = new int[wordsAmount];
        }

        foreach (Message message in messages)
        {
            if (message.text_entities.Any() == true)
            {
                foreach (string word in wordsToFind)
                {
                    if (message.text_entities[0].text.Contains(word, StringComparison.OrdinalIgnoreCase)
                        && (fromSpecificPerson == message.from || string.IsNullOrEmpty(fromSpecificPerson)))
                    {
                        int index = Array.IndexOf(wordsToFind, word);
                        DateOnly messageDate = DateOnly.FromDateTime(message.date);
                        wordsStats[messageDate][index]++;
                    }
                }
            }
        }
        return wordsStats;
    }

    // Function to calculate message count per hour
    // Parameters: messages -> all messages
    //             yourName, personName -> optional filters by sender name
    // Returns: Dictionary of hour (0-23) -> normalized frequency (0-1)
    public static Dictionary<int, double> messagePerHour(List<Message> messages, string yourName = "", string personName = "")
    {
        Dictionary<int, double> timeStat = Enumerable.Range(0, 24)
          .ToDictionary(hour => hour, _ => 0.0);

        foreach (Message message in messages)
        {
            int hour = message.date.Hour;
            if (!string.IsNullOrEmpty(yourName) && message.from == yourName)
                timeStat[hour]++;
            else if (!string.IsNullOrEmpty(personName) && message.from == personName)
                timeStat[hour]++;
            else if (string.IsNullOrEmpty(yourName) && string.IsNullOrEmpty(personName))
                timeStat[hour]++;
        }

        double summ = timeStat.Values.Sum();

        foreach (var key in timeStat.Keys)
        {
            double value = timeStat[key] / summ;
            timeStat[key] = Math.Round(value, 4);
        }
        return timeStat;
    }

    // Function to calculate message count per weekday
    // Parameters: messages -> all messages
    //             yourName, personName -> optional filters by sender name
    // Returns: Dictionary of weekday (0-6) -> scaled frequency (0-10000)
    public static Dictionary<int, int> messagePerWeekDay(List<Message> messages, string yourName = "", string personName = "")
    {
        Dictionary<int, int> weekStat = Enumerable.Range(0, 7)
            .ToDictionary(weekDay => weekDay, _ => 0);

        foreach (Message message in messages)
        {
            int weekDay = (int)DateOnly.FromDateTime(message.date).DayOfWeek;
            if (!string.IsNullOrEmpty(yourName) && message.from == yourName)
                weekStat[weekDay]++;
            else if (!string.IsNullOrEmpty(personName) && message.from == personName)
                weekStat[weekDay]++;
            else if (string.IsNullOrEmpty(yourName) && string.IsNullOrEmpty(personName))
                weekStat[weekDay]++;
        }

        int summ = weekStat.Values.Sum();

        foreach (var key in weekStat.Keys)
        {
            double normalizedValue = (double)weekStat[key] / summ;
            weekStat[key] = (int)(normalizedValue * 10000);
        }

        return weekStat;
    }
}

class Program
{
    // Entry point of the application
    static void Main()
    {
        // Name of the JSON file and persons in the chat, change the names for own usage
        string filePath = "telegramChat.json";
        string firstPerson = "Daniil";
        string secondPerson = "Jorge";

        List<Message> messages = ExtractMessages.extractMessages(filePath);
        List<DateOnly> dateRange = GetStats.getDateRange(messages);

        // Uncomment lines below for additional stats / feature generation

        // Dictionary<DateOnly, List<int>> result = GetStats.countMessages(messages, firstPerson, dateRange);
        // string[] wordsToFind = { "hello", "bye" };
        // Dictionary<DateOnly, int[]> wordStats = GetStats.wordUsageStats(messages, wordsToFind, dateRange);
        // Dictionary<int, double> timeStat = GetStats.messagePerHour(messages);
        // Dictionary<int, int> weekStat = GetStats.messagePerWeekDay(messages);

        List<Dictionary<int, double>> hourWeekStat = GetDataForMl.hourAndWeekDayRate(messages.Where(m => m.from == secondPerson).ToList());
        GetDataForMl.setConversationAndResponseTimes(messages, hourWeekStat);
        // int jorgeMessageCount = messages.Count(m => m.from == "Jorge");
        GetDataForMl.convertToCSV(messages, 10000, "mlDataTelegram.csv");

        // Export basic message info to CSV
        Message.WriteToFile(messages, "newStat1.csv");
    }
}
