using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using System.Threading;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Newtonsoft.Json;
using System.ComponentModel;
using System.IO;
using Microsoft.Win32;
using Telegram.Bot.Types.InputFiles;
using System.Linq;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Generic;

namespace TelegramBotWithWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {        
        ObservableCollection<TelegramUser> Users;
        TelegramBotClient bot;
        public MainWindow()
        {
            InitializeComponent();

            if (System.IO.File.Exists("_bill.json"))
            {
                string json = System.IO.File.ReadAllText("_bill.json");
                Users = JsonConvert.DeserializeObject<ObservableCollection<TelegramUser>>(json)!;
            }
            else
            {
                System.IO.File.Create("_bill.json");
                Users = new ObservableCollection<TelegramUser>();
            }                      

            usersList.ItemsSource = Users;            

            bot = new TelegramBotClient(BotConfiguration.BotToken);

            var cts = new CancellationTokenSource();

            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            var receiverOptions = new ReceiverOptions()
            {
                AllowedUpdates = Array.Empty<UpdateType>(),
                ThrowPendingUpdates = true,
            };            

            bot.StartReceiving(updateHandler: HandleUpdateAsync,
                               pollingErrorHandler: PollingErrorHandler,
                               receiverOptions: receiverOptions,
                               cancellationToken: cts.Token);

            txtBoxSendMessage.KeyDown += (s, e) => { if (e.Key == Key.Return) { SendMessage(); } };        
                        
            this.Closed += MainWindow_OnClosed;
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {

            string msg = "Вы действительно хотите выйти из приложения?";
            MessageBoxResult result = MessageBox.Show(msg,
            "Бот сопровождения", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }      
        }
        private void MainWindow_OnClosed(object? sender, EventArgs e)
        {
            string json = JsonConvert.SerializeObject(Users);
            System.IO.File.WriteAllText("_bill.json", json);
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {

            var handler = update.Type switch
            {
                UpdateType.Message => BotOnMessageReceived(botClient, update.Message!),
                UpdateType.CallbackQuery => BotOnCallbackQueryReceived(botClient, update.CallbackQuery!),
                _ => UnknownUpdateHandlerAsync(botClient, update)
            };

            try
            {
                await handler;
            }
#pragma warning disable CA1031
            catch (Exception exception)
#pragma warning restore CA1031
            {
                await PollingErrorHandler(botClient, exception, cancellationToken);
            }
        }

        public async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
        {
            string msg = $"{DateTime.Now} {message.Chat.FirstName} {message.Chat.Id} {message.Text}";

            System.IO.File.AppendAllText("data.log", $"{msg}\n");

            Dispatcher.Invoke(() =>
            {
                var person = new TelegramUser(message.Chat.FirstName!, message.Chat.Id);
                if (!Users.Contains(person)) Users.Add(person);                
                Users[Users.IndexOf(person)].AddMessage($"{person.Nick}: {message.Text}\t Время: {DateTime.Now}");
            });

            var chatUserName = message.Chat.FirstName;

            DirectoryInfo subDirectory = new DirectoryInfo(".");
            var subDir = subDirectory.CreateSubdirectory(chatUserName!);

            if (message.Type == MessageType.Photo)
            {
                var fileId = message.Photo!.Last().FileId;
                var fileInfo = await botClient.GetFileAsync(fileId);
                var filePath = fileInfo.FilePath;

                string destinationFilePath = $"{chatUserName}\\{fileInfo.FileUniqueId}.jpg";
                await using FileStream fileStream = System.IO.File.OpenWrite(destinationFilePath);
                await botClient.DownloadFileAsync(
                    filePath: filePath!,
                    destination: fileStream);
            }

            if (message.Type == MessageType.Video)
            {
                var file = message.Video;
                var fileId = file!.FileId;
                var fileInfo = await botClient.GetFileAsync(fileId);
                var filePath = fileInfo.FilePath;

                string destinationFilePath = $"{chatUserName}\\{file.FileName}";
                await using FileStream fileStream = System.IO.File.OpenWrite(destinationFilePath);
                await botClient.DownloadFileAsync(
                    filePath: filePath!,
                    destination: fileStream);
            }

            if (message.Type == MessageType.Document)
            {
                var file = message.Document;
                var fileId = file!.FileId;
                var fileInfo = await botClient.GetFileAsync(fileId);
                var filePath = fileInfo.FilePath;

                string destinationFilePath = $"{chatUserName}\\{file.FileName}";
                await using FileStream fileStream = System.IO.File.OpenWrite(destinationFilePath);
                await botClient.DownloadFileAsync(
                    filePath: filePath!,
                    destination: fileStream);
            }

            if (message.Type == MessageType.Voice)
            {
                var file = message.Voice;
                var fileId = file!.FileId;
                var fileInfo = await botClient.GetFileAsync(fileId);
                var filePath = fileInfo.FilePath;

                string destinationFilePath = $"{chatUserName}\\{fileInfo.FileUniqueId}.mp4";
                await using FileStream fileStream = System.IO.File.OpenWrite(destinationFilePath);
                await botClient.DownloadFileAsync(
                    filePath: filePath!,
                    destination: fileStream);
            }

            if (message.Text is not { } messageText)
                return;

            var action = messageText.Split(' ')[0] switch
            {
                "/allFiles" => SendInlineKeyboard(botClient, message),                
                "/start" => Usage(botClient, message),
                _ => Usage(botClient, message)
            };

            static async Task<Message> SendInlineKeyboard(ITelegramBotClient botClient, Message message)
            {
                await botClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

                string[] fullFileNames = Directory.GetFiles($".\\{new DirectoryInfo(message.Chat.FirstName!)}");
                string[] fileNames = new string[fullFileNames.Length];

                for (int i = 0; i < fullFileNames.Length; i++)
                {
                    fileNames[i] = fullFileNames[i].Split('\\').Last();
                }

                var ilist = new List<InlineKeyboardButton[]>();

                for (int i = 0; i < fileNames.Length; i++)
                {
                    var inlineKeyboardButtons = new List<InlineKeyboardButton>()
                    {
                        InlineKeyboardButton.WithCallbackData(fileNames[i])
                    };
                    ilist.Add(inlineKeyboardButtons.ToArray());
                }

                InlineKeyboardMarkup inlineKeyboard = ilist.ToArray();

                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                            text: "Выберите файл для скачивания",
                                                            replyMarkup: inlineKeyboard);
            }

            static async Task<Message> Usage(ITelegramBotClient botClient, Message message)
            {
                const string usage = "Usage:\n" +
                                     "/allFiles   - показать все файлы, доступные для скачивания\n";
                                     

                return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                            text: usage,
                                                            replyMarkup: new ReplyKeyboardRemove());
            }
        }

        private async Task<Message> BotOnCallbackQueryReceived(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {

            await using Stream stream = System.IO.File.OpenRead($".\\{callbackQuery.From.FirstName}\\{callbackQuery.Data}");
            return await botClient.SendDocumentAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                document: new InputOnlineFile(content: stream, fileName: callbackQuery.Data));
        }

        private Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
        {
            Console.WriteLine($"Unknown update type: {update.Type}");
            return Task.CompletedTask;
        }

        public Task PollingErrorHandler(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        private void btnSendMessage(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        public void SendMessage()
        {
            if (usersList.SelectedItem != null)
            {
                var concreteUser = Users[Users.IndexOf(usersList.SelectedItem as TelegramUser)];
                string responseMsg = $"Support: {txtBoxSendMessage.Text} \t Время: {DateTime.Now}";

                string msg = $"{DateTime.Now} Support: {txtBoxSendMessage.Text} \t Время: {DateTime.Now}";

                System.IO.File.AppendAllText("data.log", $"{msg}\n");

                concreteUser.Messages.Add(responseMsg);

                long id = concreteUser.Id;

                bot.SendTextMessageAsync(id, txtBoxSendMessage.Text);

                txtBoxSendMessage.Text = String.Empty;
            }
            else
            {
                return;
            }            
        }

        private void OpenCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (usersList.SelectedItem != null)
            {
                e.CanExecute = true;
            }            
        }

        private async void OpenCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            // Создать диалоговое окно открытия файла            
            var openDlg = new OpenFileDialog {};
            // Был ли совершен щелчок на кнопке ОК?
            if (true == openDlg.ShowDialog())
            {
                using Stream stream = System.IO.File.OpenRead(openDlg.FileName);

                var concreteUser = Users[Users.IndexOf((TelegramUser)usersList.SelectedItem)];

                long id = concreteUser.Id;

                string fileName = System.IO.Path.GetFileName(openDlg.FileName);                          
                                
                await bot.SendDocumentAsync(
                    chatId: id,
                    document: new InputOnlineFile(content: stream, fileName: fileName));

                string confirmSendFile = $"Файл {openDlg.FileName} отправлен";

                concreteUser.Messages.Add(confirmSendFile);
            }            
        }        
    }
}
