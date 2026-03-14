using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Concurrent;

var botToken = "8718674261:AAGDP5QO3ZOO775lA-2WWHhG50jGhXd_CTo"; // ⚠️ ВСТАВЬ СВОЙ ТОКЕН СЮДА
var botClient = new TelegramBotClient(botToken);

// Хранилище состояний пользователей (ждем имя, ждем телефон)
var userStates = new ConcurrentDictionary<long, string>();

using CancellationTokenSource cts = new();

ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = { } // Получать все обновления
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();
Console.WriteLine($"✅ Бот @{me.Username} запущен и готов к работе!");
Console.WriteLine("📢 Нажмите любую клавишу для остановки...");
Console.ReadKey();
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Message is not { } message)
        return;
    
    if (message.Text is not { } messageText)
        return;

    var chatId = message.Chat.Id;

    // Проверяем, в каком состоянии пользователь
    if (userStates.TryGetValue(chatId, out var state))
    {
        switch (state)
        {
            case "awaiting_name":
                // Сохраняем имя и спрашиваем телефон
                userStates[chatId] = $"name:{messageText}";
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "📞 Спасибо! Теперь укажите ваш телефон:",
                    cancellationToken: cancellationToken
                );
                return;

            case string s when s.StartsWith("name:"):
                // Получили телефон, сохраняем заявку
                var name = s.Substring(5);
                var phone = messageText;
                
                // ⚠️ ВАЖНО: ВСТАВЬ СВОЙ CHAT ID СЮДА (узнай у @userinfobot)
                var adminChatId = 6350825687; // ЗАМЕНИ НА СВОЙ ID!
                
                // Отправляем заявку админу
                await botClient.SendTextMessageAsync(
                    chatId: adminChatId,
                    text: $"🔥 НОВАЯ ЗАЯВКА!\n👤 Имя: {name}\n📞 Телефон: {phone}",
                    cancellationToken: cancellationToken
                );

                // Очищаем состояние пользователя
                userStates.TryRemove(chatId, out _);

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "✅ Спасибо! Менеджер свяжется с вами в ближайшее время.",
                    cancellationToken: cancellationToken
                );
                return;
        }
    }

    // Обычная обработка команд (когда пользователь не в процессе заполнения)
    switch (messageText)
    {
        case "/start":
            // Создаем клавиатуру с кнопками
            var replyKeyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("📝 Оставить заявку") },
                new[] { new KeyboardButton("📍 Адрес"), new KeyboardButton("📞 Контакты") }
            })
            {
                ResizeKeyboard = true
            };

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "👋 Здравствуйте! Я бот-помощник.\n\nХотите оставить заявку или узнать контакты?",
                replyMarkup: replyKeyboard,
                cancellationToken: cancellationToken
            );
            break;

        case "📝 Оставить заявку":
            // Устанавливаем состояние "ждем имя"
            userStates[chatId] = "awaiting_name";
            
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "👤 Как вас зовут?",
                cancellationToken: cancellationToken
            );
            break;

        case "📍 Адрес":
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "🏢 Мы находимся по адресу:\nг. Москва, ул. Тверская, д. 1",
                cancellationToken: cancellationToken
            );
            break;

        case "📞 Контакты":
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "📱 Телефон: +7 (999) 123-45-67\n📧 Email: info@example.com\n🌐 Сайт: example.com",
                cancellationToken: cancellationToken
            );
            break;

        default:
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "🤖 Я вас не понял. Пожалуйста, используйте кнопки меню.",
                cancellationToken: cancellationToken
            );
            break;
    }
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    Console.WriteLine($"❌ Ошибка: {exception.Message}");
    return Task.CompletedTask;
}