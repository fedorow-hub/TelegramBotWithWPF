using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace TelegramBotWithWPF
{
    internal class TelegramUser : INotifyPropertyChanged, IEquatable<TelegramUser>
    {
        string message;

        string nick;

        long id;
               
        public string Message { get { return this.message; } set { this.message = value; } }
        public string Nick
        {
            get { return this.nick; }
            set
            {
                this.nick = value;
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(this.Nick)));
            }
        }
        public long Id
        {
            get { return this.id; }
            set
            {
                this.id = value;
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(this.Id)));
            }
        }

        public ObservableCollection<string> Messages { get; set; }
        public TelegramUser(string Nick, long Id)
        {
            this.id = Id;
            this.nick = Nick;
            Messages = new ObservableCollection<string>();

        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool Equals(TelegramUser other)
        {
            return other.id == this.id;
        }

        public void AddMessage(string text)
        {
            Messages.Add(text);
        }
    }
}
