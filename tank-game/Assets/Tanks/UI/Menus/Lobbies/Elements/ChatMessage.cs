using Unity.Properties;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Lobbies.Elements
{
    [UxmlElement]
    public partial class ChatMessage : VisualElement
    {
        public ChatMessage()
        {
            ContentLabel = new Label();
            ContentLabel.enableRichText = false;
            Add(ContentLabel);
            AddToClassList("chat-message");
            UpdateText();
        }

        [UxmlAttribute]
        [CreateProperty]
        public string Content
        {
            get => _contentValue;
            set
            {
                _contentValue = value;
                NotifyPropertyChanged(nameof(Content));
                UpdateText();
            }
        }

        protected Label ContentLabel { get; private set; }

        private string _contentValue = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua";
    
        protected virtual void UpdateText()
        {
            ContentLabel.text = _contentValue;
        }
    }
}
