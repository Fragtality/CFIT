using System.Windows;
using System.Windows.Documents;

namespace CFIT.Installer.Tasks
{
    public class TaskMessage
    {
        public virtual string Text { get; set; } = "";
        public virtual bool ShowCompleted { get; set; } = false;
        public virtual bool Newline { get; set; } = true;
        public virtual FontWeight FontWeight { get; set; } = FontWeights.Normal;
        public virtual FontStyle Style { get; set; } = FontStyles.Normal;
        public virtual TextDecorationCollection Decorations { get; set; } = null;

        public TaskMessage(string text, bool showCompleted = false, FontWeight? weight = null, TextDecorationCollection decorations = null, FontStyle? style = null, bool newline = true)
        {
            Text = text;
            ShowCompleted = showCompleted;
            Newline = newline;
            FontWeight = weight ?? FontWeights.Normal;
            Style = style ?? FontStyles.Normal;
            Decorations = decorations;           
        }

        public virtual Run CreateElement(bool ignoreNewline)
        {
            return new Run()
            {
                Text = (Newline && !ignoreNewline ? $"\r\n{Text}" : Text),
                FontWeight = FontWeight,
                FontStyle = Style,
                TextDecorations = Decorations
            };
        }
    }
}
