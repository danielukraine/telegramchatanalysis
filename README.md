# Chat Message Analysis and ML Feature Extraction (C#)

This project is a **chat analytics tool** built in C#. It processes exported chat data (in JSON format), extracts useful features, and prepares the data for **machine learning and behavioral analysis**.

---

## 🚀 Features

* 📥 Load chat history from JSON
* 🧠 Automatically detect conversation boundaries
* ⏱ Calculate response times between participants
* 🕒 Extract cyclical time features (sin/cos of hour, weekday, minute)
* 🧾 Export clean CSV datasets for ML models
* 📊 Hourly and weekday activity rate statistics
* 🔍 Word usage tracking per day and per sender

---

## 📁 Input

A Telegram-style JSON export file (e.g., `result.json`) with structure like:

```json
{
  "name": "Chat with Jorge",
  "messages": [
    {
      "id": 1,
      "from": "Daniil",
      "text": [{ "type": "plain", "text": "Hello" }],
      "date_unixtime": 1670000000,
      "date": "2023-03-05T14:33:00"
    },
    ...
  ]
}
```

---

## 🛠 How It Works

### 🔄 Preprocessing

* Deserializes the JSON using `Newtonsoft.Json`
* Parses each message into a custom `Message` class

### 💬 Conversation Grouping

* A new conversation is triggered if more than `600s` (10 min) passes between messages
* Messages within a conversation are tagged with sequence and activity data

### ⏳ Response Time Tracking

* Tracks how long it takes each participant to respond
* Stores:

  * Own response time
  * Other’s response time
  * Message count before receiving a response

### 🧠 Feature Engineering

Each message is enriched with:

* Message length, type
* Time features: `sin(hour)`, `cos(weekday)`, etc.
* `hourWeekdayRate` = normalized activity rate at that time
* Conversation flags: isConversation, sequenceBeforeAnswer

### 📤 CSV Export

Generates datasets like:

```csv
id;from;messageLengthBeforeResponse;messageLength;...;isConversation;messagesInConversationBeforeMessage
1;Daniil;24;30;text;text;2023-03-05;0.5;0.866;0.0;1.0;...
```

---

## 📦 Output Files

* `newStat.csv` — basic stats dump
* `firsttry.csv` — full ML feature set for selected participant (e.g., Jorge)

---

## 🧪 Example Use in Main()

```csharp
List<Message> messages = ExtractMessages.extractMessages("result.json");
var hourStats = GetDataForMl.hourAndWeekDayRate(messages);
GetDataForMl.setConversationAndResponseTimes(messages, hourStats);
GetDataForMl.convertToCSV(messages, 1000, "ml_data.csv");
```

---

## 📈 Use Cases

* Build predictive ML models (e.g., when will someone respond?)
* Analyze behavior and engagement in conversations
* Visualize chat activity trends over time

---

## ✅ Requirements

* .NET Core or .NET Framework
* Newtonsoft.Json (`Install-Package Newtonsoft.Json`)

---

## 👨‍💻 Author

**Daniil** — self-built for learning and portfolio development

---

## 💡 Tips

* You can switch the target person in `Main()` to analyze different senders
* Works best with long one-on-one conversations (not group chats)

---

## 📌 License

This project is free to use and modify for educational or non-commercial purposes.
